/*MIT License
*
*Copyright (c) 2016 Ricardo Santos
*Copyright (c) 2022 Nearmap
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


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Entities;
using IdentityServer3.Core.Services;
using NHibernate;
using NHibernate.Linq;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class ScopeStore : NhibernateStore, IScopeStore
    {
        public ScopeStore(ISession session, IMapper mapper) 
            : base(session, mapper)
        {
        }

        public async Task<IEnumerable<Core.Models.Scope>> FindScopesAsync(IEnumerable<string> scopeNames)
        {
            var scopes = await ExecuteInTransactionAsync(async session =>
            {
                var scopeEntities = GetScopesBaseQuery(session);

                if (scopeNames?.Any() ?? false)
                {
                    scopeEntities = scopeEntities.Where(scope => scopeNames.Contains(scope.Name));
                }

                return await scopeEntities.ToListAsync();
            });

            return _mapper.Map<IEnumerable<Core.Models.Scope>>(scopes);
        }

        public async Task<IEnumerable<Core.Models.Scope>> GetScopesAsync(bool publicOnly = true)
        {
            var scopes = await ExecuteInTransactionAsync(async session =>
            {
                var scopeEntities = GetScopesBaseQuery(session);

                if (publicOnly)
                {
                    scopeEntities = scopeEntities.Where(s => s.ShowInDiscoveryDocument);
                }

                return await scopeEntities.ToListAsync();
            });

            return _mapper.Map<IEnumerable<Core.Models.Scope>>(scopes);
        }

        public async Task<object> SaveAsync(Core.Models.Scope obj)
        {
            return await SaveAsync(_mapper.Map<Scope>(obj));
        }

        private IQueryable<Scope> GetScopesBaseQuery(ISession session)
        {
            return session.Query<Scope>()
                .Fetch(s => s.ScopeClaims)
                .Fetch(s => s.ScopeSecrets)
                .AsQueryable();
        }
    }
}
