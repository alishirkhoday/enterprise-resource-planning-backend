using Serilog.Core;
using Serilog.Events;

namespace ERP.Application.Common.Logging
{
    public class CreatedAtEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var createdAtProperty = propertyFactory.CreateProperty("CreatedAt", DateTime.UtcNow);
            logEvent.AddPropertyIfAbsent(createdAtProperty);
        }
    }
}
