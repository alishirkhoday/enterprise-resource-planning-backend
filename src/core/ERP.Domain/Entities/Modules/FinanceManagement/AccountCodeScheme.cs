namespace ERP.Domain.Entities.Modules.FinanceManagement;

/// <summary>
/// How an account's code is built from its place in the chart.
/// <para>
/// A top level account takes the block reserved for its type and the next free number in it, so
/// the code says what kind of account it is before anyone reads the name. Below that, a child
/// extends its parent's code with a fixed width sequence, which is what makes a whole branch of
/// the chart answerable with a prefix match.
/// </para>
/// <para>
/// This is a rule about what an account <em>is</em>, not about how one gets created, so it lives
/// in the domain. It spent its first life inside a command handler, where a fourteen case switch
/// and a hand rolled padding loop sat between reading the request and saving the row.
/// </para>
/// </summary>
public static class AccountCodeScheme
{
    /// <summary>
    /// Width of each level below the first. Four digits leaves room for 9,999 children, and a
    /// fixed width is what keeps codes sortable and prefixes unambiguous.
    /// </summary>
    public const int ChildSequenceLength = 4;

    /// <summary>Largest sequence a level can hold before the code would grow and stop lining up.</summary>
    public const int MaximumChildSequence = 9_999;

    /// <summary>
    /// The block each account type starts from. The gaps are deliberate: current assets have forty
    /// numbers before tangible fixed assets begin, so a chart can grow without renumbering.
    /// </summary>
    private static readonly Dictionary<AccountType, int> BlockStarts = new()
    {
        [AccountType.AssetCurrent] = 100,
        [AccountType.AssetNonCurrentTangible] = 140,
        [AccountType.AssetNonCurrentIntangible] = 170,
        [AccountType.LiabilityCurrent] = 200,
        [AccountType.LiabilityNonCurrent] = 250,
        [AccountType.Equity] = 300,
        [AccountType.RevenueOperating] = 400,
        [AccountType.RevenueNonOperating] = 450,
        [AccountType.ExpenseOperating] = 500,
        [AccountType.ExpenseNonOperating] = 550,
        [AccountType.AssetDecrease] = 600,
        [AccountType.LiabilityDecrease] = 700,
        [AccountType.CostPriceProductSold] = 800,
        [AccountType.Other] = 900
    };

    /// <summary>Where a type's block begins, for reports that group by it.</summary>
    public static int BlockStartFor(AccountType type) =>
        BlockStarts.TryGetValue(type, out var start)
            ? start
            : throw new ArgumentOutOfRangeException(nameof(type), type, "No code block is reserved for this account type.");

    /// <summary>
    /// The next code in a type's block. <paramref name="existingCountOfType"/> is how many top
    /// level accounts of that type already exist, so the first one issued is the block start plus
    /// one rather than the block start itself - the round number is left as the heading.
    /// </summary>
    public static string TopLevelCode(AccountType type, int existingCountOfType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(existingCountOfType, nameof(existingCountOfType));

        var start = BlockStartFor(type);
        var next = start + existingCountOfType + 1;

        // Every block is forty or fifty wide, so running past the next one means the chart has
        // outgrown the scheme. Better to say so than to hand out a code that belongs to equity.
        var nextBlockStart = BlockStarts.Values.Where(v => v > start).DefaultIfEmpty(int.MaxValue).Min();
        if (next >= nextBlockStart)
            throw new InvalidOperationException($"The code block reserved for {type} is full; the next code would fall inside another type's block.");

        return next.ToString();
    }

    /// <summary>
    /// A child's code: the parent's, extended by the next sequence padded to a fixed width.
    /// <para>
    /// The padding is computed from the number actually being appended. Deriving it from the
    /// existing count instead - which is what this used to do - produced a five digit segment for
    /// the tenth child, then the hundredth, silently breaking the alignment that makes the whole
    /// scheme work.
    /// </para>
    /// </summary>
    public static string ChildCode(string parentCode, int existingChildCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentCode, nameof(parentCode));
        ArgumentOutOfRangeException.ThrowIfNegative(existingChildCount, nameof(existingChildCount));

        var sequence = existingChildCount + 1;
        if (sequence > MaximumChildSequence)
            throw new InvalidOperationException($"Account {parentCode} already has {MaximumChildSequence} children, which is all this scheme allows.");

        return parentCode + sequence.ToString().PadLeft(ChildSequenceLength, '0');
    }
}
