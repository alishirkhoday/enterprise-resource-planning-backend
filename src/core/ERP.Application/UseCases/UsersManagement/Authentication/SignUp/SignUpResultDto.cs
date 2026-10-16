namespace ERP.Application.UseCases.UsersManagement.Authentication.SignUp
{
    public record SignUpResultDto(string username, string? mobilePhoneNumberWithRegionCode, string? email);
}
