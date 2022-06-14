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
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using NHibernate;
using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using AuthCode = IdentityServer3.Contrib.Nhibernate.Models.AuthorizationCode;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public class AuthorizationCodeStore : BaseTokenStore<AuthorizationCode>, IAuthorizationCodeStore
    {
        public AuthorizationCodeStore(ISession session, IScopeStore scopeStore, IClientStore clientStore, IMapper mapper)
            : base(session, TokenType.AuthorizationCode, scopeStore, clientStore, mapper)
        {

        }

        public override Task<AuthorizationCode> GetAsync(string key)
        {
            var toReturn = ExecuteInTransaction(session =>
            {
                var token = session
                    .Query<Token>()
                    .SingleOrDefault(t => t.Key == key && t.TokenType == TokenType);

                if (token == null)
                {
                    return null;
                }

                var authCode = ConvertFromJson<AuthCode>(token.JsonCode);

                var code = token.Expiry < DateTime.UtcNow ? null : _mapper.Map<AuthorizationCode>(authCode);

                if (code == null)
                {
                    return null;
                }

                code.Client = ClientStore.FindClientByIdAsync(authCode.ClientId).Result;
                code.RequestedScopes = ScopeStore.FindScopesAsync(
                    authCode.RequestedScopes.Select(s => s.Name)).Result.ToList();

                var claims = authCode.Subject.Claims.Select(x => new Claim(x.Type, x.Value));
                code.Subject = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        claims, authCode.Subject.AuthenticationType, Constants.ClaimTypes.Name, Constants.ClaimTypes.Role));

                return code;
            });

            return Task.FromResult(toReturn);
        }

        public override async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subjectId)
        {
            var toReturn = ExecuteInTransaction(session =>
            {
                var tokens = session.Query<Token>()
                    .Where(t => t.SubjectId == subjectId && t.TokenType == TokenType)
                    .ToList();

                if (!tokens.Any())
                {
                    return new List<ITokenMetadata>();
                }

                var tokenList = new List<ITokenMetadata>();

                foreach (var token in tokens)
                {
                    var authCode = ConvertFromJson<AuthCode>(token.JsonCode);

                    var code = token.Expiry < DateTime.UtcNow ? null : _mapper.Map<AuthorizationCode>(authCode);

                    if (code == null)
                    {
                        continue;
                    }

                    code.Client = ClientStore.FindClientByIdAsync(authCode.ClientId).Result;
                    code.RequestedScopes = ScopeStore.FindScopesAsync(
                        authCode.RequestedScopes.Select(s => s.Name)).Result.ToList();

                    var claims = authCode.Subject.Claims.Select(x => new Claim(x.Type, x.Value));
                    code.Subject = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            claims, authCode.Subject.AuthenticationType, Constants.ClaimTypes.Name, Constants.ClaimTypes.Role));

                    tokenList.Add(code);
                }

                return tokenList;
            });

            return toReturn;
        }

        public override async Task StoreAsync(string key, AuthorizationCode code)
        {
            var nhCode = new Token
            {
                Key = key,
                SubjectId = code.SubjectId,
                ClientId = code.ClientId,
                JsonCode = ConvertToJson<AuthCode>(code),
                Expiry = DateTime.UtcNow.AddSeconds(code.Client.AuthorizationCodeLifetime),
                TokenType = this.TokenType
            };

            ExecuteInTransaction(session =>
            {
                session.Save(nhCode);
            });

            await Task.CompletedTask;
        }
    }
}
