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


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using NHibernate;
using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public abstract class BaseTokenStore<T> : NhibernateStore 
        where T : class
    {
        protected readonly TokenType TokenType;
        protected readonly IScopeStore ScopeStore;
        protected readonly IClientStore ClientStore;

        protected BaseTokenStore(ISession session, TokenType tokenType, IScopeStore scopeStore, IClientStore clientStore, IDbProfileConfig dbProfile)
            : base(session, dbProfile)
        {
            ScopeStore = scopeStore ?? throw new ArgumentNullException(nameof(scopeStore));
            ClientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            TokenType = tokenType;
        }

        private JsonSerializerSettings SerializerSettings
            =>  new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

        protected string ConvertToJson<TEntity>(T value)
        {
            return JsonConvert.SerializeObject(_mapper.Map<TEntity>(value), SerializerSettings);
        }

        protected TEntity ConvertFromJson<TEntity>(string json)
        {
            return JsonConvert.DeserializeObject<TEntity>(json, SerializerSettings);
        }

        public abstract Task<T> GetAsync(string key);

        public async Task RemoveAsync(string key)
        {
            await ExecuteInTransactionAsync(async session =>
            {
                await session.CreateQuery($"DELETE {nameof(Token)} t " +
                                    $"WHERE t.{nameof(Token.Key)} = :key " +
                                    $"and t.{nameof(Token.TokenType)} = :tokenType")
                    .SetParameter("key", key)
                    .SetParameter("tokenType", TokenType)
                    .ExecuteUpdateAsync();
            });
        }

        public abstract Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId);

        public async Task RevokeAsync(string subjectId, string clientId)
        {
            await ExecuteInTransactionAsync(async session =>
            {
                await session.CreateQuery($"DELETE {nameof(Token)} t " +
                                    $"WHERE t.{nameof(Token.SubjectId)} = :subject " +
                                    $"and t.{nameof(Token.ClientId)} = :clientId " +
                                    $"and t.{nameof(Token.TokenType)} = :tokenType")
                    .SetParameter("subject", subjectId)
                    .SetParameter("clientId", clientId)
                    .SetParameter("tokenType", TokenType)
                    .ExecuteUpdateAsync();
            });
        }

        public abstract Task StoreAsync(string key, T value);
    }
}
