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
        ArgumentException.ThrowIfNullOrEmpty(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrEmpty(alt, nameof(alt));
        FileName = fileName;
        FilePath = filePath;
        Alt = alt;
    }

    public static Image Create(string fileName, string filePath, string alt)
    {
        return new Image(fileName, filePath, alt);
    }

    public void Delete()
    {
        FileName = string.Empty;
        FilePath = string.Empty;
        Alt = string.Empty;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return FilePath;
        yield return Alt;
    }
}
