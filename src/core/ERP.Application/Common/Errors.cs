using ERP.Domain.Entities.Modules.FinanceManagement;

namespace ERP.Application.Common
{
    public abstract class Errors
    {
        public abstract class General
        {
            public static Error IdIsEmpty()
            {
                return new Error("IdIsEmpty", "Id is empty.");
            }

            public static Error NotFound(string? id = null, string? entity = null)
            {
                string forId = id == null ? "" : $" for Id '{id}'";
                string forEntity = entity == null ? "" : $" from '{entity}'";
                return new Error("RecordNotFound", $"Record not found{forId}{forEntity}!");
            }

            public static Error AlreadyExists(object record)
            {
                return new Error("RecordAlreadyExistence", $"Record already exists for {record}.");
            }

            public static Error AnErrorHasOccurred()
            {
                return new Error("AnErrorHasOccurred", $"An error has occurred.");
            }

            public static Error NotSavedChanges()
            {
                return new Error("NotSavedChanges", "Not saved changes.");
            }
        }

        public abstract class User
        {
            public static Error UsernameIsUsed(string username)
            {
                return new Error("UsernameIsUsed", $"{username} is used.");
            }

            public static Error MobilePhoneNumberIsUsed(string mobilePhoneNumberRegionCode, string mobilePhoneNumber)
            {
                return new Error("UserMobilePhoneNumberIsAlreadyRegistered", $"MobilePhoneNumber {mobilePhoneNumberRegionCode}{mobilePhoneNumber} is already registered.");
            }

            public static Error EmailIsUsed(string email)
            {
                return new Error("UserEmailIsAlreadyRegistered", $"Email {email} is already registered.");
            }

            public static Error YouMustChooseOneOfTheRegistrationMethods()
            {
                return new Error("YouMustChooseOneOfTheRegistrationMethods", "You must choose one of the registration methods : Mobile phone number or Email.");
            }

            public static Error ThisMobilePhoneNumberDoesNotMatch()
            {
                return new Error("ThisMobilePhoneNumberDoesNotMatch", "This mobile phone number does not match the user's mobile phone number.");
            }

            public static Error ThisEmailDoesNotMatch()
            {
                return new Error("ThisEmailDoesNotMatch", "This email does not match the user's email.");
            }

            public static Error YouMustChooseOneOfTheMethods()
            {
                return new Error("YouMustChooseOneOfTheMethods", "You must choose one of the methods : Mobile phone number or Email.");
            }

            public static Error TheNumberOfRequestsForVerificationCodeHasBeenReached()
            {
                return new Error("TheNumberOfRequestsForVerificationCodeHasBeenReached", $"The number of requests for verification code has been reached.");
            }

            public static Error ErrorForSendVerificationCode()
            {
                return new Error("ErrorForSendVerificationCode", "Error for send verification code");
            }

            public static Error VerificationCodeIsNotValid()
            {
                return new Error("VerificationCodeIsNotValid", $"Verification code is not valid.");
            }

            public static Error UserNotFound()
            {
                return new Error("UserNotFound", $"User not found.");
            }

            public static Error MobilePhoneNumberNotConfirm(string mobilePhoneNumberRegionCode, string mobilePhoneNumber)
            {
                return new Error("UserMobilePhoneNumberNotConfirm", $"MobilePhoneNumber {mobilePhoneNumberRegionCode}{mobilePhoneNumber} not confirm.");
            }

            public static Error EmailNotConfirm(string email)
            {
                return new Error("UserEmailNotConfirm", $"Email {email} not confirm.");
            }

            public static Error UserIsNotActive()
            {
                return new Error("UserIsNotActive", $"User is not active.");
            }

            public static Error UserIsBan()
            {
                return new Error("UserIsBan", $"User is ban.");
            }

            public static Error PasswordIsWrong()
            {
                return new Error("PasswordIsWrong", $"Password is wrong.");
            }

            public static Error UserAccountIsLocked(string errorMessage)
            {
                return new Error("UserAccountIsLocked", $"User account is locked for {errorMessage}.");
            }

