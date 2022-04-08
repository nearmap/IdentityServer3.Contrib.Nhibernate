using System;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Entities;

using Entities = IdentityServer3.Contrib.Nhibernate.Entities;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<Scope, Entities.Scope>(MemberList.Source)
                .ForMember(x => x.ScopeClaims, opts => opts.MapFrom(src => src.Claims.Select(x => x)))
                .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x)));

            CreateMap<ScopeClaim, Entities.ScopeClaim>(MemberList.Source);

            CreateMap<Secret, Entities.ScopeSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));

            CreateMap<Secret, Entities.ClientSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));

            CreateMap<Client, Entities.Client>(MemberList.Source)
                .ForMember(x => x.UpdateAccessTokenOnRefresh,
                    opt => opt.MapFrom(src => src.UpdateAccessTokenClaimsOnRefresh))
                .ForMember(x => x.AllowAccessToAllGrantTypes,
                    opt => opt.MapFrom(src => src.AllowAccessToAllCustomGrantTypes))
                .ForMember(x => x.ClientSecrets, opt => opt.MapFrom(src => src.ClientSecrets))
                .ForMember(x => x.AllowedCustomGrantTypes,
                    opt => opt.MapFrom(
                        src => src.AllowedCustomGrantTypes.Select(
                            x => new ClientCustomGrantType { GrantType = x })))
                .ForMember(x => x.RedirectUris,
                    opt =>
                        opt.MapFrom(src => src.RedirectUris.Select(x => new ClientRedirectUri { Uri = x })))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(
                        src => src.PostLogoutRedirectUris.Select(
                            x => new ClientPostLogoutRedirectUri { Uri = x })))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(
                        src => src.IdentityProviderRestrictions.Select(
                            x => new ClientIdPRestriction { Provider = x })))
                .ForMember(x => x.AllowedScopes,
                    opt => opt.MapFrom(src => src.AllowedScopes.Select(x => new ClientScope { Scope = x })))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(
                        src => src.AllowedCorsOrigins.Select(x => new ClientCorsOrigin { Origin = x })))
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(
                        src => src.Claims.Select(x => new ClientClaim { Type = x.Type, Value = x.Value })));

            CreateMap<Entities.Scope, Scope>(MemberList.Destination)
                    .ForMember(x => x.Claims, opts => opts.MapFrom(src => src.ScopeClaims.Select(x => x)))
                    .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x)));

            CreateMap<Entities.ScopeClaim, ScopeClaim>(MemberList.Destination);

            CreateMap<Entities.ScopeSecret, Secret>(MemberList.Destination);

            CreateMap<Entities.ClientSecret, Secret>(MemberList.Destination);

            CreateMap<Entities.Client, Client>(MemberList.Destination)
                .ForMember(x => x.UpdateAccessTokenClaimsOnRefresh,
                    opt => opt.MapFrom(src => src.UpdateAccessTokenOnRefresh))
                .ForMember(x => x.AllowAccessToAllCustomGrantTypes,
                    opt => opt.MapFrom(src => src.AllowAccessToAllGrantTypes))
                .ForMember(x => x.AllowedCustomGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedCustomGrantTypes.Select(x => x.GrantType)))
                .ForMember(x => x.RedirectUris, opt => opt.MapFrom(src => src.RedirectUris.Select(x => x.Uri)))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src => src.PostLogoutRedirectUris.Select(x => x.Uri)))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(src => src.IdentityProviderRestrictions.Select(x => x.Provider)))
                .ForMember(x => x.AllowedScopes, opt => opt.MapFrom(src => src.AllowedScopes.Select(x => x.Scope)))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins.Select(x => x.Origin)))
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(src => src.Claims.Select(x => new Claim(x.Type, x.Value))));
        }
    }
}
