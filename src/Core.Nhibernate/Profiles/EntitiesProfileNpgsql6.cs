using System;

// ReSharper disable once CheckNamespace
namespace IdentityServer3.Core.Models // TODO - relocate this namespace
{
    public class EntitiesProfileNpgSql6 : EntitiesProfile
    {
        public EntitiesProfileNpgSql6() : base()
        {
            CreateMap<DateTimeOffset, DateTime>().ConstructUsing(dto => dto.UtcDateTime);
        }
    }
}