            public static Error UsernameNotFound(string username)
            {
                return new Error("UsernameIsNotRegistered", $"Username {username} is not registered.");
            }

            public static Error MobilePhoneNumberNotFound(string mobilePhoneNumberWithRegionCode)
            {
                return new Error("MobilePhoneNumberIsNotRegistered", $"MobilePhoneNumber {mobilePhoneNumberWithRegionCode} is not registered.");
            }

            public static Error EmailNotFound(string email)
            {
                return new Error("EmailIsNotRegistered", $"Email {email} is not registered.");
            }

            public static Error CurrentPasswordIsWrong()
            {
                return new Error("CurrentPasswordIsWrong", $"Current password is wrong.");
            }
        }

        public abstract class Human
        {
            public static Error ErrorForCreateUser()
            {
                return new Error("ErrorForCreateUser", "Error for create user");
            }

            public static Error NationalIdIsUsed(string nationalId)
            {
                return new Error("NationalIdIsUsed", $"{nationalId} is used.");
            }

            public static Error PassportIdIsUsed(string passportId)
            {
                return new Error("PassportIdIsUsed", $"{passportId} is used.");
            }
        }

        public abstract class Account
        {
            public static Error NameIsUsed(string name)
            {
                return new Error("NameIsUsed", $"{name} is used.");
            }

            public static Error AccountNotFound()
            {
                return new Error("AccountNotFound", $"Account not found.");
            }

            public static Error ParentAccountIdIsEmpty()
            {
                return new Error("ParentAccountIdIsEmpty", "Parent account id is empty.");
            }

            public static Error ParentAccountNotFound()
            {
                return new Error("ParentAccountNotFound", $"Parent account not found.");
            }

            public static Error ParentAccountIsFinal()
            {
                return new Error("ParentAccountIsFinal", $"Parent account is final.");
            }

            public static Error ParentAccountGroupIsEqualToAccountGroup()
            {
                return new Error("ParentAccountGroupIsEqualToAccountGroup", $"An account has to sit at a deeper level than its parent.");
            }

            public static Error AccountTypeDoesNotMatchParent(AccountType requested, AccountType parent)
            {
                return new Error(
                    "AccountTypeDoesNotMatchParent",
                    $"A {requested} account cannot sit under a {parent} account; a child takes its parent's type.");
            }

            /// <summary>
            /// Only a final account carries postings, so only a final account can be budgeted
            /// against or charged: a group account would never accumulate an actual.
            /// </summary>
            public static Error AccountIsNotFinal(string code)
            {
                return new Error("AccountIsNotFinal", $"Account {code} is a group, not a final account, so nothing can be posted to it.");
            }

            public static Error CodeBlockIsFull(AccountType type)
            {
                return new Error("CodeBlockIsFull", $"The code block reserved for {type} accounts is full.");
            }
        }

        public abstract class Planning
        {
            public static Error ForecastIsNotApproved(string code)
            {
                return new Error("ForecastIsNotApproved", $"Forecast {code} is not approved, so nothing should be committed against it.");
            }

            public static Error ItemHasNoUnitOfMeasure(string item)
            {
                return new Error("ItemHasNoUnitOfMeasure", $"{item} has no base unit of measure, so a quantity for it means nothing.");
            }

            public static Error NothingToPlan()
            {
                return new Error("NothingToPlan", "No item in scope has a reorder policy to plan against.");
            }

            public static Error PlanIsNotActionable(string status)
            {
                return new Error("PlanIsNotActionable", $"A run in state {status} is no longer worth acting on.");
            }

            public static Error LineIsNotOnThePlan(Guid lineId)
            {
                return new Error("LineIsNotOnThePlan", $"Line {lineId} does not belong to this plan.");
            }

            public static Error LineIsAlreadyReleased(string referenceType, string referenceId)
            {
                return new Error("LineIsAlreadyReleased", $"That suggestion was already released as {referenceType} {referenceId}.");
            }

