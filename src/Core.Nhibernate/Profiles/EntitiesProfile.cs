using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using ContribModels = IdentityServer3.Contrib.Nhibernate.Models;
using CoreModels = IdentityServer3.Core.Models;

namespace IdentityServer3.Contrib.Nhibernate.Profiles
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<CoreModels.Scope, Entities.Scope>(MemberList.Source)
                .ForMember(x => x.ScopeClaims, opts => opts.MapFrom(src => src.Claims.Select(x => x).ToList()))
                .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x).ToList()));

            CreateMap<CoreModels.ScopeClaim, Entities.ScopeClaim>(MemberList.Source);

            CreateMap<CoreModels.Secret, Entities.ScopeSecret>(MemberList.Source);

            CreateMap<CoreModels.Secret, Entities.ClientSecret>(MemberList.Source);

            CreateMap<DateTimeOffset, DateTime>().ConstructUsing(dto => dto.UtcDateTime);

            CreateMap<CoreModels.Client, Entities.Client>(MemberList.Source)
                .ForMember(x => x.UpdateAccessTokenOnRefresh,
                    opt => opt.MapFrom(src => src.UpdateAccessTokenClaimsOnRefresh))
                .ForMember(x => x.AllowAccessToAllGrantTypes,
                    opt => opt.MapFrom(src => src.AllowAccessToAllCustomGrantTypes));

            CreateMap<Claim, Entities.ClientClaim>();

            CreateMap<Entities.ClientClaim, Claim>();

            CreateMap<string, Entities.ClientScope>().ForMember(x => x.Scope, opt => opt.MapFrom(src => src));

            CreateMap<string, Entities.ClientCorsOrigin>().ForMember(x => x.Origin, opt => opt.MapFrom(src => src));

            CreateMap<string, Entities.ClientCustomGrantType>().ForMember(x => x.GrantType, opt => opt.MapFrom(src => src));

            CreateMap<string, Entities.ClientIdPRestriction>().ForMember(x => x.Provider, opt => opt.MapFrom(src => src));

            CreateMap<string, Entities.ClientRedirectUri>().ForMember(x => x.Uri, opt => opt.MapFrom(src => src));

            CreateMap<string, Entities.ClientPostLogoutRedirectUri>().ForMember(x => x.Uri, opt => opt.MapFrom(src => src));

            CreateMap<Entities.Scope, CoreModels.Scope>(MemberList.Destination)
                    .ForMember(x => x.Claims, opts => opts.MapFrom(src => src.ScopeClaims.Select(x => x).ToList()))
                    .ForMember(x => x.ScopeSecrets, opts => opts.MapFrom(src => src.ScopeSecrets.Select(x => x).ToList()));

            CreateMap<Entities.ScopeClaim, CoreModels.ScopeClaim>(MemberList.Destination);

            CreateMap<Entities.ScopeSecret, CoreModels.Secret>(MemberList.Destination);

            CreateMap<Entities.ClientSecret, CoreModels.Secret>(MemberList.Destination);

            CreateMap<Entities.Client, CoreModels.Client>(MemberList.Destination)
                .ForMember(x => x.UpdateAccessTokenClaimsOnRefresh,
                    opt => opt.MapFrom(src => src.UpdateAccessTokenOnRefresh))
                .ForMember(x => x.AllowAccessToAllCustomGrantTypes,
                    opt => opt.MapFrom(src => src.AllowAccessToAllGrantTypes))
                .ForMember(x => x.AllowedCustomGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedCustomGrantTypes.Select(x => x.GrantType).ToList()))
                .ForMember(x => x.RedirectUris, opt => opt.MapFrom(src => src.RedirectUris.Select(x => x.Uri).ToList()))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src => src.PostLogoutRedirectUris.Select(x => x.Uri).ToList()))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(src => src.IdentityProviderRestrictions.Select(x => x.Provider).ToList()))
                .ForMember(x => x.AllowedScopes, opt => opt.MapFrom(src => src.AllowedScopes.Select(x => x.Scope).ToList()))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins.Select(x => x.Origin).ToList()));

            CreateMap<Entities.Consent, CoreModels.Consent>()
                .ForMember(dest => dest.Scopes, 
                    opt => opt.MapFrom(src => src.Scopes.Split(',')));

            CreateMap<CoreModels.Consent, Entities.Consent>()
                .ForMember(dest => dest.Scopes,
                    opt => opt.MapFrom(src => string.Join(",", src.Scopes)));

            CreateMap<CoreModels.Token, Entities.Token>();

            CreateMap<CoreModels.RefreshToken, Entities.Token>();

            CreateMap<CoreModels.AuthorizationCode, ContribModels.AuthorizationCode>();

            CreateMap<Claim, ContribModels.ClaimLite>();

            CreateMap<ClaimsPrincipal, ContribModels.ClaimsPrincipalLite>()
                .ForMember(dest => dest.AuthenticationType,
                    opt => opt.MapFrom(src => src.Identity.AuthenticationType));

            CreateMap<CoreModels.Client, ContribModels.ClientLite>();

            CreateMap<CoreModels.Scope, ContribModels.ScopeLite>();

            CreateMap<CoreModels.RefreshToken, ContribModels.RefreshToken>();

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
                .ForMember(dest => dest.Subject,
                    opt => opt.MapFrom(src => GetClaimsPrincipal(src.Subject)));

            CreateMap<ContribModels.Token, CoreModels.Token>()
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Claims,
                    opt => opt.MapFrom(src => GetClaims(src.Claims)));
        }

        private IEnumerable<Claim> GetClaims(IEnumerable<ContribModels.ClaimLite> claims)
        {
            return claims.Select(source => new Claim(source.Type, source.Value)).ToList();
        }

        private ClaimsPrincipal GetClaimsPrincipal(ContribModels.ClaimsPrincipalLite source)
        {
            var claims = source.Claims.Select(x => new Claim(x.Type, x.Value)).ToList();
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
