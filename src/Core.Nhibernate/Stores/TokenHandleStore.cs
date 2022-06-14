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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using NHibernate;
using EntityTokenModel = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using ClientEntity = IdentityServer3.Contrib.Nhibernate.Entities.Client;
using NHibTokenModel = IdentityServer3.Contrib.Nhibernate.Models.Token;
using CoreTokenModel = IdentityServer3.Core.Models.Token;
using CoreClientModel = IdentityServer3.Core.Models.Client;
using System.Collections.Generic;
using IdentityServer3.Core.Models;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class TokenHandleStore : BaseTokenStore<CoreTokenModel>, ITokenHandleStore
    {
        public TokenHandleStore(ISession session, IScopeStore scopeStore, IClientStore clientStore, IMapper mapper)
            : base(session, TokenType.TokenHandle, scopeStore, clientStore, mapper)
        {

        }

        public override Task<CoreTokenModel> GetAsync(string key)
        {
            var toReturn = ExecuteInTransaction(session =>
            {
                var token = session
                    .Query<EntityTokenModel>()
                    .SingleOrDefault(t => t.Key == key && t.TokenType == TokenType);

                if (token == null)
                {
                    return null;
                }

                var tModel = ConvertFromJson<NHibTokenModel>(token.JsonCode);

                var tokenModel = token.Expiry < DateTime.UtcNow ? null : _mapper.Map<CoreTokenModel>(tModel);

                if (tokenModel == null)
                {
                    return null;
                }

                var clientEntity = session.Query<ClientEntity>()
                    .SingleOrDefault(x => x.ClientId == tModel.ClientId);

                tokenModel.Client = clientEntity == null ? null : _mapper.Map<CoreClientModel>(clientEntity);

                return tokenModel;
            });

            return Task.FromResult(toReturn);
        }

        public override async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId)
        {
            var toReturn = ExecuteInTransaction(session =>
            {
                var tokens = session.Query<EntityTokenModel>()
                    .Where(t => t.SubjectId == subjectId && t.TokenType == TokenType)
                    .ToList();

                if (!tokens.Any()) return new List<ITokenMetadata>();

                var tokenList = new List<ITokenMetadata>();

                foreach(var token in tokens)
                {
                    var tModel = ConvertFromJson<NHibTokenModel>(token.JsonCode);

                    if (tModel == null)
                    {
                        continue;
                    }

                    var tokenModel = _mapper.Map<CoreTokenModel>(tModel);

                    var clientEntity = session.Query<ClientEntity>()
                        .SingleOrDefault(x => x.ClientId == tModel.ClientId);

                    tokenModel.Client = clientEntity == null ? null : _mapper.Map<CoreClientModel>(clientEntity);

                    tokenList.Add(tokenModel);
                }
                
                return tokenList;
            });

            return toReturn;
        }

        public override async Task StoreAsync(string key, CoreTokenModel value)
        {
            ExecuteInTransaction(session =>
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

                session.Save(nhCode);
            });

            await Task.CompletedTask;
        }
    }
}
