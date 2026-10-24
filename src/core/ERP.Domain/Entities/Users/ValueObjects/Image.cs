namespace ERP.Domain.Entities.Users.ValueObjects;

/// <summary>
/// A stored picture and what is needed to serve it back.
/// </summary>
public sealed class Image : ValueObject
{
    public string FileName { get; private set; } = default!;
    public string FilePath { get; private set; } = default!;
    public string Alt { get; private set; } = default!;

    private Image()
    {
    }

    public Image(string fileName, string filePath, string alt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(alt, nameof(alt));
        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        Alt = alt.Trim();
    }

    public static Image Create(string fileName, string filePath, string alt)
    {
        return new Image(fileName, filePath, alt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return FilePath;
        yield return Alt;
    }
}
