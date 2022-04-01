using AutoMapper;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer3.Core.Models
{
    public static class ClientExtensions
    {
        public static Contrib.Nhibernate.Entities.Client ToEntity(this Client s, IMapper mapper)
        {
            if (s == null) return null;

            if (s.ClientSecrets == null)
            {
                s.ClientSecrets = new List<Secret>();
            }
            if (s.RedirectUris == null)
            {
                s.RedirectUris = new List<string>();
            }
            if (s.PostLogoutRedirectUris == null)
            {
                s.PostLogoutRedirectUris = new List<string>();
            }
            if (s.AllowedScopes == null)
            {
                s.AllowedScopes = new List<string>();
            }
            if (s.IdentityProviderRestrictions == null)
            {
                s.IdentityProviderRestrictions = new List<string>();
            }
            if (s.Claims == null)
            {
                s.Claims = new List<Claim>();
            }
            if (s.AllowedCustomGrantTypes == null)
            {
                s.AllowedCustomGrantTypes = new List<string>();
            }
            if (s.AllowedCorsOrigins == null)
            {
                s.AllowedCorsOrigins = new List<string>();
            }

            return mapper.Map<Client, Contrib.Nhibernate.Entities.Client>(s);
        }
    }
}
