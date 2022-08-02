namespace IdentityServer3.Contrib.Nhibernate.Entities
{
    internal abstract class BaseEntity { }

    public abstract class BaseEntity<TKey>
    {
        public virtual TKey Id { get; set; }
    }
}
