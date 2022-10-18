/*MIT License
*
*Copyright (c) 2016 Ricardo Santos
*
*Permission is hereby granted, free of charge, to any person obtaining a copy
*of this software and associated documentation files (the "Software"), to deal
*in the Software without restriction, including without limitation the rights
*to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
*copies of the Software, and to permit persons to whom the Software is
*furnished to do so, subject to the following conditions:
*
*The above copyright notice and this permission notice shall be included in all
*copies or substantial portions of the Software.
*
*THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
*SOFTWARE.
*/


using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Helpers;
using IdentityServer3.Contrib.Nhibernate.Entities;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer3.Contrib.Nhibernate.NhibernateConfig
{
    public static class MappingHelper
    {
        public static AutoPersistenceModel GetNhibernateServicesMappings(
            bool registerOperationalServices,
            bool registerConfigurationServices,
            IEnumerable<IConvention> conventions = null)
        {
            var config = new AutomappingConfiguration(registerOperationalServices, registerConfigurationServices);

            var map = AutoMap.AssemblyOf<BaseEntity>(config)
                .Conventions.Add(DefaultCascade.All())
                .UseOverridesFromAssemblyOf<BaseEntity>()
                .IgnoreBase(typeof(BaseEntity<>));

            if (conventions != null)
            {
                map.Conventions.Add(conventions.ToArray());
            }

            return map;
        }

        public static void ConfigureNhibernateServicesMappings(
            this MappingConfiguration m, 
            bool registerOperationalServices, 
            bool registerConfigurationServices, 
            IEnumerable<IConvention> conventions = null)
            => m.AutoMappings.Add(
                GetNhibernateServicesMappings(registerOperationalServices, registerConfigurationServices, conventions));
    }
}
