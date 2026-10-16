namespace ERP.Test.xUnit.Domain
{
    /// <summary>
    /// The whole point of normalizing is that the same person typing their number in different ways
    /// lands on one stored value, so a lookup finds them however they registered.
    /// </summary>
    public sealed class NormalizationTests
    {
        [Theory]
        [InlineData("98", "09123456789")]
        [InlineData("98", "9123456789")]
        [InlineData("+98", "0912 345 6789")]
        [InlineData("0098", "0912-345-6789")]
        [InlineData(" 98 ", " 09123456789 ")]
        public void Every_way_of_writing_one_number_normalizes_to_the_same_value(string regionCode, string nationalNumber)
        {
            Assert.Equal("989123456789", Normalization.NormalizeMobilePhoneNumber(regionCode, nationalNumber));
        }

        [Fact]
        public void The_trunk_prefix_is_dropped_rather_than_concatenated()
        {
            // The old behaviour simply joined the two strings and produced 9809123456789.
            var result = Normalization.NormalizeMobilePhoneNumber("98", "09123456789");

            Assert.DoesNotContain("980", result);
            Assert.Equal(12, result.Length);
        }

        [Theory]
        [InlineData("98", "abc")]
        [InlineData("++", "09123456789")]
        public void A_value_with_no_digits_is_refused(string regionCode, string nationalNumber)
        {
            Assert.Throws<ArgumentException>(() => Normalization.NormalizeMobilePhoneNumber(regionCode, nationalNumber));
        }

        [Fact]
        public void A_username_is_lowered_and_trimmed()
        {
            Assert.Equal("arash_esfandiari", Normalization.NormalizeUsername("  ArAsh_EsfanDiaRi "));
        }

        [Fact]
        public void An_email_is_lowered_and_trimmed()
        {
            Assert.Equal("arash@email.com", Normalization.NormalizeEmail(" ArAsh@EmAil.cOm "));
        }
    }
}
