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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Entities;
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Contrib.Nhibernate.Stores;
using Xunit;

using TokenModel = IdentityServer3.Core.Models.Token;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class TokenHandleStoreTests : BaseStoreTests
    {
        private readonly TokenHandleStore sut;

        public TokenHandleStoreTests()
        {
            sut = new TokenHandleStore(Session, ScopeStore, ClientStore, Mapper);
        }

        private string GetJsonCodeFromRefreshToken(TokenModel code)
        {
            var jsonBuilder = new StringBuilder();

            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"Audience\":\"{code.Audience}\",");
            jsonBuilder.Append($"\"Issuer\":\"{code.Issuer}\",");
            jsonBuilder.Append($"\"CreationTime\":\"{code.CreationTime:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\",");
            jsonBuilder.Append($"\"Lifetime\":{code.Lifetime},");
            jsonBuilder.Append($"\"Type\":\"{code.Type}\",");
            jsonBuilder.Append("\"Client\":{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\"");
            jsonBuilder.Append("},");
            jsonBuilder.Append("\"Claims\":[");
            foreach (var claim in code.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append("\"Type\":\"sub\",");
                jsonBuilder.Append($"\"Value\":\"{claim.Value}\"");
                jsonBuilder.Append("},");
            }
            // Remove the final comma appended above if it exists
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("],");
            jsonBuilder.Append($"\"Version\":{code.Version},");
            jsonBuilder.Append($"\"SubjectId\":\"{code.SubjectId}\",");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\",");
            jsonBuilder.Append("\"Scopes\":[");
            foreach (var scope in code.Scopes)
            {
                jsonBuilder.Append($"\"{scope}\",");
            }
            // Remove the final comma appended above if it exists
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("]");
            jsonBuilder.Append("}");

            return jsonBuilder.ToString();
        }

        [Fact]
        public async Task StoreAsync()
        {
            //Arrange
            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle();

            //Act
            await sut.StoreAsync(testKey, testCode);

            ExecuteInTransaction(session =>
            {
                //Assert
                var token = session.Query<Token>()
                    .SingleOrDefault(t =>
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
                token.Expiry.ToUniversalTime().Should().BeCloseTo(DateTime.UtcNow.AddSeconds(testCode.Lifetime), new TimeSpan(0, 1, 0));
                token.TokenType.Should().Be(TokenType.TokenHandle);

                //CleanUp
                session.Delete(token);
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

            ExecuteInTransaction(session =>
            {
                //Act
                token = session
                    .Query<Token>()
                    .SingleOrDefault(t => t.Key == testKey && t.TokenType == TokenType.TokenHandle);

                //Assert
                token.Should().NotBeNull();
                token.JsonCode.Should().Be(expected);
            });

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(token);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            //Arrange
            var testClient = SetupClient();

            var testKey = Guid.NewGuid().ToString();
            var testCode = ObjectCreator.GetTokenHandle(client: testClient);

            var tokenHandle = new Token
            {
                Key = testKey,
                SubjectId = testCode.SubjectId,
                ClientId = testCode.ClientId,
                JsonCode = ConvertToJson(testCode),
                Expiry = DateTime.UtcNow.AddSeconds(testCode.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            ExecuteInTransaction(session =>
            {
                session.Save(tokenHandle);
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
            ExecuteInTransaction(session =>
            {
                session.Delete(tokenHandle);
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
                JsonCode = ConvertToJson(testCode),
                Expiry = DateTime.UtcNow.AddSeconds(testCode.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            ExecuteInTransaction(session =>
            {
                session.Save(tokenHandle);
            });

            //Act
            await sut.RemoveAsync(testKey);

            ExecuteInTransaction(session =>
            {
                //Assert
                var token = session.Query<Token>()
                    .SingleOrDefault(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKey);

                token.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            var testClient = SetupClient();

            var subjectId1 = Guid.NewGuid().ToString();
            var subjectId2 = Guid.NewGuid().ToString();

            var testKey1 = Guid.NewGuid().ToString();
            var testCode1 = ObjectCreator.GetTokenHandle(testClient, subjectId1);
            var tokenHandle1 = new Token
            {
                Key = testKey1,
                SubjectId = testCode1.SubjectId,
                ClientId = testCode1.ClientId,
                JsonCode = ConvertToJson(testCode1),
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
                JsonCode = ConvertToJson(testCode2),
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
                JsonCode = ConvertToJson(testCode3),
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
                JsonCode = ConvertToJson(testCode4),
                Expiry = DateTime.UtcNow.AddSeconds(testCode4.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            ExecuteInTransaction(session =>
            {
                session.SaveOrUpdate(tokenHandle1);
                session.SaveOrUpdate(tokenHandle2);
                session.SaveOrUpdate(tokenHandle3);
                session.SaveOrUpdate(tokenHandle4);
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
            ExecuteInTransaction(session =>
            {
                session.Delete(tokenHandle1);
                session.Delete(tokenHandle2);
                session.Delete(tokenHandle3);
                session.Delete(tokenHandle4);
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
                JsonCode = ConvertToJson(testCode),
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
                JsonCode = ConvertToJson(testCodeToRevoke),
                Expiry = DateTime.UtcNow.AddSeconds(testCodeToRevoke.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.TokenHandle
            };

            ExecuteInTransaction(session =>
            {
                session.Save(tokenHandle);
                session.Save(tokenHandleToRevoke);
            });

            //Act
            await sut.RevokeAsync(subjectIdToRevoke, clientIdToRevoke);

            ExecuteInTransaction(session =>
            {
                //Assert
                var tokenRevoked = session.Query<Token>()
                    .SingleOrDefault(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKeyToRevoke);

                var tokenNotRevoked = session.Query<Token>()
                    .SingleOrDefault(t =>
                        t.TokenType == TokenType.TokenHandle &&
                        t.Key == testKey);

                tokenRevoked.Should().BeNull();
                tokenNotRevoked.Should().BeEquivalentTo(tokenHandle);

                //CleanUp
                session.Delete(tokenNotRevoked);
            });
        }

        #endregion
    }
}