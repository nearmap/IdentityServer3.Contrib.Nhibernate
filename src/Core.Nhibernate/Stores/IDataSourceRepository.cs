namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public interface IDataSourceRepository
    {
        TType Get<TType>();

        void RegisterInstance<TType>(TType instance) where TType : class;

        void RegisterType<TType>();
    }
}
