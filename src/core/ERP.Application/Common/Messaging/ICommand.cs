namespace ERP.Application.Common.Messaging;

/// <summary>
/// A request that changes something. Marking it as such is what puts it inside a unit of work:
/// the handler mutates the domain and the pipeline commits afterwards, so no handler has to
/// remember to save and none of them can decide to save halfway through.
/// <para>
/// A command with an irreversible external side effect - sending a message, charging a card - is
/// deliberately left implementing <see cref="MediatR.IRequest{TResponse}"/> directly instead. Such
/// a command has to own the order of its own commit, because a side effect that runs before the
/// transaction commits can never be taken back if the commit then fails.
/// </para>
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;
