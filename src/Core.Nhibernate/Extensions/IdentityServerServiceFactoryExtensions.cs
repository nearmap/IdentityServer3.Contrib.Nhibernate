using System;
using System.Linq;
using IdentityServer3.Core.Services;
using NHibernate;
using IdentityServer3.Contrib.Nhibernate.Services;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;

namespace IdentityServer3.Core.Configuration
{
    public static class IdentityServerServiceFactoryExtensions
    {

        public static void RegisterNhibernateStores(this IdentityServerServiceFactory factory,
            ISessionFactory nHibernateSessionFactory,
            IDbProfileConfig dbProfileConfig, 
            bool registerOperationalServices = false, 
            bool registerConfigurationServices = false)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));
            _ = nHibernateSessionFactory ?? throw new ArgumentNullException(nameof(nHibernateSessionFactory));
            _ = dbProfileConfig ?? throw new ArgumentNullException(nameof(dbProfileConfig));

            if (registerOperationalServices || registerConfigurationServices)
            {
                RegisterSessionFactory(factory, nHibernateSessionFactory, dbProfileConfig);
            }

            if (registerOperationalServices)
            {
                RegisterOperationalServices(factory);
            }

            if (registerConfigurationServices)
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

        private static void RegisterSessionFactory(
            IdentityServerServiceFactory factory, 
            ISessionFactory NhibernateSessionFactory,
            IDbProfileConfig dbProfileConfig)
        {
            if (factory.Registrations.All(r => r.DependencyType != typeof(ISessionFactory)))
            {
                factory.Register(new Registration<IDbProfileConfig>(dbProfileConfig));
                factory.Register(new Registration<ISessionFactory>(NhibernateSessionFactory));
                factory.Register(new Registration<ISession>(c => c.Resolve<ISessionFactory>().OpenSession())
                {
                    Mode = RegistrationMode.InstancePerHttpRequest
                });
            }
        }
    }
}
