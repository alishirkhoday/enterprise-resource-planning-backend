using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace ERP.Infrastructure.MainDatabase.Context
{
    /// <summary>
    /// Teaches Entity Framework how to store the domain value objects.
    /// <para>
    /// The domain expresses amounts, percentages, quantities, periods and identifiers as value
    /// objects rather than loose primitives. Each one maps to a set of columns on its owner table,
    /// which is what an owned type is. Doing this by convention keeps the several hundred entities
    /// from each needing a configuration class that would say the same thing every time.
    /// </para>
    /// <para>
    /// Anything already configured by hand in an <c>IEntityTypeConfiguration</c> is left alone, so a
    /// specific mapping always wins over this default.
    /// </para>
    /// </summary>
    internal static class ValueObjectConvention
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            // The model is modified while walking it, so the list is taken up front.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.IsOwned())
                    continue;

                // A value object that Entity Framework mistook for an entity of its own. Calling
                // Entity() on it here would confirm that mistake, and it has no key to give.
                if (typeof(ValueObject).IsAssignableFrom(entityType.ClrType))
                    continue;

                var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                ConfigureOwner(entityBuilder.Metadata, entityType.ClrType, propertyName => entityBuilder.Ignore(propertyName), (type, name) => entityBuilder.OwnsOne(type, name));
            }
        }

        private static void ConfigureOwner(
            IMutableEntityType metadata,
            Type clrType,
            Action<string> ignore,
            Func<Type, string, object> ownsOne)
        {
            foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyType = property.PropertyType;

                // A read-only property with no backing field is derived from other state, so there
                // is nothing to store and nothing to read back into.
                if (IsComputed(clrType, property))
                {
                    if (metadata.FindProperty(property.Name) is null && metadata.FindNavigation(property.Name) is null)
                        ignore(property.Name);
                    continue;
                }

                if (!typeof(ValueObject).IsAssignableFrom(propertyType))
                    continue;

                // A single column with its own converter was configured by hand; leave it be.
                if (metadata.FindProperty(property.Name) is not null)
                    continue;

                // A navigation already exists. If it points at an owned type the mapping is done,
                // but if Entity Framework guessed the value object was an entity of its own, it has
                // to be turned into an owned type here or the model will demand a key for it.
                var navigation = metadata.FindNavigation(property.Name);
                if (navigation is not null && navigation.TargetEntityType.IsOwned())
                    continue;

                var ownedBuilder = ownsOne(propertyType, property.Name);
                ConfigureNestedValueObjects(ownedBuilder, propertyType);
            }
        }

        /// <summary>
        /// Handles a value object that itself holds another one, such as a shipping address that
        /// wraps a postal address.
        /// </summary>
        private static void ConfigureNestedValueObjects(object ownedBuilder, Type ownedClrType)
        {
            var builderType = ownedBuilder.GetType();

            var ownsOneMethod = builderType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "OwnsOne"
                                     && !m.IsGenericMethod
                                     && m.GetParameters().Length == 2
                                     && m.GetParameters()[0].ParameterType == typeof(Type)
                                     && m.GetParameters()[1].ParameterType == typeof(string));

            if (ownsOneMethod is null)
                return;

            foreach (var property in ownedClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsComputed(ownedClrType, property))
                    continue;

                if (!typeof(ValueObject).IsAssignableFrom(property.PropertyType))
                    continue;

                var nestedBuilder = ownsOneMethod.Invoke(ownedBuilder, [property.PropertyType, property.Name]);
                if (nestedBuilder is not null)
                    ConfigureNestedValueObjects(nestedBuilder, property.PropertyType);
            }
        }

        /// <summary>
        /// A property is treated as derived when it cannot be written and keeps no field of its own.
        /// </summary>
        private static bool IsComputed(Type declaringType, PropertyInfo property)
        {
            if (property.SetMethod is not null)
                return false;

            var backingField = declaringType.GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            return backingField is null;
        }
    }
}
