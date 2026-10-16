using StackExchange.Redis;

namespace ERP.Application.Common.Interfaces.DbContext
{
    public interface ICacheDbContext
    {
        IDatabase Db { get; }
        string Prefix { get; }

        ICacheDbContext WithPrefix(string prefix);
    }
}
