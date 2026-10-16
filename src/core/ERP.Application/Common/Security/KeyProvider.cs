using ERP.Application.Common.Interfaces.Security;
using Microsoft.Extensions.Configuration;

namespace ERP.Application.Common.Security
{
    public sealed class KeyProvider : IKeyProvider
    {
        private readonly IConfiguration _configuration;

        public KeyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IReadOnlyDictionary<string, string> LoadAllKeys()
        {
            var section = _configuration.GetSection("Encryption:Keys");
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var child in section.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Value)) continue;
                dict[child.Key] = child.Value.Trim(); // base64 string
            }

            return dict;
        }

        public string? GetActiveKeyId()
        {
            return _configuration.GetValue<string>("Encryption:CurrentKeyId");
        }
    }
}
