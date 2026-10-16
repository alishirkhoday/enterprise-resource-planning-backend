using Microsoft.EntityFrameworkCore.Metadata;

namespace ERP.Infrastructure.MainDatabase.Context
{
    /// <summary>
    /// Adds the indexes that the model implies but that Entity Framework will not create on its own.
    /// <para>
    /// Real foreign keys are indexed automatically. What is not indexed is everything the domain
    /// references loosely: the branch a document belongs to, the cost centre it is charged to, the
    /// polymorphic pair that says what an attachment or an approval is about. Those columns are
    /// filtered on constantly and would otherwise force a table scan.
    /// </para>
    /// <para>
    /// Unique business keys are declared explicitly rather than guessed, because uniqueness is a
    /// rule about the business and getting it wrong by convention is worse than not having it.
    /// </para>
    /// </summary>
    internal static class IndexConvention
    {
        /// <summary>
        /// Identifier columns that point at something without a navigation property, so Entity
        /// Framework never sees them as a relationship.
        /// </summary>
        private static readonly HashSet<string> SoftReferenceNames = new(StringComparer.Ordinal)
        {
            "BranchId", "OrganizationId", "CostCenterId", "ProjectId", "WarehouseId", "StorageLocationId",
            "DetailAccountId", "PartyDetailAccountId", "AccountId", "DocumentId", "FinancialYearId",
            "AccountingPeriodId", "PaymentTermId", "PriceListId", "CustomerGroupId", "SupplierId",
            "ItemBatchId", "SerialNumberId", "UnitOfMeasureId", "CurrencyCode", "WorkCalendarId",
            "SalesRepHumanId", "AssignedToHumanId", "ManagerHumanId", "ResponsibleHumanId",
            "CustodianHumanId", "HolderHumanId", "RequestedByHumanId", "ApprovedByHumanId",
            "CountedByHumanId", "ReceivedByHumanId", "IssuedByHumanId", "EvaluatedByHumanId",
            "HiringManagerHumanId", "InterviewerHumanId", "OwnerHumanId", "SettlementId",
            "ReceivableId", "PayableId", "PurchaseOrderItemId", "SaleOrderItemId", "ApprovalRequestId",
            "TemplateId", "RecipientUserId", "CampaignId", "OpportunityId", "QuotationId",
            "SaleOrderId", "PurchaseOrderId", "GoodsReceiptId", "PurchaseInvoiceId", "InvoiceId",
            "PayrollId", "SalaryStructureId", "ContractId", "BillOfMaterialsId", "RoutingId",
            "InstallmentPlanId", "WalletId", "PaymentId", "ReturnRequestId", "RefundId",
            "ChequeId", "ChequeBookId", "BankBranchAccountId", "ReconciliationId", "BankTransactionId",
            "AttachmentId", "InspectionPlanId", "ProductionOrderId", "WarehouseTransactionId",
            "RequisitionId", "RequestForQuotationId", "TaxId", "TaxReturnId", "DiscountCodeId",
            "ReportTemplateId", "ScheduleId", "DowntimeLogId", "MachineId", "WorkStationId",
            "FixedAssetId", "OriginalInvoiceId", "SalesContractId", "ConvertedCustomerId",
            "ReplacementOrderId", "CreditNoteInvoiceId", "SoldToDetailAccountId", "BuyerDetailAccountId"
        };

