namespace ERP.Application.Common
{
    public static class Helpers
    {
        public static class ValidatorsMessages
        {
            //User
            public const string UserIdIsRequired = "User id is required.";
            public const string UsernameIsRequired = "Username is required.";
            public const string UsernameIsNotValid = "Username should be greater than 3 and less than 50 characters and include letter and number and underscore.";
            public const string PasswordIsRequired = "Password is required.";
            public const string PasswordIsNotValid = "Password should be greater than 6 and less than 50 characters and include uppercase letters, lowercase letters, numbers and one of ( #_@ ).";
            public const string MobilePhoneNumberRegionCodeIsRequired = "Mobile phone number region code is required.";
            public const string MobilePhoneNumberRegionCodeIsNotValid = "Mobile phone number region code is not valid.";
            public const string MobilePhoneNumberRegionCodeLength = "Mobile phone number region code should be less than 10 characters.";
            public const string MobilePhoneNumberIsRequired = "Mobile phone number is required.";
            public const string MobilePhoneNumberIsNotValid = "Mobile phone number is not valid.";
            public const string MobilePhoneNumberLength = "Mobile phone number should be less than 15 characters.";
            public const string MobilePhoneNumberWithRegionCodeLength = "Mobile phone number should be less than 20 characters.";
            public const string EmailIsRequired = "Email address is required.";
            public const string EmailIsNotValid = "Email address is not valid.";
            public const string EmailLength = "Email address should be less than 256 characters.";
            //VerificationCode
            public const string VerificationCodeIsRequired = "Verification code is required.";
        }
    }
}
