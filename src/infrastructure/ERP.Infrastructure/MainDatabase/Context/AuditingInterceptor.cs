using ERP.Application.Common.Interfaces.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Infrastructure.MainDatabase.Context;

/// <summary>
/// Stamps who created and who last changed each row, on the way to the database.
/// <para>
/// It used to be the handler's job, via a <c>Touch()</c> call before saving. Exactly one of the
/// eight handlers remembered it, and <c>SetCreatedBy</c> was never called anywhere at all - which
/// is the predictable result of asking every author to remember the same line. Here nothing has to
/// be remembered: a row cannot be written without passing through this.
/// </para>
/// </summary>
public sealed class AuditingInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var actor = currentUser.UserName;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(actor);
                    break;

                // A soft delete arrives here as a modification, and Delete() has already recorded
                // who did it and rotated the stamp. Touching it again would overwrite the deleting
                // user with the same name and pointlessly rotate the concurrency stamp twice.
                case EntityState.Modified when !entry.Entity.IsDeleted || !WasJustDeleted(entry):
                    entry.Entity.Touch(actor);
                    break;
            }
        }
    }

    private static bool WasJustDeleted(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry)
    {
        var isDeleted = entry.Property(nameof(BaseEntity.IsDeleted));
        return isDeleted.IsModified && isDeleted.CurrentValue is true;
    }
}
