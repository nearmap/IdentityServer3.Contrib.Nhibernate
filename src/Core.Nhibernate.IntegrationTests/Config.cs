using System;
using System.Configuration;
using FluentNHibernate.Cfg.Db;
using IdentityServer3.Contrib.Nhibernate.Postgres;
using NHibernate.Tool.hbm2ddl;

using PostgreSQLConfiguration = IdentityServer3.Contrib.Nhibernate.Postgres.PostgreSQLConfiguration;
using NHibConfiguration = NHibernate.Cfg.Configuration;

namespace Core.Nhibernate.IntegrationTests
{
    internal struct Config
    {
        /// <summary>
        /// Use this config for Postgres
        /// </summary>
        #region Postgres

        public static IPersistenceConfigurer DbConfig =
            PostgreSQLConfiguration.PostgresSQL93.ConnectionString(
                ConfigurationManager.ConnectionStrings["IdSvr3Config"].ConnectionString)
            .ShowSql()
            .FormatSql()
            .AdoNetBatchSize(20);

        public static Action<NHibConfiguration> ConfigAction = (cfg) =>
        {
            SchemaMetadataUpdater.QuoteTableAndColumns(cfg, new PostgresSQL93Dialect());
        };

        #endregion Postgres

        #region MSSQL

        /// <summary>
        /// Use this config for Microsoft SQL Server
        /// </summary>
        //public static IPersistenceConfigurer DbConfig =
        //    MsSqlConfiguration.MsSql2012.ConnectionString(
        //        ConfigurationManager.ConnectionStrings["IdSvr3Config"].ConnectionString)
        //    .ShowSql()
        //    .FormatSql()
        //    .AdoNetBatchSize(20);

        //public static Action<NHibConfiguration> ConfigAction = (cfg) =>
        //{

        //};

        #endregion MSSQL
    }
}
