using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using AutoMapper;
using FluentNHibernate.Cfg;
using IdentityServer3.Contrib.Nhibernate;
using IdentityServer3.Contrib.Nhibernate.Postgres;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Models;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Client = IdentityServer3.Core.Models.Client;
using Configuration = NHibernate.Cfg.Configuration;
using Scope = IdentityServer3.Core.Models.Scope;
using Id3Postgres = IdentityServer3.Contrib.Nhibernate.Postgres;

namespace WebHost.Config
{
    static class Factory
    {
        public static IdentityServerServiceFactory Configure(IMapper mapper, ILogger logger)
        {
            var nhSessionFactory = GetNHibernateSessionFactory();
            var nhSession = nhSessionFactory.OpenSession();
            var tokenCleanUpSession = nhSessionFactory.OpenSession();

            var cleanup = new TokenCleanup(tokenCleanUpSession, logger, 60);
            cleanup.Start();

            // these two calls just pre-populate the test DB from the in-memory config
            ConfigureClients(Clients.Get(), nhSession, mapper);
            ConfigureScopes(Scopes.Get(), nhSession, mapper);

            var factory = new IdentityServerServiceFactory();

            factory.RegisterNhibernateStores(new NhibernateServiceOptions(nhSessionFactory)
            {
                RegisterOperationalServices = true,
                RegisterConfigurationServices = true
            });

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
                    SchemaMetadataUpdater.QuoteTableAndColumns(cfg);
                    BuildSchema(cfg);
                })
                .BuildSessionFactory();

            return sessionFactory;
        }

        private static void BuildSchema(Configuration cfg)
        {
            new SchemaUpdate(cfg).Execute(false, true);
        }

        public static void ConfigureClients(ICollection<Client> clients, ISession nhSession, IMapper mapper)
        {
            using (var tx = nhSession.BeginTransaction())
            {
                var clientsInDb = nhSession.Query<IdentityServer3.Contrib.Nhibernate.Entities.Client>();

                if (clientsInDb.Any()) return;

                var toSave = clients.Select(c => c.ToEntity(mapper)).ToList();

                foreach (var client in toSave)
                {
                    nhSession.Save(client);
                }

                tx.Commit();
            }
        }

        public static void ConfigureScopes(ICollection<Scope> scopes, ISession nhSession, IMapper mapper)
        {
            using (var tx = nhSession.BeginTransaction())
            {
                var scopesInDb = nhSession.Query<IdentityServer3.Contrib.Nhibernate.Entities.Scope>();

                if (scopesInDb.Any()) return;

                var toSave = scopes.Select(s => s.ToEntity(mapper)).ToList();

                foreach (var scope in toSave)
                {
                    nhSession.Save(scope);
                }

                tx.Commit();
            }
        }
    }
}
