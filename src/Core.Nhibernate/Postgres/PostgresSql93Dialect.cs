using System.Data;
using NHibernate.Dialect;

namespace IdentityServer3.Contrib.Nhibernate.Postgres
{
    public class PostgresSQL93Dialect : PostgreSQL82Dialect
    {
        public PostgresSQL93Dialect()
        {
            RegisterColumnType(DbType.DateTimeOffset, "timestamp with time zone");
        }
    }
}
