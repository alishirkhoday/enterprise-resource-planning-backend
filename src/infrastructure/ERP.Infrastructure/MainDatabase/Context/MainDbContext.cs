using ERP.Application.Common.Interfaces.Branches;
using ERP.Application.Common.Interfaces.DbContext;
using ERP.Domain.Entities.Auditing;
using ERP.Domain.Entities.Branches;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Files;
using ERP.Domain.Entities.Integration;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;
using ERP.Domain.Entities.Modules.OrderManagement;
using ERP.Domain.Entities.Modules.PlanningManagement;
using ERP.Domain.Entities.Modules.PricingManagement;
using ERP.Domain.Entities.Modules.ProductionManagement;
using ERP.Domain.Entities.Modules.ProjectManagement;
using ERP.Domain.Entities.Modules.ReportingManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Entities.Modules.WarehouseManagement;
using ERP.Domain.Entities.Notifications;
using ERP.Domain.Entities.Organizations;
using ERP.Domain.Entities.Security;
using ERP.Domain.Entities.Users;
using ERP.Domain.Entities.Workflows;
using System.Linq.Expressions;

namespace ERP.Infrastructure.MainDatabase.Context
{
    public class MainDbContext : DbContext, IMainDbContext
    {
        private readonly IBranchContext _branchContext;

        /// <summary>
        /// Read by the branch query filter. They are fields on the context rather than values
        /// captured when the model was built, because the model is cached once per context type
        /// while these change with every caller - baking them in would hand the first user's
        /// access level to everyone who came afterwards.
        /// </summary>
        private bool CanAccessAllBranches => _branchContext.CanAccessAllBranches;

        private IReadOnlyCollection<Guid> AccessibleBranchIds => _branchContext.AccessibleBranchIds;

        public MainDbContext(DbContextOptions<MainDbContext> options)
            : this(options, UnrestrictedBranchContext.Instance)
        {
        }

        public MainDbContext(DbContextOptions<MainDbContext> options, IBranchContext branchContext) : base(options)
        {
            _branchContext = branchContext ?? UnrestrictedBranchContext.Instance;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);

            // Hand written configurations run first, so anything mapped explicitly wins over the
            // value object convention that fills in the rest.
            ValueObjectConvention.Apply(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.ClrType == typeof(BaseEntity))
                    continue;

                var builder = modelBuilder.Entity(entityType.ClrType);

                builder.Property(nameof(BaseEntity.Id))
                       .IsRequired();

                builder.Property(nameof(BaseEntity.CreationDateTime))
                       .IsRequired();

                builder.Property(nameof(BaseEntity.CreatedBy))
                       .HasMaxLength(100)
                       .IsRequired(false);

                // The stamp rotates on every change, so EF can use it to detect a concurrent write.
                builder.Property(nameof(BaseEntity.ConcurrencyStamp))
                       .HasMaxLength(100)
                       .IsConcurrencyToken()
                       .IsRequired(false);

                builder.Property(nameof(BaseEntity.ModifiedBy))
                       .HasMaxLength(100)
                       .IsRequired(false);

                builder.Property(nameof(BaseEntity.ModificationDateTime))
                       .IsRequired(false);

                builder.Property(nameof(BaseEntity.IsDeleted))
                       .HasDefaultValue(false)
                       .IsRequired();

                builder.Property(nameof(BaseEntity.DeletedBy))
                       .HasMaxLength(100)
                       .IsRequired(false);

                builder.Property(nameof(BaseEntity.DeletionDateTime))
                       .IsRequired(false);

                // Soft deleted rows stay in the table but drop out of every ordinary query, and a
                // branch scoped row also drops out unless the caller may see that branch. EF allows
                // one filter per entity, so the two conditions are built as a single expression.
                builder.HasQueryFilter(BuildQueryFilter(entityType.ClrType));

                // Domain events live only in memory; they are dispatched, not stored.
                builder.Ignore(nameof(BaseEntity.DomainEvents));
            }

