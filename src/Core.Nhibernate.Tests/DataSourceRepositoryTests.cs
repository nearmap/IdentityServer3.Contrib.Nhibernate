using Autofac.Core.Registration;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Moq;
using NHibernate;
using System;
using Xunit;

namespace Core.Nhibernate.Tests
{
    public class DataSourceRepositoryTests
    {
        private readonly Mock<ISession> mockSession = new Mock<ISession>();
        private readonly Mock<ISessionFactory> mockSessionFactory = new Mock<ISessionFactory>();
        private readonly Mock<IDbProfileConfig> mockProfileConfig = new Mock<IDbProfileConfig>();

        private readonly DataSourceRepository sut;
        private readonly DataSourceRepository sutWithFactory;

        public DataSourceRepositoryTests()
        {
            sut = new DataSourceRepository(mockSession.Object, mockProfileConfig.Object);
            sutWithFactory = new DataSourceRepository(mockSessionFactory.Object, mockProfileConfig.Object);
            mockSessionFactory.Setup(x => x.OpenSession())
                .Returns(mockSession.Object);
        }

        [Fact]
        public void Get_IAuthorizationCodeStore_ReturnsAuthorizationCodeStore()
            => sut.Get<IAuthorizationCodeStore>().Should().BeOfType<AuthorizationCodeStore>().And.NotBeNull();

        [Fact]
        public void Get_IClientStore_ReturnsClientStore()
            => sut.Get<IClientStore>().Should().BeOfType<ClientStore>().And.NotBeNull();

        [Fact]
        public void Get_IConsentStore_ReturnsConsentStore()
            => sut.Get<IConsentStore>().Should().BeOfType<ConsentStore>().And.NotBeNull();

        [Fact]
        public void Get_IRefreshTokenStore_ReturnsRefreshTokenStore()
            => sut.Get<IRefreshTokenStore>().Should().BeOfType<RefreshTokenStore>().And.NotBeNull();

        [Fact]
        public void Get_IScopeStore_ReturnsScopeStore()
            => sut.Get<IScopeStore>().Should().BeOfType<ScopeStore>().And.NotBeNull();

        [Fact]
        public void Get_ITokenHandleStore_ReturnsTokenHandleStore()
            => sut.Get<ITokenHandleStore>().Should().BeOfType<TokenHandleStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_IAuthorizationCodeStore_ReturnsAuthorizationCodeStore()
            => sutWithFactory.Get<IAuthorizationCodeStore>().Should().BeOfType<AuthorizationCodeStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_IClientStore_ReturnsClientStore()
            => sutWithFactory.Get<IClientStore>().Should().BeOfType<ClientStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_IConsentStore_ReturnsConsentStore()
            => sutWithFactory.Get<IConsentStore>().Should().BeOfType<ConsentStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_IRefreshTokenStore_ReturnsRefreshTokenStore()
            => sutWithFactory.Get<IRefreshTokenStore>().Should().BeOfType<RefreshTokenStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_IScopeStore_ReturnsScopeStore()
            => sutWithFactory.Get<IScopeStore>().Should().BeOfType<ScopeStore>().And.NotBeNull();

        [Fact]
        public void Get_Factory_ITokenHandleStore_ReturnsTokenHandleStore()
            => sutWithFactory.Get<ITokenHandleStore>().Should().BeOfType<TokenHandleStore>().And.NotBeNull();

        [Fact]
        public void Get_TypeNotExist_Successful()
        {
            Action action = () => sut.Get<IStubDataSource>();
            action.Should().Throw<ComponentNotRegisteredException>();

            action = () => sut.Get<StubDataSource>();
            action.Should().Throw<ComponentNotRegisteredException>();
        }

        [Fact]
        public void RegisterType_IStubDataSource_Successful()
        {
            sut.RegisterType<StubDataSource>();

            sut.Get<IStubDataSource>().Should().BeOfType<StubDataSource>().And.NotBeNull();
            sut.Get<StubDataSource>().Should().BeOfType<StubDataSource>().And.NotBeNull();
        }

        [Fact]
        public void RegisterInstance_StubDataSource_Successful()
        {
            sut.RegisterInstance(new StubDataSource());

            sut.Get<IStubDataSource>().Should().BeOfType<StubDataSource>().And.NotBeNull();
            sut.Get<StubDataSource>().Should().BeOfType<StubDataSource>().And.NotBeNull();
        }
    }

    internal interface IStubDataSource
    {
        // Left empty intentionally
    }

    internal class StubDataSource : IStubDataSource
    {
        // Left empty intentionally
    }
}
