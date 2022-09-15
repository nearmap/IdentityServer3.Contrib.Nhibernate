using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.NhibernateConfig;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Services;

namespace Autofac
{
    public static class AutofacExtensions
    {
        /// <summary>
        /// Register AutoMapper instance using given profiles
        /// </summary>
        /// <param name="builder">The AutoFac container builder</param>
        /// <param name="profileConfigs">AutoMapper profiles to apply to the mapper</param>
        /// <returns>A <see cref="ContainerBuilder"/></returns>
        public static ContainerBuilder RegisterAutoMapper(this ContainerBuilder builder, params Profile[] profileConfigs)
        {
            builder.RegisterInstance(MappingHelper.CreateMapper(profileConfigs));
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IAuthorizationCodeStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <param name="useInternalSupport">Defaults to true to use default support providers, when false, 
        /// <see cref="IClientStore"/> and <see cref="IScopeStore"/> providers must be provided to the
        /// Autofac container</param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterAuthorizationCodeStore(this ContainerBuilder builder, bool useInternalSupport = true)
        {
            builder.RegisterBaseSources(useInternalSupport);
            builder.RegisterType<AuthorizationCodeStore>().AsSelf().As<IAuthorizationCodeStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IClientStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterClientStore(this ContainerBuilder builder)
        {
            builder.RegisterType<ClientStore>().AsSelf().As<IClientStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IConsentStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterConsentStore(this ContainerBuilder builder)
        {
            builder.RegisterType<ConsentStore>().AsSelf().As<IConsentStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IRefreshTokenStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <param name="useInternalSupport">Defaults to true to use default support providers, when false, 
        /// <see cref="IClientStore"/> and <see cref="IScopeStore"/> providers must be provided to the
        /// Autofac container</param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterRefreshTokenStore(this ContainerBuilder builder, bool useInternalSupport = true)
        {
            builder.RegisterBaseSources(useInternalSupport);
            builder.RegisterType<RefreshTokenStore>().AsSelf().As<IRefreshTokenStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IScopeStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterScopeStore(this ContainerBuilder builder)
        {
            builder.RegisterType<ScopeStore>().AsSelf().As<IScopeStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="ITokenHandleStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <param name="useInternalSupport">Defaults to true to use default support providers, when false, 
        /// <see cref="IClientStore"/> and <see cref="IScopeStore"/> providers must be provided to the
        /// Autofac container</param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder RegisterTokenHandleStore(this ContainerBuilder builder, bool useInternalSupport = true)
        {
            builder.RegisterBaseSources(useInternalSupport);
            builder.RegisterType<TokenHandleStore>().AsSelf().As<ITokenHandleStore>();
            return builder;
        }

        private static ContainerBuilder RegisterBaseSources(this ContainerBuilder builder, bool useInternalSupport)
        {
            if (useInternalSupport)
            {
                builder.RegisterClientStore();
                builder.RegisterScopeStore();
            }
            
            return builder;
        }

        /// <summary>
        /// Provides access to all Contrib library providers
        /// - <see cref="IAuthorizationCodeStore"/>
        /// - <see cref="IClientStore"/>
        /// - <see cref="IConsentStore"/>
        /// - <see cref="IRefreshTokenStore"/>
        /// - <see cref="IScopeStore"/>
        /// - <see cref="ITokenHandleStore"/>
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder UseAllContribDataSources(this ContainerBuilder builder)
        {
            builder.RegisterBaseSources(true);
            builder.RegisterType<AuthorizationCodeStore>().AsSelf().As<IAuthorizationCodeStore>();
            builder.RegisterType<ClientStore>().AsSelf().As<IClientStore>();
            builder.RegisterType<ConsentStore>().AsSelf().As<IConsentStore>();
            builder.RegisterType<RefreshTokenStore>().AsSelf().As<IRefreshTokenStore>();
            builder.RegisterType<ScopeStore>().AsSelf().As<IScopeStore>();
            builder.RegisterType<TokenHandleStore>().AsSelf().As<ITokenHandleStore>();
            return builder;
        }
    }
}
