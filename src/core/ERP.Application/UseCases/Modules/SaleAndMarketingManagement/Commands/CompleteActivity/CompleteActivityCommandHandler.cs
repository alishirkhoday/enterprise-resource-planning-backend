using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Mapping;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;

namespace ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CompleteActivity
{
    /// <summary>
    /// Records what came of an activity, and books the next step if there is one.
    /// <para>
    /// The follow up is part of the same command because that is the moment it gets decided. A
    /// salesperson who has to open a second screen to book the next call books it less often, and
    /// a deal with no next step is how a pipeline goes quiet without anybody noticing.
    /// </para>
    /// </summary>
    public class CompleteActivityCommandHandler(IActivityRepository activities)
        : IRequestHandler<CompleteActivityCommand, Result<CompleteActivityResultDto, Error>>
    {
        public async Task<Result<CompleteActivityResultDto, Error>> Handle(CompleteActivityCommand request, CancellationToken cancellationToken)
        {
            var activity = await activities.GetByIdAsync(request.ActivityId, cancellationToken);
            if (activity is null)
                return Result.Failure<CompleteActivityResultDto, Error>(Errors.General.NotFound(request.ActivityId.ToString(), nameof(Activity)));

            Activity? followUp = null;

            try
            {
                activity.Complete(request.Outcome, request.DurationMinutes);

                if (request.FollowUp is not null)
                {
                    followUp = request.FollowUp.ToEntity();
                    await activities.CreateAsync(followUp, cancellationToken);
                }
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return Result.Failure<CompleteActivityResultDto, Error>(Errors.Marketing.ActivityCannotBeChanged(exception.Message));
            }

            await activities.UpdateAsync(activity, cancellationToken);

            return Result.Success<CompleteActivityResultDto, Error>(
                new CompleteActivityResultDto(activity.ToDto(), followUp?.ToDto()));
        }
    }
}
