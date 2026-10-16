using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ScheduleActivity
{
    /// <summary>
    /// Books a call, meeting or follow up - فعالیت.
    /// <para>
    /// An activity always points at something - a lead, a deal, a customer - because an activity
    /// that belongs to nothing drops out of every list that matters and is never done.
    /// </para>
    /// </summary>
    public class ScheduleActivityCommandHandler(IActivityRepository activities)
        : IRequestHandler<ScheduleActivityCommand, Result<ActivityDto, Error>>
    {
        public async Task<Result<ActivityDto, Error>> Handle(ScheduleActivityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var activity = ((ScheduleActivityDto)request).ToEntity();

                await activities.CreateAsync(activity, cancellationToken);

                return Result.Success<ActivityDto, Error>(activity.ToDto());
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<ActivityDto, Error>(Errors.Marketing.ActivityCannotBeChanged(exception.Message));
            }
        }
    }
}
