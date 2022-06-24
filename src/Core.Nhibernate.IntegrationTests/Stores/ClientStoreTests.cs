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
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Services;
using Xunit;

using ClientEntity = IdentityServer3.Contrib.Nhibernate.Entities.Client;
using ClientModel = IdentityServer3.Core.Models.Client;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class ClientStoreTests : BaseStoreTests
    {
        private readonly IClientStore sut;

        private readonly ClientEntity testClient1Entity;
        private readonly ClientEntity testClient2Entity;
        private readonly ClientEntity testClient3Entity;

        public ClientStoreTests()
        {
            sut = new ClientStore(Session, Mapper);

            testClient1Entity = Mapper.Map<ClientModel, ClientEntity>(ObjectCreator.GetClient());
            testClient2Entity = Mapper.Map<ClientModel, ClientEntity>(ObjectCreator.GetClient());
            testClient3Entity = Mapper.Map<ClientModel, ClientEntity>(ObjectCreator.GetClient());
        }

        [Fact]
        public async Task FindClientByIdAsync()
        {
            //Arrange
            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(testClient1Entity);
                await session.SaveAsync(testClient2Entity);
                await session.SaveAsync(testClient3Entity);
            });
            var testClientToFind = ObjectCreator.GetClient();
            var testClientToFindEntity = Mapper.Map<ClientModel, ClientEntity>(testClientToFind);

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(testClientToFindEntity);
            });

            //Act
            var result = await sut.FindClientByIdAsync(testClientToFind.ClientId);

            //Assert
            result.Should().BeEquivalentTo(
                testClientToFind,
                options => options
                    .IgnoringCyclicReferences()
                    .Using<DateTimeOffset>(
                        ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, new TimeSpan(10)))
                    .WhenTypeIs<DateTimeOffset>());

            //CleanUp
            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(testClientToFindEntity);
                await session.DeleteAsync(testClient1Entity);
                await session.DeleteAsync(testClient2Entity);
                await session.DeleteAsync(testClient3Entity);
            });
        }
    }
}