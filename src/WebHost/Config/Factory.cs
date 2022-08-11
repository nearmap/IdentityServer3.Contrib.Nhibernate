using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using FluentNHibernate.Cfg;
using IdentityServer3.Contrib.Nhibernate;
using IdentityServer3.Contrib.Nhibernate.Postgres;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Models;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Client = IdentityServer3.Core.Models.Client;
using Configuration = NHibernate.Cfg.Configuration;
using Scope = IdentityServer3.Core.Models.Scope;
using Entities = IdentityServer3.Contrib.Nhibernate.Entities;

namespace WebHost.Config
{
    static class Factory
    {
        private static readonly IDbProfileConfig dbConfig = new Npgsql6ProviderConfig();

        public static IdentityServerServiceFactory Configure(ILogger logger)
        {
            var nhSessionFactory = GetNHibernateSessionFactory();
            var nhSession = nhSessionFactory.OpenSession();
            var tokenCleanUpSession = nhSessionFactory.OpenSession();

            var cleanup = new TokenCleanup(tokenCleanUpSession, logger, 60);
            cleanup.Start();

            // these two calls just pre-populate the test DB from the in-memory config
            ConfigureClients(Clients.Get(), nhSession);
            ConfigureScopes(Scopes.Get(), nhSession);

            var factory = new IdentityServerServiceFactory();

            factory.RegisterNhibernateStores(nhSessionFactory, dbConfig, true, true);

            factory.UseInMemoryUsers(Users.Get().ToList());

            return factory;
        }

        private static ISessionFactory GetNHibernateSessionFactory()
        {
            var connString = ConfigurationManager.ConnectionStrings["IdSvr3Config"];

            var sessionFactory = Fluently.Configure()
                .Database(PostgreSQLConfiguration.PostgresSQL93.ConnectionString(connString.ToString())
                    //.Database(MsSqlConfiguration.MsSql2012.ConnectionString(connString.ToString())
                    .ShowSql()
                    .FormatSql()
                    .AdoNetBatchSize(20)
                )
                .Mappings(
                    m => m.AutoMappings.Add(MappingHelper.GetNhibernateServicesMappings(true, true))
                )
                .Mappings(m => m.FluentMappings.Conventions.Add(typeof(TimeStampConvention)))
                .ExposeConfiguration(cfg =>
                {
                    SchemaMetadataUpdater.QuoteTableAndColumns(cfg, new PostgresSQL93Dialect());
                    BuildSchema(cfg);
                })
                .BuildSessionFactory();

            return sessionFactory;
        }

        private static void BuildSchema(Configuration cfg)
        {
            new SchemaUpdate(cfg).Execute(false, true);    
        }

        public static void ConfigureClients(ICollection<Client> clients, ISession nhSession)
        {
            using (var tx = nhSession.BeginTransaction())
            {
                var clientsInDb = nhSession.Query<Entities.Client>();

                if (clientsInDb.Any()) return;

                var clientStore = new ClientStore(nhSession, dbConfig);

                foreach (var client in clients)
                {
                    clientStore.Save(client);
                }

                tx.Commit();
            }
        }

        public static void ConfigureScopes(ICollection<Scope> scopes, ISession nhSession)
        {
            using (var tx = nhSession.BeginTransaction())
            {
                var scopesInDb = nhSession.Query<Entities.Scope>();

                if (scopesInDb.Any()) return;

                var scopeStore = new ScopeStore(nhSession, dbConfig);

                foreach (var scope in scopes)
                {
                    scopeStore.Save(scope);
                }

                tx.Commit();
            }
        }
    }
}
