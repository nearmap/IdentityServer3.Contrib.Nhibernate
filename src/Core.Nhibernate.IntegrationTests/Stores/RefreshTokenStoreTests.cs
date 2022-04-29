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
using IdentityServer3.Contrib.Nhibernate.Enums;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Xunit;

using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class RefreshTokenStoreTests : BaseStoreTests
    {
        private readonly IRefreshTokenStore sut;

        private readonly string testKey = Guid.NewGuid().ToString();
        private readonly RefreshToken testCode = ObjectCreator.GetRefreshToken();
        private readonly Token tokenHandle;

        public RefreshTokenStoreTests()
        {
            sut = new RefreshTokenStore(Session, ScopeStoreMock.Object, ClientStoreMock.Object, Mapper);

            tokenHandle = GetToken(testKey, testCode);
        }

        private Token GetToken(string key, RefreshToken token)
            => new Token
            {
                Key = key,
                SubjectId = token.SubjectId,
                ClientId = token.ClientId,
                JsonCode = ConvertToJson(token),
                Expiry = token.CreationTime.UtcDateTime.AddSeconds(token.LifeTime),
                TokenType = TokenType.RefreshToken
            };

        private string GetJsonCodeFromRefreshToken(RefreshToken code)
        {
            var jsonBuilder = new StringBuilder();

            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\",");
            jsonBuilder.Append($"\"CreationTime\":\"{code.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz")}\",");
            jsonBuilder.Append($"\"LifeTime\":{code.LifeTime},");
            jsonBuilder.Append("\"AccessToken\":{");
            jsonBuilder.Append($"\"Audience\":\"{code.AccessToken.Audience}\",");
            jsonBuilder.Append($"\"Issuer\":\"{code.AccessToken.Issuer}\",");
            jsonBuilder.Append($"\"CreationTime\":\"{code.AccessToken.CreationTime:yyyy-MM-ddTHH:mm:ss.fffffffzzz}\",");
            jsonBuilder.Append($"\"Lifetime\":{code.AccessToken.Lifetime},");
            jsonBuilder.Append($"\"Type\":\"{code.AccessToken.Type}\",");
            jsonBuilder.Append("\"Client\":{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\"");
            jsonBuilder.Append("},");
            jsonBuilder.Append("\"Claims\":[");
            
            foreach(var accesTokenClaim in code.AccessToken.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append("\"Type\":\"sub\",");
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
            jsonBuilder.Append($"\"AuthenticationType\":null,");
            jsonBuilder.Append("\"Claims\":[");
            foreach (var claim in code.Subject.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append("\"Type\":\"sub\",");
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
            //Act
            await sut.StoreAsync(testKey, testCode);

            ExecuteInTransaction(session =>
            {
                //Assert
                var token = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKey);

                Assert.NotNull(token);

                //CleanUp
                session.Delete(token);
            });
        }

        [Fact]
        public async Task VerifyJsonCodeDataStructure()
        {
            // Setup
            var expected = GetJsonCodeFromRefreshToken(testCode);

            await sut.StoreAsync(testKey, testCode);

            ExecuteInTransaction(session =>
            {
                //Act
                var token = session
                    .Query<Token>()
                    .SingleOrDefault(t => t.Key == testKey && t.TokenType == TokenType.RefreshToken);

                //Assert
                Assert.NotNull(token);
                Assert.Equal(expected, token.JsonCode);
            });

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(tokenHandle);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            //Arrange
            SetupScopeStoreMock();

            ExecuteInTransaction(session =>
            {
                session.SaveOrUpdate(tokenHandle);
            });

            //Act
            var resultToken = await sut.GetAsync(testKey);

            //Assert
            Assert.NotNull(resultToken);

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
            ExecuteInTransaction(session =>
            {
                session.SaveOrUpdate(tokenHandle);
            });

            //Act
            await sut.RemoveAsync(testKey);

            //Assert
            ExecuteInTransaction(session =>
            {
                var token = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKey);

                Assert.Null(token);
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            var subjectId1 = GetNewGuidString();
            var subjectId2 = GetNewGuidString();

            var tokenHandle1 = GetToken(GetNewGuidString(), ObjectCreator.GetRefreshToken(subjectId1));
            var tokenHandle2 = GetToken(GetNewGuidString(), ObjectCreator.GetRefreshToken(subjectId1));
            var tokenHandle3 = GetToken(GetNewGuidString(), ObjectCreator.GetRefreshToken(subjectId2));
            var tokenHandle4 = GetToken(GetNewGuidString(), ObjectCreator.GetRefreshToken(subjectId2));

            SetupScopeStoreMock();

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
            Assert.True(tokens.Count == 2);
            Assert.True(tokens.All(t => t.SubjectId == subjectId1));

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
            var subjectIdToRevoke = GetNewGuidString();
            var clientIdToRevoke = GetNewGuidString();

            var testKeyToRevoke = GetNewGuidString();
            var testCodeToRevoke = ObjectCreator.GetRefreshToken(subjectIdToRevoke, clientIdToRevoke);
            var tokenHandleToRevoke = GetToken(testKeyToRevoke, testCodeToRevoke);

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
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKeyToRevoke);

                var tokenNotRevoked = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.RefreshToken &&
                    t.Key == testKey);

                Assert.Null(tokenRevoked);
                Assert.NotNull(tokenNotRevoked);

                //CleanUp
                session.Delete(tokenNotRevoked);
            });
        }

        #endregion
    }
}