        /// <summary>
        /// Business identifiers that must not repeat. Each is listed with the entity it belongs to,
        /// so the rule is visible rather than inferred from a naming pattern.
        /// </summary>
        private static readonly (string EntityName, string[] Properties)[] UniqueKeys =
        [
            ("Currency", ["Code"]),
            ("Country", ["Code"]),
            ("Organization", ["Name"]),
            ("Branch", ["Code"]),
            ("CostCenter", ["Code"]),
            ("NumberingSeries", ["Key", "BranchId", "FinancialYearId"]),
            ("SystemSetting", ["Key", "BranchId"]),
            ("Permission", ["Key"]),
            ("Role", ["NormalizedTitle"]),
            ("UserRole", ["UserId", "RoleId", "BranchId"]),
            ("RolePermission", ["RoleId", "PermissionId"]),
            ("Account", ["Code"]),
            ("DetailAccountGroup", ["Code"]),
            ("DetailAccount", ["GroupId", "Code"]),
            ("AccountDetailGroup", ["AccountId", "Level"]),
            ("AccountingPeriod", ["FinancialYearId", "Number"]),
            ("PaymentTerm", ["Code"]),
            ("CashBox", ["Code"]),
            ("PettyCash", ["Code"]),
            ("ChequeBook", ["SerialNumber"]),
            ("AssetCategory", ["Code"]),
            ("FixedAsset", ["Code"]),
            ("Tax", ["Code"]),
            ("SalaryStructure", ["Code"]),
            ("InsurancePolicy", ["Code"]),
            ("Department", ["Code"]),
            ("Skill", ["Name"]),
            ("TrainingCourse", ["Code"]),
            ("ItemGroup", ["Code"]),
            ("UnitOfMeasure", ["Code"]),
            ("Supplier", ["Code"]),
            ("Customer", ["Code"]),
            ("CustomerGroup", ["Code"]),
            ("Warehouse", ["Code"]),
            ("Carrier", ["Code"]),
            ("PriceList", ["Code"]),
            ("PriceListItem", ["PriceListId", "ItemId", "MinimumQuantity"]),
            ("PurchaseRequisition", ["Number"]),
            ("RequestForQuotation", ["Number"]),
            ("PurchaseOrder", ["Number"]),
            ("GoodsReceipt", ["Number"]),
            ("PurchaseInvoice", ["Number"]),
            ("PurchaseReturn", ["Number"]),
            ("SaleOrder", ["Number"]),
            ("Invoice", ["Number"]),
            ("Quotation", ["Number"]),
            ("SalesContract", ["Number"]),
            ("SalesTerritory", ["Code"]),
            ("Order", ["Code"]),
            ("DiscountCode", ["Code"]),
            ("Ticket", ["Number"]),
            ("StockCount", ["Number"]),
            ("StockTransferOrder", ["Number"]),
            ("Shipment", ["Number"]),
            ("WorkCenter", ["Code"]),
            ("Machine", ["Code"]),
            ("ProductionOrder", ["Code"]),
            ("MaterialIssue", ["Number"]),
            ("ProductionReceipt", ["Number"]),
            ("MaintenanceWorkOrder", ["Number"]),
            ("QualityParameter", ["Code"]),
            ("Project", ["Code"]),
            ("ItemBarcode", ["Value"]),
            ("ItemSupplier", ["ItemId", "SupplierId", "UnitOfMeasureId"]),
            ("ItemInventory", ["ItemId", "WarehouseId", "StorageLocationId"]),
            ("IncomeTaxBracket", ["Year", "Order"]),
            ("LeaveEntitlement", ["HumanId", "LeaveType", "Year"]),
            ("Wallet", ["CustomerId", "CurrencyCode"]),

            // Planning
            ("DemandForecast", ["Code"]),
            ("DemandForecastLine", ["ForecastId", "ItemId", "BucketStartDate"]),
            ("MaterialRequirementPlan", ["Code"]),
            ("CapacityPlan", ["Code"]),
            ("CapacityPlanLine", ["PlanId", "WorkCenterId", "BucketStartDate"]),

            // Purchasing agreements
            ("PurchaseContract", ["Number"]),
            ("PurchaseContractItem", ["ContractId", "ItemId"]),

            // After sales service
            ("ServiceContract", ["Number"]),
            ("WarrantyClaim", ["Number"]),
            ("ServiceOrder", ["Number"]),

            // A registration number is what makes a letter citable, so nothing may reuse one.
            ("Letter", ["Number"]),

            // The whole point of the inbox: a redelivery is recognised by the sender's own
            // identifier, and the database is what refuses the second row rather than a check that
            // two concurrent callbacks could both pass.
            ("InboxMessage", ["Source", "ExternalMessageId"])
        ];

        /// <summary>
        /// Pairs of columns that together say what a row is about. They are only useful looked up
        /// together, so they are indexed together.
        /// </summary>
        private static readonly string[][] CompositeLookups =
        [
            ["ReferenceType", "ReferenceId"],
            ["EntityName", "EntityId"]
        ];

        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.IsOwned() || entityType.GetTableName() is null)
                    continue;

                AddSoftReferenceIndexes(entityType);
                AddCompositeLookupIndexes(entityType);
                AddUniqueBusinessKey(entityType);
            }
        }

        private static void AddSoftReferenceIndexes(IMutableEntityType entityType)
        {
            foreach (var property in entityType.GetProperties())
            {
                if (!SoftReferenceNames.Contains(property.Name))
                    continue;

                // A real relationship is already indexed by Entity Framework.
                if (entityType.GetForeignKeys().Any(fk => fk.Properties.Count == 1 && fk.Properties[0] == property))
                    continue;

                if (entityType.FindIndex(property) is not null)
                    continue;

                entityType.AddIndex(property);
            }
        }

        private static void AddCompositeLookupIndexes(IMutableEntityType entityType)
        {
            foreach (var columnNames in CompositeLookups)
            {
                var properties = columnNames
                    .Select(entityType.FindProperty)
                    .OfType<IMutableProperty>()
                    .ToList();

                if (properties.Count != columnNames.Length)
                    continue;

                if (entityType.FindIndex(properties) is not null)
                    continue;

                entityType.AddIndex(properties);
            }
        }

        private static void AddUniqueBusinessKey(IMutableEntityType entityType)
        {
            var declaration = UniqueKeys.FirstOrDefault(u => u.EntityName == entityType.ClrType.Name);
            if (declaration.Properties is null)
                return;

            var properties = declaration.Properties
                .Select(entityType.FindProperty)
                .OfType<IMutableProperty>()
                .ToList();

            if (properties.Count != declaration.Properties.Length)
                return;

            var existing = entityType.FindIndex(properties);
            if (existing is not null)
            {
                existing.IsUnique = true;
                return;
            }

            var index = entityType.AddIndex(properties);
            index.IsUnique = true;
        }
    }
}
