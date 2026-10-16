using ERP.Application.Common;

namespace ERP.WebApi.Controllers
{
    public class Envelope<T>
    {
        public T? Data { get; }
        public string? ErrorCode { get; }
        public string? ErrorMessage { get; }
        public string? InvalidField { get; }
        public DateTimeOffset TimeGenerated { get; }

        private Envelope(T? result, Error? error, string? invalidField)
        {
            Data = result;
            ErrorCode = error?.Code;
            ErrorMessage = error?.Message;
            InvalidField = invalidField;
            TimeGenerated = DateTimeOffset.UtcNow;
        }

        public static Envelope<T> Ok(T? result = default)
        {
            return new Envelope<T>(result, null, null);
        }

        public static Envelope<T> Error(Error error, string? invalidField = null)
        {
            return new Envelope<T>(default, error, invalidField);
        }
    }
}
