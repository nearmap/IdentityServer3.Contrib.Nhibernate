using System;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models // TODO - relocate this namespace
{
    public class EntitiesProfileNpgSql4 : EntitiesProfile
    {
        public EntitiesProfileNpgSql4() : base()
        {
            CreateMap<DateTimeOffset, DateTime>().ConstructUsing(dto => dto.DateTime);
        }
    }
}
