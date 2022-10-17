using Autofac;
using AutoMapper;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Services;
using Moq;
using NHibernate;
using Xunit;

namespace Core.Nhibernate.Tests
{
    public class AutofacExtensionTests
    {
        private readonly ContainerBuilder sut;

        public AutofacExtensionTests()
        {
            sut = new ContainerBuilder();
            sut.RegisterInstance(new Mock<ISession>().Object);
            sut.RegisterInstance(new Mock<IMapper>().Object);
        }

        [Fact]
        public void UseAuthorizationCodeStore_ResolvesAuthorizationCodeStore()
            => sut.RegisterAuthorizationCodeStore().ShouldResolve<IAuthorizationCodeStore, AuthorizationCodeStore>();

        [Fact]
        public void UseClientStore_ResolvesClientStore()
            => sut.RegisterClientStore().ShouldResolve<IClientStore, ClientStore>();

        [Fact]
        public void UseConsentStore_ResolvesConsentStore()
            => sut.RegisterConsentStore().ShouldResolve<IConsentStore, ConsentStore>();

        [Fact]
        public void UseRefreshTokenStore_ResolvesRefreshTokenStore()
            => sut.RegisterRefreshTokenStore().ShouldResolve<IRefreshTokenStore, RefreshTokenStore>();


        [Fact]
        public void UseScopeStore_ReturnsScopeStore()
            => sut.RegisterScopeStore().ShouldResolve<IScopeStore, ScopeStore>();

        [Fact]
        public void UseTokenHandleStore_ResolvesTokenHandleStore()
            => sut.RegisterTokenHandleStore().ShouldResolve<ITokenHandleStore, TokenHandleStore>();

        [Fact]
        public void UseAllContribDataSources_ResolvesAllStores()
            => sut
                .UseAllContribDataSources()
                .ShouldResolve<IAuthorizationCodeStore, AuthorizationCodeStore>().And
                .ShouldResolve<IClientStore, ClientStore>().And
                .ShouldResolve<IConsentStore, ConsentStore>().And
                .ShouldResolve<IRefreshTokenStore, RefreshTokenStore>().And
                .ShouldResolve<IScopeStore, ScopeStore>().And
                .ShouldResolve<ITokenHandleStore, TokenHandleStore>();
    }

    internal static class TestExtensions
    {
        public static AndConstraint<IContainer> ShouldResolve<TInterface, TClass>(this ContainerBuilder builder)
            => builder.Build().ShouldResolve<TInterface, TClass>();

        public static AndConstraint<IContainer> ShouldResolve<TInterface, TClass>(this IContainer container)
        {
            _ = container.Resolve<TInterface>().Should().BeOfType<TClass>().And.NotBeNull();
            return new AndConstraint<IContainer>(container);
        }
    }
}
