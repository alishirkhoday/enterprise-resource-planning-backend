using ERP.Application.Common.Security;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Users;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Users;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.Commands.CreateNewHuman
{
    public class CreateNewHumanCommandHandler(IUserRepository userRepository, IHumanRepository humanRepository) : IRequestHandler<CreateNewHumanCommand, Result<string, Error>>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IHumanRepository _humanRepository = humanRepository;

        public async Task<Result<string, Error>> Handle(CreateNewHumanCommand request, CancellationToken cancellationToken)
        {
            var isExitsUsername = await _userRepository.CheckUserExistsByUsernameAsync(request.Username, cancellationToken);
            if (isExitsUsername)
            {
                return Result.Failure<string, Error>(Errors.User.UsernameIsUsed(request.Username));
            }
            var newUser = new User(request.Username, request.Password.HashPassword());
            await _userRepository.CreateAsync(newUser, cancellationToken);
            var newHuman = new Human(newUser, request.Name, request.Family, request.Gender, request.BirthDate, request.MaritalStatus);
            var isExistenceNationalId = await _humanRepository.IsExistenceByNationalIdAsync(request.NationalId, cancellationToken);
            if (isExistenceNationalId)
            {
                return Result.Failure<string, Error>(Errors.Human.NationalIdIsUsed(request.NationalId));
            }
            newHuman.SetNationalId(request.NationalId);
            if (request.PassportId is not null)
            {
                var isExistencePassportId = await _humanRepository.IsExistenceByPassportIdAsync(request.PassportId, cancellationToken);
                if (isExistencePassportId)
                {
                    return Result.Failure<string, Error>(Errors.Human.PassportIdIsUsed(request.PassportId));
                }
                newHuman.SetPassportId(request.PassportId);
            }
            await _humanRepository.CreateAsync(newHuman, cancellationToken);
            return Result.Success<string, Error>($"{newHuman.Name} {newHuman.Family}");
        }
    }
}
