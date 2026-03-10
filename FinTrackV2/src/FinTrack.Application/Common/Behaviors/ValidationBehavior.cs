using FinTrack.Domain.Common;
using FluentValidation;
using MediatR;

namespace FinTrack.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation before every handler.
/// If validation fails, the handler never executes — we return a failure Result immediately.
/// This means handlers can assume their input is always valid.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Take the first validation error and wrap it as a Result failure
        var first = failures[0];
        var error = new Error($"Validation.{first.PropertyName}", first.ErrorMessage);

        // We need to construct Result<T> or Result dynamically
        var resultType = typeof(TResponse);

        if (resultType == typeof(Result))
            return (TResponse)Result.Failure(error);

        // For Result<T>, call the generic factory
        var genericArg = resultType.GetGenericArguments()[0];
        var method = typeof(Result)
            .GetMethods()
            .First(m => m.Name == "Failure" && m.IsGenericMethod)
            .MakeGenericMethod(genericArg);

        return (TResponse)method.Invoke(null, [error])!;
    }
}
