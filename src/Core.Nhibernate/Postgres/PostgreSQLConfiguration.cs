using FluentNHibernate.Cfg.Db;
using NHibernate.Driver;

namespace IdentityServer3.Contrib.Nhibernate.Postgres
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Minor Code Smell", "S101:Types should be named in PascalCase", 
        Justification = "Matching Heirarcy")]
    public class PostgreSQLConfiguration :
        PersistenceConfiguration<PostgreSQLConfiguration, PostgreSQLConnectionStringBuilder>
    {
        public PostgreSQLConfiguration()
        {
            Driver<NpgsqlDriver>();
        }

        public static PostgreSQLConfiguration PostgresSQL93
            => new PostgreSQLConfiguration().Dialect<PostgresSQL93Dialect>();
    }
}
