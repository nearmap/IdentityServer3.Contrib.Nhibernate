using System;
using AutoMapper;
using Entities = IdentityServer3.Contrib.Nhibernate.Entities;
using CoreModels = IdentityServer3.Core.Models;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models // TODO - relocate this namespace
{
    public class EntitiesProfileNpgSql4 : EntitiesProfile
    {
        public EntitiesProfileNpgSql4() : base()
        {
            CreateMap<CoreModels.Secret, Entities.ScopeSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime : null));

            CreateMap<CoreModels.Secret, Entities.ClientSecret>(MemberList.Source)
                .ForMember(x => x.Expiration, opts => opts.MapFrom(
                    src => src.Expiration.HasValue ? (DateTime?)src.Expiration.Value.DateTime : null));
        }
    }
}
