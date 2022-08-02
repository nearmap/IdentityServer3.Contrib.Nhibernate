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



using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Xunit;

using Entities = IdentityServer3.Contrib.Nhibernate.Entities;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public abstract class ScopeStoreTests : BaseStoreTests
    {
        private readonly IScopeStore sut;
        private readonly Scope testScope1 = ObjectCreator.GetScope();
        private readonly Scope testScope2 = ObjectCreator.GetScope();
        private readonly Scope testScope3 = ObjectCreator.GetScope();
        private readonly Entities.Scope testScope1Entity;
        private readonly Entities.Scope testScope2Entity;
        private readonly Entities.Scope testScope3Entity;

        protected ScopeStoreTests(IDbProfileConfig dbProfile) : base(dbProfile)
        {
            sut = new ScopeStore(Session, dbProfile);
            testScope1Entity = Mapper.Map<Scope, Entities.Scope>(testScope1);
            testScope2Entity = Mapper.Map<Scope, Entities.Scope>(testScope2);
            testScope3Entity = Mapper.Map<Scope, Entities.Scope>(testScope3);
        }

        [Fact]
        public async Task FindScopesAsync()
        {
            //Arrange
            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(testScope1Entity);
                await session.SaveAsync(testScope2Entity);
                await session.SaveAsync(testScope3Entity);
            });

            //Act
            var result = (await sut.FindScopesAsync(new List<string>
                {
                    testScope1.Name,
                    testScope2.Name
                }))
                .ToList();

            var scopeNames = result.Select(s => s.Name).ToList();

            //Assert
            scopeNames.Should().BeEquivalentTo(new[] { testScope1.Name, testScope2.Name });

            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(testScope1Entity);
                await session.DeleteAsync(testScope2Entity);
                await session.DeleteAsync(testScope3Entity);
            });
        }

        [Fact]
        public async Task GetScopesAsync()
        {
            //Arrange
            testScope1Entity.ShowInDiscoveryDocument = true;
            testScope2Entity.ShowInDiscoveryDocument = true;
            testScope3Entity.ShowInDiscoveryDocument = false;

            await ExecuteInTransactionAsync(async session =>
            {
                await session.SaveAsync(testScope1Entity);
                await session.SaveAsync(testScope2Entity);
                await session.SaveAsync(testScope3Entity);
            });

            //Act
            var result = (await sut.GetScopesAsync())
                .ToList();

            var scopeNames = result.Select(s => s.Name).ToList();

            //Assert
            scopeNames
                .Should().Contain(testScope1.Name)
                .And.Subject
                .Should().Contain(testScope2.Name);

            await ExecuteInTransactionAsync(async session =>
            {
                await session.DeleteAsync(testScope1Entity);
                await session.DeleteAsync(testScope2Entity);
                await session.DeleteAsync(testScope3Entity);
            });
        }
    }
}