namespace ERP.Domain.Primitives;

/// <summary>
/// A value with no identity of its own: two of them are the same thing when they hold the same
/// data, which is what separates a value object from an entity.
/// <para>
/// Money, a national identifier or a date range have no meaningful "which one" - a hundred rials
/// is a hundred rials. Deriving from this is what stops two equal amounts comparing as different
/// because they happen to be different objects.
/// </para>
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// The fields that make this value what it is. Equality and the hash code are built from
    /// them, so anything left out here is a field two "equal" values are allowed to differ on.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (left is null ^ right is null)
            return false;

        return left is null || left.Equals(right);
    }

    public static bool NotEqualOperator(ValueObject? left, ValueObject? right) => !EqualOperator(left, right);
    public static bool operator ==(ValueObject? left, ValueObject? right) => EqualOperator(left, right);
    public static bool operator !=(ValueObject? left, ValueObject? right) => NotEqualOperator(left, right);

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode() => GetEqualityComponents().Aggregate(0, (hash, obj) => HashCode.Combine(hash, obj));
    public override string ToString() => string.Join(", ", GetEqualityComponents());
}
