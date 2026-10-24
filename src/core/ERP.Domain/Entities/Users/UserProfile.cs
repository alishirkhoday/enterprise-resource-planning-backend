using ERP.Domain.Entities.Users.ValueObjects;

namespace ERP.Domain.Entities.Users;

/// <summary>
/// The parts of an account that are about presentation rather than about signing in. Kept off
/// User so a profile picture cannot be a reason to load an account during authentication.
/// </summary>
public sealed class UserProfile : AggregateRoot
{
    public User User { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public Image? Image { get; private set; }

    private UserProfile() { }

    public UserProfile(User user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        User = user;
        UserId = user.Id;
    }

    public void SetImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = new Image(image.FileName, image.FilePath, image.Alt);
    }
    public void DeleteImage() => Image = null;
}
