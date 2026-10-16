using ERP.Application.Common.Behaviors;
using ERP.Application.Common.Interfaces.Events;
using ERP.Application.Services.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ERP.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());

                // Order matters. Logging wraps everything so a rejected request still shows up;
                // validation runs before the handler; the unit of work closes around the work the
                // handler did and nothing else.
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // The facts nothing announces about itself. Run on a schedule; see IObservedEventScanner
            // for why these are published directly rather than through the outbox.
            services.AddScoped<IObservedEventScanner, ObservedEventScanner>();

            return services;
        }
    }
}
