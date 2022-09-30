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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;
using NHibernate.Linq;
using Xunit;
using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using AuthorizationCodeEntity = IdentityServer3.Contrib.Nhibernate.Models.AuthorizationCode;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class AuthorizationCodeStoreTests : BaseStoreTests
    {
        private readonly IAuthorizationCodeStore sut;

        private readonly string testKey = Guid.NewGuid().ToString();
        private AuthorizationCode testCode;
        private Client client;

        private Token nhCode;

        protected AuthorizationCodeStoreTests(IMapper mapper) : base(mapper)
        {
            sut = new AuthorizationCodeStore(Session, ScopeStore, ClientStore, mapper);
        }

        private async Task SetupTestData()
        {
            client = await SetupClientAsync();
            testCode = ObjectCreator.GetAuthorizationCode(
                ObjectCreator.GetSubject(),
                client,
                await SetupScopesAsync(3));
            nhCode = GetToken(testKey, testCode);
        }

        private Token GetToken(string key, AuthorizationCode code)
            => new Token
            {
                Key = key,
                SubjectId = code.SubjectId,
                ClientId = code.ClientId,
                JsonCode = ConvertToJson<AuthorizationCode, AuthorizationCodeEntity>(code),
                Expiry = DateTime.UtcNow.AddSeconds(code.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.AuthorizationCode
            };

        private string GetJsonCodeFromAuthorizationCode(AuthorizationCode code)
        {
            var obj = new
            {
                CreationTime = code.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"),
                Client = new { code.ClientId },
                Subject = new
                {
                    code.Subject.Identity.AuthenticationType,
                    Claims = code.Subject.Claims.Select(x => new { x.Type, x.Value }),
                },
                code.IsOpenId,
                RequestedScopes = code.RequestedScopes.Select(x => new { x.Name }),
                code.RedirectUri,
                code.Nonce,
                code.WasConsentShown,
                code.SessionId,
                code.CodeChallenge,
                code.CodeChallengeMethod,
                code.SubjectId,
                code.ClientId,
                Scopes = code.Scopes.Select(x => x)
            };

            return JsonConvert.SerializeObject(obj);
        }

        [Fact]
        public async Task StoreAsync()
        {
            //Arrange
            await SetupTestData();
            //Act
            await sut.StoreAsync(testKey, testCode);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => t.TokenType == TokenType.AuthorizationCode && t.Key == testKey);

                token.Should().BeEquivalentTo(new
                {
                    TokenType = TokenType.AuthorizationCode,
                    Key = testKey,
                    testCode.ClientId
                });

                //CleanUp
                await session.DeleteAsync(token);
            });
        }

        [Fact]
        public async Task VerifyJsonCodeDataStructure()
        {
            // Setup
            await SetupTestData();
            var expected = GetJsonCodeFromAuthorizationCode(testCode);

            await sut.StoreAsync(testKey, testCode);

            await ExecuteInTransactionAsync(async session =>
            {
                //Act
                var token = await session
                    .Query<Token>()
                    .SingleOrDefaultAsync(t => t.Key == testKey && t.TokenType == TokenType.AuthorizationCode);

                //Assert
                token.Should().NotBeNull();
                token.JsonCode.Should().BeEquivalentTo(expected);
            });

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(nhCode);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            await SetupTestData();

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(nhCode);
            });

            //Act
            var token = await sut.GetAsync(testKey);

            //Assert
            token.Should().BeOfType<AuthorizationCode>()
                .Subject.Should().BeEquivalentTo(
                testCode,
                options => options
                    .IgnoringCyclicReferences()
                    .Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TestConstants.AllowableTimeVariance))
                    .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(nhCode);
                session.Clear();
            });
        }

        [Fact]
        public async Task RemoveAsync()
        {
            await SetupTestData();

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(nhCode);
            });

            //Act
            await sut.RemoveAsync(testKey);

            //Assert
            await ExecuteInTransactionAsync(async session =>
            {
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKey);

                token.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            await SetupTestData();
            var subjectId1 = GetNewGuidString();
            var subjectId2 = GetNewGuidString();

            var scopes = await SetupScopesAsync(3);

            var authCode1 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId1), client, scopes);
            var authCode2 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId1), client, scopes);
            var authCode3 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId2), client, scopes);
            var authCode4 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId2), client, scopes);

            var nhCode1 = GetToken(GetNewGuidString(), authCode1);
            var nhCode2 = GetToken(GetNewGuidString(), authCode2);
            var nhCode3 = GetToken(GetNewGuidString(), authCode3);
            var nhCode4 = GetToken(GetNewGuidString(), authCode4);

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(nhCode1);
                await session.SaveAsync(nhCode2);
                await session.SaveAsync(nhCode3);
                await session.SaveAsync(nhCode4);
            });

            //Act
            var tokens = (await sut.GetAllAsync(subjectId1)).ToList();

            tokens.Should().HaveCount(2)
                .And.AllBeOfType<AuthorizationCode>()
                .And.BeEquivalentTo(
                    new[] { authCode1, authCode2 },
                    options => options
                        .IgnoringCyclicReferences()
                        .Using<DateTimeOffset>(
                            ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TestConstants.AllowableTimeVariance))
                        .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(nhCode1);
                await session.DeleteAsync(nhCode2);
                await session.DeleteAsync(nhCode3);
                await session.DeleteAsync(nhCode4);
            });
        }

        [Fact]
        public async Task RevokeAsync()
        {
            //Arrange
            await SetupTestData();
            var subjectIdToRevoke = GetNewGuidString();
            var clientIdToRevoke = GetNewGuidString();

            var testKeyToRevoke = GetNewGuidString();
            var testCodeToRevoke = ObjectCreator.GetAuthorizationCode(
                ObjectCreator.GetSubject(subjectIdToRevoke), 
                ObjectCreator.GetClient(clientIdToRevoke),
                await SetupScopesAsync(3));
            var nhCodeToRevoke = GetToken(testKeyToRevoke, testCodeToRevoke);

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(nhCode);
                await session.SaveAsync(nhCodeToRevoke);
            });

            //Act
            await sut.RevokeAsync(subjectIdToRevoke, clientIdToRevoke);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var tokenRevoked = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKeyToRevoke);

                var tokenNotRevoked = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKey);

                tokenRevoked.Should().BeNull();
                tokenNotRevoked.Should().BeOfType<Token>()
                    .Subject.Should().BeEquivalentTo(
                    nhCode,
                    options => options
                        .IgnoringCyclicReferences()
                        .Using<DateTimeOffset>(
                            ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TestConstants.AllowableTimeVariance))
                        .WhenTypeIs<DateTimeOffset>());

                //CleanUp
                await session.DeleteAsync(tokenNotRevoked);
            });
        }

        #endregion
    }
}
