namespace ERP.Application.Common.Messaging;

/// <summary>
/// A request that only reads. It never reaches the unit of work, so a query that accidentally
/// mutated something would not be able to persist it.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
