/*MIT License
*
*Copyright (c) 2016 Ricardo Santos
*Copyright (c) 2022 Jason F. Bridgman
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
    public class ConsentStore : NhibernateStore, IConsentStore
    {
        public ConsentStore(ISession session, IMapper mapper)
            : base(session, mapper)
        {
        }

        public async Task<Core.Models.Consent> LoadAsync(string subject, string client)
        {
            var item = await ExecuteInTransactionAsync(async session =>
            {
                var consent = await session.Query<Consent>()
                    .SingleOrDefaultAsync(c => c.Subject == subject && c.ClientId == client);

                _ = consent?.Scopes; // Force access of the Scopes link within the transaction.

                return consent;
            });

            return item == null
                ? null
                : new Core.Models.Consent
                {
                    Subject = item.Subject,
                    ClientId = item.ClientId,
                    Scopes = ParseScopes(item.Scopes)
                };
        }

        public async Task UpdateAsync(Core.Models.Consent consent)
            => await ExecuteInTransactionAsync(async session => await UpdateInnerAsync(session, consent));

        private async Task UpdateInnerAsync(ISession session, Core.Models.Consent consent)
        {
            var item = await session.Query<Consent>()
                    .SingleOrDefaultAsync(c => c.Subject == consent.Subject && c.ClientId == consent.ClientId);

            if (item == null)
            {
                if (!consent.Scopes?.Any() ?? true) { return; }

                item = new Consent
                {
                    Subject = consent.Subject,
                    ClientId = consent.ClientId,
                    Scopes = string.Join(",", consent.Scopes)
                };

                await session.SaveAsync(item);
            }
            else
            {
                if (!consent.Scopes?.Any() ?? true)
                {
                    await session.DeleteAsync(item);
                }
                
                item.Scopes = string.Join(",", consent.Scopes);
                await session.SaveOrUpdateAsync(item);
            }
        }

        public async Task<IEnumerable<Core.Models.Consent>> LoadAllAsync(string subject)
        {
            var items = await ExecuteInTransactionAsync(async session =>
            {
                return await session.Query<Consent>()
                    .Where(c => c.Subject == subject)
                    .ToListAsync();
            });

            return items.Select(i => new IdentityServer3.Core.Models.Consent
            {
                Subject = i.Subject,
                ClientId = i.ClientId,
                Scopes = ParseScopes(i.Scopes)
            }).ToList();
        }

        private IEnumerable<string> ParseScopes(string scopes)
            => string.IsNullOrWhiteSpace(scopes) ? Enumerable.Empty<string>() : scopes.Split(',');

        public async Task RevokeAsync(string subject, string client)
            => await ExecuteInTransactionAsync(async session =>
            {
                await session.CreateQuery($"DELETE {nameof(Consent)} c " +
                                    $"WHERE c.{nameof(Consent.Subject)} = :subject " +
                                    $"and c.{nameof(Consent.ClientId)} = :clientId")
                    .SetParameter("subject", subject)
                    .SetParameter("clientId", client)
                    .ExecuteUpdateAsync();
            });
    }
}
