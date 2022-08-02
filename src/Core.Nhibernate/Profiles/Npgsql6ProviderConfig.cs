using AutoMapper;

namespace IdentityServer3.Core.Models
{
    public class Npgsql6ProviderConfig : IDbProfileConfig
    {
        public Profile GetProfile()
        {
            return new EntitiesProfileNpgSql6();
        }
    }
}
