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

    public static Error ToError(this Exception exception) => exception is ErrorException ee
        ? ee.ToError()
        : Error.New(exception);

    public interface IFunctionAsync<TInput, TOutput>
    {
        Aff<TOutput> Apply(TInput input, CancellationToken ct);
    }

    public interface IFunction<TInput, TOutput>
    {
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

    public abstract class FunctionAsync<TOutput> : FunctionAsync<TOutput>.Func
    {
        public interface Func : IFunctionAsync<Unit, TOutput>
        { }

        Aff<TOutput> IFunctionAsync<Unit, TOutput>.Apply(Unit _, CancellationToken ct) =>
            from output in Apply(ct) select output;

        protected abstract Aff<TOutput> Apply(CancellationToken ct);
    }

    public abstract class Function<TOutput> : Function<TOutput>.Func
    {
        public interface Func : IFunction<Unit, TOutput>
        { }

        Eff<TOutput> IFunction<Unit, TOutput>.Apply(Unit _) =>
            from output in Apply() select output;

        protected abstract Eff<TOutput> Apply();
    }

    public static Aff<TOutput> Apply<TOutput>(this IFunctionAsync<Unit, TOutput> func, CancellationToken ct) =>
        func.Apply(unit, ct);

    public static Eff<TOutput> Apply<TOutput>(this IFunction<Unit, TOutput> func) =>
        func.Apply(unit);

    #region Delegates

    public delegate Aff<TOutput> FunctionAff<TInput, TOutput>(TInput input, CancellationToken ct);
    public delegate Eff<TOutput> FunctionEff<TInput, TOutput>(TInput input);

    public static Func<TInput, CancellationToken, Aff<TOutput>> ToDelegate<TInput, TOutput>(
        this IFunctionAsync<TInput, TOutput> func) => func.Apply;

    public static Func<TInput, Eff<TOutput>> ToDelegate<TInput, TOutput>(
        this IFunction<TInput, TOutput> func) => func.Apply;

    #endregion
}
