namespace IdentityServer3.Contrib.Nhibernate.Entities
{
    public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
    {
        public virtual TKey Id { get; set; }
    }
}
