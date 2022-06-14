/*MIT License
*
*Copyright (c) 2016 Ricardo Santos
*Copyright (c) 2022 Jason F. Bridgman
*
*Permission is hereby granted, free of charge, to any person obtaining a copy
*of this software and associated documentation files (the "Software"), to deal
*in the Software without restriction, including without limitation the rights
*to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
*copies of the Software, and to permit persons to whom the Software is
*furnished to do so, subject to the following conditions:
*
*The above copyright notice and this permission notice shall be included in all
*copies or substantial portions of the Software.
*
*THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
*SOFTWARE.
*/



using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;
using AutoMapper;
using FluentNHibernate.Cfg;
using Core.Nhibernate.IntegrationTests.Serialization;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Contrib.Nhibernate.Postgres;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Configuration = NHibernate.Cfg.Configuration;
using ClientEntity = IdentityServer3.Contrib.Nhibernate.Entities.Client;
using ClientModel = IdentityServer3.Core.Models.Client;
using ScopeEntity = IdentityServer3.Contrib.Nhibernate.Entities.Scope;
using ScopeModel = IdentityServer3.Core.Models.Scope;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class BaseStoreTests
    {
        protected readonly IMapper Mapper;
        protected ISessionFactory NhSessionFactory;

        protected ISession Session { get; }
        private readonly ISession _readSession;

        protected readonly IScopeStore ScopeStore;
        protected readonly IClientStore ClientStore;

        protected BaseStoreTests()
        {
            Mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
            })
                .CreateMapper();

            NhSessionFactory = GetNHibernateSessionFactory();

            _readSession = NhSessionFactory.OpenSession();
            Session = NhSessionFactory.OpenSession();

            ScopeStore = new ScopeStore(Session, Mapper);
            ClientStore = new ClientStore(Session, Mapper);
        }

        protected void RemoveTrailingComma(StringBuilder jsonBuilder)
        {
            if (jsonBuilder[jsonBuilder.Length - 1] == ',')
            {
                jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
            }
        }

        private ISessionFactory GetNHibernateSessionFactory()
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

        private void BuildSchema(Configuration cfg)
        {
            new SchemaUpdate(cfg).Execute(false, true);
        }

        protected void ExecuteInTransaction(Action<ISession> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_readSession.Transaction != null && _readSession.Transaction.IsActive)
            {
                actionToExecute.Invoke(_readSession);
            }
            else
            {
                using (var tx = _readSession.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        actionToExecute.Invoke(_readSession);
                        tx.Commit();
                    }
                    catch (Exception)
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        protected string GetNewGuidString() => Guid.NewGuid().ToString();

        private JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ClaimConverter());
            settings.Converters.Add(new ClaimsPrincipalConverter());
            settings.Converters.Add(new ClientConverter(ClientStore));
            settings.Converters.Add(new ScopeConverter(ScopeStore));
            return settings;
        }

        protected string ConvertToJson<T>(T value)
        {
            return JsonConvert.SerializeObject(value, GetJsonSerializerSettings());
        }

        protected T ConvertFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, GetJsonSerializerSettings());
        }

        protected ClientModel SetupClient(string clientId = null)
        {
            var testClient = ObjectCreator.GetClient(clientId);

            ExecuteInTransaction(session =>
            {
                session.Save(Mapper.Map<ClientEntity>(testClient));
            });

            return testClient;
        }

        protected IEnumerable<ScopeModel> SetupScopes(int count)
        {
            var scopes = ObjectCreator.GetScopes(count);

            ExecuteInTransaction(session =>
            {
                foreach(var scope in scopes)
                {
                    session.Save(Mapper.Map<ScopeEntity>(scope));
                }
            });

            return scopes;
        }
    }
}