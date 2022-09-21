using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Profiles;

namespace Core.Nhibernate.IntegrationTests.Npgsql4
{
    internal struct Config
    {
        internal static IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new EntitiesProfileNpgSql4()))
            .CreateMapper();
    }
}
