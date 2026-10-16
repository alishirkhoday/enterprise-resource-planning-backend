using ERP.Application.Common.Interfaces.DbContext;
using StackExchange.Redis;

namespace ERP.Infrastructure.CacheDatabase.Context
{
    public sealed class CacheDbContext : ICacheDbContext
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly string _prefix;

        public CacheDbContext(IConnectionMultiplexer connection, string prefix = "")
        {
            _connection = connection;
            _prefix = prefix;
        }

        public IDatabase Db => _connection.GetDatabase();
        public string Prefix => _prefix;

        public ICacheDbContext WithPrefix(string prefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix, nameof(prefix));
            return new CacheDbContext(_connection, "ERP:" + _prefix + prefix);
        }
    }
}
