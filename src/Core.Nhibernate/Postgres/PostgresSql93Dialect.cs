using System.Data;
using NHibernate.Dialect;

namespace IdentityServer3.Contrib.Nhibernate.Postgres
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Minor Code Smell", "S101:Types should be named in PascalCase",
    Justification = "Matching Heirarcy")]
    public class PostgresSQL93Dialect : PostgreSQL82Dialect
    {
        public PostgresSQL93Dialect()
            => RegisterColumnType(DbType.DateTimeOffset, "timestamp with time zone");
    }
}
