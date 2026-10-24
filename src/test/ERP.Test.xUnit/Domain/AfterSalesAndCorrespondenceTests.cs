using ERP.Domain.Entities.Integration;
using ERP.Domain.Entities.Modules.AfterSalesServiceManagement;
using ERP.Domain.Entities.Modules.CorrespondenceManagement;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.InventoryManagement;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// The rules that decide who pays for a visit, whether a delivery has been seen before, and
    /// whether a letter can be filed away while somebody is still holding it.
    /// </summary>
    public sealed class AfterSalesAndCorrespondenceTests
    {
        private static readonly Guid Unit = Guid.NewGuid();

        private static Customer CreateCustomer() => new("C-1", "Sepehr Trading", CustomerType.Organization);

        private static Item CreateItem() => new(new ItemGroup("G-1", "Pumps"), ItemType.Product);

        private static ServiceOrder CreateOrder(ServiceOrderType type) =>
            new("SO-1", CreateCustomer(), type, "Will not start.", "IRR");

        [Fact]
        public void Completing_a_visit_announces_it()
        {
            var order = CreateOrder(ServiceOrderType.Chargeable);
            order.Start();

            order.Complete(new DateOnly(2026, 4, 2), "Replaced the impeller.");

            var completed = Assert.Single(order.DomainEvents.OfType<ServiceOrderCompleted>());
            Assert.Equal(new DateOnly(2026, 4, 2), completed.CompletedOn);
        }

        /// <summary>
        /// The reason the visit is happening decides whether it can ever reach an invoice. Warranty
        /// work is already paid for, and billing it twice is the failure this guards.
        /// </summary>
        [Theory]
        [InlineData(ServiceOrderType.Warranty)]
        [InlineData(ServiceOrderType.Contract)]
        [InlineData(ServiceOrderType.Goodwill)]
        [InlineData(ServiceOrderType.Rework)]
        public void Work_that_is_already_paid_for_cannot_be_invoiced(ServiceOrderType type)
        {
            var order = CreateOrder(type);
            order.Start();
            order.Complete(new DateOnly(2026, 4, 2), "Replaced the impeller.");

            Assert.False(order.IsChargeable);
            Assert.Throws<InvalidOperationException>(() => order.MarkAsInvoiced(Guid.NewGuid()));
        }

        [Fact]
        public void A_chargeable_visit_cannot_be_invoiced_twice()
        {
            var order = CreateOrder(ServiceOrderType.Chargeable);
            order.Start();
            order.Complete(new DateOnly(2026, 4, 2), "Replaced the impeller.");

            order.MarkAsInvoiced(Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(() => order.MarkAsInvoiced(Guid.NewGuid()));
        }

        [Fact]
        public void An_unfinished_visit_cannot_be_invoiced()
        {
            var order = CreateOrder(ServiceOrderType.Chargeable);
            order.Start();

            Assert.Throws<InvalidOperationException>(() => order.MarkAsInvoiced(Guid.NewGuid()));
        }

        /// <summary>
        /// A part fitted under warranty still leaves the van, so it is recorded; it simply carries
        /// nothing to the bill.
        /// </summary>
        [Fact]
        public void A_part_on_a_warranty_visit_is_issued_but_not_charged()
        {
            var order = CreateOrder(ServiceOrderType.Warranty);
            var part = new ServiceOrderPart(order, CreateItem(), 1, 2m, Money.Create(500_000m, "IRR"), Unit);
            order.AddPart(part);

            Assert.False(part.IsChargeable);
            Assert.True(part.LineTotal.IsZero);
            Assert.True(order.PartsTotal().IsZero);
        }

        [Fact]
        public void A_part_on_a_warranty_visit_cannot_be_made_chargeable()
        {
            var order = CreateOrder(ServiceOrderType.Warranty);
            var part = new ServiceOrderPart(order, CreateItem(), 1, 2m, Money.Create(500_000m, "IRR"), Unit);

            order.AddPart(part);
            Assert.Throws<InvalidOperationException>(() => order.MakeServiceOrderPartChargeable(part.Id));
        }

        [Fact]
        public void A_chargeable_visit_totals_parts_labour_and_travel()
        {
            var order = CreateOrder(ServiceOrderType.Chargeable);
            order.AddPart(new ServiceOrderPart(order, CreateItem(), 1, 2m, Money.Create(500_000m, "IRR"), Unit));
            order.RecordLabour(3m, Money.Create(900_000m, "IRR"), Money.Create(100_000m, "IRR"));

            Assert.Equal(1_000_000m, order.PartsTotal().Amount);
            Assert.Equal(2_000_000m, order.GrandTotal().Amount);
        }

        [Fact]
        public void A_fault_cannot_be_reported_before_it_happened()
        {
            Assert.Throws<ArgumentException>(() => new WarrantyClaim(
                "WC-1",
                CreateCustomer(),
                CreateItem(),
                faultOccurredOn: new DateOnly(2026, 4, 10),
                reportedOn: new DateOnly(2026, 4, 1),
                "Seized."));
        }

        [Fact]
        public void An_approved_claim_needs_a_decided_resolution()
        {
            var claim = new WarrantyClaim("WC-2", CreateCustomer(), CreateItem(), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3), "Seized.");

            Assert.Throws<ArgumentException>(() =>
                claim.Approve(new DateOnly(2026, 4, 5), WarrantyClaimResolution.Pending));
        }

        [Fact]
        public void A_decided_claim_cannot_be_decided_again()
        {
            var claim = new WarrantyClaim("WC-3", CreateCustomer(), CreateItem(), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3), "Seized.");
            claim.Approve(new DateOnly(2026, 4, 5), WarrantyClaimResolution.Repair);

            Assert.Throws<InvalidOperationException>(() =>
                claim.Reject(new DateOnly(2026, 4, 6), "Out of cover."));
        }

        /// <summary>
        /// Age is counted to the close, not to today, so a claim settled last year does not keep
        /// growing older in the reports.
        /// </summary>
        [Fact]
        public void A_closed_claim_stops_ageing()
        {
            var claim = new WarrantyClaim("WC-4", CreateCustomer(), CreateItem(), new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 3), "Seized.");
            claim.Approve(new DateOnly(2026, 4, 5), WarrantyClaimResolution.Replace);
            claim.Close(new DateOnly(2026, 4, 13));

            Assert.Equal(10, claim.AgeInDays(new DateOnly(2026, 9, 21)));
        }

        [Fact]
        public void A_contract_covers_a_named_unit_and_a_blanket_item_alike()
        {
            var item = CreateItem();
            var serialNumberId = Guid.NewGuid();
            var contract = new ServiceContract(
                "SC-1",
                "Annual cover",
                CreateCustomer(),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                ServiceCoverageType.Comprehensive,
                Money.Create(50_000_000m, "IRR"));
            contract.Cover(new ServiceContractCoverage(contract, item));
            contract.Activate(new DateOnly(2026, 1, 1));

            Assert.True(contract.Covers(item.Id, serialNumberId, new DateOnly(2026, 6, 1)));
            Assert.False(contract.Covers(Guid.NewGuid(), serialNumberId, new DateOnly(2026, 6, 1)));
            Assert.False(contract.Covers(item.Id, serialNumberId, new DateOnly(2027, 6, 1)));
        }

        [Fact]
        public void A_contract_covering_nothing_cannot_be_activated()
        {
            var contract = new ServiceContract(
                "SC-2",
                "Annual cover",
                CreateCustomer(),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                ServiceCoverageType.LabourOnly,
                Money.Create(10_000_000m, "IRR"));

            Assert.Throws<InvalidOperationException>(() => contract.Activate(new DateOnly(2026, 1, 1)));
        }

        [Fact]
        public void Resolution_cannot_be_promised_sooner_than_the_first_response()
        {
            var contract = new ServiceContract(
                "SC-3",
                "Annual cover",
                CreateCustomer(),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                ServiceCoverageType.Comprehensive,
                Money.Create(10_000_000m, "IRR"));

            Assert.Throws<ArgumentException>(() => contract.SetServiceLevel(responseHours: 8, resolutionHours: 4));
        }

        [Fact]
        public void Included_visits_run_out()
        {
            var item = CreateItem();
            var contract = new ServiceContract(
                "SC-4",
                "Annual cover",
                CreateCustomer(),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                ServiceCoverageType.PreventiveMaintenance,
                Money.Create(10_000_000m, "IRR"));
            contract.SetIncludedVisits(2);
            contract.Cover(new ServiceContractCoverage(contract, item));
            contract.Activate(new DateOnly(2026, 1, 1));

            contract.RecordVisit();
            Assert.True(contract.HasVisitsRemaining);

            contract.RecordVisit();
            Assert.False(contract.HasVisitsRemaining);
        }

        /// <summary>
        /// The whole point of the inbox: a redelivery is recognised, and a message already settled
        /// is not work to do again.
        /// </summary>
        [Fact]
        public void A_processed_message_is_settled()
        {
            var message = new InboxMessage(IntegrationSource.PaymentGateway, "gw-88213", "payment.succeeded", "{}");

            message.MarkAsProcessed("Payment", Guid.NewGuid().ToString());

            Assert.True(message.IsSettled);
            Assert.Equal(InboxMessageStatus.Processed, message.Status);
        }

        [Fact]
        public void A_message_still_being_retried_is_not_settled()
        {
            var message = new InboxMessage(IntegrationSource.TaxAuthority, "ta-1", "filing.result", "{}");

            message.MarkAsFailed("Endpoint refused the acknowledgement.");

            Assert.False(message.IsSettled);
            Assert.Equal(InboxMessageStatus.Received, message.Status);
            Assert.NotNull(message.NextAttemptOn);
        }

        [Fact]
        public void A_message_is_abandoned_rather_than_retried_forever()
        {
            var message = new InboxMessage(IntegrationSource.Bank, "bank-77", "statement.line", "{}");

            for (var attempt = 0; attempt < 5; attempt++)
            {
                message.MarkAsFailed("Still failing.");
            }

            Assert.Equal(InboxMessageStatus.Abandoned, message.Status);
            Assert.Null(message.NextAttemptOn);
            Assert.False(message.IsSettled);
        }

        [Fact]
        public void Referring_a_letter_announces_it_and_moves_the_letter_along()
        {
            var letter = new Letter("1405/1234", LetterDirection.Inbound, "Tax assessment", "Tax Authority", new DateOnly(2026, 9, 10));
            var toUserId = Guid.NewGuid();

            letter.ReferTo(toUserId, Guid.NewGuid(), "Please reply within a week.");

            var referred = Assert.Single(letter.DomainEvents.OfType<LetterReferred>());
            Assert.Equal(toUserId, referred.ToUserId);
            Assert.Equal(LetterStatus.InProgress, letter.Status);
        }

        [Fact]
        public void The_same_person_cannot_hold_the_same_letter_twice()
        {
            var letter = new Letter("1405/1235", LetterDirection.Inbound, "Tax assessment", "Tax Authority", new DateOnly(2026, 9, 10));
            var toUserId = Guid.NewGuid();
            letter.ReferTo(toUserId, Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(() => letter.ReferTo(toUserId, Guid.NewGuid()));
        }

        [Fact]
        public void A_letter_somebody_is_still_holding_cannot_be_closed()
        {
            var letter = new Letter("1405/1236", LetterDirection.Inbound, "Tax assessment", "Tax Authority", new DateOnly(2026, 9, 10));
            var referral = letter.ReferTo(Guid.NewGuid(), Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(letter.Close);

            referral.Letter.RespondLetterReferral(referral.Id, "Answered by letter 1405/1300.");
            letter.Close();

            Assert.Equal(LetterStatus.Closed, letter.Status);
        }

        [Fact]
        public void An_archived_letter_can_no_longer_be_referred()
        {
            var letter = new Letter("1405/1237", LetterDirection.Inbound, "Tax assessment", "Tax Authority", new DateOnly(2026, 9, 10));
            letter.Archive("Cabinet 4, folder 12");

            Assert.Throws<InvalidOperationException>(() => letter.ReferTo(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Equal("1405/1237", letter.Number);
        }

        [Fact]
        public void A_letter_cannot_be_referred_to_the_person_referring_it()
        {
            var letter = new Letter("1405/1238", LetterDirection.Internal, "Budget note", "Finance", new DateOnly(2026, 9, 10));
            var userId = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() => letter.ReferTo(userId, userId));
        }

        [Fact]
        public void A_purchase_contract_gives_the_agreed_price_only_while_it_is_effective()
        {
            var item = CreateItem();
            var contract = new PurchaseContract(
                "PC-1",
                "Fastener supply",
                new Supplier("S-1", "Pars Fasteners", SupplierType.Organization, "sales@pars.example"),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                "IRR");
            contract.AddItem(new PurchaseContractItem(contract, item, Money.Create(12_000m, "IRR"), Unit));
            contract.Activate(new DateOnly(2026, 1, 1));

            Assert.Equal(12_000m, contract.PriceFor(item.Id, new DateOnly(2026, 6, 1))!.Amount);
            Assert.Null(contract.PriceFor(item.Id, new DateOnly(2027, 6, 1)));
        }

        /// <summary>
        /// Renewing starts the volume promise again. Carrying last term's consumption forward would
        /// make the new commitment look met before a single order was placed.
        /// </summary>
        [Fact]
        public void Renewing_a_purchase_contract_starts_the_commitment_again()
        {
            var item = CreateItem();
            var contract = new PurchaseContract(
                "PC-2",
                "Fastener supply",
                new Supplier("S-1", "Pars Fasteners", SupplierType.Organization, "sales@pars.example"),
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                "IRR");
            var line = new PurchaseContractItem(contract, item, Money.Create(12_000m, "IRR"), Unit);
            contract.AddItem(line);
            line.Contract.SetPurchaseContractItemCommittedQuantity(line.Id, 1_000m);
            contract.SetCommittedValue(Money.Create(12_000_000m, "IRR"));
            contract.Activate(new DateOnly(2026, 1, 1));

            contract.RecordConsumption(Money.Create(5_000_000m, "IRR"));
            line.Contract.RecordPurchaseContractItemConsumption(line.Id, 400m);
            Assert.Equal(7_000_000m, contract.RemainingCommitment!.Amount);

            contract.Renew(DateRange.Create(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)));

            Assert.Equal(12_000_000m, contract.RemainingCommitment!.Amount);
            Assert.Equal(0m, line.ConsumedQuantity);
            Assert.False(line.IsCommitmentMet);
        }
    }
}
