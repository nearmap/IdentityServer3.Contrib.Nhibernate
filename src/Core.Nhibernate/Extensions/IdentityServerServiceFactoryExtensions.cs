﻿using System;
using System.Linq;
using IdentityServer3.Core.Services;
using NHibernate;
using IdentityServer3.Contrib.Nhibernate.Services;
using IdentityServer3.Contrib.Nhibernate.Stores;
using AutoMapper;

namespace IdentityServer3.Core.Configuration
{
    public static class IdentityServerServiceFactoryExtensions
    {
        public static void RegisterNhibernateStores(this IdentityServerServiceFactory factory,
            ISessionFactory nHibernateSessionFactory,
            IMapper mapper,
            bool registerOperationalServices = false, 
            bool registerConfigurationServices = false)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));
            _ = nHibernateSessionFactory ?? throw new ArgumentNullException(nameof(nHibernateSessionFactory));
            _ = mapper ?? throw new ArgumentNullException(nameof(mapper));

            if (registerOperationalServices || registerConfigurationServices)
            {
                RegisterSessionFactory(factory, nHibernateSessionFactory, mapper);
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
            IMapper mapper)
        {
            if (factory.Registrations.All(r => r.DependencyType != typeof(ISessionFactory)))
            {
                factory.Register(new Registration<IMapper>(mapper));
                factory.Register(new Registration<ISessionFactory>(NhibernateSessionFactory));
                factory.Register(new Registration<ISession>(c => c.Resolve<ISessionFactory>().OpenSession())
                {
                    Mode = RegistrationMode.InstancePerHttpRequest
                });
            }
        }
    }
}
