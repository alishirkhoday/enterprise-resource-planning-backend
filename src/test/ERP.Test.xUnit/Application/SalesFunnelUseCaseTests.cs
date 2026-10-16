using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.AdvanceOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CloseOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.ConvertLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.CreateOpportunity;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.QualifyLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Commands.RegisterLead;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.DTOs;
using ERP.Application.UseCases.Modules.SaleAndMarketingManagement.Queries.GetSalesPipeline;
using ERP.Domain.Entities.Common;
using ERP.Domain.Entities.Modules.CustomersRelationshipManagement;
using ERP.Domain.Entities.Modules.SaleAndMarketingManagement;
using ERP.Domain.Repositories.Common;
using ERP.Domain.Repositories.Modules.CustomersRelationshipManagement;
using ERP.Domain.Repositories.Modules.SaleAndMarketingManagement;
using Moq;

namespace ERP.Test.xUnit.Application
{
    /// <summary>
    /// The funnel, from a name on a form to a deal that is won or lost.
    /// <para>
    /// The cases worth having are the ones where the chain breaks silently: a duplicate lead two
    /// people both call, a deal belonging to nobody that still counts toward the forecast, and a
    /// win that never reaches the target it was supposed to hit.
    /// </para>
    /// </summary>
    public sealed class SalesFunnelUseCaseTests
    {
        private static readonly DateOnly Today = new(2026, 5, 10);
        private static readonly DateTimeOffset Now = new(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);

        private static CancellationToken Token => TestContext.Current.CancellationToken;

