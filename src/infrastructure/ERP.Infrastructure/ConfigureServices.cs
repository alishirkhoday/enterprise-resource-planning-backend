using ERP.Application.Common.Interfaces.Authentication;
using ERP.Application.Common.Interfaces.Events;
using ERP.Application.Common.Interfaces.Identity;
using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Integration;
using ERP.Domain.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Domain.Repositories.Modules.CorrespondenceManagement;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.FinanceManagement;
using ERP.Domain.Repositories.Modules.HumanResourcesManagement;
using ERP.Domain.Repositories.Modules.InventoryManagement;
using ERP.Domain.Repositories.Modules.OrderManagement;
using ERP.Domain.Repositories.Modules.PlanningManagement;
using ERP.Domain.Repositories.Modules.PricingManagement;
using ERP.Domain.Repositories.Modules.ProductionManagement;
using ERP.Domain.Repositories.Modules.ProjectManagement;
using ERP.Domain.Repositories.Modules.ReportingManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Modules.WarehouseManagement;
using ERP.Domain.Repositories.Notifications;
using ERP.Domain.Repositories.Security;
using ERP.Domain.Repositories.Users;
using ERP.Domain.Repositories.Workflows;
using ERP.Infrastructure.CacheDatabase.Context;
using ERP.Infrastructure.MainDatabase.Context;
using ERP.Infrastructure.MainDatabase.Repositories.Common;
using ERP.Infrastructure.MainDatabase.Repositories.Integration;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.AfterSalesServiceManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.CorrespondenceManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.FinanceManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.HumanResourcesManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.InventoryManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.OrderManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.PlanningManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.PricingManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.ProductionManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.ProjectManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.ReportingManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.SaleAndMarketingManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Modules.WarehouseManagement;
using ERP.Infrastructure.MainDatabase.Repositories.Notifications;
using ERP.Infrastructure.MainDatabase.Repositories.Security;
using ERP.Infrastructure.MainDatabase.Repositories.Users;
using ERP.Infrastructure.MainDatabase.Repositories.Workflows;
using ERP.Infrastructure.Services.Authentication;
using ERP.Infrastructure.Services.Events;
using ERP.Infrastructure.Services.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace ERP.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // A fallback only. The web layer replaces it with one that reads the caller's token;
            // anything running without a request - a background job, the migration tooling - keeps
            // this one and audits its work under a name rather than under nothing.
            services.TryAddScoped<ICurrentUser>(_ => new SystemUser("system"));

            services.AddScoped<AuditingInterceptor>();

            services.AddDbContext<IMainDbContext, MainDbContext>((serviceProvider, option) =>
            {
                var sqlServerConnectionString = configuration["ConnectionStrings:MainDatabase"];
                option.UseSqlServer(sqlServerConnectionString);
                option.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
            });

            services.AddRepositories();

            // Domain events: recorded into the outbox when a change is saved, drained by the
            // processor on a schedule, and closed over their real type on the way to a handler.
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();

            services.TryAddSingleton(TimeProvider.System);
            services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
            services.Configure<ObservedEventScanOptions>(configuration.GetSection(ObservedEventScanOptions.SectionName));

            // Nothing was draining the outbox, so every fact the domain recorded was stored and
            // never delivered. These two are what make the pipeline actually run.
            services.AddHostedService<OutboxBackgroundService>();
            services.AddHostedService<ObservedEventScanBackgroundService>();

            services.AddScoped<ICacheDbContext, CacheDbContext>();
            services.AddScoped<IOneTimePasswordService, OneTimePasswordService>();
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = configuration["ConnectionStrings:CacheDatabase"];
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString ?? throw new ArgumentNullException(redisConnectionString));
                //configurationOptions.User = "";
                //configurationOptions.Password = "";
                configurationOptions.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            return services;
        }

        /// <summary>
        /// One registration per aggregate root. Entities that are only reachable through a root -
        /// an order line, a document article - have no repository of their own and are loaded with
        /// the root that owns them.
        /// </summary>
        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Users and access control
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            services.AddScoped<IUserTokenRepository, UserTokenRepository>();
            services.AddScoped<IPermissionRepository, UserPermissionRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();

            // Shared reference data
            services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
            services.AddScoped<INumberingSeriesRepository, NumberingSeriesRepository>();
            services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

            // Finance
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IDetailAccountRepository, DetailAccountRepository>();
            services.AddScoped<IFinancialYearRepository, FinancialYearRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IChequeRepository, ChequeRepository>();
            services.AddScoped<IBudgetRepository, BudgetRepository>();
            services.AddScoped<IReceivableRepository, ReceivableRepository>();
            services.AddScoped<IPayableRepository, PayableRepository>();
            services.AddScoped<ISettlementRepository, SettlementRepository>();
            services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
            services.AddScoped<IPayrollRepository, PayrollRepository>();

            // Inventory and purchasing
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IItemInventoryRepository, ItemInventoryRepository>();
            services.AddScoped<IStockLedgerEntryRepository, StockLedgerEntryRepository>();
            services.AddScoped<IItemBatchRepository, ItemBatchRepository>();
            services.AddScoped<ISerialNumberRepository, SerialNumberRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddScoped<IPurchaseContractRepository, PurchaseContractRepository>();
            services.AddScoped<ISaleOrderRepository, SaleOrderRepository>();
            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IStockTransferOrderRepository, StockTransferOrderRepository>();
            services.AddScoped<IStockCountRepository, StockCountRepository>();

            // Customers, sales and orders
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<ITicketSubjectRepository, TicketSubjectRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<ILeadRepository, LeadRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<ICommissionRepository, CommissionRepository>();
            services.AddScoped<ISalesTargetRepository, SalesTargetRepository>();
            services.AddScoped<ISalesContractRepository, SalesContractRepository>();
            services.AddScoped<IPriceListRepository, PriceListRepository>();

            // Production
            services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
            services.AddScoped<IBillOfMaterialsRepository, BillOfMaterialsRepository>();
            services.AddScoped<IMachineRepository, MachineRepository>();
            services.AddScoped<IMaterialIssueRepository, MaterialIssueRepository>();
            services.AddScoped<IQualityControlCheckRepository, QualityControlCheckRepository>();

            // Planning
            services.AddScoped<IDemandForecastRepository, DemandForecastRepository>();
            services.AddScoped<IMaterialRequirementPlanRepository, MaterialRequirementPlanRepository>();
            services.AddScoped<ICapacityPlanRepository, CapacityPlanRepository>();

            // After sales service
            services.AddScoped<IServiceContractRepository, ServiceContractRepository>();
            services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();
            services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();

            // Projects
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<ITimesheetRepository, TimesheetRepository>();

            // Human resources
            services.AddScoped<IHumanRepository, HumanRepository>();
            services.AddScoped<IEmploymentContractRepository, EmploymentContractRepository>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<ILeaveRepository, LeaveRepository>();

            // Cross-cutting
            services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
            services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
            services.AddScoped<ILetterRepository, LetterRepository>();

            // Reporting
            services.AddScoped<IReportTemplateRepository, ReportTemplateRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();

            return services;
        }
    }
}
