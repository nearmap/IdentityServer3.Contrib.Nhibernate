using AutoMapper;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Models;
using Microsoft.Owin;
using Owin;
using Serilog;
using WebHost.Config;

[assembly: OwinStartup(typeof(WebHost.Startup))]

namespace WebHost
{
    internal class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Trace()
                .WriteTo.File(@"Log-{Date}.log")
                .CreateLogger();

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
            }).CreateMapper();

            appBuilder.Map("/core", core =>
            {
                var options = new IdentityServerOptions
                {
                    SiteName = "IdentityServer3 (Nhibernate)",
                    SigningCertificate = Certificate.Get(),
                    Factory = Factory.Configure("IdSvr3Config", mapper)
                };

                core.UseIdentityServer(options);
            });
        }
    }
}
