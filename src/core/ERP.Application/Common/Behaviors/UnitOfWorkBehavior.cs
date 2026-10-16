using ERP.Application.Common.Interfaces.DbContext;

namespace ERP.Application.Common.Behaviors;

/// <summary>
/// Commits a command's work once, after its handler has finished.
/// <para>
/// Handlers used to save for themselves and then judge the row count, which made every one of them
/// responsible for a transaction boundary it could not see the whole of. Here the boundary belongs
/// to the pipeline: a handler changes the domain and says whether it worked, and nothing else.
/// </para>
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IMainDbContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        // A refused command keeps nothing. Several handlers reject halfway through, after they have
        // already put something in the change tracker - creating the user before discovering the
        // national identifier is taken - and saving that would leave the half of the work that
        // happened to run first.
        if (response is IResult { IsFailure: true })
            return response;

        await context.SaveChanges(cancellationToken);

        return response;
    }
}
