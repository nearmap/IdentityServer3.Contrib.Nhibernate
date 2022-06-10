﻿/*MIT License
*
*Copyright (c) 2016 Ricardo Santos
*Copyright (c) 2022 Jason Bridgman
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
using FluentAssertions;
using IdentityServer3.Contrib.Nhibernate.Entities;
using IdentityServer3.Contrib.Nhibernate.Stores;
using IdentityServer3.Core.Services;
using Xunit;

namespace Core.Nhibernate.IntegrationTests.Stores
{
    public class ConsentStoreTests : BaseStoreTests, IDisposable
    {
        private readonly IConsentStore sut;

        private Consent testConsent1 = ObjectCreator.GetConsent();
        private Consent testConsent2 = ObjectCreator.GetConsent();
        private bool disposedValue;
        private readonly Consent testConsent3 = ObjectCreator.GetConsent();

        public ConsentStoreTests()
        {
            sut = new ConsentStore(Session, Mapper);

            ExecuteInTransaction(session =>
            {
                session.Save(testConsent3);
            });
        }

        [Fact]
        public async Task LoadAsync()
        {
            //Arrange
            ExecuteInTransaction(session =>
            {
                session.Save(testConsent1);
                session.Save(testConsent2);
            });

            //Act
            var result = await sut.LoadAsync(testConsent1.Subject, testConsent1.ClientId);

            //Assert
            result.Should().BeEquivalentTo(Mapper.Map<IdentityServer3.Core.Models.Consent>(testConsent1));
        }

        [Fact]
        public async Task LoadAllAsync()
        {
            //Arrange
            var subject = Guid.NewGuid().ToString();
            testConsent1 = ObjectCreator.GetConsent(null, subject);
            testConsent2 = ObjectCreator.GetConsent(null, subject);

            ExecuteInTransaction(session =>
            {
                session.Save(testConsent1);
                session.Save(testConsent2);
            });

            //Act
            var result = (await sut.LoadAllAsync(subject))
                .ToList();

            //Assert
            result.Should().BeEquivalentTo(Mapper.Map<IEnumerable<IdentityServer3.Core.Models.Consent>>(new[] { testConsent1, testConsent2 }));
        }

        [Fact]
        public async Task UpdateAsync_WithNewId()
        {
            var updatedClientId = "updatedClientId";

            //Arrange
            ExecuteInTransaction(session =>
            {
                session.Save(testConsent1);
                session.Save(testConsent2);
            });

            var modelToUpdate = await sut.LoadAsync(testConsent1.Subject, testConsent1.ClientId);

            //Act
            modelToUpdate.ClientId = updatedClientId;
            await sut.UpdateAsync(modelToUpdate);

            ExecuteInTransaction(session =>
            {
                //Assert
                var updatedEntity = session.Query<Consent>()
                    .SingleOrDefault(c => c.ClientId == modelToUpdate.ClientId && c.Subject == modelToUpdate.Subject);

                updatedEntity.Should().BeEquivalentTo(
                    Mapper.Map<Consent>(modelToUpdate),
                    options => options.Excluding(x => x.Id));

                //CleanUp
                session.Delete(updatedEntity);
            });
        }

        [Fact]
        public async Task UpdateAsync_WithUpdatedScopes()
        {
            //Arrange
            ExecuteInTransaction(session =>
            {
                session.Save(testConsent1);
                session.Save(testConsent2);
            });

            var modelToUpdate = await sut.LoadAsync(testConsent1.Subject, testConsent1.ClientId);
            modelToUpdate.Scopes = ObjectCreator.GetScopes(5).Select(s => s.Name);

            //Act
            await sut.UpdateAsync(modelToUpdate);

            //Assert
            var updatedModel = await sut.LoadAsync(modelToUpdate.Subject, modelToUpdate.ClientId);

            updatedModel.Should().BeEquivalentTo(
                Mapper.Map<IdentityServer3.Core.Models.Consent>(modelToUpdate));
        }

        [Fact]
        public async Task RevokeAsync()
        {
            //Arrange
            var consentToRevoke = ObjectCreator.GetConsent();

            ExecuteInTransaction(session =>
            {
                session.Save(testConsent1);
                session.Save(testConsent2);
                session.Save(consentToRevoke);
            });

            //Act
            await sut.RevokeAsync(consentToRevoke.Subject, consentToRevoke.ClientId);

            //Assert
            var revokedConsent = await sut.LoadAsync(consentToRevoke.Subject, consentToRevoke.ClientId);

            revokedConsent.Should().BeNull();
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
                        session.Delete(testConsent1);
                        session.Delete(testConsent2);
                        session.Delete(testConsent3);
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