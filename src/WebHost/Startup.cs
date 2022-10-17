using IdentityServer3.Core.Configuration;
using Microsoft.Owin;
using Owin;
using Serilog;
using Serilog.Extensions.Logging;
using WebHost.Config;

[assembly: OwinStartup(typeof(WebHost.Startup))]

namespace WebHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var providers = new LoggerProviderCollection();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .WriteTo.File(@"Log-{Date}.log")
                .CreateLogger();

            var factory = new SerilogLoggerFactory(Log.Logger, true, providers);

            var logger = factory.CreateLogger("main");

            appBuilder.Map("/core", async core =>
            {
                var options = new IdentityServerOptions
                {
                    SiteName = "IdentityServer3 (Nhibernate)",
                    SigningCertificate = Certificate.Get(),
                    Factory = await Factory.ConfigureAsync(logger)
                };

                core.UseIdentityServer(options);
            });
        }
    }
}
