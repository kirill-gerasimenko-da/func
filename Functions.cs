// ReSharper disable TypeParameterCanBeVariant
// ReSharper disable CheckNamespace
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable VirtualMemberNeverOverridden.Global

using FluentValidation;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public static class Functions
{
    public static readonly Atom<Error> FunctionValidationError = Atom(Error.New(1_000_001, "Validation error"));

    public delegate void InputValidator<TInput>(AbstractValidator<TInput> validator);

    public delegate Aff<TOutput> FunctionAff<TInput, TOutput>(TInput input, CancellationToken ct);
    public delegate Aff<TOutput> FunctionAff<TOutput>(CancellationToken ct);
    public delegate Eff<TOutput> FunctionEff<TInput, TOutput>(TInput input);

    public static Error ToError(this Exception exception) => exception is ErrorException ee
        ? ee.ToError()
        : Error.New(exception);

    public interface IFunctionAsync<TInput, TOutput>
    {
        /// <summary>
        /// Applies input to the function, returning async effect.
        /// When run it will invoke the function with specified input arguments.
        /// </summary>
        Aff<TOutput> Apply(TInput input, CancellationToken ct);
    }

    public interface IFunctionAsync<TOutput>
    {
        /// <summary>
        /// Applies input to the function, returning async effect.
        /// When run it will invoke the function with specified input arguments.
        /// </summary>
        Aff<TOutput> Apply(CancellationToken ct);
    }

    public interface IFunction<TInput, TOutput>
    {
        /// <summary>
        /// Applies input to the function, returning sync effect.
        /// When run it will invoke the function with specified input arguments.
        /// </summary>
        Eff<TOutput> Apply(TInput input);
    }

    class ValidatorImpl<T> : AbstractValidator<T>
    {
        public ValidatorImpl(InputValidator<T> registerValidators) => registerValidators(this);
    }

    public abstract class FunctionAsync<TInput, TOutput> : FunctionAsync<TInput, TOutput>.Func
    {
        public interface Func : IFunctionAsync<TInput, TOutput>
        { }

        readonly ValidatorImpl<TInput> _validator;

        protected FunctionAsync() => _validator = new ValidatorImpl<TInput>(Validator);

        protected virtual InputValidator<TInput> Validator { get; } = _ => { };
        protected abstract Aff<TOutput> Apply(TInput input, CancellationToken ct);

        Aff<TOutput> IFunctionAsync<TInput, TOutput>.Apply(TInput input, CancellationToken ct) =>
            from _1 in guardnot(ReferenceEquals(input, null),
                Error.New(FunctionValidationError.Value.Code, "Input could not be null"))
            from validator in Eff(() => _validator)
            from validationResult in Eff(() => _validator.Validate(input))
            from _2 in guard(validationResult.IsValid,
                Error.New(FunctionValidationError.Value.Code,
                    $"Validation failed for {input.GetType().Name}",
                    (Exception) new ValidationException(validationResult.Errors)))
            from output in Apply(input, ct)
            select output;
    }

    public abstract class Function<TInput, TOutput> : Function<TInput, TOutput>.Func
    {
        public interface Func : IFunction<TInput, TOutput>
        { }

        readonly ValidatorImpl<TInput> _validator;

        protected Function() => _validator = new ValidatorImpl<TInput>(Validator);

        protected virtual InputValidator<TInput> Validator { get; } = _ => { };
        protected abstract Eff<TOutput> Apply(TInput input);

        Eff<TOutput> IFunction<TInput, TOutput>.Apply(TInput input) =>
            from _1 in guardnot(ReferenceEquals(input, null),
                Error.New(FunctionValidationError.Value.Code, "Input could not be null"))
            from validator in Eff(() => _validator)
            from validationResult in Eff(() => _validator.Validate(input))
            from _2 in guard(validationResult.IsValid,
                Error.New(FunctionValidationError.Value.Code,
                    $"Validation failed for {input.GetType().Name}",
                    (Exception) new ValidationException(validationResult.Errors)))
            from output in Apply(input)
            select output;
    }

    #region Delegates

    // class FunctionAsyncImpl<TInput, TOutput> : FunctionAsync<TInput, TOutput>
    // {
    //     readonly FunctionAff<TInput, TOutput> _func;
    //     public FunctionAsyncImpl(FunctionAff<TInput, TOutput> func) => _func = func;
    //     protected override Aff<TOutput> Apply(TInput input, CancellationToken ct) => _func(input, ct);
    // }
    //
    // class FunctionImpl<TInput, TOutput> : Function<TInput, TOutput>
    // {
    //     readonly FunctionEff<TInput, TOutput> _func;
    //     public FunctionImpl(FunctionEff<TInput, TOutput> func) => _func = func;
    //     protected override Eff<TOutput> Apply(TInput input) => _func(input);
    // }
    //
    // public static IFunctionAsync<TInput, TOutput> ToFunction<TInput, TOutput>(
    //     this FunctionAff<TInput, TOutput> func) => new FunctionAsyncImpl<TInput, TOutput>(func);
    //
    // public static IFunction<TInput, TOutput> ToFunction<TInput, TOutput>(
    //     this FunctionEff<TInput, TOutput> func) => new FunctionImpl<TInput, TOutput>(func);
    //
    // public static IFunctionAsync<TInput, TOutput> ToFunction<TInput, TOutput>(
    //     this Func<TInput, CancellationToken, Aff<TOutput>> func) =>
    //     new FunctionAsyncImpl<TInput, TOutput>(new(func));
    //
    // public static IFunction<TInput, TOutput> ToFunction<TInput, TOutput>(
    //     this Func<TInput, Eff<TOutput>> func) => new FunctionImpl<TInput, TOutput>(new(func));
    //
    // public static FunctionAff<TInput, TOutput> ToDelegate<TInput, TOutput>(this IFunctionAsync<TInput, TOutput> func) =>
    //     func.Apply;
    //
    // public static FunctionEff<TInput, TOutput> ToDelegate<TInput, TOutput>(this IFunction<TInput, TOutput> func) =>
    //     func.Apply;

    #endregion

    public abstract class FunctionAsync<TOutput> : FunctionAsync<TOutput>.Func
    {
        public interface Func : IFunctionAsync<TOutput>
        { }

        Aff<TOutput> IFunctionAsync<TOutput>.Apply(CancellationToken ct) =>
            from output in Apply(ct) select output;

        protected abstract Aff<TOutput> Apply(CancellationToken ct);
    }
}
