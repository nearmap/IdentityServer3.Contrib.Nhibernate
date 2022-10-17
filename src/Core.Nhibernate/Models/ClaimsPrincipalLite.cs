namespace IdentityServer3.Contrib.Nhibernate.Models
{
    internal class ClaimsPrincipalLite
    {
        public string AuthenticationType { get; set; }
        public ClaimLite[] Claims { get; set; }
    }
}
