using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using Entities = IdentityServer3.Contrib.Nhibernate.Entities;
using ContribModels = IdentityServer3.Contrib.Nhibernate.Models;
using CoreModels = IdentityServer3.Core.Models;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models // TODO - relocate this namespace
{
    internal class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<CoreModels.Scope, Entities.Scope>(MemberList.Source)
                .ForMember(x => x.ScopeClaims, opts => opts.MapFrom(src => src.Claims.Select(x => x)))
                .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x)));

            CreateMap<CoreModels.ScopeClaim, Entities.ScopeClaim>(MemberList.Source);

            CreateMap<CoreModels.Secret, Entities.ScopeSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));

            CreateMap<CoreModels.Secret, Entities.ClientSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));

            CreateMap<CoreModels.Client, Entities.Client>(MemberList.Source)
                .ForMember(x => x.UpdateAccessTokenOnRefresh,
                    opt => opt.MapFrom(src => src.UpdateAccessTokenClaimsOnRefresh))
                .ForMember(x => x.AllowAccessToAllGrantTypes,
                    opt => opt.MapFrom(src => src.AllowAccessToAllCustomGrantTypes))
                .ForMember(x => x.ClientSecrets, opt => opt.MapFrom(src => src.ClientSecrets))
                .ForMember(x => x.AllowedCustomGrantTypes,
                    opt => opt.MapFrom(
                        src => src.AllowedCustomGrantTypes.Select(
                            x => new Entities.ClientCustomGrantType { GrantType = x })))
                .ForMember(x => x.RedirectUris,
                    opt =>
                        opt.MapFrom(src => src.RedirectUris.Select(x => new Entities.ClientRedirectUri { Uri = x })))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(
                        src => src.PostLogoutRedirectUris.Select(
                            x => new Entities.ClientPostLogoutRedirectUri { Uri = x })))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(
                        src => src.IdentityProviderRestrictions.Select(
                            x => new Entities.ClientIdPRestriction { Provider = x })))
                .ForMember(x => x.AllowedScopes,
                    opt => opt.MapFrom(src => src.AllowedScopes.Select(x => new Entities.ClientScope { Scope = x })))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(
                        src => src.AllowedCorsOrigins.Select(x => new Entities.ClientCorsOrigin { Origin = x })))
                .ForMember(x => x.Claims,
                    opt => opt.MapFrom(
                        src => src.Claims.Select(x => new Entities.ClientClaim { Type = x.Type, Value = x.Value })));

            CreateMap<Entities.Scope, CoreModels.Scope>(MemberList.Destination)
                    .ForMember(x => x.Claims, opts => opts.MapFrom(src => src.ScopeClaims.Select(x => x)))
                    .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x)));

            CreateMap<Entities.ScopeClaim, CoreModels.ScopeClaim>(MemberList.Destination);

            CreateMap<Entities.ScopeSecret, CoreModels.Secret>(MemberList.Destination);

            CreateMap<Entities.ClientSecret, CoreModels.Secret>(MemberList.Destination);

            CreateMap<Entities.Client, CoreModels.Client>(MemberList.Destination)
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

            CreateMap<Entities.Consent, CoreModels.Consent>()
                .ForMember(dest => dest.Scopes,
                    opt => opt.MapFrom(src => src.Scopes.Split(',')));

            CreateMap<CoreModels.Consent, Entities.Consent>()
                .ForMember(dest => dest.Scopes,
                    opt => opt.MapFrom(src => string.Join(",", src.Scopes)));

            CreateMap<CoreModels.Token, Entities.Token>();

            CreateMap<CoreModels.RefreshToken, Entities.Token>();

            CreateMap<CoreModels.AuthorizationCode, ContribModels.AuthorizationCode>()
                .ForMember(dest => dest.Scopes, opt => opt.Ignore());
            CreateMap<Claim, ContribModels.ClaimLite>();
            CreateMap<ClaimsPrincipal, ContribModels.ClaimsPrincipalLite>()
                .ForMember(dest => dest.AuthenticationType,
                    opt => opt.MapFrom(src => src.Identity.AuthenticationType));
            CreateMap<CoreModels.Client, ContribModels.ClientLite>();
            CreateMap<CoreModels.Scope, ContribModels.ScopeLite>();

            CreateMap<CoreModels.RefreshToken, ContribModels.RefreshToken>()
                .ForMember(dest => dest.Scopes, opt => opt.Ignore());

            CreateMap<CoreModels.Token, ContribModels.Token>();

            CreateMap<ContribModels.AuthorizationCode, CoreModels.AuthorizationCode>()
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Subject,
                    opt => opt.MapFrom(src => GetClaimsPrincipal(src.Subject)))
                .ForMember(dest => dest.RequestedScopes, opt => opt.Ignore())
                .ForMember(dest => dest.Scopes, opt => opt.Ignore());

            CreateMap<ContribModels.ClaimsPrincipalLite, ClaimsPrincipal>()
                .ForMember(dest => dest.Claims,
                    opt => opt.MapFrom(src => GetClaims(src.Claims)));

            CreateMap<ContribModels.RefreshToken, CoreModels.RefreshToken>()
                .ForMember(dest => dest.Scopes, opt => opt.Ignore())
                .ForMember(dest => dest.Subject,
                    opt => opt.MapFrom(src => GetClaimsPrincipal(src.Subject)));

            CreateMap<ContribModels.Token, CoreModels.Token>()
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Claims,
                    opt => opt.MapFrom(src => GetClaims(src.Claims)))
                .ForMember(dest => dest.Scopes, opt => opt.Ignore());
        }

        private IEnumerable<Claim> GetClaims(IEnumerable<ContribModels.ClaimLite> claims)
        {
            return claims.Select(source => new Claim(source.Type, source.Value));
        }

        private ClaimsPrincipal GetClaimsPrincipal(ContribModels.ClaimsPrincipalLite source)
        {
            var claims = source.Claims.Select(x => new Claim(x.Type, x.Value));
            var id = new ClaimsIdentity(
                claims, 
                source.AuthenticationType,
                IdentityServer3.Core.Constants.ClaimTypes.Name,
                IdentityServer3.Core.Constants.ClaimTypes.Role);

            var target = new ClaimsPrincipal(id);

            return target;
        }
    }
}
