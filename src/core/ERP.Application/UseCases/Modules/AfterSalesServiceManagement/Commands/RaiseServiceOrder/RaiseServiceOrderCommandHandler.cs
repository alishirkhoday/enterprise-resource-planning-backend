using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Mapping;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RaiseServiceOrder
{
    /// <summary>
    /// Raises a service visit and works out who is paying for it.
    /// <para>
    /// That decision is made here, once, on the way in - not by the caller and not at the end. A
    /// visit whose type is settled after the parts are fitted is a visit that gets argued about,
    /// and service revenue disappears more reliably that way than by any other route.
    /// </para>
    /// </summary>
    public class RaiseServiceOrderCommandHandler(
        ICustomerRepository customers,
        ISerialNumberRepository serialNumbers,
        IServiceContractRepository serviceContracts,
        IServiceOrderRepository serviceOrders,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<RaiseServiceOrderCommand, Result<ServiceOrderDto, Error>>
    {
        private const string ServiceOrderNumberingKey = "ServiceOrder";

        public async Task<Result<ServiceOrderDto, Error>> Handle(RaiseServiceOrderCommand request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<ServiceOrderDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            var cover = await DetermineCoverAsync(request, today, cancellationToken);

            var series = await numberingSeries.GetForUpdateAsync(ServiceOrderNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<ServiceOrderDto, Error>(Errors.Order.NumberingSeriesMissing(ServiceOrderNumberingKey));

            var order = new ServiceOrder(
                series.Next(today),
                customer,
                cover.Type,
                request.ReportedFault,
                request.CurrencyCode);

            order.IdentifyEquipment(request.ItemId, request.SerialNumberId);
            order.SetPriority(request.Priority);

            if (cover.Contract is not null)
            {
                order.LinkToServiceContract(cover.Contract.Id);

                // The promise comes from the contract that covers the equipment. Recorded as
                // moments now, so a breach later is a comparison rather than an argument about
                // which contract was in force.
                order.PromiseServiceLevel(cover.Contract.ResponseHours, cover.Contract.ResolutionHours);
            }

            if (request.TicketId is not null)
                order.LinkToTicket(request.TicketId.Value);

            if (request.AssignedToHumanId is not null)
                order.AssignTo(request.AssignedToHumanId.Value);

            if (request.ScheduledOn is not null)
                order.Schedule(request.ScheduledOn.Value);

            if (request.WarehouseId is not null)
                order.IssuePartsFrom(request.WarehouseId.Value);

            if (request.BranchId is not null)
                order.AssignToBranch(request.BranchId.Value);

            await serviceOrders.CreateAsync(order, cancellationToken);

            return Result.Success<ServiceOrderDto, Error>(order.ToDto());
        }

        /// <summary>
        /// Warranty first, then a service contract, then the customer pays. That order matters:
        /// an in-warranty unit covered by a contract should consume the warranty, which the
        /// manufacturer carries, rather than a visit the customer has already paid for.
        /// </summary>
        private async Task<(ServiceOrderType Type, ServiceContract? Contract)> DetermineCoverAsync(
            RaiseServiceOrderCommand request,
            DateOnly today,
            CancellationToken cancellationToken)
        {
            if (request.SerialNumberId is not null)
            {
                var unit = await serialNumbers.GetByIdAsync(request.SerialNumberId.Value, cancellationToken);
                if (unit?.WarrantyExpiresOn is not null && unit.WarrantyExpiresOn.Value >= today)
                    return (ServiceOrderType.Warranty, null);
            }

            if (request.ItemId is not null)
            {
                var contract = await serviceContracts.GetCoveringAsync(
                    request.CustomerId,
                    request.ItemId.Value,
                    request.SerialNumberId,
                    today,
                    cancellationToken);

                // A contract whose included visits are used up still covers the equipment, but the
                // visit itself is now chargeable - which is the whole point of counting them.
                if (contract is not null)
                    return contract.HasVisitsRemaining
                        ? (ServiceOrderType.Contract, contract)
                        : (ServiceOrderType.Chargeable, contract);
            }

            return (ServiceOrderType.Chargeable, null);
        }
    }
}
