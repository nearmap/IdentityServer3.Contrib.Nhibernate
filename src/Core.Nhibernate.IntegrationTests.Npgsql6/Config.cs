using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Profiles;

namespace Core.Nhibernate.IntegrationTests.Npgsql6
{
    internal struct Config
    {
        internal static IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new EntitiesProfileNpgSql6()))
            .CreateMapper();
    }
}
