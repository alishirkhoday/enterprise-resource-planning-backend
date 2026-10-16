using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.DTOs;
using ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Mapping;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;

namespace ERP.Application.UseCases.Modules.AfterSalesServiceManagement.Commands.RegisterWarrantyClaim
{
    /// <summary>
    /// Records a customer saying something broke that should not have.
    /// <para>
    /// Whether they are right is not decided here. Registering a claim and assessing it are
    /// separate acts, so the clock on the customer wait starts when they call rather than when
    /// somebody gets round to looking at it.
    /// </para>
    /// <para>
    /// The serial number is recorded on the claim but deliberately not looked up here. Whether the
    /// unit was in warranty is the assessor judgement, made later against the fault date, and
    /// deciding it at registration would answer the claim before anybody had examined it.
    /// </para>
    /// </summary>
    public class RegisterWarrantyClaimCommandHandler(
        ICustomerRepository customers,
        IItemRepository items,
        IServiceContractRepository serviceContracts,
        IWarrantyClaimRepository claims,
        INumberingSeriesRepository numberingSeries,
        TimeProvider timeProvider)
        : IRequestHandler<RegisterWarrantyClaimCommand, Result<WarrantyClaimDto, Error>>
    {
        private const string ClaimNumberingKey = "WarrantyClaim";

        public async Task<Result<WarrantyClaimDto, Error>> Handle(RegisterWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

            var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return Result.Failure<WarrantyClaimDto, Error>(Errors.General.NotFound(request.CustomerId.ToString(), nameof(Customer)));

            var item = await items.GetByIdAsync(request.ItemId, cancellationToken);
            if (item is null)
                return Result.Failure<WarrantyClaimDto, Error>(Errors.General.NotFound(request.ItemId.ToString(), nameof(Item)));

            var series = await numberingSeries.GetForUpdateAsync(ClaimNumberingKey, request.BranchId, cancellationToken: cancellationToken);
            if (series is null)
                return Result.Failure<WarrantyClaimDto, Error>(Errors.Order.NumberingSeriesMissing(ClaimNumberingKey));

            WarrantyClaim claim;
            try
            {
                claim = new WarrantyClaim(
                    series.Next(today),
                    customer,
                    item,
                    request.FaultOccurredOn,
                    request.ReportedOn,
                    request.FaultDescription);
            }
            catch (ArgumentException exception)
            {
                return Result.Failure<WarrantyClaimDto, Error>(Errors.AfterSales.ClaimCannotBeDecided(exception.Message));
            }

            claim.IdentifyUnit(request.SerialNumberId, request.ItemBatchId);

            if (request.InvoiceId is not null)
                claim.LinkToSale(request.InvoiceId.Value);

            if (request.BranchId is not null)
                claim.AssignToBranch(request.BranchId.Value);

            // Cover is judged on when it failed, not on when the paperwork was done. A claim
            // raised a day after expiry for a failure a week before is still a valid claim.
            var contract = await serviceContracts.GetCoveringAsync(
                customer.Id,
                item.Id,
                request.SerialNumberId,
                request.FaultOccurredOn,
                cancellationToken);

            if (contract is not null)
                claim.LinkToServiceContract(contract.Id);

            await claims.CreateAsync(claim, cancellationToken);

            return Result.Success<WarrantyClaimDto, Error>(claim.ToDto());
        }
    }
}
