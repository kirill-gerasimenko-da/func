using System.Diagnostics;
using Serilog;
using SimpleInjector;

using ILogger = Serilog.ILogger;
using static Functions;

public static partial class Decorators
{
    public class ErrorLoggingFunctionAsync<TInput, TOutput> : FunctionAsync<TInput, TOutput>.Func
    {
        readonly Func<FunctionAsync<TInput, TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public ErrorLoggingFunctionAsync(
            DependencyMetadata<FunctionAsync<TInput, TOutput>.Func> metadata,
            Func<FunctionAsync<TInput, TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Aff<TOutput> Apply(TInput input, CancellationToken ct) => _decorated().Apply(input, ct).MapFail(error =>
        {
            _logger.Error(error.ToException(), "{Function} failed", _functionName);
            return error;
        });
    }

    public class ErrorLoggingFunctionAsync<TOutput> : FunctionAsync<TOutput>.Func
    {
        readonly Func<FunctionAsync<TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public ErrorLoggingFunctionAsync(
            DependencyMetadata<FunctionAsync<TOutput>.Func> metadata,
            Func<FunctionAsync<TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Aff<TOutput> Apply(CancellationToken ct) => _decorated().Apply(ct).MapFail(error =>
        {
            _logger.Error(error.ToException(), "{Function} failed", _functionName);
            return error;
        });
    }

    public class ErrorLoggingFunction<TInput, TOutput> : Function<TInput, TOutput>.Func
    {
        readonly Func<Function<TInput, TOutput>.Func> _decorated;
        readonly string _functionName;
        readonly ILogger _logger;

        public ErrorLoggingFunction(
            DependencyMetadata<Function<TInput, TOutput>.Func> metadata,
            Func<Function<TInput, TOutput>.Func> decorated,
            ILogger logger)
        {
            _decorated = decorated;
            _logger = logger;
            _functionName = metadata.ImplementationType.FullName;
        }

        public Eff<TOutput> Apply(TInput input) => _decorated().Apply(input).MapFail(error =>
        {
            _logger.Error(error.ToException(), "{Function} failed", _functionName);
            return error;
        });
    }
}