            public static Error LineSuggestsNothing(Guid lineId)
            {
                return new Error("LineSuggestsNothing", $"Line {lineId} suggests no action, so there is nothing to release.");
            }
        }

        public abstract class AfterSales
        {
            public static Error ServiceOrderIsClosed(string status)
            {
                return new Error("ServiceOrderIsClosed", $"A service order in state {status} can no longer be worked on.");
            }

            public static Error ServiceOrderCannotBeCompleted(string detail)
            {
                return new Error("ServiceOrderCannotBeCompleted", detail);
            }

            public static Error NoWarehouseToIssuePartsFrom(string number)
            {
                return new Error("NoWarehouseToIssuePartsFrom", $"Service order {number} names no warehouse, so parts cannot be issued against it.");
            }

            public static Error NoStockForPart(string item)
            {
                return new Error("NoStockForPart", $"{item} has no inventory record in that warehouse.");
            }

            public static Error NotEnoughStockForPart(string item, decimal requested, decimal available)
            {
                return new Error("NotEnoughStockForPart", $"{requested} of {item} was fitted and {available} is on hand.");
            }

            public static Error ClaimCannotBeDecided(string detail)
            {
                return new Error("ClaimCannotBeDecided", detail);
            }
        }

        public abstract class Correspondence
        {
            public static Error LetterNumberIsUsed(string number)
            {
                return new Error("LetterNumberIsUsed", $"A letter is already registered under {number}.");
            }

            public static Error LetterCannotBeReferred(string detail)
            {
                return new Error("LetterCannotBeReferred", detail);
            }

            public static Error ReferralCannotBeAnswered(string detail)
            {
                return new Error("ReferralCannotBeAnswered", detail);
            }
        }

        public abstract class Projects
        {
            public static Error TimesheetIsAlreadyApproved(DateOnly from, DateOnly to)
            {
                return new Error("TimesheetIsAlreadyApproved", $"The timesheet for {from:yyyy-MM-dd} to {to:yyyy-MM-dd} is approved and can no longer be changed.");
            }

            public static Error TimesheetCannotBeSubmitted(string detail)
            {
                return new Error("TimesheetCannotBeSubmitted", detail);
            }

            public static Error TimesheetCannotBeDecided(string detail)
            {
                return new Error("TimesheetCannotBeDecided", detail);
            }

            public static Error MilestoneCannotBeAchieved(string detail)
            {
                return new Error("MilestoneCannotBeAchieved", detail);
            }
        }

        public abstract class HumanResources
        {
            public static Error LeaveCannotBeGranted(string detail)
            {
                return new Error("LeaveCannotBeGranted", detail);
            }

            public static Error EmployeeIsTerminated(string name)
            {
                return new Error("EmployeeIsTerminated", $"{name} has been terminated.");
            }

            public static Error EmployeeIsNotActive(string name)
            {
                return new Error("EmployeeIsNotActive", $"{name} is not an active employee.");
            }

            public static Error ContractOverlaps(string name, DateOnly startDate)
            {
                return new Error("ContractOverlaps", $"{name} already has a contract in effect on {startDate:yyyy-MM-dd}.");
            }

            public static Error ExitBeforeEntry(DateOnly date)
            {
                return new Error("ExitBeforeEntry", $"On {date:yyyy-MM-dd} the exit time is earlier than the recorded entry.");
            }

