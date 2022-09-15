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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using NHibernate;
using NHibernate.Linq;
using EntityTokenModel = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using ClientEntity = IdentityServer3.Contrib.Nhibernate.Entities.Client;
using NHibTokenModel = IdentityServer3.Contrib.Nhibernate.Models.Token;
using CoreTokenModel = IdentityServer3.Core.Models.Token;
using CoreClientModel = IdentityServer3.Core.Models.Client;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class TokenHandleStore : BaseTokenStore<CoreTokenModel>, ITokenHandleStore
    {
        public TokenHandleStore(ISession session, IScopeStore scopeStore, IClientStore clientStore, IMapper mapper)
            : base(session, TokenType.TokenHandle, scopeStore, clientStore, mapper)
        {

        }

        public override async Task<CoreTokenModel> GetAsync(string key)
            => await ExecuteInTransactionAsync(session => GetInnerAsync(session, key));

        private async Task<CoreTokenModel> GetInnerAsync(ISession session, string key)
        {
            var token = await session
                    .Query<EntityTokenModel>()
                    .SingleOrDefaultAsync(t => t.Key == key && t.TokenType == TokenType);

            if (token == null) { return null; }

            var tModel = ConvertFromJson<NHibTokenModel>(token.JsonCode);

            var tokenModel = token.Expiry < DateTime.UtcNow ? null : _mapper.Map<CoreTokenModel>(tModel);

            if (tokenModel == null) { return null; }

            var clientEntity = await session.Query<ClientEntity>()
                .SingleOrDefaultAsync(x => x.ClientId == tModel.ClientId);

            tokenModel.Client = clientEntity == null ? null : _mapper.Map<CoreClientModel>(clientEntity);

            return tokenModel;
        }

        public override async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId)
            => await ExecuteInTransactionAsync(session => GetAllInnerAsync(session, subjectId));

        private async Task<IEnumerable<ITokenMetadata>> GetAllInnerAsync(ISession session, string subjectId)
        {
            var tokens = await session.Query<EntityTokenModel>()
                .Where(t => t.SubjectId == subjectId && t.TokenType == TokenType)
                .ToListAsync();

            if (!tokens.Any())
            {
                return new List<ITokenMetadata>();
            }

            var tokenList = new List<ITokenMetadata>();

            foreach (var token in tokens)
            {
                var tModel = ConvertFromJson<NHibTokenModel>(token.JsonCode);

                if (tModel == null)
                {
                    continue;
                }

                var tokenModel = _mapper.Map<CoreTokenModel>(tModel);

                var clientEntity = await session.Query<ClientEntity>()
                    .SingleOrDefaultAsync(x => x.ClientId == tModel.ClientId);

                tokenModel.Client = clientEntity == null ? null : _mapper.Map<CoreClientModel>(clientEntity);

                tokenList.Add(tokenModel);
            }

            return tokenList;
        }

        public override async Task StoreAsync(string key, CoreTokenModel value)
        {
            var nhCode = new EntityTokenModel
            {
                Key = key,
                SubjectId = value.SubjectId,
                ClientId = value.ClientId,
                JsonCode = ConvertToJson<NHibTokenModel>(value),
                Expiry = DateTime.UtcNow.AddSeconds(value.Lifetime),
                TokenType = this.TokenType
            };

            await SaveAsync(nhCode);
        }
    }
}
