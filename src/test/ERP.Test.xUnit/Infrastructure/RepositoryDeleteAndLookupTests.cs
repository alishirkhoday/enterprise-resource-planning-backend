using ERP.Domain.Entities.Common;
using ERP.Domain.Repositories;
using ERP.Infrastructure.MainDatabase.Context;
using ERP.Infrastructure.MainDatabase.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Test.xUnit.Infrastructure;

/// <summary>
/// Covers the two repository behaviours the model depends on: that deleting hides a row rather
/// than destroying it, and that looking a row up by its identifier as text still finds it.
/// </summary>
public sealed class RepositoryDeleteAndLookupTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly IRepository<Currency> _repository;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class CurrencyRepository(MainDbContext context) : Repository<Currency>(context);

    public RepositoryDeleteAndLookupTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase($"repository-tests-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new MainDbContext(options);
        _repository = new CurrencyRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<Currency> GivenACurrency()
    {
        var currency = new Currency("IRR", "Iranian rial");
        await _repository.CreateAsync(currency, Token);
        await _context.SaveChanges(Token);
        return currency;
    }

    [Fact]
    public async Task Deleting_marks_the_row_instead_of_removing_it()
    {
        var currency = await GivenACurrency();

        await _repository.DeleteAsync(currency, Token);
        await _context.SaveChanges(Token);

        // The row survives, so anything that referred to it still resolves.
        var stillThere = await _context.Currencies.IgnoreQueryFilters().SingleAsync(c => c.Id == currency.Id, Token);
        Assert.True(stillThere.IsDeleted);
        Assert.NotNull(stillThere.DeletionDateTime);
    }

    [Fact]
    public async Task A_deleted_row_disappears_from_ordinary_queries()
    {
        var currency = await GivenACurrency();

        await _repository.DeleteAsync(currency, Token);
        await _context.SaveChanges(Token);

        Assert.Empty(await _context.Currencies.ToListAsync(Token));
        Assert.Null(await _repository.GetByIdAsync(currency.Id, Token));
        Assert.False(await _repository.IsExistenceByIdAsync(currency.Id, Token));
    }

    [Fact]
    public async Task A_deleted_row_can_be_restored()
    {
        var currency = await GivenACurrency();
        await _repository.DeleteAsync(currency, Token);
        await _context.SaveChanges(Token);

        var deleted = await _context.Currencies.IgnoreQueryFilters().SingleAsync(c => c.Id == currency.Id, Token);
        deleted.Restore();
        await _context.SaveChanges(Token);

        Assert.NotNull(await _repository.GetByIdAsync(currency.Id, Token));
    }

    [Fact]
    public async Task An_identifier_given_as_text_finds_the_row()
    {
        var currency = await GivenACurrency();

        var found = await _repository.GetByIdAsync(currency.Id.ToString(), Token);

        Assert.NotNull(found);
        Assert.Equal(currency.Id, found.Id);
        Assert.True(await _repository.IsExistenceByIdAsync(currency.Id.ToString(), Token));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Text_that_is_not_an_existing_identifier_matches_nothing(string key)
    {
        await GivenACurrency();

        Assert.Null(await _repository.GetByIdAsync(key, Token));
        Assert.False(await _repository.IsExistenceByIdAsync(key, Token));
    }
}
