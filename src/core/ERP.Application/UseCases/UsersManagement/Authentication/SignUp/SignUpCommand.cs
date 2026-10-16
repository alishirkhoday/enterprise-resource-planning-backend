namespace ERP.Application.UseCases.UsersManagement.Authentication.SignUp
{
    /// <summary>
    /// Deliberately not an <see cref="ICommand{TResponse}"/>. Signing up sends a verification code,
    /// and a message that has gone out cannot be recalled if the commit then fails - so this
    /// command owns the order of its own save rather than leaving it to the pipeline.
    /// </summary>
    public sealed record SignUpCommand : SignUpDto, IRequest<Result<SignUpResultDto, Error>>
    {
    }
}
