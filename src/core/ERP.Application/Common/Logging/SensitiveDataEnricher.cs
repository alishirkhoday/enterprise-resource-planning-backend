using Serilog.Core;
using Serilog.Events;

namespace ERP.Application.Common.Logging
{
    public class SensitiveDataEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var prop in logEvent.Properties)
            {
                if (prop.Key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, "***"));
                }
            }
        }
    }
}
