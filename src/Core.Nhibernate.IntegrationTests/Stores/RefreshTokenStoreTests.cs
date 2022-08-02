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
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using NHibernate.Linq;
using Xunit;

using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;
using RefreshTokenEntity = IdentityServer3.Contrib.Nhibernate.Models.RefreshToken;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class RefreshTokenStoreTests : BaseStoreTests
    {
        private readonly IRefreshTokenStore sut;

        private readonly string testKey = Guid.NewGuid().ToString();
        private RefreshToken testCode;
        private Token tokenHandle;

        protected RefreshTokenStoreTests(IDbProfileConfig dbProfile) : base(dbProfile)
        {
            sut = new RefreshTokenStore(Session, ScopeStore, ClientStore, dbProfile);
        }

        private async Task SetupTestData()
        {
            var client = await SetupClientAsync();
            testCode = ObjectCreator.GetRefreshToken(client);
            tokenHandle = GetToken(testKey, testCode);
        }

        private Token GetToken(string key, RefreshToken token)
            => new Token
            {
                Key = key,
                SubjectId = token.SubjectId,
                ClientId = token.ClientId,
                JsonCode = ConvertToJson<RefreshToken, RefreshTokenEntity>(token),
                Expiry = token.CreationTime.UtcDateTime.AddSeconds(token.LifeTime),
                TokenType = TokenType.RefreshToken
            };

        private string GetJsonCodeFromRefreshToken(RefreshToken code)
        {
            var jsonBuilder = new StringBuilder();

            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\",");
            jsonBuilder.Append($"\"CreationTime\":\"{code.CreationTime:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\",");
            jsonBuilder.Append($"\"LifeTime\":{code.LifeTime},");
            jsonBuilder.Append("\"AccessToken\":{");
            jsonBuilder.Append($"\"Audience\":\"{code.AccessToken.Audience}\",");
            jsonBuilder.Append($"\"Issuer\":\"{code.AccessToken.Issuer}\",");
            jsonBuilder.Append($"\"CreationTime\":\"{code.AccessToken.CreationTime:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\",");
            jsonBuilder.Append($"\"Lifetime\":{code.AccessToken.Lifetime},");
            jsonBuilder.Append($"\"Type\":\"{code.AccessToken.Type}\",");
            jsonBuilder.Append("\"Client\":{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\"");
            jsonBuilder.Append("},");
            jsonBuilder.Append("\"Claims\":[");
            
            foreach(var accesTokenClaim in code.AccessToken.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append($"\"Type\":\"{accesTokenClaim.Type}\",");
                jsonBuilder.Append($"\"Value\":\"{accesTokenClaim.Value}\"");
                jsonBuilder.Append("},");
            }
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("],");
            jsonBuilder.Append($"\"Version\":{code.AccessToken.Version},");
            jsonBuilder.Append($"\"SubjectId\":\"{code.SubjectId}\",");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\",");
            jsonBuilder.Append("\"Scopes\":[");
            foreach (var scope in code.AccessToken.Scopes)
            {
                jsonBuilder.Append($"\"{scope}\",");
            }
            // Remove the final comma appended above if it exists
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("]");
            jsonBuilder.Append("},");
            jsonBuilder.Append("\"Subject\":{");
            jsonBuilder.Append($"\"AuthenticationType\":\"{code.Subject.Identity.AuthenticationType}\",");
            jsonBuilder.Append("\"Claims\":[");
            foreach (var claim in code.Subject.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append($"\"Type\":\"{claim.Type}\",");
                jsonBuilder.Append($"\"Value\":\"{claim.Value}\"");
                jsonBuilder.Append("},");
            }
            // Remove the final comma appended above if it exists
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("]");
            jsonBuilder.Append("},");
            jsonBuilder.Append($"\"Version\":{code.Version},");
            jsonBuilder.Append($"\"SubjectId\":\"{code.SubjectId}\",");
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
            await SetupTestData();
            //Act
            await sut.StoreAsync(testKey, testCode);

            await ExecuteInTransactionAsync(async session =>
            {
                //Assert
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.RefreshToken &&
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
                token.Expiry.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(testCode.LifeTime), new TimeSpan(0, 1, 0));
                token.TokenType.Should().Be(TokenType.RefreshToken);

                //CleanUp
                await session.DeleteAsync(token);
            });
        }

        [Fact]
        public async Task VerifyJsonCodeDataStructure()
        {
            // Setup
            await SetupTestData();
            var expected = GetJsonCodeFromRefreshToken(testCode);

            await sut.StoreAsync(testKey, testCode);

            await ExecuteInTransactionAsync(async session =>
            {
                //Act
                var token = await session
                    .Query<Token>()
                    .SingleOrDefaultAsync(t => t.Key == testKey && t.TokenType == TokenType.RefreshToken);

                //Assert
                token.JsonCode.Should().Be(expected);
            });

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(tokenHandle);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            //Arrange
            await SetupTestData();
            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveOrUpdateAsync(tokenHandle);
            });

            //Act
            var resultToken = await sut.GetAsync(testKey);

            //Assert
            resultToken.Should().BeOfType<RefreshToken>()
                .Subject.Should().BeEquivalentTo(
                testCode,
                options => options
                    .IgnoringCyclicReferences()
                    .Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
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
            await SetupTestData();
            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveOrUpdateAsync(tokenHandle);
            });

            //Act
            await sut.RemoveAsync(testKey);

            //Assert
            await ExecuteInTransactionAsync(async session =>
            {
                var token = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKey);

                token.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            await SetupTestData();
            var client = await SetupClientAsync();
            var subjectId1 = GetNewGuidString();
            var subjectId2 = GetNewGuidString();

            var refreshToken1 = ObjectCreator.GetRefreshToken(client, subjectId1);
            var refreshToken2 = ObjectCreator.GetRefreshToken(client, subjectId1);
            var refreshToken3 = ObjectCreator.GetRefreshToken(client, subjectId2);
            var refreshToken4 = ObjectCreator.GetRefreshToken(client, subjectId2);

            var tokenHandle1 = GetToken(GetNewGuidString(), refreshToken1);
            var tokenHandle2 = GetToken(GetNewGuidString(), refreshToken2);
            var tokenHandle3 = GetToken(GetNewGuidString(), refreshToken3);
            var tokenHandle4 = GetToken(GetNewGuidString(), refreshToken4);

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
                .And.AllBeOfType<RefreshToken>()
                .And.BeEquivalentTo(
                new[] { refreshToken1, refreshToken2 },
                options => options
                    .IgnoringCyclicReferences()
                    .Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
                    .WhenTypeIs<DateTimeOffset>());

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
            await SetupTestData();
            var subjectIdToRevoke = GetNewGuidString();
            var clientIdToRevoke = GetNewGuidString();
            var client = await SetupClientAsync(clientIdToRevoke);

            var testKeyToRevoke = GetNewGuidString();
            var testCodeToRevoke = ObjectCreator.GetRefreshToken(client, subjectIdToRevoke);
            var tokenHandleToRevoke = GetToken(testKeyToRevoke, testCodeToRevoke);

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
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKeyToRevoke);

                var tokenNotRevoked = await session.Query<Token>()
                    .SingleOrDefaultAsync(t => 
                    t.TokenType == TokenType.RefreshToken &&
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