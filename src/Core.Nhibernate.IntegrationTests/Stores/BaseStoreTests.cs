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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Contrib.Nhibernate.Serialization;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Moq;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;
using Configuration = NHibernate.Cfg.Configuration;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class BaseStoreTests
    {
        protected readonly IMapper Mapper;
        protected ISessionFactory NhSessionFactory;

        protected ISession NhibernateSession { get; }
        private readonly ISession _nhibernateAuxSession;

        protected readonly Mock<IScopeStore> ScopeStoreMock = new Mock<IScopeStore>();
        protected readonly Mock<IClientStore> ClientStoreMock = new Mock<IClientStore>();

        protected BaseStoreTests()
        {
            Mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
            })
                .CreateMapper();

            NhSessionFactory = GetNHibernateSessionFactory();

            _nhibernateAuxSession = NhSessionFactory.OpenSession();
            NhibernateSession = NhSessionFactory.OpenSession();
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
            if (_nhibernateAuxSession.Transaction != null && _nhibernateAuxSession.Transaction.IsActive)
            {
                actionToExecute.Invoke(_nhibernateAuxSession);
            }
            else
            {
                using (var tx = _nhibernateAuxSession.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        actionToExecute.Invoke(_nhibernateAuxSession);
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

        private JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ClaimConverter());
            settings.Converters.Add(new ClaimsPrincipalConverter());
            settings.Converters.Add(new ClientConverter(ClientStoreMock.Object));
            settings.Converters.Add(new ScopeConverter(ScopeStoreMock.Object));
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

        protected virtual void SetupScopeStoreMock()
        {
            ScopeStoreMock.Setup(st => st.FindScopesAsync(It.IsAny<IEnumerable<string>>()))
                .Returns((IEnumerable<string> scopeNames) =>
                {
                    return Task.FromResult(
                        scopeNames.Select(s => new Scope { Name = s, DisplayName = s }));
                });
        }
    }

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

    public class PostgresSQL93Dialect : PostgreSQL82Dialect
    {
        public PostgresSQL93Dialect()
        {
            RegisterColumnType(DbType.DateTimeOffset, "timestamp with time zone");
        }
    }

    /// <summary>
    /// NHibernate loses milliseconds precision when mapping with <see cref="DateTime"/>
    /// Use "NHibernate.Type.TimeStampType" by default when mapping from <see cref="DateTime"/>
    /// Credit: http://stackoverflow.com/a/10085574
    /// </summary>
    public class TimeStampConvention : IPropertyConvention, IPropertyConventionAcceptance
    {
        public void Apply(IPropertyInstance instance)
        {
            instance.CustomType<DateTimeType>();
        }

        public void Accept(IAcceptanceCriteria<IPropertyInspector> criteria)
        {
            criteria.Expect(p =>
                p.Type == typeof(DateTime) ||
                p.Type == typeof(DateTime?));
        }
    }
}