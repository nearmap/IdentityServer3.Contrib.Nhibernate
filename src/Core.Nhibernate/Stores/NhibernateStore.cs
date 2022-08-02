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
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using IdentityServer3.Core.Models;
using NHibernate;

namespace IdentityServer3.Contrib.Nhibernate.Stores
{
    public abstract class NhibernateStore
    {
        protected readonly IMapper _mapper;
        private readonly ISession _nhSession;

        protected NhibernateStore(ISession session, IDbProfileConfig dbProfile)
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(dbProfile?.GetProfile() ?? new EntitiesProfile());
            })
                .CreateMapper();

            _nhSession = session ?? throw new ArgumentNullException(nameof(session));
        }

        protected async Task<object> SaveAsync(object obj)
            => await ExecuteInTransactionAsync(session => session.SaveAsync(obj));

        [Obsolete("Use ExecuteInTransactionAsync method instead")]
        protected void ExecuteInTransaction(Action<ISession> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            => ExecuteInTransactionAsync(
                session => { actionToExecute(session); return Task.CompletedTask; },
                isolationLevel)
            .GetAwaiter().GetResult();

        [Obsolete("Use ExecuteInTransactionAsync method instead")]
        protected T ExecuteInTransaction<T>(Func<ISession, T> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            => ExecuteInTransactionAsync(
                session => { return Task.FromResult(actionToExecute(session)); },
                isolationLevel)
                .GetAwaiter().GetResult();

        protected async Task ExecuteInTransactionAsync(Func<ISession, Task> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var transaction = _nhSession.Transaction;

            if (transaction != null && transaction.IsActive)
            {
                try
                {
                    await actionToExecute(_nhSession);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                using (var tx = _nhSession.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        await actionToExecute(_nhSession);
                        await tx.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }

            }
        }

        protected async Task<T> ExecuteInTransactionAsync<T>(Func<ISession, Task<T>> actionToExecute, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            var transaction = _nhSession.Transaction;

            if (transaction != null && transaction.IsActive)
            {
                try
                {
                    return await actionToExecute(_nhSession);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                using (var tx = _nhSession.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        var result = await actionToExecute(_nhSession);
                        await tx.CommitAsync();
                        return result;
                    }
                    catch (Exception)
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}
