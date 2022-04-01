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



using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using Xunit;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class ClientStoreTests : BaseStoreTests
    {
        private readonly IMapper _mapper;

        public ClientStoreTests()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EntitiesProfile>();
            })
                .CreateMapper();
        }

        [Fact]
        public async Task FindClientByIdAsync()
        {
            var clientIdToFind = "ClientIdToFind";

            //Arrange
            var sut = new ClientStore(NhibernateSession, _mapper);
            var testClient1Entity = ObjectCreator.GetClient().ToEntity(_mapper);
            var testClient2Entity = ObjectCreator.GetClient().ToEntity(_mapper);
            var testClient3Entity = ObjectCreator.GetClient().ToEntity(_mapper);

            var testClientToFind = ObjectCreator.GetClient(clientIdToFind);
            var testClientToFindEntity = testClientToFind.ToEntity(_mapper);

            ExecuteInTransaction(session =>
            {
                session.Save(testClient1Entity);
                session.Save(testClient2Entity);
                session.Save(testClient3Entity);
                session.Save(testClientToFindEntity);
            });

            //Act
            var result = await sut.FindClientByIdAsync(clientIdToFind);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(clientIdToFind, result.ClientId);

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(testClient1Entity);
                session.Delete(testClient2Entity);
                session.Delete(testClient3Entity);
                session.Delete(testClientToFindEntity);
            });
        }
    }
}