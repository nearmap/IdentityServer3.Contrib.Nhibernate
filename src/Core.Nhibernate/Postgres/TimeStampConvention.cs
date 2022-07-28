using System;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;
using NHibernate.Type;

namespace IdentityServer3.Contrib.Nhibernate.Postgres
{
    /// <summary>
    /// NHibernate loses milliseconds precision when mapping with <see cref="DateTime"/>
    /// Use "NHibernate.Type.TimeStampType" by default when mapping from <see cref="DateTime"/>
    /// Credit: http://stackoverflow.com/a/10085574
    /// </summary>
    public class TimeStampConvention : IPropertyConvention, IPropertyConventionAcceptance
    {
        public void Apply(IPropertyInstance instance)
            => instance.CustomType<DateTimeOffsetType>();

        public void Accept(IAcceptanceCriteria<IPropertyInspector> criteria)
            => criteria.Expect(p =>
                p.Type == typeof(DateTime) ||
                p.Type == typeof(DateTime?) ||
                p.Type == typeof(DateTimeOffset) ||
                p.Type == typeof(DateTimeOffset?)
            );
    }
}
