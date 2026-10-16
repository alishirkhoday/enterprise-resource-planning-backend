namespace ERP.Application.UseCases.UsersManagement.Authentication.Verify
{
    public sealed record VerifyCommand : VerifyDto, ICommand<Result<string, Error>>
    {
    }
}
