using System;

namespace IdentityServer3.Contrib.Nhibernate.Profiles
{
    public class EntitiesProfileNpgSql4 : EntitiesProfile
    {
        public EntitiesProfileNpgSql4()
        {
            CreateMap<DateTimeOffset, DateTime>().ConstructUsing(dto => dto.DateTime);
        }
    }
}
