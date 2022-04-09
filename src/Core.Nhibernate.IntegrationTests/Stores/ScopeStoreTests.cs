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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Xunit;

using Entities = IdentityServer3.Contrib.Nhibernate.Entities;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class ScopeStoreTests : BaseStoreTests, IDisposable
    {
        private readonly IScopeStore sut;
        private readonly Scope testScope1 = ObjectCreator.GetScope();
        private readonly Scope testScope2 = ObjectCreator.GetScope();
        private readonly Scope testScope3 = ObjectCreator.GetScope();
        private readonly Entities.Scope testScope1Entity;
        private readonly Entities.Scope testScope2Entity;
        private readonly Entities.Scope testScope3Entity;
        private bool disposedValue;

        public ScopeStoreTests()
        {
            sut = new ScopeStore(Session, Mapper);
            testScope1Entity = Mapper.Map<Scope, Entities.Scope>(testScope1);
            testScope2Entity = Mapper.Map<Scope, Entities.Scope>(testScope2);
            testScope3Entity = Mapper.Map<Scope, Entities.Scope>(testScope3);
        }

        [Fact]
        public async Task FindScopesAsync()
        {
            //Arrange
            ExecuteInTransaction(session =>
            {
                session.Save(testScope1Entity);
                session.Save(testScope2Entity);
                session.Save(testScope3Entity);
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
            Assert.Contains(testScope1.Name, scopeNames);
            Assert.Contains(testScope2.Name, scopeNames);
            Assert.DoesNotContain(testScope3.Name, scopeNames);
        }

        [Fact]
        public async Task GetScopesAsync()
        {
            //Arrange
            testScope1Entity.ShowInDiscoveryDocument = true;
            testScope2Entity.ShowInDiscoveryDocument = true;
            testScope3Entity.ShowInDiscoveryDocument = false;

            ExecuteInTransaction(session =>
            {
                session.Save(testScope1Entity);
                session.Save(testScope2Entity);
                session.Save(testScope3Entity);
            });

            //Act
            var result = (await sut.GetScopesAsync())
                .ToList();

            var scopeNames = result.Select(s => s.Name).ToList();

            //Assert
            Assert.Contains(testScope1.Name, scopeNames);
            Assert.Contains(testScope2.Name, scopeNames);
            Assert.DoesNotContain(testScope3.Name, scopeNames);
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
                        session.Delete(testScope1Entity);
                        session.Delete(testScope2Entity);
                        session.Delete(testScope3Entity);
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