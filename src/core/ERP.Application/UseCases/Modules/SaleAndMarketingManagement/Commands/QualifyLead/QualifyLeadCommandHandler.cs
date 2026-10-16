using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.QualifyLead
{
    /// <summary>
    /// The decision about whether a lead is worth pursuing - صلاحیت‌سنجی سرنخ.
    /// <para>
    /// Qualifying and disqualifying are one command because they are one decision, taken at one
    /// moment by one person. Splitting them into two endpoints invites a lead to sit in neither
    /// state because the answer was no and nobody wanted to click the negative button.
    /// </para>
    /// </summary>
    public class QualifyLeadCommandHandler(ILeadRepository leads)
        : IRequestHandler<QualifyLeadCommand, Result<LeadDto, Error>>
    {
        public async Task<Result<LeadDto, Error>> Handle(QualifyLeadCommand request, CancellationToken cancellationToken)
        {
            var lead = await leads.GetWithHistoryAsync(request.LeadId, cancellationToken);
            if (lead is null)
                return Result.Failure<LeadDto, Error>(Errors.General.NotFound(request.LeadId.ToString(), nameof(Lead)));

            try
            {
                if (request.Score is not null)
                    lead.SetScore(request.Score.Value);

                if (request.IsQualified)
                    lead.Qualify();
                else
                    lead.Disqualify(request.Reason!);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return Result.Failure<LeadDto, Error>(Errors.Marketing.LeadCannotBeChanged(exception.Message));
            }

            await leads.UpdateAsync(lead, cancellationToken);

            return Result.Success<LeadDto, Error>(lead.ToDto());
        }
    }
}