        private sealed class FixedClock(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private static Lead CreateLead(LeadStatus status = LeadStatus.New, Guid? campaignId = null)
        {
            var lead = new Lead("Reza Mohammadi", "+989121234567", LeadSource.Website);
            lead.Update("Reza Mohammadi", "Sepehr Trading", "Buyer");

            if (campaignId is not null)
                lead.AttributeToCampaign(campaignId.Value);

            if (status is LeadStatus.Qualified or LeadStatus.Converted)
                lead.Qualify();

            if (status == LeadStatus.Disqualified)
                lead.Disqualify("No budget.");

            return lead;
        }

        private static Campaign CreateCampaign(bool running = true)
        {
            var campaign = new Campaign("CMP-1", "Spring push", "Email",
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                Money.Create(50_000_000m, "IRR"));

            if (running)
                campaign.Launch();

            return campaign;
        }

        private sealed class RegisterHarness
        {
            public Mock<ILeadRepository> Leads { get; } = new();
            public Mock<ICampaignRepository> Campaigns { get; } = new();
            public Lead? Created { get; private set; }

            public RegisterHarness(Lead? duplicate, Campaign? campaign = null)
            {
                Leads.Setup(r => r.GetByMobileNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(duplicate);

                Campaigns.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

                Leads.Setup(r => r.CreateAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()))
                    .Callback<Lead, CancellationToken>((lead, _) => Created = lead)
                    .Returns(Task.CompletedTask);
            }

            public RegisterLeadCommandHandler Build() => new(Leads.Object, Campaigns.Object);
        }

        private static RegisterLeadCommand RegisterCommand(Guid? campaignId = null) => new()
        {
            FullName = "Reza Mohammadi",
            MobilePhoneNumberWithRegionCode = "+989121234567",
            Source = LeadSource.Website,
            CampaignId = campaignId
        };

        [Fact]
        public async Task A_second_form_from_the_same_number_is_refused()
        {
            var harness = new RegisterHarness(CreateLead());

            var result = await harness.Build().Handle(RegisterCommand(), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LeadAlreadyExists", result.Error.Code);
            Assert.Null(harness.Created);
        }

        [Theory]
        [InlineData(LeadStatus.Disqualified)]
        [InlineData(LeadStatus.Converted)]
        public async Task A_closed_lead_does_not_block_the_same_person_coming_back(LeadStatus closedAs)
        {
            // History, not a duplicate. Somebody who was turned away a year ago and returns is a
            // new opportunity, and refusing them would lose it.
            var previous = CreateLead(closedAs);

            if (closedAs == LeadStatus.Converted)
                previous.Convert(Guid.NewGuid(), Today.AddYears(-1));

            var harness = new RegisterHarness(previous);

            var result = await harness.Build().Handle(RegisterCommand(), Token);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task A_campaign_that_has_stopped_takes_no_more_attribution()
        {
            // Letting a lead in after a campaign is paused flatters something that had already
            // stopped spending.
            var campaign = CreateCampaign(running: false);
            var harness = new RegisterHarness(null, campaign);

            var result = await harness.Build().Handle(RegisterCommand(campaign.Id), Token);

            Assert.True(result.IsFailure);
            Assert.Equal("CampaignIsNotRunning", result.Error.Code);
        }

        [Fact]
        public async Task Disqualifying_needs_a_reason_to_be_recorded()
        {
            var lead = CreateLead();

            var leads = new Mock<ILeadRepository>();
            leads.Setup(r => r.GetWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(lead);

            var result = await new QualifyLeadCommandHandler(leads.Object).Handle(
                new QualifyLeadCommand { LeadId = lead.Id, IsQualified = false, Reason = "No budget this year." },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(LeadStatus.Disqualified, result.Value.Status);
            Assert.Equal("No budget this year.", result.Value.DisqualificationReason);
        }

        private sealed class ConvertHarness
        {
            public Mock<ILeadRepository> Leads { get; } = new();
            public Mock<ICustomerRepository> Customers { get; } = new();
            public Mock<IOpportunityRepository> Opportunities { get; } = new();
            public Mock<INumberingSeriesRepository> Series { get; } = new();
            public Customer? CreatedCustomer { get; private set; }
            public Opportunity? CreatedOpportunity { get; private set; }

            public ConvertHarness(Lead lead, Customer? existingCustomer = null)
            {
                Leads.Setup(r => r.GetWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(lead);
                Customers.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingCustomer);

                Series.Setup(r => r.GetForUpdateAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new NumberingSeries("Customer", paddingLength: 5));

                Customers.Setup(r => r.CreateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                    .Callback<Customer, CancellationToken>((customer, _) => CreatedCustomer = customer)
                    .Returns(Task.CompletedTask);

                Opportunities.Setup(r => r.CreateAsync(It.IsAny<Opportunity>(), It.IsAny<CancellationToken>()))
                    .Callback<Opportunity, CancellationToken>((opportunity, _) => CreatedOpportunity = opportunity)
                    .Returns(Task.CompletedTask);
            }

            public ConvertLeadCommandHandler Build() => new(
                Leads.Object, Customers.Object, Opportunities.Object, Series.Object);
        }

        [Fact]
        public async Task Converting_a_lead_creates_the_customer_and_closes_the_lead()
        {
            var lead = CreateLead(LeadStatus.Qualified);
            var harness = new ConvertHarness(lead);

            var result = await harness.Build().Handle(
                new ConvertLeadCommand { LeadId = lead.Id, ConvertedOn = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.NotNull(harness.CreatedCustomer);
            Assert.Equal(LeadStatus.Converted, lead.Status);
            Assert.Equal(harness.CreatedCustomer!.Id, lead.ConvertedCustomerId);
        }

        [Fact]
        public async Task An_unqualified_lead_cannot_be_converted()
        {
            var lead = CreateLead();
            var harness = new ConvertHarness(lead);

            var result = await harness.Build().Handle(
                new ConvertLeadCommand { LeadId = lead.Id, ConvertedOn = Today },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("LeadIsNotQualified", result.Error.Code);
            Assert.Null(harness.CreatedCustomer);
        }

        [Fact]
        public async Task A_deal_opened_on_conversion_inherits_the_campaign()
        {
            // The whole reason the chain exists: without it nobody can say afterwards which
            // campaign paid for which revenue.
            var campaignId = Guid.NewGuid();
            var lead = CreateLead(LeadStatus.Qualified, campaignId);
            var harness = new ConvertHarness(lead);

            var result = await harness.Build().Handle(
                new ConvertLeadCommand
                {
                    LeadId = lead.Id,
                    ConvertedOn = Today,
                    Opportunity = new OpenOpportunityDto
                    {
                        Title = "First order",
                        EstimatedValue = 500_000_000m,
                        CurrencyCode = "IRR",
                        ExpectedCloseDate = Today.AddMonths(2)
                    }
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.NotNull(harness.CreatedOpportunity);
            Assert.Equal(campaignId, harness.CreatedOpportunity!.CampaignId);
            Assert.Equal(harness.CreatedOpportunity.Id, result.Value.OpportunityId);
        }

        [Fact]
        public async Task A_deal_belonging_to_nobody_is_refused()
        {
            var handler = new CreateOpportunityCommandHandler(
                new Mock<IOpportunityRepository>().Object,
                new Mock<ICustomerRepository>().Object,
                new Mock<ILeadRepository>().Object);

            var result = await handler.Handle(
                new CreateOpportunityCommand
                {
                    Title = "Ghost deal",
                    EstimatedValue = 1_000_000m,
                    CurrencyCode = "IRR",
                    ExpectedCloseDate = Today.AddMonths(1)
                },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OpportunityNeedsACounterparty", result.Error.Code);
        }

        private static Opportunity CreateOpportunity(decimal value = 100_000_000m, Guid? assignedTo = null, Guid? campaignId = null)
        {
            var opportunity = new Opportunity("Bracket supply", Money.Create(value, "IRR"), Today.AddMonths(1));

            if (assignedTo is not null)
                opportunity.AssignTo(assignedTo.Value);

            if (campaignId is not null)
                opportunity.AttributeToCampaign(campaignId.Value);

            return opportunity;
        }

        [Fact]
        public async Task Moving_a_stage_resets_the_probability_to_the_agreed_one()
        {
            // Left to their own numbers every deal sits at ninety percent until the day it is
            // lost, which makes one salesperson's forecast incomparable with another's.
            var opportunity = CreateOpportunity();
            opportunity.SetProbability(Percentage.Create(90m));

            var opportunities = new Mock<IOpportunityRepository>();
            opportunities.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(opportunity);

            var result = await new AdvanceOpportunityCommandHandler(opportunities.Object).Handle(
                new AdvanceOpportunityCommand { OpportunityId = opportunity.Id, Stage = OpportunityStage.Qualification },
                Token);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(90m, result.Value.Probability);
        }

        [Fact]
        public async Task A_deal_cannot_be_won_by_moving_its_stage()
        {
            var opportunity = CreateOpportunity();

            var opportunities = new Mock<IOpportunityRepository>();
            opportunities.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(opportunity);

            var result = await new AdvanceOpportunityCommandHandler(opportunities.Object).Handle(
                new AdvanceOpportunityCommand { OpportunityId = opportunity.Id, Stage = OpportunityStage.ClosedWon },
                Token);

            Assert.True(result.IsFailure);
            Assert.Equal("OpportunityCannotBeChanged", result.Error.Code);
            Assert.True(opportunity.IsOpen);
        }

        private sealed class CloseHarness
        {
            public Mock<IOpportunityRepository> Opportunities { get; } = new();
            public Mock<ISalesTargetRepository> Targets { get; } = new();
            public Mock<ICampaignRepository> Campaigns { get; } = new();

            public CloseHarness(Opportunity opportunity, SalesTarget? target = null, Campaign? campaign = null)
            {
                Opportunities.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(opportunity);

                Targets.Setup(r => r.GetForScopeAsync(
                        It.IsAny<SalesTargetScope>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(target is null ? [] : [target]);

                Campaigns.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(campaign);
            }

            public CloseOpportunityCommandHandler Build() => new(Opportunities.Object, Targets.Object, Campaigns.Object);
        }

        private static SalesTarget CreateTarget(Guid salesRepId, decimal amount = 1_000_000_000m, string currencyCode = "IRR") =>
            new("2026 quota", SalesTargetScope.SalesRepresentative, salesRepId,
                DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
                Money.Create(amount, currencyCode));

        [Fact]
        public async Task Winning_a_deal_credits_the_salespersons_target()
        {
            var salesRepId = Guid.NewGuid();
            var opportunity = CreateOpportunity(value: 250_000_000m, assignedTo: salesRepId);
            var target = CreateTarget(salesRepId);

            var harness = new CloseHarness(opportunity, target);

            var result = await harness.Build().Handle(
                new CloseOpportunityCommand { OpportunityId = opportunity.Id, IsWon = true, ClosedOn = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(250_000_000m, target.AchievedAmount.Amount);
            Assert.Equal(1, target.AchievedCount);
        }

        [Fact]
        public async Task A_target_in_another_currency_is_left_alone_rather_than_converted()
        {
            // Silently converting at a rate nobody supplied is how a scoreboard stops being
            // checkable against anything.
            var salesRepId = Guid.NewGuid();
            var opportunity = CreateOpportunity(value: 250_000_000m, assignedTo: salesRepId);
            var target = CreateTarget(salesRepId, 100_000m, "EUR");

            var harness = new CloseHarness(opportunity, target);

            var result = await harness.Build().Handle(
                new CloseOpportunityCommand { OpportunityId = opportunity.Id, IsWon = true, ClosedOn = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, target.AchievedAmount.Amount);
        }

        [Fact]
        public async Task Winning_a_deal_credits_the_campaign_that_brought_it_in()
        {
            var campaign = CreateCampaign();
            var opportunity = CreateOpportunity(value: 250_000_000m, campaignId: campaign.Id);

            var harness = new CloseHarness(opportunity, campaign: campaign);

            var result = await harness.Build().Handle(
                new CloseOpportunityCommand { OpportunityId = opportunity.Id, IsWon = true, ClosedOn = Today },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(250_000_000m, campaign.AttributedRevenue.Amount);
        }

        [Fact]
        public async Task Losing_a_deal_credits_nothing_and_keeps_the_reason()
        {
            var salesRepId = Guid.NewGuid();
            var opportunity = CreateOpportunity(assignedTo: salesRepId);
            var target = CreateTarget(salesRepId);

            var harness = new CloseHarness(opportunity, target);

            var result = await harness.Build().Handle(
                new CloseOpportunityCommand
                {
                    OpportunityId = opportunity.Id,
                    IsWon = false,
                    ClosedOn = Today,
                    LostReason = "Price."
                },
                Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(OpportunityStage.ClosedLost, result.Value.Stage);
            Assert.Equal("Price.", result.Value.LostReason);
            Assert.Equal(0m, target.AchievedAmount.Amount);
        }

        [Fact]
        public async Task The_pipeline_reports_every_stage_even_the_empty_ones()
        {
            var opportunities = new Mock<IOpportunityRepository>();
            opportunities.Setup(r => r.GetOpenAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([CreateOpportunity(value: 100_000_000m)]);

            var result = await new GetSalesPipelineQueryHandler(opportunities.Object, new FixedClock(Now))
                .Handle(new GetSalesPipelineQuery { CurrencyCode = "IRR" }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.Value.Stages.Count);
            Assert.Equal(1, result.Value.OpenCount);
        }

        [Fact]
        public async Task The_weighted_value_is_reported_next_to_the_raw_estimate()
        {
            // The gap between them is how much of the pipeline is hope, and a board shown only
            // the estimate will plan around money nobody has won.
            var opportunity = CreateOpportunity(value: 100_000_000m);
            opportunity.SetProbability(Percentage.Create(25m));

            var opportunities = new Mock<IOpportunityRepository>();
            opportunities.Setup(r => r.GetOpenAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([opportunity]);

            var result = await new GetSalesPipelineQueryHandler(opportunities.Object, new FixedClock(Now))
                .Handle(new GetSalesPipelineQuery { CurrencyCode = "IRR" }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(100_000_000m, result.Value.TotalEstimatedValue);
            Assert.Equal(25_000_000m, result.Value.TotalWeightedValue);
        }

        [Fact]
        public async Task Deals_in_another_currency_are_left_out_of_the_totals()
        {
            var inRials = CreateOpportunity(value: 100_000_000m);
            var inEuros = new Opportunity("Export deal", Money.Create(50_000m, "EUR"), Today.AddMonths(1));

            var opportunities = new Mock<IOpportunityRepository>();
            opportunities.Setup(r => r.GetOpenAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([inRials, inEuros]);

            var result = await new GetSalesPipelineQueryHandler(opportunities.Object, new FixedClock(Now))
                .Handle(new GetSalesPipelineQuery { CurrencyCode = "IRR" }, Token);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.OpenCount);
            Assert.Equal(100_000_000m, result.Value.TotalEstimatedValue);
        }
    }
}
