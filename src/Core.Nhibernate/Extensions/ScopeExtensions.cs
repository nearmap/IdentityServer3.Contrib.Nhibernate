using AutoMapper;
using System.Collections.Generic;

namespace IdentityServer3.Core.Models
{
    public static class ScopeExtensions
    {
        public static Contrib.Nhibernate.Entities.Scope ToEntity(this Scope s, IMapper mapper)
        {
            if (s == null) return null;

            if (s.Claims == null)
            {
                s.Claims = new List<ScopeClaim>();
            }
            if (s.ScopeSecrets == null)
            {
                s.ScopeSecrets = new List<Secret>();
            }

            return mapper.Map<Scope, Contrib.Nhibernate.Entities.Scope>(s);
        }
    }
}
