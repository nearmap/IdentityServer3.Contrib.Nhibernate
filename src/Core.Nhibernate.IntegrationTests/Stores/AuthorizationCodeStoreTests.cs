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
using System.Threading.Tasks;
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
        private readonly AuthorizationCode testCode = ObjectCreator.GetAuthorizationCode();

        private readonly Token nhCode;

        public AuthorizationCodeStoreTests()
        {
            sut = new AuthorizationCodeStore(Session, ScopeStoreMock.Object, ClientStoreMock.Object, Mapper);

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

        [Fact]
        public async Task StoreAsync()
        {
            //Act
            await sut.StoreAsync(testKey, testCode);

            ExecuteInTransaction(session =>
            {
                //Assert
                var token = session.Query<Token>()
                    .SingleOrDefault(t => t.TokenType == TokenType.AuthorizationCode &&
                    t.Key == testKey);

                Assert.NotNull(token);

                //CleanUp
                session.Delete(token);
            });
        }

        #region BaseTokenStore

        [Fact]
        public async Task GetAsync()
        {
            SetupScopeStoreMock();

            ExecuteInTransaction(session =>
            {
                session.Save(nhCode);
            });

            //Act
            var token = await sut.GetAsync(testKey);

            //Assert
            Assert.NotNull(token);

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

                Assert.Null(token);
            });
        }

        [Fact]
        public async Task GetAllAsync()
        {
            //Arrange
            var subjectId1 = GetNewGuidString();
            var subjectId2 = GetNewGuidString();

            var nhCode1 = GetToken(GetNewGuidString(), ObjectCreator.GetAuthorizationCode(subjectId1));
            var nhCode2 = GetToken(GetNewGuidString(), ObjectCreator.GetAuthorizationCode(subjectId1));
            var nhCode3 = GetToken(GetNewGuidString(), ObjectCreator.GetAuthorizationCode(subjectId2));
            var nhCode4 = GetToken(GetNewGuidString(), ObjectCreator.GetAuthorizationCode(subjectId2));

            SetupScopeStoreMock();

            ExecuteInTransaction(session =>
            {
                session.Save(nhCode1);
                session.Save(nhCode2);
                session.Save(nhCode3);
                session.Save(nhCode4);
            });

            //Act
            var tokens = (await sut.GetAllAsync(subjectId1)).ToList();

            //Assert
            Assert.True(tokens.Count == 2);
            Assert.True(tokens.All(t => t.SubjectId == subjectId1));

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
            var testCodeToRevoke = ObjectCreator.GetAuthorizationCode(subjectIdToRevoke, clientIdToRevoke);
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

                Assert.Null(tokenRevoked);
                Assert.NotNull(tokenNotRevoked);

                //CleanUp
                session.Delete(tokenNotRevoked);
            });
        }

        #endregion
    }
}
