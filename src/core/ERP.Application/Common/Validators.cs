using System.Text.RegularExpressions;

namespace ERP.Application.Common
{
    public static class Validators
    {
        public static bool IsValidateUsername(this string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            var isValidated = new Regex(@"^[a-zA-Z0-9_]{3,50}$").IsMatch(username);
            return isValidated;
        }

        public static bool IsValidatePassword(this string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }
            var isValidated = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[@#_!])[a-zA-Z0-9@#_!]{6,32}$").IsMatch(password);
            return isValidated;
        }

        public static bool IsValidateMobilePhoneNumberRegionCode(this string mobilePhoneNumberRegionCode)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumberRegionCode))
            {
                return false;
            }
            var isValidated = new Regex(@"^((98)|(1)|(44))$").IsMatch(mobilePhoneNumberRegionCode);
            return isValidated;
        }

        public static bool IsValidateMobilePhoneNumber(this string mobilePhoneNumber)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                return false;
            }
            var isValidated = new Regex(@"^(9){1}[0-9]{9}$").IsMatch(mobilePhoneNumber);
            return isValidated;
        }

        public static bool IsValidateEmail(this string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }
            var isValidated = new Regex(@"^[a-zA-Z0-9._]+@([a-zA-Z0-9]+\.)+[a-zA-Z0-9]{2,4}$").IsMatch(email);
            return isValidated;
        }

        public static bool IsValidateIranianNationalId(this string nationalId)
        {
            if (string.IsNullOrEmpty(nationalId))
            {
                return false;
            }
            //input has 10 digits that all of them are not equal
            if (!Regex.IsMatch(nationalId, @"^(?!(\d)\1{9})\d{10}$"))
                return false;

            var check = Convert.ToInt32(nationalId.Substring(9, 1));
            var sum = Enumerable.Range(0, 9).Select(x => Convert.ToInt32(nationalId.Substring(x, 1)) * (10 - x)).Sum() % 11;
            var result = sum < 2 && check == sum || sum >= 2 && check + sum == 11;
            return result;
        }

        public static bool IsValidateIranianMobilePhoneNumber(this string mobilePhoneNumber)
        {
            if (string.IsNullOrEmpty(mobilePhoneNumber))
            {
                return false;
            }
            var isValidated = new Regex(@"^(((98)|(\+98)|(0098)|0)(9){1}[0-9]{9})$").IsMatch(mobilePhoneNumber);
            return isValidated;
        }

        public static bool IsValidateIranianHomePhoneNumber(this string homePhoneNumber)
        {
            if (string.IsNullOrEmpty(homePhoneNumber))
            {
                return false;
            }
            var isValidated = new Regex(@"^(((98)|(\+98)|(0098)|0)((41)|(44)|(45)|(31)|(84)|(77)|(21)|(38)|(51)|(56)|(58)|(61)|(24)|(23)|(54)|(71)|(26)|(25)|(28)|(87)|(34)|(83)|(74)|(17)|(13)|(66)|(11)|(86)|(76)|(81)|(35))[0-9]{8})$").IsMatch(homePhoneNumber);
            return isValidated;
        }
    }
}
