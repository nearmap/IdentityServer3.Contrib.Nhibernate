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
using System.Threading.Tasks;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Xunit;

using Client = IdentityServer3.Contrib.Nhibernate.Entities.Client;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class ClientStoreTests : BaseStoreTests, IDisposable
    {
        private readonly IClientStore sut;

        private readonly Client testClient1Entity;
        private readonly Client testClient2Entity;
        private readonly Client testClient3Entity;

        private readonly string _clientIdToFind = "ClientIdToFind";
        private bool disposedValue;

        public ClientStoreTests()
        {
            sut = new ClientStore(Session, Mapper);

            testClient1Entity = ObjectCreator.GetClient().ToEntity(Mapper);
            testClient2Entity = ObjectCreator.GetClient().ToEntity(Mapper);
            testClient3Entity = ObjectCreator.GetClient().ToEntity(Mapper);

            ExecuteInTransaction(session =>
            {
                session.Save(testClient1Entity);
                session.Save(testClient2Entity);
                session.Save(testClient3Entity);
            });
        }

        [Fact]
        public async Task FindClientByIdAsync()
        {
            //Arrange
            var testClientToFind = ObjectCreator.GetClient(_clientIdToFind);
            var testClientToFindEntity = testClientToFind.ToEntity(Mapper);

            ExecuteInTransaction(session =>
            {
                session.Save(testClientToFindEntity);
            });

            //Act
            var result = await sut.FindClientByIdAsync(_clientIdToFind);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(_clientIdToFind, result.ClientId);

            //CleanUp
            ExecuteInTransaction(session =>
            {
                session.Delete(testClientToFindEntity);
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //CleanUp
                    ExecuteInTransaction(session =>
                    {
                        session.Delete(testClient1Entity);
                        session.Delete(testClient2Entity);
                        session.Delete(testClient3Entity);
                    });
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}