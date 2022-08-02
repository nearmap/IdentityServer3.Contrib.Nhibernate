using AutoMapper;

namespace IdentityServer3.Core.Models
{
    public class Npgsql4ProviderConfig : IDbProfileConfig
    {
        public Profile GetProfile()
        {
            return new EntitiesProfileNpgSql4();
        }
    }
}
