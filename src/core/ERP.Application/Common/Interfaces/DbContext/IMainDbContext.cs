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
using ERP.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Common.Interfaces.DbContext
{
    public interface IMainDbContext
    {
        Task<int> SaveChanges(CancellationToken cancellationToken = default);

        //EF Core
        DbSet<TEntity> SetEntity<TEntity>() where TEntity : BaseEntity;

        // Organization and branches
        DbSet<Organization> Organizations { get; }
        DbSet<Branch> Branches { get; }
        DbSet<BranchUser> BranchUsers { get; }
        DbSet<BranchUserRole> BranchUserRoles { get; }
        DbSet<BranchCustomer> BranchCustomers { get; }
        DbSet<BranchHuman> BranchHumans { get; }
        DbSet<BranchOrder> BranchOrders { get; }

        // Shared master data
        DbSet<Currency> Currencies { get; }
        DbSet<ExchangeRate> ExchangeRates { get; }
        DbSet<Country> Countries { get; }
        DbSet<Province> Provinces { get; }
        DbSet<City> Cities { get; }
        DbSet<NumberingSeries> NumberingSeries { get; }
        DbSet<SystemSetting> SystemSettings { get; }
        DbSet<WorkCalendar> WorkCalendars { get; }
        DbSet<Holiday> Holidays { get; }
        DbSet<CostCenter> CostCenters { get; }

        // Identity and security
        DbSet<User> Users { get; }
        DbSet<UserProfile> UsersProfiles { get; }
        DbSet<UserSession> UsersSessions { get; }
        DbSet<UserToken> UserTokens { get; }
        DbSet<UserActivityHistory> UserActivityHistories { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }

        // Auditing, notifications and workflow
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<AuditLogChange> AuditLogChanges { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<NotificationTemplate> NotificationTemplates { get; }
        DbSet<OutboxMessage> OutboxMessages { get; }
        DbSet<ApprovalWorkflow> ApprovalWorkflows { get; }
        DbSet<ApprovalWorkflowStep> ApprovalWorkflowSteps { get; }
        DbSet<ApprovalRequest> ApprovalRequests { get; }
        DbSet<ApprovalAction> ApprovalActions { get; }

        DbSet<Attachment> Attachments { get; }

        // Human resources
        DbSet<Department> Departments { get; }
        DbSet<Position> Positions { get; }
        DbSet<Human> Humans { get; }
        DbSet<HumanContactInformation> HumansContactInformation { get; }
        DbSet<HumanAddress> HumansAddresses { get; }
        DbSet<HumanBankAccount> HumansBankAccounts { get; }
        DbSet<HumanEducation> HumanEducations { get; }
        DbSet<HumanEducationFieldOfStudy> HumanEducationFieldOfStudies { get; }
        DbSet<HumanEducationUniversity> HumanEducationUniversities { get; }
        DbSet<HumanWorkExperience> HumanWorkExperiences { get; }
        DbSet<HumanContract> HumanContracts { get; }
        DbSet<HumanAttendanceRecord> HumanAttendanceRecords { get; }
        DbSet<HumanRequest> HumanLeaveRequests { get; }
        DbSet<HumanPerformanceReview> HumanPerformanceReviews { get; }
        DbSet<HumanDependent> HumanDependents { get; }
        DbSet<HumanDocument> HumanDocuments { get; }
        DbSet<HumanSkill> HumanSkills { get; }
        DbSet<Skill> Skills { get; }
        DbSet<LeaveEntitlement> LeaveEntitlements { get; }
        DbSet<WorkSchedule> WorkSchedules { get; }
        DbSet<WorkScheduleDay> WorkScheduleDays { get; }
        DbSet<Overtime> Overtimes { get; }
        DbSet<Mission> Missions { get; }
        DbSet<MissionExpense> MissionExpenses { get; }
        DbSet<PerformanceGoal> PerformanceGoals { get; }
        DbSet<JobPosting> JobPostings { get; }
        DbSet<Candidate> Candidates { get; }
        DbSet<JobApplication> JobApplications { get; }
        DbSet<Interview> Interviews { get; }
        DbSet<TrainingCourse> TrainingCourses { get; }
        DbSet<TrainingSession> TrainingSessions { get; }
        DbSet<TrainingEnrollment> TrainingEnrollments { get; }
        DbSet<Company> Companies { get; }
        DbSet<Job> Jobs { get; }

        // Finance
        DbSet<FinancialYear> FinancialYears { get; }
        DbSet<AccountingPeriod> AccountingPeriods { get; }
        DbSet<Bank> Banks { get; }
        DbSet<BankBranch> BankBranches { get; }
        DbSet<BankBranchAccount> BankBranchAccounts { get; }
        DbSet<BankTransaction> BankTransactions { get; }
        DbSet<BankReconciliation> BankReconciliations { get; }
        DbSet<BankReconciliationItem> BankReconciliationItems { get; }
        DbSet<Account> Accounts { get; }
        DbSet<DetailAccount> DetailAccounts { get; }
        DbSet<DetailAccountGroup> DetailAccountGroups { get; }
        DbSet<AccountDetailGroup> AccountDetailGroups { get; }
        DbSet<AccountPeriodBalance> AccountPeriodBalances { get; }
        DbSet<Document> Documents { get; }
        DbSet<DocumentItem> DocumentItems { get; }
        DbSet<CashBox> CashBoxes { get; }
        DbSet<PettyCash> PettyCashes { get; }
        DbSet<Cheque> Cheques { get; }
        DbSet<ChequeBook> ChequeBooks { get; }
        DbSet<ChequeOperation> ChequeOperations { get; }
        DbSet<PaymentTerm> PaymentTerms { get; }
        DbSet<Receivable> Receivables { get; }
        DbSet<Payable> Payables { get; }
        DbSet<Settlement> Settlements { get; }
        DbSet<SettlementAllocation> SettlementAllocations { get; }
        DbSet<AssetCategory> AssetCategories { get; }
        DbSet<FixedAsset> FixedAssets { get; }
        DbSet<DepreciationEntry> DepreciationEntries { get; }
        DbSet<AssetDisposal> AssetDisposals { get; }
        DbSet<AssetTransfer> AssetTransfers { get; }
        DbSet<Tax> Taxes { get; }
        DbSet<TaxTransaction> TaxTransactions { get; }
        DbSet<TaxReturn> TaxReturns { get; }
        DbSet<Budget> Budgets { get; }
        DbSet<BudgetLine> BudgetLines { get; }
        DbSet<SalaryStructure> SalaryStructures { get; }
        DbSet<SalaryStructureItem> SalaryStructureItems { get; }
        DbSet<Payroll> Payrolls { get; }
        DbSet<PayrollItem> PayrollItems { get; }
        DbSet<Reward> Rewards { get; }
        DbSet<Deduction> Deductions { get; }
        DbSet<PayrollReward> PayrollRewards { get; }
        DbSet<PayrollDeduction> PayrollDeductions { get; }
        DbSet<IncomeTaxBracket> IncomeTaxBrackets { get; }
        DbSet<InsurancePolicy> InsurancePolicies { get; }
        DbSet<Loan> Loans { get; }
        DbSet<LoanInstallment> LoanInstallments { get; }

        // Warehouse
        DbSet<Warehouse> Warehouses { get; }
        DbSet<WarehouseStorageLocation> WarehouseStorageLocations { get; }
        DbSet<WarehouseTransaction> WarehouseTransactions { get; }
        DbSet<WarehouseTransactionItem> WarehouseTransactionItems { get; }
        DbSet<StockTransferOrder> StockTransferOrders { get; }
        DbSet<StockTransferOrderItem> StockTransferOrderItems { get; }
        DbSet<Shipment> Shipments { get; }
        DbSet<ShipmentItem> ShipmentItems { get; }
        DbSet<Carrier> Carriers { get; }

        // Inventory and purchasing
        DbSet<ItemGroup> ItemGroups { get; }
        DbSet<ItemAttribute> ItemAttributes { get; }
        DbSet<ItemAttributeValue> ItemAttributesValues { get; }
        DbSet<Item> Items { get; }
        DbSet<ItemSpecification> ItemSpecifications { get; }
        DbSet<ItemInventory> ItemsInventories { get; }
        DbSet<ItemBatch> ItemBatches { get; }
        DbSet<ItemBarcode> ItemBarcodes { get; }
        DbSet<ItemSupplier> ItemSuppliers { get; }
        DbSet<SerialNumber> SerialNumbers { get; }
        DbSet<StockLedgerEntry> StockLedgerEntries { get; }
        DbSet<StockCount> StockCounts { get; }
        DbSet<StockCountItem> StockCountItems { get; }
        DbSet<UnitOfMeasure> UnitOfMeasures { get; }
        DbSet<UnitOfMeasureConversion> UnitOfMeasureConversions { get; }
        DbSet<ItemUnitOfMeasure> ItemUnitOfMeasures { get; }
        DbSet<Supplier> Suppliers { get; }
        DbSet<SupplierContact> SupplierContacts { get; }
        DbSet<SupplierBankAccount> SupplierBankAccounts { get; }
        DbSet<SupplierEvaluation> SupplierEvaluations { get; }
        DbSet<PurchaseRequisition> PurchaseRequisitions { get; }
        DbSet<PurchaseRequisitionItem> PurchaseRequisitionItems { get; }
        DbSet<RequestForQuotation> RequestsForQuotation { get; }
        DbSet<RequestForQuotationItem> RequestForQuotationItems { get; }
        DbSet<SupplierQuotation> SupplierQuotations { get; }
        DbSet<SupplierQuotationItem> SupplierQuotationItems { get; }
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
        DbSet<GoodsReceipt> GoodsReceipts { get; }
        DbSet<GoodsReceiptItem> GoodsReceiptItems { get; }
        DbSet<PurchaseInvoice> PurchaseInvoices { get; }
        DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; }
        DbSet<PurchaseReturn> PurchaseReturns { get; }
        DbSet<PurchaseReturnItem> PurchaseReturnItems { get; }
        DbSet<SaleOrder> SaleOrders { get; }
        DbSet<SaleOrderItem> SaleOrderItems { get; }

        // Pricing
        DbSet<PriceList> PriceLists { get; }
        DbSet<PriceListItem> PriceListItems { get; }

        // Customers
        DbSet<Customer> Customers { get; }
        DbSet<CustomerAddress> CustomerAddresses { get; }
        DbSet<CustomerContact> CustomerContacts { get; }
        DbSet<CustomerGroup> CustomerGroups { get; }
        DbSet<Ticket> CustomerTickets { get; }
        DbSet<TicketSubject> CustomerTicketSubjects { get; }
        DbSet<TicketMessage> CustomerTicketMessages { get; }
        DbSet<Invoice> Invoices { get; }
        DbSet<InvoiceItem> InvoiceItems { get; }

        DbSet<Meeting> Meetings { get; }
        DbSet<MeetingAttendee> MeetingAttendees { get; }

        // Orders and payments
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<OrderShipmentStatusHistory> OrderShipmentStatusHistories { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Refund> Refunds { get; }
        DbSet<InstallmentPlan> InstallmentPlans { get; }
        DbSet<Installment> Installments { get; }
        DbSet<ReturnRequest> ReturnRequests { get; }
        DbSet<ReturnRequestItem> ReturnRequestItems { get; }
        DbSet<Wallet> Wallets { get; }
        DbSet<WalletTransaction> WalletTransactions { get; }
        DbSet<DiscountCode> DiscountCodes { get; }

        // Sales and marketing
        DbSet<Lead> Leads { get; }
        DbSet<LeadInteraction> LeadInteractions { get; }
        DbSet<LeadNote> LeadNotes { get; }
        DbSet<Opportunity> Opportunities { get; }
        DbSet<Campaign> Campaigns { get; }
        DbSet<Quotation> Quotations { get; }
        DbSet<QuotationItem> QuotationItems { get; }
        DbSet<SalesContract> SalesContracts { get; }
        DbSet<SalesTarget> SalesTargets { get; }
        DbSet<SalesTerritory> SalesTerritories { get; }
        DbSet<Commission> Commissions { get; }
        DbSet<Activity> Activities { get; }

        // Production
        DbSet<WorkCenter> WorkCenters { get; }
        DbSet<WorkStation> WorkStations { get; }
        DbSet<Machine> Machines { get; }
        DbSet<Shift> Shifts { get; }
        DbSet<Routing> Routings { get; }
        DbSet<RoutingOperation> RoutingOperations { get; }
        DbSet<BillOfMaterials> BillsOfMaterials { get; }
        DbSet<BillOfMaterialItem> BillOfMaterialItems { get; }
        DbSet<ProductionOrder> ProductionOrders { get; }
        DbSet<ProductionTask> ProductionTasks { get; }
        DbSet<TaskAssignment> TaskAssignments { get; }
        DbSet<MaterialIssue> MaterialIssues { get; }
        DbSet<MaterialIssueItem> MaterialIssueItems { get; }
        DbSet<ProductionReceipt> ProductionReceipts { get; }
        DbSet<ProductionSchedule> ProductionSchedules { get; }
        DbSet<ProductionBatch> ProductionBatches { get; }
        DbSet<ProductionReport> ProductionReports { get; }
        DbSet<ProductionDefectReport> ProductionDefectReports { get; }
        DbSet<ProductionSensorLog> ProductionSensorLogs { get; }
        DbSet<QualityParameter> QualityParameters { get; }
        DbSet<QualityControlCheck> QualityControlChecks { get; }
        DbSet<QualityMeasurement> QualityMeasurements { get; }
        DbSet<MaintenanceSchedule> MaintenanceSchedules { get; }
        DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders { get; }
        DbSet<MaintenancePartUsage> MaintenancePartUsages { get; }
        DbSet<MachineUsageLog> MachineUsageLogs { get; }
        DbSet<DowntimeLog> DowntimeLogs { get; }

        // Projects
        DbSet<Project> Projects { get; }
        DbSet<ProjectPhase> ProjectPhases { get; }
        DbSet<ProjectTask> ProjectTasks { get; }
        DbSet<ProjectMilestone> ProjectMilestones { get; }
        DbSet<ProjectMember> ProjectMembers { get; }
        DbSet<Timesheet> Timesheets { get; }
        DbSet<TimesheetEntry> TimesheetEntries { get; }

        // Reporting
        DbSet<ReportTemplate> ReportTemplates { get; }
        DbSet<ReportFilterDefinition> ReportFilterDefinitions { get; }
        DbSet<ReportExecution> ReportExecutions { get; }
        DbSet<ScheduledReport> ScheduledReports { get; }
        DbSet<Dashboard> Dashboards { get; }
        DbSet<DashboardWidget> DashboardWidgets { get; }

        // Planning
        DbSet<DemandForecast> DemandForecasts { get; }
        DbSet<DemandForecastLine> DemandForecastLines { get; }
        DbSet<MaterialRequirementPlan> MaterialRequirementPlans { get; }
        DbSet<MaterialRequirementPlanLine> MaterialRequirementPlanLines { get; }
        DbSet<CapacityPlan> CapacityPlans { get; }
        DbSet<CapacityPlanLine> CapacityPlanLines { get; }

        // Purchasing agreements
        DbSet<PurchaseContract> PurchaseContracts { get; }
        DbSet<PurchaseContractItem> PurchaseContractItems { get; }

        // After sales service
        DbSet<ServiceContract> ServiceContracts { get; }
        DbSet<ServiceContractCoverage> ServiceContractCoverages { get; }
        DbSet<WarrantyClaim> WarrantyClaims { get; }
        DbSet<ServiceOrder> ServiceOrders { get; }
        DbSet<ServiceOrderPart> ServiceOrderParts { get; }

        // Correspondence
        DbSet<Letter> Letters { get; }
        DbSet<LetterRecipient> LetterRecipients { get; }
        DbSet<LetterReferral> LetterReferrals { get; }

        // Integration
        DbSet<InboxMessage> InboxMessages { get; }
    }
}
