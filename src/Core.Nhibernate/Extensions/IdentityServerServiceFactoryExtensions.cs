using System;
using System.Linq;
using IdentityServer3.Core.Services;
using NHibernate;
using IdentityServer3.Contrib.Nhibernate;
using IdentityServer3.Contrib.Nhibernate.Services;
using IdentityServer3.Contrib.Nhibernate.Stores;

namespace IdentityServer3.Core.Configuration
{
    public static class IdentityServerServiceFactoryExtensions
    {

        public static void RegisterNhibernateStores(this IdentityServerServiceFactory factory,
            NhibernateServiceOptions serviceOptions)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));
            _ = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));

            if (serviceOptions.RegisterOperationalServices || serviceOptions.RegisterConfigurationServices)
            {
                RegisterSessionFactory(factory, serviceOptions);
            }

            if (serviceOptions.RegisterOperationalServices)
            {
                RegisterOperationalServices(factory);
            }

            if (serviceOptions.RegisterConfigurationServices)
            {
                RegisterConfigurationServices(factory);
            }
        }
        private static void RegisterOperationalServices(IdentityServerServiceFactory factory)
        {
            factory.AuthorizationCodeStore = new Registration<IAuthorizationCodeStore, AuthorizationCodeStore>();
            factory.TokenHandleStore = new Registration<ITokenHandleStore, TokenHandleStore>();
            factory.ConsentStore = new Registration<IConsentStore, ConsentStore>();
            factory.RefreshTokenStore = new Registration<IRefreshTokenStore, RefreshTokenStore>();
        }

        private static void RegisterConfigurationServices(IdentityServerServiceFactory factory)
        {
            factory.ClientStore = new Registration<IClientStore, ClientStore>();
            factory.CorsPolicyService = new Registration<ICorsPolicyService, ClientConfigurationCorsPolicyService>();
            factory.ScopeStore = new Registration<IScopeStore, ScopeStore>();
        }

        private static void RegisterSessionFactory(IdentityServerServiceFactory factory, NhibernateServiceOptions serviceOptions)
        {
            if (factory.Registrations.All(r => r.DependencyType != typeof(ISessionFactory)))
            {
                factory.Register(
                    new Registration<ISessionFactory>(serviceOptions.NhibernateSessionFactory));
                factory.Register(new Registration<ISession>(c => c.Resolve<ISessionFactory>().OpenSession())
                {
                    Mode = RegistrationMode.InstancePerHttpRequest
                });
            }
        }
    }
}
