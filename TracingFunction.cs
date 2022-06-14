using Serilog;
using SimpleInjector;

using static Functions;

public partial static class Decorators
{
    public class TracingFunctionAsync<TInput, TOutput> : FunctionAsync<TInput, TOutput>.Func
    {
        readonly Func<FunctionAsync<TInput, TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public TracingFunctionAsync(
            DependencyMetadata<FunctionAsync<TInput, TOutput>.Func> metadata,
            Func<FunctionAsync<TInput, TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Aff<TOutput> Apply(TInput input, CancellationToken ct) =>
            (
                from _ in Eff(() =>
                {
                    _logger.Information("{Function} called with {Params}", _functionName, input);
                    return unit;
                })
                from result in _decorated().Apply(input, ct)
                select result
            )
            .Map(r =>
            {
                _logger.Information("{Function} returned {Result}", _functionName, r);
                return r;
            });
    }

    public class TracingFunctionAsync<TOutput> : FunctionAsync<TOutput>.Func
    {
        readonly Func<FunctionAsync<TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public TracingFunctionAsync(
            DependencyMetadata<FunctionAsync<TOutput>.Func> metadata,
            Func<FunctionAsync<TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Aff<TOutput> Apply(CancellationToken ct) =>
            (
                from _ in Eff(() =>
                {
                    _logger.Information("{Function} called", _functionName);
                    return unit;
                })
                from result in _decorated().Apply(ct)
                select result
            )
            .Map(r =>
            {
                _logger.Information("{Function} returned {Result}", _functionName, r);
                return r;
            });
    }

    public class TracingFunction<TInput, TOutput> : Function<TInput, TOutput>.Func
    {
        readonly Func<Function<TInput, TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public TracingFunction(
            DependencyMetadata<Function<TInput, TOutput>.Func> metadata,
            Func<Function<TInput, TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Eff<TOutput> Apply(TInput input) =>
            (
                from _ in Eff(() =>
                {
                    _logger.Information("{Function} called with {Params}", _functionName, input);
                    return unit;
                })
                from result in _decorated().Apply(input)
                select result
            )
            .Map(r =>
            {
                _logger.Information("{Function} returned {Result}", _functionName, r);
                return r;
            });
    }
}
