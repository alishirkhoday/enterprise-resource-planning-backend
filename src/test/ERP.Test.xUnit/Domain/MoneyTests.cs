namespace ERP.Test.xUnit.Domain
{
    public sealed class MoneyTests
    {
        [Fact]
        public void Adding_two_amounts_in_the_same_currency_sums_them()
        {
            var result = Money.Create(1_000m, "IRR").Add(Money.Create(500m, "IRR"));

            Assert.Equal(1_500m, result.Amount);
            Assert.Equal("IRR", result.CurrencyCode);
        }

        [Fact]
        public void Adding_two_different_currencies_is_refused()
        {
            var rial = Money.Create(1_000m, "IRR");
            var dollar = Money.Create(10m, "USD");

            Assert.Throws<InvalidOperationException>(() => rial.Add(dollar));
        }

        [Fact]
        public void Currency_code_is_normalized_to_upper_case()
        {
            Assert.Equal("USD", Money.Create(1m, "usd").CurrencyCode);
        }

        [Fact]
        public void Percent_returns_the_portion_not_the_remainder()
        {
            var vat = Money.Create(1_000m, "IRR").Percent(Percentage.Create(9m));

            Assert.Equal(90m, vat.Amount);
        }

        [Fact]
        public void Two_amounts_with_the_same_value_and_currency_are_equal()
        {
            Assert.Equal(Money.Create(250m, "IRR"), Money.Create(250m, "IRR"));
            Assert.NotEqual(Money.Create(250m, "IRR"), Money.Create(250m, "USD"));
        }

        [Fact]
        public void Dividing_by_zero_is_refused()
        {
            Assert.Throws<DivideByZeroException>(() => Money.Create(100m, "IRR").Divide(0m));
        }
    }
}
