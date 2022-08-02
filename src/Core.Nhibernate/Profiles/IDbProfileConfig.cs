using AutoMapper;

namespace IdentityServer3.Core.Models
{
    public interface IDbProfileConfig
    {
        Profile GetProfile();
    }
}
