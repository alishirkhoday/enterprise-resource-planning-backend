using ERP.Domain.Primitives.Pagination;

namespace ERP.Test.xUnit.Domain
{
    public sealed class PaginationTests
    {
        [Fact]
        public void A_query_that_sets_nothing_still_asks_for_a_real_page()
        {
            // A request that omits the paging parameters never calls the setters. Before, that left
            // the backing fields at zero and asked the database for no rows on page zero.
            var query = new SearchablePaginationQueryDto();

            Assert.Equal(SearchablePaginationQueryDto.DefaultPageSize, query.PageSize);
            Assert.Equal(SearchablePaginationQueryDto.FirstPageNumber, query.PageNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(SearchablePaginationQueryDto.MaximumPageSize + 1)]
        public void An_unusable_page_size_falls_back_to_the_default(int pageSize)
        {
            var query = new SearchablePaginationQueryDto { PageSize = pageSize };

            Assert.Equal(SearchablePaginationQueryDto.DefaultPageSize, query.PageSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void A_page_number_below_one_falls_back_to_the_first_page(int pageNumber)
        {
            var query = new SearchablePaginationQueryDto { PageNumber = pageNumber };

            Assert.Equal(SearchablePaginationQueryDto.FirstPageNumber, query.PageNumber);
        }

        [Fact]
        public void A_usable_value_is_kept_as_given()
        {
            var query = new SearchablePaginationQueryDto { PageSize = 25, PageNumber = 4 };

            Assert.Equal(25, query.PageSize);
            Assert.Equal(4, query.PageNumber);
        }
    }
}
