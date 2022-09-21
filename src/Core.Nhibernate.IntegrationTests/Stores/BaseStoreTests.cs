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
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using FluentNHibernate.Cfg;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Contrib.Nhibernate.Postgres;
using IdentityServer3.Contrib.Nhibernate.Stores;
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

        protected BaseStoreTests(IMapper mapper)
        {
            Mapper = mapper;
            NhSessionFactory = GetNHibernateSessionFactory();

            _readSession = NhSessionFactory.OpenSession();
            Session = NhSessionFactory.OpenSession();

            ScopeStore = new ScopeStore(Session, mapper);
            ClientStore = new ClientStore(Session, mapper);
        }

        private ISessionFactory GetNHibernateSessionFactory()
        {
            var sessionFactory = Fluently.Configure()
                .Database(Config.DbConfig)
                .Mappings(
                    m => m.AutoMappings.Add(MappingHelper.GetNhibernateServicesMappings(true, true))
                )
                .Mappings(m => m.FluentMappings.Conventions.Add(typeof(TimeStampConvention)))
                .ExposeConfiguration(cfg =>
                {
                    Config.ConfigAction(cfg);
                    BuildSchema(cfg);
                })
                .BuildSessionFactory();

            return sessionFactory;
        }

        private void BuildSchema(Configuration cfg)
        {
            new SchemaUpdate(cfg).Execute(false, true);
        }

        protected async Task ExecuteInTransactionAsync(Func<ISession, Task> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_readSession.Transaction != null && _readSession.Transaction.IsActive)
            {
                await actionToExecute(_readSession);
            }
            else
            {
                using (var tx = _readSession.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        await actionToExecute(_readSession);
                        await tx.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        protected string GetNewGuidString() => Guid.NewGuid().ToString();

        protected static JsonSerializerSettings SerializerSettings
            => new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

        protected string ConvertToJson<T, TEntity>(T value)
        {
            return JsonConvert.SerializeObject(Mapper.Map<TEntity>(value), SerializerSettings);
        }

        protected T ConvertFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }

        protected async Task<ClientModel> SetupClientAsync(string clientId = null)
        {
            var testClient = ObjectCreator.GetClient(clientId);

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(Mapper.Map<ClientEntity>(testClient));
            });

            return testClient;
        }

        protected async Task<IEnumerable<ScopeModel>> SetupScopesAsync(int count)
        {
            var scopes = ObjectCreator.GetScopes(count);

            await ExecuteInTransactionAsync(async session =>
            {
                foreach(var scope in scopes)
                {
                    await session.SaveAsync(Mapper.Map<ScopeEntity>(scope));
                }
            });

            return scopes;
        }
    }
}