            // Column shapes and indexes are applied last, once every property the model will have
            // actually exists.
            StorageConvention.Apply(modelBuilder);
            IndexConvention.Apply(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private LambdaExpression BuildQueryFilter(Type entityClrType)
        {
            var parameter = Expression.Parameter(entityClrType, "e");

            Expression body = Expression.Equal(
                Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                Expression.Constant(false));

            if (entityClrType.IsAssignableTo(typeof(IBranchScoped)))
                body = Expression.AndAlso(body, BuildBranchCondition(parameter));

            return Expression.Lambda(body, parameter);
        }

        /// <summary>
        /// Builds <c>e.BranchId == null || CanAccessAllBranches || AccessibleBranchIds.Contains(e.BranchId.Value)</c>.
        /// <para>
        /// All three terms live inside the expression. Deciding in C# which of them to emit would
        /// put the first caller's access level into the cached model; leaving them in makes EF read
        /// the context and send them as parameters on every query instead.
        /// </para>
        /// </summary>
        private Expression BuildBranchCondition(ParameterExpression parameter)
        {
            var branchId = Expression.Property(parameter, nameof(IBranchScoped.BranchId));
            var self = Expression.Constant(this);

            var isOrganizationWide = Expression.Equal(branchId, Expression.Constant(null, typeof(Guid?)));

            var canSeeEverything = Expression.Property(self, nameof(CanAccessAllBranches));

            var isAccessible = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(Guid)],
                Expression.Property(self, nameof(AccessibleBranchIds)),
                Expression.Property(branchId, nameof(Nullable<Guid>.Value)));

