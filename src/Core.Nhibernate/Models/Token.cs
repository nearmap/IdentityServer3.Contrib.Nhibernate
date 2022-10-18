using System;
using System.Collections.Generic;

namespace IdentityServer3.Contrib.Nhibernate.Models
{
    internal class Token
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public int Lifetime { get; set; }
        public string Type { get; set; }
        public ClientLite Client { get; set; }
        public IEnumerable<ClaimLite> Claims { get; set; }
        public int Version { get; set; }
    }
}
