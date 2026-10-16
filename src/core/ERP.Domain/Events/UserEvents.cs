namespace ERP.Domain.Events;

/// <summary>An account was created.</summary>
/// <param name="UserId">The new account.</param>
/// <param name="NormalizedUsername">The username in the single form the system compares on.</param>
public sealed record UserRegistered(Guid UserId, string NormalizedUsername) : DomainEvent;

/// <summary>
/// Somebody signed in. The address and device travel with it because a sign-in from an unfamiliar
/// one is the only sign a stolen password gives before it is used.
/// </summary>
/// <param name="UserId">Who signed in.</param>
/// <param name="InternetProtocolAddress">Where from, when the request carried one.</param>
/// <param name="DeviceName">What they signed in on, when the client reported it.</param>
public sealed record UserSignedIn(Guid UserId, string? InternetProtocolAddress, string? DeviceName) : DomainEvent;

/// <summary>
/// Too many failed attempts locked the account. Carries the count as well as the release time,
/// because a lockout after three tries and one after thirty are different events.
/// </summary>
/// <param name="UserId">The account that was locked.</param>
/// <param name="LockoutEnd">When it unlocks by itself.</param>
/// <param name="FailedAttempts">How many attempts caused it.</param>
public sealed record UserLockedOut(Guid UserId, DateTimeOffset LockoutEnd, int FailedAttempts) : DomainEvent;

/// <summary>
/// An account was banned by a person, as against locked out by the system. It does not expire.
/// </summary>
/// <param name="UserId">The account that was banned.</param>
/// <param name="Reason">Why, when one was given.</param>
public sealed record UserBanned(Guid UserId, string? Reason) : DomainEvent;

/// <summary>
/// The password changed. Carries no password and no hash: what any listener needs to know is
/// that existing sessions and tokens should stop being trusted.
/// </summary>
/// <param name="UserId">Whose password changed.</param>
public sealed record UserPasswordChanged(Guid UserId) : DomainEvent;

/// <summary>
/// Somebody was given a role. The branch is part of the fact because the same role can be held
/// in one branch and not another.
/// </summary>
/// <param name="UserId">Who was given it.</param>
/// <param name="RoleId">Which role.</param>
/// <param name="BranchId">Where it applies, or null for the whole organisation.</param>
public sealed record UserRoleAssigned(Guid UserId, Guid RoleId, Guid? BranchId) : DomainEvent;

/// <summary>A role was taken away, scoped the same way it was granted.</summary>
/// <param name="UserId">Who lost it.</param>
/// <param name="RoleId">Which role.</param>
/// <param name="BranchId">Where it applied, or null for the whole organisation.</param>
public sealed record UserRoleRevoked(Guid UserId, Guid RoleId, Guid? BranchId) : DomainEvent;
