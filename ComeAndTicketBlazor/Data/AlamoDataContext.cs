using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ComeAndTicketBlazor.Data
{
    public interface IComeAndTicketDataService
    {
        Task InitializeAsync();
        Task<IEnumerable<Market>> GetMarketsAsync();
        Task GetMoviesForMarketAsync(Market market);
        Task<User> GetUserAsync(string userName);
        Task<int> Save();
    }

    public class ComeAndTicketDataService : IComeAndTicketDataService
    {
        private readonly SemaphoreSlim _dbContextSemaphore = new SemaphoreSlim(1);
        private readonly ComeAndTicketContext _dbContext;

        public ComeAndTicketDataService(ComeAndTicketContext context)
        {
            _dbContext = context;
        }

        public async Task InitializeAsync()
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        public async Task ExecuteActionInContext(Func<Task> action)
        {
            try
            {
                await _dbContextSemaphore.WaitAsync();
                await action();
            }
            finally
            {
                _dbContextSemaphore.Release();
            }
        }

        public async Task<T> ExecuteActionInContext<T>(Func<Task<T>> action)
        {
            try
            {
                await _dbContextSemaphore.WaitAsync();
                return await action();
            }
            finally
            {
                _dbContextSemaphore.Release();
            }
        }

        public async Task<IEnumerable<Market>> GetMarketsAsync() => 
            await ExecuteActionInContext(() => InnerGetMarketsAsync());
        private async Task<IEnumerable<Market>> InnerGetMarketsAsync()
        {
            return await _dbContext.GetMarketsFromWebAsync();
        }

        public async Task GetMoviesForMarketAsync(Market market) => 
            await ExecuteActionInContext(() => InnerGetMoviesForMarketAsync(market));
        private async Task InnerGetMoviesForMarketAsync(Market market)
        {
            await _dbContext.GetCinemasFromWebAsync(market);
        }

        public async Task<User> GetUserAsync(string userName) =>
            await ExecuteActionInContext(() => InnerGetUserAsync(userName));
        private async Task<User> InnerGetUserAsync(string userName)
        {
            var dbUser = await _dbContext.GetUserFromDbAsync(userName);

            if (dbUser == null)
            {
                dbUser = new User() { UserName = userName };
                _dbContext.Users.Add(dbUser);
                await _dbContext.SaveChangesAsync();
            }

            return dbUser;
        }

        public async Task<int> Save() => await ExecuteActionInContext(() => InnerSave());
        private async Task<int> InnerSave()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
