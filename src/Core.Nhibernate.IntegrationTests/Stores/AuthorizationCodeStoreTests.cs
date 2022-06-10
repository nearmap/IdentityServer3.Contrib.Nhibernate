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
using Xunit;
using Token = IdentityServer3.Contrib.Nhibernate.Entities.Token;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class AuthorizationCodeStoreTests : BaseStoreTests
    {
        private readonly IAuthorizationCodeStore sut;

        private readonly string testKey = Guid.NewGuid().ToString();
        private readonly AuthorizationCode testCode;
        private readonly Client client;

        private readonly Token nhCode;

        public AuthorizationCodeStoreTests()
        {
            client = SetupClient();
            testCode = ObjectCreator.GetAuthorizationCode(
                ObjectCreator.GetSubject(),
                client,
                SetupScopes(3));
            sut = new AuthorizationCodeStore(Session, ScopeStore, ClientStore, Mapper);

            nhCode = GetToken(testKey, testCode);
        }

        private Token GetToken(string key, AuthorizationCode code)
            => new Token
            {
                Key = key,
                SubjectId = code.SubjectId,
                ClientId = code.ClientId,
                JsonCode = ConvertToJson(code),
                Expiry = DateTime.UtcNow.AddSeconds(code.Client.AuthorizationCodeLifetime),
                TokenType = TokenType.AuthorizationCode
            };

        private string GetJsonCodeFromAuthorizationCode(AuthorizationCode code)
        {
            var jsonBuilder = new StringBuilder();

            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"CreationTime\":\"{code.CreationTime:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\",");
            jsonBuilder.Append("\"Client\":{");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\"");
            jsonBuilder.Append("},");
            jsonBuilder.Append("\"Subject\":{");
            jsonBuilder.Append($"\"AuthenticationType\":\"{code.Subject.Identity.AuthenticationType}\",");
            jsonBuilder.Append("\"Claims\":[");
            foreach (var claim in code.Subject.Claims)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append("\"Type\":\"sub\",");
                jsonBuilder.Append($"\"Value\":\"{claim.Value}\"");
                jsonBuilder.Append("}");
            }
            jsonBuilder.Append("]");
            jsonBuilder.Append("},");
	        jsonBuilder.Append($"\"IsOpenId\":{code.IsOpenId.ToString().ToLowerInvariant()},");
            jsonBuilder.Append("\"RequestedScopes\":[");
            foreach(var reqScope in code.RequestedScopes)
            {
                jsonBuilder.Append("{");
                jsonBuilder.Append($"\"Name\":\"{reqScope.Name}\"");
                jsonBuilder.Append("},");
            }
            // Remove the final comma appended above if it exists
            RemoveTrailingComma(jsonBuilder);
            jsonBuilder.Append("],");
            jsonBuilder.Append($"\"RedirectUri\":\"{code.RedirectUri}\",");
            jsonBuilder.Append($"\"Nonce\":\"{code.Nonce}\",");
            jsonBuilder.Append($"\"WasConsentShown\":{code.WasConsentShown.ToString().ToLowerInvariant()},");
            jsonBuilder.Append($"\"SessionId\":\"{code.SessionId}\",");
            jsonBuilder.Append($"\"CodeChallenge\":\"{code.CodeChallenge}\",");
            jsonBuilder.Append($"\"CodeChallengeMethod\":\"{code.CodeChallengeMethod}\",");
            jsonBuilder.Append($"\"SubjectId\":\"{code.SubjectId}\",");
            jsonBuilder.Append($"\"ClientId\":\"{code.ClientId}\",");
            jsonBuilder.Append("\"Scopes\":[");
            foreach(var scope in code.Scopes)
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
                    .SingleOrDefault(t => t.TokenType == TokenType.AuthorizationCode && t.Key == testKey);

                token.Should().NotBeNull();
                token.TokenType.Should().Be(TokenType.AuthorizationCode);
                token.Key.Should().Be(testKey);
                token.ClientId.Should().Be(testCode.ClientId);

                //CleanUp
                session.Delete(token);
            });
        }

        [Fact]
        public async Task VerifyJsonCodeDataStructure()
        {
            // Setup
            var expected = GetJsonCodeFromAuthorizationCode(testCode);

            await sut.StoreAsync(testKey, testCode);

            ExecuteInTransaction(session =>
            {
                //Act
                var token = session
                    .Query<Token>()
                    .SingleOrDefault(t => t.Key == testKey && t.TokenType == TokenType.AuthorizationCode);

                //Assert
                token.Should().NotBeNull();
                token.JsonCode.Should().BeEquivalentTo(expected);
            });

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(nhCode);
                session.Clear();
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            ExecuteInTransaction(session =>
            {
                session.Save(nhCode);
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
                        ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
                    .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(nhCode);
                session.Clear();
            });
        }

        [Fact]
        public async Task RemoveAsync()
        {
            ExecuteInTransaction(session =>
            {
                session.Save(nhCode);
            });

            //Act
            await sut.RemoveAsync(testKey);

            //Assert
            ExecuteInTransaction(session =>
            {
                var token = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKey);

                token.Should().BeNull();
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            var subjectId1 = GetNewGuidString();
            var subjectId2 = GetNewGuidString();

            var scopes = SetupScopes(3);

            var authCode1 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId1), client, scopes);
            var authCode2 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId1), client, scopes);
            var authCode3 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId2), client, scopes);
            var authCode4 = ObjectCreator.GetAuthorizationCode(ObjectCreator.GetSubject(subjectId2), client, scopes);

            var nhCode1 = GetToken(GetNewGuidString(), authCode1);
            var nhCode2 = GetToken(GetNewGuidString(), authCode2);
            var nhCode3 = GetToken(GetNewGuidString(), authCode3);
            var nhCode4 = GetToken(GetNewGuidString(), authCode4);

            ExecuteInTransaction(session =>
            {
                session.Save(nhCode1);
                session.Save(nhCode2);
                session.Save(nhCode3);
                session.Save(nhCode4);
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
                            ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
                        .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(nhCode1);
                session.Delete(nhCode2);
                session.Delete(nhCode3);
                session.Delete(nhCode4);
            });
        }

        [Fact]
        public async Task RevokeAsync()
        {
            //Arrange
            var subjectIdToRevoke = GetNewGuidString();
            var clientIdToRevoke = GetNewGuidString();

            var testKeyToRevoke = GetNewGuidString();
            var testCodeToRevoke = ObjectCreator.GetAuthorizationCode(
                ObjectCreator.GetSubject(subjectIdToRevoke), 
                ObjectCreator.GetClient(clientIdToRevoke),
                SetupScopes(3));
            var nhCodeToRevoke = GetToken(testKeyToRevoke, testCodeToRevoke);

            ExecuteInTransaction(session =>
            {
                session.Save(nhCode);
                session.Save(nhCodeToRevoke);
            });

            //Act
            await sut.RevokeAsync(subjectIdToRevoke, clientIdToRevoke);

            ExecuteInTransaction(session =>
            {
                //Assert
                var tokenRevoked = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKeyToRevoke);

                var tokenNotRevoked = session.Query<Token>()
                    .SingleOrDefault(t => 
                    t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKey);

                tokenRevoked.Should().BeNull();
                tokenNotRevoked.Should().BeOfType<Token>()
                    .Subject.Should().BeEquivalentTo(
                    nhCode,
                    options => options
                        .IgnoringCyclicReferences()
                        .Using<DateTimeOffset>(
                            ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
                        .WhenTypeIs<DateTimeOffset>());

                //CleanUp
                session.Delete(tokenNotRevoked);
            });
        }

        #endregion
    }
}
