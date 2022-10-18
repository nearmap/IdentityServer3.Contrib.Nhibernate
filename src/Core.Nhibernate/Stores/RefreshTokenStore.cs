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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using NHibernate;
using NHibernate.Linq;
using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using NHibRefreshToken = IdentityServer3.Contrib.Nhibernate.Models.RefreshToken;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class RefreshTokenStore : BaseTokenStore<RefreshToken>, IRefreshTokenStore
    {
        public RefreshTokenStore(ISession session, IScopeStore scopeStore, IClientStore clientStore, IMapper mapper)
            : base(session, TokenType.RefreshToken, scopeStore, clientStore, mapper)
        {

        }

        public override async Task<RefreshToken> GetAsync(string key)
            => await ExecuteInTransactionAsync(session => GetInnerAsync(session, key));

        private async Task<RefreshToken> GetInnerAsync(ISession session, string key)
        {
            var token = await session
                .Query<Token>()
                .SingleOrDefaultAsync(t => t.Key == key && t.TokenType == TokenType && t.Expiry > DateTime.UtcNow);

            if (token == null) { return null; }

            var refreshToken = await GetRefreshTokenFromToken(token);

            return refreshToken;
        }

        public override async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId)
            => await ExecuteInTransactionAsync(session => GetAllInnerAsync(session, subjectId));

        private async Task<IEnumerable<ITokenMetadata>> GetAllInnerAsync(ISession session, string subjectId)
        {
            var tokens = await session.Query<Token>()
                .Where(t => t.SubjectId == subjectId && t.TokenType == TokenType && t.Expiry > DateTime.UtcNow)
                .ToListAsync();

            if (!tokens.Any()) return new List<ITokenMetadata>();

            var tokenList = new List<ITokenMetadata>();

            foreach (var token in tokens)
            {
                var refreshToken = await GetRefreshTokenFromToken(token);

                if (refreshToken is null) { continue; }

                tokenList.Add(refreshToken);
            }

            return tokenList;
        }

        private async Task<RefreshToken> GetRefreshTokenFromToken(Token token)
        {
            var nhibRefreshToken = ConvertFromJson<NHibRefreshToken>(token.JsonCode);

            var refreshToken = _mapper.Map<RefreshToken>(nhibRefreshToken);

            if (refreshToken == null) { return null; }

            refreshToken.AccessToken.Client = await ClientStore
                .FindClientByIdAsync(nhibRefreshToken.AccessToken.Client.ClientId);

            var claims = nhibRefreshToken.Subject.Claims.Select(x => new Claim(x.Type, x.Value));
            refreshToken.Subject = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims, nhibRefreshToken.Subject.AuthenticationType, Constants.ClaimTypes.Name, Constants.ClaimTypes.Role));

            return refreshToken;
        }

        public override async Task StoreAsync(string key, RefreshToken value)
            => await ExecuteInTransactionAsync(session => StoreInnerAsync(session, key, value));

        private async Task StoreInnerAsync(ISession session, string key, RefreshToken value)
        {
            var token = await session
                    .Query<Token>()
                    .SingleOrDefaultAsync(t => t.Key == key && t.TokenType == TokenType);

            if (token == null)
            {
                token = new Token
                {
                    Key = key,
                    SubjectId = value.SubjectId,
                    ClientId = value.ClientId,
                    TokenType = TokenType
                };
            }

            token.JsonCode = ConvertToJson<NHibRefreshToken>(value);
            token.Expiry = value.CreationTime.UtcDateTime.AddSeconds(value.LifeTime);

            await session.SaveOrUpdateAsync(token);
        }
    }
}
