namespace ERP.Domain.Entities.Files;

/// <summary>
/// A file held against some record. The hash lets identical uploads share storage and lets a
/// file be proved unchanged.
/// </summary>
public sealed class Attachment : BaseEntity, IBranchScoped
{
    public string ReferenceId { get; private set; } = default!;
    public string ReferenceType { get; private set; } = default!;

    public string FileName { get; private set; } = default!;
    public string FilePath { get; private set; } = default!;

    /// <summary>The name the file had when it was uploaded, which is what the user recognises.</summary>
    public string? OriginalFileName { get; private set; }

    public string? ContentType { get; private set; }
    public long SizeInBytes { get; private set; }

    /// <summary>SHA-256 of the contents, used to detect duplicates and tampering.</summary>
    public string? ContentHash { get; private set; }

    public AttachmentCategory Category { get; private set; }
    public string UploadedBy { get; private set; } = default!;
    public DateTimeOffset UploadDateTime { get; private set; }

    /// <summary>Marks a file that must not be served to anyone without an explicit check.</summary>
    public bool IsConfidential { get; private set; }

    public Guid? BranchId { get; private set; }
    public string? Description { get; private set; }

    private Attachment() { }

    public Attachment(string referenceType, string referenceId, string fileName, string filePath, string uploadedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceType, nameof(referenceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId, nameof(referenceId));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedBy, nameof(uploadedBy));

        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId.Trim();
        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        UploadedBy = uploadedBy.Trim();
        UploadDateTime = DateTimeOffset.UtcNow;
        Category = AttachmentCategory.Other;
    }

    public void SetFileMetadata(string? originalFileName, string? contentType, long sizeInBytes, string? contentHash)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes, nameof(sizeInBytes));
        OriginalFileName = string.IsNullOrWhiteSpace(originalFileName) ? null : originalFileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        SizeInBytes = sizeInBytes;
        ContentHash = string.IsNullOrWhiteSpace(contentHash) ? null : contentHash.Trim();
    }

    public void SetCategory(AttachmentCategory category) => Category = category;

    public void MarkAsConfidential() => IsConfidential = true;

    public void AssignToBranch(Guid branchId) => BranchId = branchId == Guid.Empty ? null : branchId;

    public void SetDescription(string? description) => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public string Extension => Path.GetExtension(FileName);

    public bool IsImage => ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
}
