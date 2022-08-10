using Autofac;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Moq;
using NHibernate;
using Xunit;

namespace Core.Nhibernate.Tests
{
    public class AutofacExtensionTests
    {
        private readonly Mock<ISession> mockSession = new Mock<ISession>();
        private readonly Mock<IDbProfileConfig> mockProfileConfig = new Mock<IDbProfileConfig>();

        private readonly ContainerBuilder sut;

        public AutofacExtensionTests()
        {
            sut = new ContainerBuilder();
            sut.RegisterInstance(mockSession.Object);
            sut.RegisterInstance(mockProfileConfig.Object);
        }

        [Fact]
        public void UseAuthorizationCodeStore_ResolvesAuthorizationCodeStore()
            => sut.UseAuthorizationCodeStore().ShouldResolve<IAuthorizationCodeStore, AuthorizationCodeStore>();

        [Fact]
        public void UseClientStore_ResolvesClientStore()
            => sut.UseClientStore().ShouldResolve<IClientStore, ClientStore>();

        [Fact]
        public void UseConsentStore_ResolvesConsentStore()
            => sut.UseConsentStore().ShouldResolve<IConsentStore, ConsentStore>();

        [Fact]
        public void UseRefreshTokenStore_ResolvesRefreshTokenStore()
            => sut.UseRefreshTokenStore().ShouldResolve<IRefreshTokenStore, RefreshTokenStore>();


        [Fact]
        public void UseScopeStore_ReturnsScopeStore()
            => sut.UseScopeStore().ShouldResolve<IScopeStore, ScopeStore>();

        [Fact]
        public void UseTokenHandleStore_ResolvesTokenHandleStore()
            => sut.UseTokenHandleStore().ShouldResolve<ITokenHandleStore, TokenHandleStore>();

        [Fact]
        public void UseAllContribDataSources_ResolvesTokenHandleStore()
            => sut.UseAllContribDataSources()
                .ShouldResolve<IAuthorizationCodeStore, AuthorizationCodeStore>().And
                .ShouldResolve<IClientStore, ClientStore>().And
                .ShouldResolve<IConsentStore, ConsentStore>().And
                .ShouldResolve<IRefreshTokenStore, RefreshTokenStore>().And
                .ShouldResolve<IScopeStore, ScopeStore>().And
                .ShouldResolve<ITokenHandleStore, TokenHandleStore>();

        [Fact]
        public void UseDataSourceRepository_ResolvesDataSourceRepository()
            => sut.UseDataSourceRepository().ShouldResolve<IDataSourceRepository, DataSourceRepository>();

        [Fact]
        public void UseNpgsql4ProviderConfig_ResolvesNpgsql4ProviderConfig()
            => sut.UseNpgsql4ProviderConfig().ShouldResolve<IDbProfileConfig, Npgsql4ProviderConfig>();

        [Fact]
        public void UseNpgsql6ProviderConfig_ResolvesNpgsql6ProviderConfig()
            => sut.UseNpgsql6ProviderConfig().ShouldResolve<IDbProfileConfig, Npgsql6ProviderConfig>();
    }

    internal static class TestExtensions
    {
        public static AndConstraint<IContainer> ShouldResolve<TInterface, TClass>(this ContainerBuilder builder)
        {
            var container = builder.Build();

            container.Resolve<TInterface>().Should().BeOfType<TClass>().And.NotBeNull();

            return new AndConstraint<IContainer>(container);
        }

        public static AndConstraint<IContainer> ShouldResolve<TInterface, TClass>(this IContainer container)
        {
            _ = container.Resolve<TInterface>().Should().BeOfType<TClass>().And.NotBeNull();
            return new AndConstraint<IContainer>(container);
        }
    }
}
