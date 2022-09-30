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
using IdentityServer3.Contrib.Nhibernate.Entities;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Contrib.Nhibernate.Stores;
using Newtonsoft.Json;
using NHibernate.Linq;
using Xunit;

using TokenModel = IdentityServer3.Core.Models.Token;
using TokenEntity = IdentityServer3.Contrib.Nhibernate.Models.Token;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class TokenHandleStoreTests : BaseStoreTests
    {
        private readonly TokenHandleStore sut;

        protected TokenHandleStoreTests(IMapper mapper) : base(mapper)
        {
            sut = new TokenHandleStore(Session, ScopeStore, ClientStore, mapper);
        }

        private string GetJsonCodeFromRefreshToken(TokenModel code)
        {
            var obj = new
            {
                code.Audience,
                code.Issuer,
                CreationTime = code.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"),
                code.Lifetime,
                code.Type,
                Client = new
                {
                    code.ClientId
                },
                Claims = code.Claims.Select(x => new { x.Type, x.Value }),
                code.Version,
                code.SubjectId,
                code.ClientId,
                Scopes = code.Scopes.Select(x => x),

            };

            return JsonConvert.SerializeObject(obj);
        }

        [Fact]
        public async Task StoreAsync()
        {
            //Arrange
            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle();

            //Act
            await sut.StoreAsync(testKey, testCode);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t =>
                    t.TokenType == TokenType.TokenHandle &&
                    t.Key == testKey);

                token.Should().BeEquivalentTo(
                    Mapper.Map<Token>(testCode),
                    options => options
                        .Excluding(x => x.Key)
                        .Excluding(x => x.Expiry)
                        .Excluding(x => x.JsonCode)
                        .Excluding(x => x.TokenType)
                        .Excluding(x => x.Id));
                token.Key.Should().Be(testKey);
                // Assume all times stored in the DB are UTC, don't convert
                token.Expiry.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(testCode.Lifetime), new TimeSpan(0, 1, 0));
                token.TokenType.Should().Be(TokenType.TokenHandle);

                //CleanUp
                await session.DeleteAsync(token);
            });
        }

        [Fact]
        public async Task VerifyJsonCodeDataStructure()
        {
            // Setup
            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle();
            var expected = GetJsonCodeFromRefreshToken(testCode);

            await sut.StoreAsync(testKey, testCode);

            Token token = default;

            await ExecuteInTransactionAsync(async session =>
            {
                //Act
                token = await session
                    .Query<Token>()
                    .SingleOrDefaultAsync(t => t.Key == testKey && t.TokenType == TokenType.TokenHandle);

                //Assert
                token.Should().NotBeNull();
                token.JsonCode.Should().Be(expected);
            });

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(token);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            //Arrange
            var testClient = await SetupClientAsync();

            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle(client: testClient);

            var tokenHandle = new Token
            {
                Key = testKey,
                SubjectId = testCode.SubjectId,
                ClientId = testCode.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode),
                Expiry = DateTime.UtcNow.AddSeconds(testCode.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(tokenHandle);
            });

            //Act
            var token = await sut.GetAsync(testKey);

            //Assert
            token.Should().BeOfType<TokenModel>()
                .And.BeEquivalentTo(testCode,
                options => options.Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should()
                        .BeCloseTo(ctx.Expectation, new TimeSpan(0, 0, 1)))
                    .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(tokenHandle);
            });
        }

        [Fact]
        public async Task RemoveAsync()
        {
            //Arrange
            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle();

            var tokenHandle = new Token
            {
                Key = testKey,
                SubjectId = testCode.SubjectId,
                ClientId = testCode.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode),
                Expiry = DateTime.UtcNow.AddSeconds(testCode.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(tokenHandle);
            });

            //Act
            await sut.RemoveAsync(testKey);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKey);

                token.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            var testClient = await SetupClientAsync();

            var subjectId1 = Guid.NewGuid().ToString();
            var subjectId2 = Guid.NewGuid().ToString();

            var testKey1 = Guid.NewGuid().ToString();
            var testCode1 = ObjectCreator.GetTokenHandle(testClient, subjectId1);
            var tokenHandle1 = new Token
            {
                Key = testKey1,
                SubjectId = testCode1.SubjectId,
                ClientId = testCode1.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode1),
                Expiry = DateTime.UtcNow.AddSeconds(testCode1.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            var testKey2 = Guid.NewGuid().ToString();
            var testCode2 = ObjectCreator.GetTokenHandle(testClient, subjectId1);
            var tokenHandle2 = new Token
            {
                Key = testKey2,
                SubjectId = testCode2.SubjectId,
                ClientId = testCode2.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode2),
                Expiry = DateTime.UtcNow.AddSeconds(testCode2.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            var testKey3 = Guid.NewGuid().ToString();
            var testCode3 = ObjectCreator.GetTokenHandle(testClient, subjectId2);
            var tokenHandle3 = new Token
            {
                Key = testKey3,
                SubjectId = testCode3.SubjectId,
                ClientId = testCode3.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode3),
                Expiry = DateTime.UtcNow.AddSeconds(testCode3.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            var testKey4 = Guid.NewGuid().ToString();
            var testCode4 = ObjectCreator.GetTokenHandle(testClient, subjectId2);
            var tokenHandle4 = new Token
            {
                Key = testKey4,
                SubjectId = testCode4.SubjectId,
                ClientId = testCode4.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode4),
                Expiry = DateTime.UtcNow.AddSeconds(testCode4.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveOrUpdateAsync(tokenHandle1);
                await session.SaveOrUpdateAsync(tokenHandle2);
                await session.SaveOrUpdateAsync(tokenHandle3);
                await session.SaveOrUpdateAsync(tokenHandle4);
            });

            //Act
            var tokens = (await sut.GetAllAsync(subjectId1)).ToList();

            //Assert
            tokens.Should().HaveCount(2)
                .And.AllBeOfType<TokenModel>()
                .And.BeEquivalentTo(
                    new[] { testCode1, testCode2 },
                    options => options.Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should()
                        .BeCloseTo(ctx.Expectation, new TimeSpan(0, 0, 1)))
                    .WhenTypeIs<DateTimeOffset>())
                ;

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(tokenHandle1);
                await session.DeleteAsync(tokenHandle2);
                await session.DeleteAsync(tokenHandle3);
                await session.DeleteAsync(tokenHandle4);
            });
        }

        [Fact]
        public async Task RevokeAsync()
        {
            //Arrange
            var subjectIdToRevoke = Guid.NewGuid().ToString();
            var clientIdToRevoke = Guid.NewGuid().ToString();

            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle();

            var tokenHandle = new Token
            {
                Key = testKey,
                SubjectId = testCode.SubjectId,
                ClientId = testCode.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCode),
                Expiry = DateTime.UtcNow.AddSeconds(testCode.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            var testKeyToRevoke = Guid.NewGuid().ToString();
            var testCodeToRevoke = ObjectCreator.GetTokenHandle(subjectIdToRevoke, clientIdToRevoke);

            var tokenHandleToRevoke = new Token
            {
                Key = testKeyToRevoke,
                SubjectId = testCodeToRevoke.SubjectId,
                ClientId = testCodeToRevoke.ClientId,
                JsonCode = ConvertToJson<TokenModel, TokenEntity>(testCodeToRevoke),
                Expiry = DateTime.UtcNow.AddSeconds(testCodeToRevoke.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(tokenHandle);
                await session.SaveAsync(tokenHandleToRevoke);
            });

            //Act
            await sut.RevokeAsync(subjectIdToRevoke, clientIdToRevoke);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var tokenRevoked = await session.Query<Token>()
                    .SingleOrDefaultAsync(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKeyToRevoke);

                var tokenNotRevoked = await session.Query<Token>()
                    .SingleOrDefaultAsync(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKey);

                tokenRevoked.Should().BeNull();
                tokenNotRevoked.Should().BeEquivalentTo(tokenHandle);

                //CleanUp
                await session.DeleteAsync(tokenNotRevoked);
            });
        }

        #endregion
    }
}