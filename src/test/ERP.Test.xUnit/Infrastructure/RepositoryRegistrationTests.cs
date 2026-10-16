using System.Reflection;
using ERP.Domain.Repositories;
using ERP.Infrastructure;
using ERP.Infrastructure.MainDatabase.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Test.xUnit.Infrastructure
{
    /// <summary>
    /// Repositories are wired up by hand, so nothing stops a new one from being written and never
    /// registered. The failure would only show up as a resolution error at run time, on whichever
    /// endpoint happens to need it first. These tests move that to build time.
    /// </summary>
    public sealed class RepositoryRegistrationTests
    {
        private static readonly Assembly DomainAssembly = typeof(IRepository<>).Assembly;
        private static readonly Assembly InfrastructureAssembly = typeof(ConfigureServices).Assembly;

        public static TheoryData<Type> RepositoryContracts()
        {
            var data = new TheoryData<Type>();
            foreach (var contract in DomainAssembly.GetTypes()
                         .Where(t => t.IsInterface
                                     && t.Name.EndsWith("Repository", StringComparison.Ordinal)
                                     && !t.IsGenericTypeDefinition)
                         .OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                data.Add(contract);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(RepositoryContracts))]
        public void Every_repository_contract_has_exactly_one_implementation(Type contract)
        {
            var implementations = InfrastructureAssembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && contract.IsAssignableFrom(t))
                .ToArray();

            Assert.True(
                implementations.Length == 1,
                $"{contract.Name} has {implementations.Length} implementations: {string.Join(", ", implementations.Select(t => t.Name))}.");
        }

        [Theory]
        [MemberData(nameof(RepositoryContracts))]
        public void Every_repository_contract_is_registered_as_scoped(Type contract)
        {
            var services = BuildServiceCollection();

            var descriptor = services.SingleOrDefault(d => d.ServiceType == contract);

            Assert.True(descriptor is not null, $"{contract.Name} is not registered in AddInfrastructureServices.");
            Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
        }

        /// <summary>
        /// A repository holding the context for longer than the request that opened it would leak
        /// tracked entities between requests, so none of them may be a singleton.
        /// </summary>
        [Fact]
        public void No_repository_is_registered_as_a_singleton()
        {
            var offenders = BuildServiceCollection()
                .Where(d => d.Lifetime == ServiceLifetime.Singleton
                            && d.ServiceType.Name.EndsWith("Repository", StringComparison.Ordinal))
                .Select(d => d.ServiceType.Name)
                .ToArray();

            Assert.True(offenders.Length == 0, $"Registered as singletons: {string.Join(", ", offenders)}.");
        }

        /// <summary>
        /// Every implementation derives from the shared base, which is what gives soft delete,
        /// paging and the no-tracking read path a single definition.
        /// </summary>
        [Fact]
        public void Every_repository_derives_from_the_shared_base()
        {
            var offenders = InfrastructureAssembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false }
                            && t.Name.EndsWith("Repository", StringComparison.Ordinal)
                            && !DerivesFromRepositoryBase(t))
                .Select(t => t.Name)
                .ToArray();

            Assert.True(offenders.Length == 0, $"Not derived from Repository<T>: {string.Join(", ", offenders)}.");
        }

        private static bool DerivesFromRepositoryBase(Type type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Repository<>))
                    return true;
            }

            return false;
        }

        private static IServiceCollection BuildServiceCollection()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MainDatabase"] = "Server=localhost;Database=RegistrationCheck;Trusted_Connection=True;",
                    ["ConnectionStrings:CacheDatabase"] = "localhost:6379"
                })
                .Build();

            return new ServiceCollection().AddInfrastructureServices(configuration);
        }
    }
}
