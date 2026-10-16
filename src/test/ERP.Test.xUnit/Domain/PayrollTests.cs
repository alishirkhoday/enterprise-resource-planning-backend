using ERP.Domain.Entities.Modules.FinanceManagement;
using ERP.Domain.Entities.Modules.HumanResourcesManagement;
using ERP.Domain.Entities.Users;

namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// A pay slip has to be explainable line by line: gross, deductions and net are all derived from
    /// the items rather than stored as loose numbers that can drift apart.
    /// </summary>
    public sealed class PayrollTests
    {
        private static readonly string Currency = "IRR";

        private static Human CreateEmployee()
        {
            var user = new User("payroll.employee", "hash:hash");
            return new Human(user, "Reza", "Karimi", HumanGender.Male, new DateOnly(1990, 1, 1), maritalStatus: true);
        }

        private static Payroll CreatePayroll(decimal baseSalary = 100_000_000m) =>
            new(CreateEmployee(), new DateOnly(2025, 4, 1), new DateOnly(2025, 4, 31 > 30 ? 30 : 30), Money.Create(baseSalary, Currency));

        private static PayrollItem Earning(Payroll payroll, string code, decimal amount, bool taxable = true, bool insurable = true)
        {
            var item = new PayrollItem(payroll, code, code, SalaryItemNature.Earning, Money.Create(amount, Currency));
            item.SetTaxTreatment(taxable, insurable);
            return item;
        }

        private static PayrollItem Deduction(Payroll payroll, string code, decimal amount) =>
            new(payroll, code, code, SalaryItemNature.Deduction, Money.Create(amount, Currency));

        [Fact]
        public void Net_pay_is_gross_less_deductions()
        {
            var payroll = CreatePayroll();
            payroll.AddItem(Earning(payroll, "BASE", 100_000_000m));
            payroll.AddItem(Earning(payroll, "HOUSING", 9_000_000m));
            payroll.AddItem(Deduction(payroll, "INSURANCE", 7_630_000m));

            Assert.Equal(109_000_000m, payroll.GrossPay.Amount);
            Assert.Equal(7_630_000m, payroll.TotalDeductions.Amount);
            Assert.Equal(101_370_000m, payroll.NetPayable.Amount);
        }

        [Fact]
        public void A_non_taxable_earning_stays_out_of_the_tax_base()
        {
            var payroll = CreatePayroll();
            payroll.AddItem(Earning(payroll, "BASE", 100_000_000m));
            payroll.AddItem(Earning(payroll, "MEALS", 5_000_000m, taxable: false, insurable: false));

            Assert.Equal(105_000_000m, payroll.GrossPay.Amount);
            Assert.Equal(100_000_000m, payroll.TaxableBase.Amount);
            Assert.Equal(100_000_000m, payroll.InsurableBase.Amount);
        }

        [Fact]
        public void The_employer_contribution_is_a_cost_but_not_a_deduction()
        {
            var payroll = CreatePayroll();
            payroll.AddItem(Earning(payroll, "BASE", 100_000_000m));
            payroll.AddItem(new PayrollItem(payroll, "EMP-INS", "Employer insurance",
                SalaryItemNature.EmployerContribution, Money.Create(23_000_000m, Currency)));

            Assert.Equal(100_000_000m, payroll.NetPayable.Amount);
            Assert.Equal(123_000_000m, payroll.TotalEmployerCost.Amount);
        }

        [Fact]
        public void A_payroll_with_no_lines_cannot_be_confirmed()
        {
            var payroll = CreatePayroll();

            Assert.Throws<InvalidOperationException>(payroll.Confirm);
        }

        [Fact]
        public void A_payroll_that_would_pay_a_negative_amount_is_refused()
        {
            var payroll = CreatePayroll();
            payroll.AddItem(Earning(payroll, "BASE", 10_000_000m));
            payroll.AddItem(Deduction(payroll, "LOAN", 12_000_000m));

            Assert.Throws<InvalidOperationException>(payroll.Confirm);
        }

        [Fact]
        public void Confirming_closes_the_slip_and_raises_an_event()
        {
            var payroll = CreatePayroll();
            payroll.AddItem(Earning(payroll, "BASE", 100_000_000m));
            payroll.Confirm();

            Assert.Equal(PayrollStatus.Definite, payroll.Status);
            Assert.Contains(payroll.DomainEvents, e => e is PayrollFinalized);
            Assert.Throws<InvalidOperationException>(() => payroll.AddItem(Earning(payroll, "EXTRA", 1m)));
        }

        [Fact]
        public void A_line_in_another_currency_is_refused()
        {
            var payroll = CreatePayroll();
            var foreign = new PayrollItem(payroll, "USD-BONUS", "Bonus", SalaryItemNature.Earning, Money.Create(100m, "USD"));

            Assert.Throws<InvalidOperationException>(() => payroll.AddItem(foreign));
        }
    }
}
