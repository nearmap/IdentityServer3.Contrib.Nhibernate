using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;

namespace Autofac
{
    public static class AutofacExtensions
    {
        /// <summary>
        /// Use mapping profile required for correct data interpretation with Npgsql 4 driver
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder UseNpgsql4ProviderConfig(this ContainerBuilder builder)
        {
            builder.RegisterType<Npgsql4ProviderConfig>().As<IDbProfileConfig>();
            return builder;
        }

        /// <summary>
        /// Use mapping profile required for correct data interpretation with Npgsql 6 driver
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder UseNpgsql6ProviderConfig(this ContainerBuilder builder)
        {
            builder.RegisterType<Npgsql6ProviderConfig>().As<IDbProfileConfig>();
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
        public static ContainerBuilder UseAuthorizationCodeStore(this ContainerBuilder builder, bool useInternalSupport = true)
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
        public static ContainerBuilder UseClientStore(this ContainerBuilder builder)
        {
            builder.RegisterType<ClientStore>().AsSelf().As<IClientStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IConsentStore"/> provider
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder UseConsentStore(this ContainerBuilder builder)
        {
            builder.RegisterType<ConsentStore>().AsSelf().As<IConsentStore>();
            return builder;
        }

        /// <summary>
        /// Provides access to the Contrib library <see cref="IDataSourceRepository"/>
        /// </summary>
        /// <param name="builder">The <see cref="Autofac.ContainerBuilder"/></param>
        /// <returns>The <see cref="Autofac.ContainerBuilder"/> instance for method chaining</returns>
        public static ContainerBuilder UseDataSourceRepository(this ContainerBuilder builder)
        {
            builder.RegisterType<DataSourceRepository>().AsSelf().As<IDataSourceRepository>();
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
        public static ContainerBuilder UseRefreshTokenStore(this ContainerBuilder builder, bool useInternalSupport = true)
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
        public static ContainerBuilder UseScopeStore(this ContainerBuilder builder)
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
        public static ContainerBuilder UseTokenHandleStore(this ContainerBuilder builder, bool useInternalSupport = true)
        {
            builder.RegisterBaseSources(useInternalSupport);
            builder.RegisterType<TokenHandleStore>().AsSelf().As<ITokenHandleStore>();
            return builder;
        }

        private static ContainerBuilder RegisterBaseSources(this ContainerBuilder builder, bool useInternalSupport)
        {
            if (useInternalSupport)
            {
                builder.RegisterType<ClientStore>().AsSelf().As<IClientStore>();
                builder.RegisterType<ScopeStore>().AsSelf().As<IScopeStore>();
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
