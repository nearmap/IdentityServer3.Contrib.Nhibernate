﻿/*MIT License
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
using System.Security.Claims;
using IdentityServer3.Core;
using IdentityServer3.Core.Models;
using AutoFixture;
using AutoFixture.AutoMoq;
using Consent = IdentityServer3.Contrib.Nhibernate.Entities.Consent;
using System.Linq;

namespace Core.Nhibernate.IntegrationTests
{
    public static class ObjectCreator
    {
        private static readonly IFixture AFixture = new Fixture()
            .Customize(new AutoMoqCustomization());

        public static AuthorizationCode GetAuthorizationCode(ClaimsPrincipal subject, Client client, IEnumerable<Scope> scopes)
        {
            var codeBuilder = AFixture.Build<AuthorizationCode>()
                .Without(ac => ac.Client)
                .Without(ac => ac.Subject)
                .Without(ac => ac.RequestedScopes)
                .Without(ac => ac.CodeChallengeMethod);

            var code = codeBuilder.Create();

            code.Client = client;
            code.Subject = subject;
            code.RequestedScopes = scopes;
            code.CodeChallengeMethod = Constants.CodeChallengeMethods.Plain;

            return code;
        }

        public static Token GetTokenHandle(string subjectId = null, string clientId = null)
            => GetTokenHandle(GetClient(clientId), subjectId);

        public static Token GetTokenHandle(Client client, string subjectId = null)
        {
            var tokenBuilder = AFixture.Build<Token>()
                .Without(t => t.Client)
                .With(t => t.Claims, new List<Claim>()
                {
                    new Claim(Constants.ClaimTypes.Subject, subjectId ?? Guid.NewGuid().ToString())
                });

            var token = tokenBuilder.Create();

            token.Client = client;

            return token;
        }

        public static RefreshToken GetRefreshToken(Client client, string subjectId = null)
        {
            var tokenBuilder = AFixture.Build<RefreshToken>()
                .Without(rt => rt.Subject)
                .With(rt => rt.CreationTime, DateTime.UtcNow)
                .With(rt => rt.AccessToken, GetAccessToken(subjectId, client));

            var token = tokenBuilder.Create();

            token.Subject = GetSubject(subjectId);

            return token;
        }

        private static Token GetAccessToken(string subjectId, Client client)
        {
            var tokenBuilder = AFixture.Build<Token>()
                .Without(t => t.Client)
                .With(t => t.Claims, new List<Claim>()
                {
                    new Claim(Constants.ClaimTypes.Subject, subjectId ?? Guid.NewGuid().ToString()),
                    new Claim(Constants.ClaimTypes.Scope, AFixture.Create<string>()),
                    new Claim(Constants.ClaimTypes.Scope, AFixture.Create<string>()),
                    new Claim(Constants.ClaimTypes.Scope, AFixture.Create<string>())
                });

            var token = tokenBuilder.Create();

            token.Client = client;

            return token;
        }

        public static ClaimsPrincipal GetSubject(string subjectId = null)
        {
            var claimsIdentity = new ClaimsIdentity(
                authenticationType: "test",
                nameType: Constants.ClaimTypes.Name, 
                roleType: Constants.ClaimTypes.Role);
            claimsIdentity.AddClaim(new Claim(Constants.ClaimTypes.Subject, subjectId ?? Guid.NewGuid().ToString()));
            
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            return claimsPrincipal;
        }

        public static Client GetClient(string clientId = null)
        {
            var clientBuiler = AFixture.Build<Client>()
                .Without(c => c.Claims)
                .With(c => c.ClientId, clientId ?? Guid.NewGuid().ToString());

            var client = clientBuiler.Create();

            client.Claims = new List<Claim>(GetClaims(3));

            return client;
        }

        public static IEnumerable<Claim> GetClaims(int nClaimsToGet)
            => Enumerable.Range(0, nClaimsToGet).Select(x => GetClaim()).ToList();

        public static Claim GetClaim()
            => new Claim(AFixture.Create<string>(), AFixture.Create<string>());

        public static IEnumerable<Scope> GetScopes(int nScopesToGet)
            => AFixture.CreateMany<Scope>(nScopesToGet);

        public static Scope GetScope() => AFixture.Create<Scope>();

        public static Consent GetConsent(string clientId = null, string subject = null)
        {
            var consent = AFixture.Create<Consent>();

            if (clientId != null)
            {
                consent.ClientId = clientId;
            }
                
            if (subject != null)
            {
                consent.Subject = subject;
            }

            return consent;
        }
    }
}