using Autofac;
using AutoMapper;
using NHibernate;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class DataSourceRepository : IDataSourceRepository
    {
        private readonly ContainerBuilder _containerBuilder;
        private IContainer _container;

        public DataSourceRepository(ISession session, IMapper mapper)
        {
            _containerBuilder = new ContainerBuilder();

            _containerBuilder.RegisterInstance(session).As<ISession>();
            _containerBuilder.RegisterInstance(mapper).As<IMapper>();

            _containerBuilder.RegisterAssemblyTypes(typeof(DataSourceRepository).Assembly)
                .AsImplementedInterfaces();
        }

        public DataSourceRepository(ISessionFactory sessionFactory, IMapper mapper)
        {
            _containerBuilder = new ContainerBuilder();

            _containerBuilder.RegisterInstance(sessionFactory).As<ISessionFactory>().SingleInstance();
            _containerBuilder.Register(c => c.Resolve<ISessionFactory>().OpenSession()).InstancePerLifetimeScope();
            _containerBuilder.RegisterInstance(mapper).As<IMapper>();

            _containerBuilder.RegisterAssemblyTypes(typeof(DataSourceRepository).Assembly)
                .AsImplementedInterfaces();
        }

        public TType Get<TType>()
        {
            if (_container == null)
            {
                _container = _containerBuilder.Build();
            }

            return _container.Resolve<TType>();
        }

        public void RegisterInstance<TType>(TType instance) where TType : class
        {
            _containerBuilder.RegisterInstance(instance).AsImplementedInterfaces().AsSelf();
        }

        public void RegisterType<TType>()
        {
            _containerBuilder.RegisterType<TType>().AsImplementedInterfaces().AsSelf();
        }
    }
}