            return Expression.OrElse(isOrganizationWide, Expression.OrElse(canSeeEverything, isAccessible));
        }

        /// <summary>
        /// Writes every fact the tracked entities recorded into the outbox, in the same transaction
        /// as the change that produced them.
        /// <para>
        /// This is what makes an event reliable. Publishing before the save lets a handler observe
        /// something that then rolls back; publishing after the commit loses the event if the
        /// process dies in between. A row written alongside the change can do neither.
        /// </para>
        /// </summary>
        public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
        {
            var carriers = ChangeTracker
                .Entries<BaseEntity>()
                .Where(entry => entry.Entity.DomainEvents.Count > 0)
                .Select(entry => entry.Entity)
                .ToList();

            foreach (var carrier in carriers)
            {
                foreach (var domainEvent in carrier.DomainEvents)
                {
                    Set<OutboxMessage>().Add(new OutboxMessage(
                        DomainEventSerializer.TypeNameOf(domainEvent),
                        DomainEventSerializer.Serialize(domainEvent),
                        domainEvent.EventId.ToString("N")));
                }
            }

            var written = await base.SaveChangesAsync(cancellationToken);

            // Cleared only once the write succeeded. Clearing first would drop the facts on the
            // floor if the save threw and the caller retried.
            foreach (var carrier in carriers)
            {
                carrier.ClearDomainEvents();
            }

            return written;
        }

        public DbSet<TEntity> SetEntity<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        // Organization and branches
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<BranchUser> BranchUsers => Set<BranchUser>();
        public DbSet<BranchUserRole> BranchUserRoles => Set<BranchUserRole>();
        public DbSet<BranchCustomer> BranchCustomers => Set<BranchCustomer>();
        public DbSet<BranchHuman> BranchHumans => Set<BranchHuman>();
        public DbSet<BranchOrder> BranchOrders => Set<BranchOrder>();

        // Shared master data
        public DbSet<Currency> Currencies => Set<Currency>();
        public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<Province> Provinces => Set<Province>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<NumberingSeries> NumberingSeries => Set<NumberingSeries>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
        public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();
        public DbSet<Holiday> Holidays => Set<Holiday>();
        public DbSet<CostCenter> CostCenters => Set<CostCenter>();

        // Identity and security
        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UsersProfiles => Set<UserProfile>();
        public DbSet<UserSession> UsersSessions => Set<UserSession>();
        public DbSet<UserToken> UserTokens => Set<UserToken>();
        public DbSet<UserActivityHistory> UserActivityHistories => Set<UserActivityHistory>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        // Auditing, notifications and workflow
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AuditLogChange> AuditLogChanges => Set<AuditLogChange>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
        public DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps => Set<ApprovalWorkflowStep>();
        public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
        public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();

        public DbSet<Attachment> Attachments => Set<Attachment>();

        // Human resources
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Human> Humans => Set<Human>();
        public DbSet<HumanContactInformation> HumansContactInformation => Set<HumanContactInformation>();
        public DbSet<HumanAddress> HumansAddresses => Set<HumanAddress>();
        public DbSet<HumanBankAccount> HumansBankAccounts => Set<HumanBankAccount>();
        public DbSet<HumanEducation> HumanEducations => Set<HumanEducation>();
        public DbSet<HumanEducationFieldOfStudy> HumanEducationFieldOfStudies => Set<HumanEducationFieldOfStudy>();
        public DbSet<HumanEducationUniversity> HumanEducationUniversities => Set<HumanEducationUniversity>();
        public DbSet<HumanWorkExperience> HumanWorkExperiences => Set<HumanWorkExperience>();
        public DbSet<HumanContract> HumanContracts => Set<HumanContract>();
        public DbSet<HumanAttendanceRecord> HumanAttendanceRecords => Set<HumanAttendanceRecord>();
        public DbSet<HumanRequest> HumanLeaveRequests => Set<HumanRequest>();
        public DbSet<HumanPerformanceReview> HumanPerformanceReviews => Set<HumanPerformanceReview>();
        public DbSet<HumanDependent> HumanDependents => Set<HumanDependent>();
        public DbSet<HumanDocument> HumanDocuments => Set<HumanDocument>();
        public DbSet<HumanSkill> HumanSkills => Set<HumanSkill>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<LeaveEntitlement> LeaveEntitlements => Set<LeaveEntitlement>();
        public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
        public DbSet<WorkScheduleDay> WorkScheduleDays => Set<WorkScheduleDay>();
        public DbSet<Overtime> Overtimes => Set<Overtime>();
        public DbSet<Mission> Missions => Set<Mission>();
        public DbSet<MissionExpense> MissionExpenses => Set<MissionExpense>();
        public DbSet<PerformanceGoal> PerformanceGoals => Set<PerformanceGoal>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<Interview> Interviews => Set<Interview>();
        public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
        public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
        public DbSet<TrainingEnrollment> TrainingEnrollments => Set<TrainingEnrollment>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Job> Jobs => Set<Job>();

        // Finance
        public DbSet<FinancialYear> FinancialYears => Set<FinancialYear>();
        public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
        public DbSet<Bank> Banks => Set<Bank>();
        public DbSet<BankBranch> BankBranches => Set<BankBranch>();
        public DbSet<BankBranchAccount> BankBranchAccounts => Set<BankBranchAccount>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
        public DbSet<BankReconciliationItem> BankReconciliationItems => Set<BankReconciliationItem>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<DetailAccount> DetailAccounts => Set<DetailAccount>();
        public DbSet<DetailAccountGroup> DetailAccountGroups => Set<DetailAccountGroup>();
        public DbSet<AccountDetailGroup> AccountDetailGroups => Set<AccountDetailGroup>();
        public DbSet<AccountPeriodBalance> AccountPeriodBalances => Set<AccountPeriodBalance>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
        public DbSet<CashBox> CashBoxes => Set<CashBox>();
        public DbSet<PettyCash> PettyCashes => Set<PettyCash>();
        public DbSet<Cheque> Cheques => Set<Cheque>();
        public DbSet<ChequeBook> ChequeBooks => Set<ChequeBook>();
        public DbSet<ChequeOperation> ChequeOperations => Set<ChequeOperation>();
        public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
        public DbSet<Receivable> Receivables => Set<Receivable>();
        public DbSet<Payable> Payables => Set<Payable>();
        public DbSet<Settlement> Settlements => Set<Settlement>();
        public DbSet<SettlementAllocation> SettlementAllocations => Set<SettlementAllocation>();
        public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
        public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
        public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();
        public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();
        public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();
        public DbSet<Tax> Taxes => Set<Tax>();
        public DbSet<TaxTransaction> TaxTransactions => Set<TaxTransaction>();
        public DbSet<TaxReturn> TaxReturns => Set<TaxReturn>();
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
        public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
        public DbSet<SalaryStructureItem> SalaryStructureItems => Set<SalaryStructureItem>();
        public DbSet<Payroll> Payrolls => Set<Payroll>();
        public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
        public DbSet<Reward> Rewards => Set<Reward>();
        public DbSet<Deduction> Deductions => Set<Deduction>();
        public DbSet<PayrollReward> PayrollRewards => Set<PayrollReward>();
        public DbSet<PayrollDeduction> PayrollDeductions => Set<PayrollDeduction>();
        public DbSet<IncomeTaxBracket> IncomeTaxBrackets => Set<IncomeTaxBracket>();
        public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();

        // Warehouse
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseStorageLocation> WarehouseStorageLocations => Set<WarehouseStorageLocation>();
        public DbSet<WarehouseTransaction> WarehouseTransactions => Set<WarehouseTransaction>();
        public DbSet<WarehouseTransactionItem> WarehouseTransactionItems => Set<WarehouseTransactionItem>();
        public DbSet<StockTransferOrder> StockTransferOrders => Set<StockTransferOrder>();
        public DbSet<StockTransferOrderItem> StockTransferOrderItems => Set<StockTransferOrderItem>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
        public DbSet<Carrier> Carriers => Set<Carrier>();

        // Inventory and purchasing
        public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
        public DbSet<ItemAttribute> ItemAttributes => Set<ItemAttribute>();
        public DbSet<ItemAttributeValue> ItemAttributesValues => Set<ItemAttributeValue>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<ItemSpecification> ItemSpecifications => Set<ItemSpecification>();
        public DbSet<ItemInventory> ItemsInventories => Set<ItemInventory>();
        public DbSet<ItemBatch> ItemBatches => Set<ItemBatch>();
        public DbSet<ItemBarcode> ItemBarcodes => Set<ItemBarcode>();
        public DbSet<ItemSupplier> ItemSuppliers => Set<ItemSupplier>();
        public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
        public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
        public DbSet<StockCount> StockCounts => Set<StockCount>();
        public DbSet<StockCountItem> StockCountItems => Set<StockCountItem>();
        public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
        public DbSet<UnitOfMeasureConversion> UnitOfMeasureConversions => Set<UnitOfMeasureConversion>();
        public DbSet<ItemUnitOfMeasure> ItemUnitOfMeasures => Set<ItemUnitOfMeasure>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierContact> SupplierContacts => Set<SupplierContact>();
        public DbSet<SupplierBankAccount> SupplierBankAccounts => Set<SupplierBankAccount>();
        public DbSet<SupplierEvaluation> SupplierEvaluations => Set<SupplierEvaluation>();
        public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
        public DbSet<PurchaseRequisitionItem> PurchaseRequisitionItems => Set<PurchaseRequisitionItem>();
        public DbSet<RequestForQuotation> RequestsForQuotation => Set<RequestForQuotation>();
        public DbSet<RequestForQuotationItem> RequestForQuotationItems => Set<RequestForQuotationItem>();
        public DbSet<SupplierQuotation> SupplierQuotations => Set<SupplierQuotation>();
        public DbSet<SupplierQuotationItem> SupplierQuotationItems => Set<SupplierQuotationItem>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
        public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
        public DbSet<SaleOrder> SaleOrders => Set<SaleOrder>();
        public DbSet<SaleOrderItem> SaleOrderItems => Set<SaleOrderItem>();

        // Pricing
        public DbSet<PriceList> PriceLists => Set<PriceList>();
        public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();

        // Customers
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
        public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
        public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
        public DbSet<Ticket> CustomerTickets => Set<Ticket>();
        public DbSet<TicketSubject> CustomerTicketSubjects => Set<TicketSubject>();
        public DbSet<TicketMessage> CustomerTicketMessages => Set<TicketMessage>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        public DbSet<Meeting> Meetings => Set<Meeting>();
        public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();

        // Orders and payments
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderShipmentStatusHistory> OrderShipmentStatusHistories => Set<OrderShipmentStatusHistory>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Refund> Refunds => Set<Refund>();
        public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
        public DbSet<Installment> Installments => Set<Installment>();
        public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
        public DbSet<ReturnRequestItem> ReturnRequestItems => Set<ReturnRequestItem>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

        // Sales and marketing
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<LeadInteraction> LeadInteractions => Set<LeadInteraction>();
        public DbSet<LeadNote> LeadNotes => Set<LeadNote>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<Campaign> Campaigns => Set<Campaign>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
        public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
        public DbSet<SalesContract> SalesContracts => Set<SalesContract>();
        public DbSet<SalesTarget> SalesTargets => Set<SalesTarget>();
        public DbSet<SalesTerritory> SalesTerritories => Set<SalesTerritory>();
        public DbSet<Commission> Commissions => Set<Commission>();
        public DbSet<Activity> Activities => Set<Activity>();

        // Production
        public DbSet<WorkCenter> WorkCenters => Set<WorkCenter>();
        public DbSet<WorkStation> WorkStations => Set<WorkStation>();
        public DbSet<Machine> Machines => Set<Machine>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<Routing> Routings => Set<Routing>();
        public DbSet<RoutingOperation> RoutingOperations => Set<RoutingOperation>();
        public DbSet<BillOfMaterials> BillsOfMaterials => Set<BillOfMaterials>();
        public DbSet<BillOfMaterialItem> BillOfMaterialItems => Set<BillOfMaterialItem>();
        public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
        public DbSet<ProductionTask> ProductionTasks => Set<ProductionTask>();
        public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
        public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
        public DbSet<MaterialIssueItem> MaterialIssueItems => Set<MaterialIssueItem>();
        public DbSet<ProductionReceipt> ProductionReceipts => Set<ProductionReceipt>();
        public DbSet<ProductionSchedule> ProductionSchedules => Set<ProductionSchedule>();
        public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
        public DbSet<ProductionReport> ProductionReports => Set<ProductionReport>();
        public DbSet<ProductionDefectReport> ProductionDefectReports => Set<ProductionDefectReport>();
        public DbSet<ProductionSensorLog> ProductionSensorLogs => Set<ProductionSensorLog>();
        public DbSet<QualityParameter> QualityParameters => Set<QualityParameter>();
        public DbSet<QualityControlCheck> QualityControlChecks => Set<QualityControlCheck>();
        public DbSet<QualityMeasurement> QualityMeasurements => Set<QualityMeasurement>();
        public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
        public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();
        public DbSet<MaintenancePartUsage> MaintenancePartUsages => Set<MaintenancePartUsage>();
        public DbSet<MachineUsageLog> MachineUsageLogs => Set<MachineUsageLog>();
        public DbSet<DowntimeLog> DowntimeLogs => Set<DowntimeLog>();

        // Projects
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectPhase> ProjectPhases => Set<ProjectPhase>();
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Timesheet> Timesheets => Set<Timesheet>();
        public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();

        // Reporting
        public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
        public DbSet<ReportFilterDefinition> ReportFilterDefinitions => Set<ReportFilterDefinition>();
        public DbSet<ReportExecution> ReportExecutions => Set<ReportExecution>();
        public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();
        public DbSet<Dashboard> Dashboards => Set<Dashboard>();
        public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();

        // Planning
        public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
        public DbSet<DemandForecastLine> DemandForecastLines => Set<DemandForecastLine>();
        public DbSet<MaterialRequirementPlan> MaterialRequirementPlans => Set<MaterialRequirementPlan>();
        public DbSet<MaterialRequirementPlanLine> MaterialRequirementPlanLines => Set<MaterialRequirementPlanLine>();
        public DbSet<CapacityPlan> CapacityPlans => Set<CapacityPlan>();
        public DbSet<CapacityPlanLine> CapacityPlanLines => Set<CapacityPlanLine>();

        // Purchasing agreements
        public DbSet<PurchaseContract> PurchaseContracts => Set<PurchaseContract>();
        public DbSet<PurchaseContractItem> PurchaseContractItems => Set<PurchaseContractItem>();

        // After sales service
        public DbSet<ServiceContract> ServiceContracts => Set<ServiceContract>();
        public DbSet<ServiceContractCoverage> ServiceContractCoverages => Set<ServiceContractCoverage>();
        public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();
        public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
        public DbSet<ServiceOrderPart> ServiceOrderParts => Set<ServiceOrderPart>();

        // Correspondence
        public DbSet<Letter> Letters => Set<Letter>();
        public DbSet<LetterRecipient> LetterRecipients => Set<LetterRecipient>();
        public DbSet<LetterReferral> LetterReferrals => Set<LetterReferral>();

        // Integration
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    }
}