            public static Error LeaveOverlaps(DateOnly from, DateOnly to, string status)
            {
                return new Error("LeaveOverlaps", $"A {status} request already covers {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
            }

            public static Error LeaveRequestIsInvalid(string detail)
            {
                return new Error("LeaveRequestIsInvalid", detail);
            }

            public static Error NoLeaveEntitlement(string name, string leaveType, int year)
            {
                return new Error("NoLeaveEntitlement", $"{name} has no {leaveType} entitlement set for {year}.");
            }

            public static Error NotEnoughLeave(string leaveType, decimal requested, decimal remaining)
            {
                return new Error("NotEnoughLeave", $"{requested} days of {leaveType} were asked for and {remaining} remain.");
            }

            public static Error RequestIsAlreadyDecided(string status)
            {
                return new Error("RequestIsAlreadyDecided", $"This request is {status} and has already been decided.");
            }

            public static Error PayrollAlreadyRun(string name, DateOnly from, DateOnly to)
            {
                return new Error("PayrollAlreadyRun", $"{name} already has a payroll for {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
            }

            public static Error NoEffectiveContract(string name, DateOnly onDate)
            {
                return new Error("NoEffectiveContract", $"{name} has no contract in effect on {onDate:yyyy-MM-dd}.");
            }

            public static Error ContractHasNoSalary(string name)
            {
                return new Error("ContractHasNoSalary", $"{name}'s contract carries no base salary.");
            }

            public static Error PayrollCannotBeConfirmed(string detail)
            {
                return new Error("PayrollCannotBeConfirmed", detail);
            }
        }

        public abstract class Purchasing
        {
            public static Error SupplierCannotBeOrderedFrom(string supplier)
            {
                return new Error("SupplierCannotBeOrderedFrom", $"{supplier} is not in a state that accepts orders.");
            }

            public static Error ItemIsNotPurchasable(string item)
            {
                return new Error("ItemIsNotPurchasable", $"{item} is not marked as purchasable.");
            }

            public static Error NoPriceForItem(string item)
            {
                return new Error("NoPriceForItem", $"{item} has no price on the request and none agreed with this supplier.");
            }

            public static Error OrderCannotBeDecided(string detail)
            {
                return new Error("OrderCannotBeDecided", detail);
            }

            public static Error OrderIsNotReceivable(string status)
            {
                return new Error("OrderIsNotReceivable", $"An order in state {status} cannot take a delivery.");
            }

            public static Error ReceiptCannotBeConfirmed(string detail)
            {
                return new Error("ReceiptCannotBeConfirmed", detail);
            }

            public static Error LineIsNotOnTheOrder(Guid purchaseOrderItemId)
            {
                return new Error("LineIsNotOnTheOrder", $"Line {purchaseOrderItemId} does not belong to this order.");
            }

            public static Error MoreReceivedThanOrdered(string item, decimal received, decimal outstanding)
            {
                return new Error(
                    "MoreReceivedThanOrdered",
                    $"{received} of {item} was received, but only {outstanding} is still outstanding on the order.");
            }

            public static Error NoInventoryRecord(string item)
            {
                return new Error("NoInventoryRecord", $"{item} has no inventory record in that warehouse to receive into.");
            }
        }

        public abstract class Finance
        {
            public static Error NoFinancialYearForDate(DateOnly date)
            {
                return new Error("NoFinancialYearForDate", $"No financial year covers {date:yyyy-MM-dd}.");
            }

            public static Error FinancialYearIsClosed(string title)
            {
                return new Error("FinancialYearIsClosed", $"Financial year {title} no longer accepts postings.");
            }

            public static Error AccountingPeriodIsClosed(string title)
            {
                return new Error("AccountingPeriodIsClosed", $"Period {title} is closed.");
            }

            public static Error AccountDoesNotAcceptPostings(string code)
            {
                return new Error("AccountDoesNotAcceptPostings", $"Account {code} is not a final account, or is locked.");
            }

            public static Error SubsidiaryAccountsAreInvalid(string accountCode, string detail)
            {
                return new Error("SubsidiaryAccountsAreInvalid", $"Account {accountCode}: {detail}");
            }

            public static Error EntryIsNotADraft(string status)
            {
                return new Error("EntryIsNotADraft", $"Only a draft entry can be posted; this one is {status}.");
            }

            public static Error EntryIsOutOfBalance(decimal debtor, decimal creditor)
            {
                return new Error("EntryIsOutOfBalance", $"The entry is out of balance: debits are {debtor} and credits are {creditor}.");
            }

            public static Error EntryCannotBePosted(string detail)
            {
                return new Error("EntryCannotBePosted", detail);
            }

            public static Error OnlyAPostedEntryCanBeReversed(string status)
            {
                return new Error("OnlyAPostedEntryCanBeReversed", $"Only a posted entry can be reversed; this one is {status}.");
            }

            public static Error EntryIsAlreadyReversed()
            {
                return new Error("EntryIsAlreadyReversed", "This entry has already been reversed.");
            }

            public static Error ChequeOperationIsNotAllowed(string detail)
            {
                return new Error("ChequeOperationIsNotAllowed", detail);
            }

            public static Error ChequeNumberIsUsed(string number)
            {
                return new Error("ChequeNumberIsUsed", $"A cheque numbered {number} is already registered.");
            }

            public static Error NothingToDepreciate(DateOnly periodEnd)
            {
                return new Error("NothingToDepreciate", $"No asset is in service and due for depreciation as at {periodEnd:yyyy-MM-dd}.");
            }

            public static Error DepreciationCannotBeCharged(string detail)
            {
                return new Error("DepreciationCannotBeCharged", detail);
            }

            public static Error BudgetCannotBeChanged(string detail)
            {
                return new Error("BudgetCannotBeChanged", detail);
            }

            public static Error BudgetLineNotFound(string account)
            {
                return new Error("BudgetLineNotFound", $"The budget carries no line for {account}.");
            }

            public static Error BudgetWouldBeExceeded(string account, decimal wouldBe, decimal planned)
            {
                return new Error(
                    "BudgetWouldBeExceeded",
                    $"Spending this would put {account} at {wouldBe:N0} against a budget of {planned:N0}.");
            }

            public static Error SettlementCannotBeChanged(string detail)
            {
                return new Error("SettlementCannotBeChanged", detail);
            }

            public static Error OverAllocated(decimal allocated, decimal amount)
            {
                return new Error(
                    "OverAllocated",
                    $"{allocated:N0} is being applied against a settlement of {amount:N0}.");
            }

            public static Error ItemIsNotOpen(string reference)
            {
                return new Error("ItemIsNotOpen", $"{reference} is not open, so nothing can be applied to it.");
            }
        }

        public abstract class Order
        {
            public static Error CustomerCannotBeSoldTo(string customer)
            {
                return new Error("CustomerCannotBeSoldTo", $"{customer} is not in a state that accepts orders.");
            }

            public static Error NoEffectivePriceList(string currencyCode)
            {
                return new Error("NoEffectivePriceList", $"No price list is in effect for {currencyCode} today.");
            }

            public static Error ItemIsNotSellable(string item)
            {
                return new Error("ItemIsNotSellable", $"{item} is not marked as sellable.");
            }

            public static Error ItemHasNoPrice(string item, string priceList)
            {
                return new Error("ItemHasNoPrice", $"{item} has no price on list {priceList} at that quantity.");
            }

            public static Error NumberingSeriesMissing(string key)
            {
                return new Error("NumberingSeriesMissing", $"No numbering series is configured under the key '{key}'.");
            }
        }

        public abstract class Sales
        {
            public static Error InvoiceCannotBeIssued(string detail)
            {
                return new Error("InvoiceCannotBeIssued", detail);
            }

            public static Error CreditLimitWouldBeExceeded(string customer, decimal wouldBe, decimal limit)
            {
                return new Error(
                    "CreditLimitWouldBeExceeded",
                    $"{customer} would owe {wouldBe:N0} against a limit of {limit:N0}.");
            }

            public static Error OnlyASalesInvoiceCanBeCredited(string type)
            {
                return new Error("OnlyASalesInvoiceCanBeCredited", $"An invoice of type {type} is not credited with a credit note.");
            }

            public static Error InvoiceCannotBeCredited(string status)
            {
                return new Error("InvoiceCannotBeCredited", $"An invoice in state {status} cannot be credited.");
            }

            public static Error CreditPredatesTheInvoice(DateOnly issueDate)
            {
                return new Error("CreditPredatesTheInvoice", $"A credit note cannot be dated before the invoice it credits, which was issued on {issueDate:yyyy-MM-dd}.");
            }

            public static Error LineIsNotOnTheInvoice(Guid invoiceItemId)
            {
                return new Error("LineIsNotOnTheInvoice", $"Line {invoiceItemId} does not belong to this invoice.");
            }

            public static Error MoreReturnedThanSold(string item, decimal returned, decimal sold)
            {
                return new Error(
                    "MoreReturnedThanSold",
                    $"{returned:N2} of {item} is being credited but only {sold:N2} was invoiced.");
            }
        }

        public abstract class Support
        {
            public static Error TicketSubjectIsInactive(string subject)
            {
                return new Error("TicketSubjectIsInactive", $"Tickets are no longer accepted under '{subject}'.");
            }

            public static Error TicketCannotBeChanged(string detail)
            {
                return new Error("TicketCannotBeChanged", detail);
            }
        }

        public abstract class Marketing
        {
            public static Error LeadAlreadyExists(string mobileNumber, string existingName)
            {
                return new Error("LeadAlreadyExists", $"{mobileNumber} is already on a lead for {existingName}.");
            }

            public static Error LeadCannotBeChanged(string detail)
            {
                return new Error("LeadCannotBeChanged", detail);
            }

            public static Error LeadIsNotQualified(string status)
            {
                return new Error("LeadIsNotQualified", $"A lead in state {status} cannot be converted; qualify it first.");
            }

            public static Error CampaignIsNotRunning(string code, string status)
            {
                return new Error("CampaignIsNotRunning", $"Campaign {code} is {status}, so a lead cannot be attributed to it.");
            }

            public static Error CampaignCodeIsUsed(string code)
            {
                return new Error("CampaignCodeIsUsed", $"A campaign with code {code} already exists.");
            }

            public static Error CampaignCannotBeChanged(string detail)
            {
                return new Error("CampaignCannotBeChanged", detail);
            }

            public static Error OpportunityCannotBeChanged(string detail)
            {
                return new Error("OpportunityCannotBeChanged", detail);
            }

            public static Error OpportunityNeedsACounterparty()
            {
                return new Error("OpportunityNeedsACounterparty", "An opportunity has to belong to either a customer or a lead.");
            }

            public static Error QuotationCannotBeChanged(string detail)
            {
                return new Error("QuotationCannotBeChanged", detail);
            }

            public static Error QuotationHasExpired(DateTimeOffset expiredOn)
            {
                return new Error("QuotationHasExpired", $"That quotation expired on {expiredOn:yyyy-MM-dd} and can no longer be accepted.");
            }

            public static Error ActivityCannotBeChanged(string detail)
            {
                return new Error("ActivityCannotBeChanged", detail);
            }

            public static Error CommissionAlreadyEarned(string referenceType, string referenceId)
            {
                return new Error("CommissionAlreadyEarned", $"Commission was already recorded on {referenceType} {referenceId}.");
            }

            public static Error CommissionCannotBeChanged(string detail)
            {
                return new Error("CommissionCannotBeChanged", detail);
            }
        }

        public abstract class Pricing
        {
            public static Error PriceListCodeIsUsed(string code)
            {
                return new Error("PriceListCodeIsUsed", $"A price list with code {code} already exists.");
            }

            public static Error PriceListCannotBeChanged(string detail)
            {
                return new Error("PriceListCannotBeChanged", detail);
            }

            public static Error ItemIsAlreadyOnTheList(string item)
            {
                return new Error("ItemIsAlreadyOnTheList", $"{item} already has a price on this list at that quantity.");
            }

            public static Error NoPriceForItem(string item, DateOnly onDate)
            {
                return new Error("NoPriceForItem", $"{item} has no price in effect on {onDate:yyyy-MM-dd}.");
            }
        }

        public abstract class Reporting
        {
            public static Error ReportNameIsUsed(string name)
            {
                return new Error("ReportNameIsUsed", $"A report called '{name}' already exists.");
            }

            public static Error DashboardTitleIsUsed(string title)
            {
                return new Error("DashboardTitleIsUsed", $"A dashboard called '{title}' already exists.");
            }

            public static Error ReportCannotBeChanged(string detail)
            {
                return new Error("ReportCannotBeChanged", detail);
            }

            public static Error CronExpressionIsInvalid(string expression)
            {
                return new Error("CronExpressionIsInvalid", $"'{expression}' is not a five field cron expression.");
            }
        }

        public abstract class Warehouse
        {
            public static Error SameSourceAndDestination(string warehouse)
            {
                return new Error("SameSourceAndDestination", $"A transfer from {warehouse} to itself moves nothing.");
            }

            public static Error TransferCannotBeChanged(string detail)
            {
                return new Error("TransferCannotBeChanged", detail);
            }

            public static Error LineIsNotOnTheTransfer(Guid transferItemId)
            {
                return new Error("LineIsNotOnTheTransfer", $"Line {transferItemId} does not belong to this transfer.");
            }

            public static Error NoInventoryRecord(string item, string warehouse)
            {
                return new Error("NoInventoryRecord", $"{item} has no stock record in {warehouse}, so there is nothing to move.");
            }

            public static Error InsufficientStock(string item, decimal requested, decimal available)
            {
                return new Error(
                    "InsufficientStock",
                    $"{requested:N2} of {item} was requested but only {available:N2} is on hand.");
            }

            public static Error CountCannotBeChanged(string detail)
            {
                return new Error("CountCannotBeChanged", detail);
            }

            public static Error LineIsNotOnTheCount(Guid stockCountItemId)
            {
                return new Error("LineIsNotOnTheCount", $"Line {stockCountItemId} does not belong to this count.");
            }

            /// <summary>
            /// Deliberately the same code the planning run answers with. It is one fact about the
            /// item, and a caller fixing it should not have to learn two names for it.
            /// </summary>
            public static Error ItemHasNoUnitOfMeasure(string item)
            {
                return new Error("ItemHasNoUnitOfMeasure", $"{item} has no base unit of measure, so a quantity for it means nothing.");
            }

            public static Error NothingToCount(string warehouse)
            {
                return new Error("NothingToCount", $"{warehouse} holds no stock records to count.");
            }

            public static Error CountIsAlreadyOpen(string number)
            {
                return new Error("CountIsAlreadyOpen", $"Count {number} is still open for that warehouse; finish it before starting another.");
            }
        }

        public abstract class Production
        {
            public static Error ItemIsNotManufactured(string item)
            {
                return new Error("ItemIsNotManufactured", $"{item} is not marked as manufactured, so it cannot be the output of a works order.");
            }

            public static Error NoBillOfMaterials(string item, DateOnly onDate)
            {
                return new Error("NoBillOfMaterials", $"{item} has no approved bill of materials in effect on {onDate:yyyy-MM-dd}.");
            }

            public static Error OrderCannotBeChanged(string detail)
            {
                return new Error("OrderCannotBeChanged", detail);
            }

            public static Error MaterialCannotBeIssued(string detail)
            {
                return new Error("MaterialCannotBeIssued", detail);
            }

            public static Error ComponentIsNotOnTheBill(string component)
            {
                return new Error("ComponentIsNotOnTheBill", $"{component} is not a component of what this order builds.");
            }

            public static Error MoreIssuedThanRequired(string component, decimal wouldBe, decimal required)
            {
                return new Error(
                    "MoreIssuedThanRequired",
                    $"Issuing this would put {wouldBe:N2} of {component} against an order that needs {required:N2}.");
            }

            public static Error NothingWasProduced()
            {
                return new Error("NothingWasProduced", "An order cannot be completed without recording what came off the line.");
            }

            public static Error QualityCheckCannotBeRecorded(string detail)
            {
                return new Error("QualityCheckCannotBeRecorded", detail);
            }
        }
    }
}
