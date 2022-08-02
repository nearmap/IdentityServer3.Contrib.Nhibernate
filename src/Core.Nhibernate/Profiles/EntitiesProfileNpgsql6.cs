using System;
using AutoMapper;
using Entities = IdentityServer3.Contrib.Nhibernate.Entities;
using CoreModels = IdentityServer3.Core.Models;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models // TODO - relocate this namespace
{
    public class EntitiesProfileNpgSql6 : EntitiesProfile
    {
        public EntitiesProfileNpgSql6() : base()
        {
            CreateMap<CoreModels.Secret, Entities.ScopeSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));

            CreateMap<CoreModels.Secret, Entities.ClientSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime.ToUniversalTime() : null));
        }
    }
}
