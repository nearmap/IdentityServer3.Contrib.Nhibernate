using System;

namespace IdentityServer3.Contrib.Nhibernate.Profiles
{
    public class EntitiesProfileNpgSql6 : EntitiesProfile
    {
        public EntitiesProfileNpgSql6()
        {
            CreateMap<DateTimeOffset, DateTime>().ConstructUsing(dto => dto.UtcDateTime);
        }
    }
}
