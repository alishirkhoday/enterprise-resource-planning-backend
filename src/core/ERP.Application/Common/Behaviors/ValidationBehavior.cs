namespace ERP.Application.Common.Behaviors;

/// <summary>
/// Runs every validator registered for a request before its handler sees it.
/// <para>
/// Validation used to be the caller's job, repeated in each controller. A controller that forgot
/// the block simply had no validation, and nothing anywhere said so. Here it cannot be forgotten:
/// a request with a validator is validated, and a request without one passes straight through.
/// </para>
/// <para>
/// Failures are thrown rather than returned. A <see cref="Result{T, E}"/> failure describes a
/// business outcome the caller asked for - a name already in use, a customer over their limit -
/// while a malformed request never reached the business at all, and flattening the two would make
/// every handler's error type carry a shape it has no opinion about.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var applicable = validators as IValidator<TRequest>[] ?? [.. validators];
        if (applicable.Length == 0)
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        // Every validator runs, so the caller is told about all of it at once rather than being
        // sent round the loop one field at a time.
        var results = await Task.WhenAll(applicable.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();

        if (failures.Length > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
