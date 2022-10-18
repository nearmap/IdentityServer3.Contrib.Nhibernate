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
using NHibAuthorizationCode = IdentityServer3.Contrib.Nhibernate.Models.AuthorizationCode;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class AuthorizationCodeStore : BaseTokenStore<AuthorizationCode>, IAuthorizationCodeStore
    {
        public AuthorizationCodeStore(ISession session, IScopeStore scopeStore, IClientStore clientStore, IMapper mapper)
            : base(session, TokenType.AuthorizationCode, scopeStore, clientStore, mapper)
        {

        }

        public override async Task<AuthorizationCode> GetAsync(string key)
        {
            return await ExecuteInTransactionAsync(async session =>
            {
                var token = await session
                    .Query<Token>()
                    .SingleOrDefaultAsync(t => t.Key == key && t.TokenType == TokenType && t.Expiry > DateTime.UtcNow);

                if (token == null) { return null; }

                return await GetAuthorizationCodeFromToken(token);
            });
        }

        public override async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId)
        {
            return await ExecuteInTransactionAsync(async session =>
            {
                var tokens = await session.Query<Token>()
                    .Where(t => t.SubjectId == subjectId && t.TokenType == TokenType && t.Expiry > DateTime.UtcNow)
                    .ToListAsync();

                if (!tokens.Any()) { return new List<ITokenMetadata>(); }

                var tokenList = new List<ITokenMetadata>();

                foreach (var token in tokens)
                {
                    var authorizationCode = await GetAuthorizationCodeFromToken(token);

                    if (authorizationCode is null) { continue; }

                    tokenList.Add(authorizationCode);
                }

                return tokenList;
            });
        }

        private async Task<AuthorizationCode> GetAuthorizationCodeFromToken(Token token)
        {
            var nhibAuthorizationCode = ConvertFromJson<NHibAuthorizationCode>(token.JsonCode);

            var authorizationCode = _mapper.Map<AuthorizationCode>(nhibAuthorizationCode);

            if (authorizationCode == null) { return null; }

            authorizationCode.Client = await ClientStore.FindClientByIdAsync(nhibAuthorizationCode.ClientId);
            authorizationCode.RequestedScopes = (await ScopeStore.FindScopesAsync(
                nhibAuthorizationCode.RequestedScopes.Select(s => s.Name))).ToList();

            var claims = nhibAuthorizationCode.Subject.Claims.Select(x => new Claim(x.Type, x.Value));
            authorizationCode.Subject = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims, nhibAuthorizationCode.Subject.AuthenticationType, Constants.ClaimTypes.Name, Constants.ClaimTypes.Role));

            return authorizationCode;
        }

        public override async Task StoreAsync(string key, AuthorizationCode code)
        {
            var nhCode = new Token
            {
                Key = key,
                SubjectId = code.SubjectId,
                ClientId = code.ClientId,
                JsonCode = ConvertToJson<NHibAuthorizationCode>(code),
                Expiry = DateTime.UtcNow.AddSeconds(code.Client.AuthorizationCodeLifetime),
                TokenType = this.TokenType
            };

            await SaveAsync(nhCode);
        }
    }
}
