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
        Task<IEnumerable<Movie>> GetMoviesForMarketAsync(Market market, Theater theater, string sortOrder, string titleFilter);
        Task<IEnumerable<ShowTime>> GetShowTimesAsync(string movieTitle, string marketName, string theaterName);
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
            // await ComeAndTicketContext.UpdateDatabaseFromWebAsync(_dbContext);
            await _dbContext.Database.EnsureCreatedAsync();
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
            return await 
                _dbContext
                .Markets
                    .Include(m => m.Theaters)
                .ToListAsync();
        }

        public async Task<IEnumerable<Movie>> GetMoviesForMarketAsync(Market market, Theater theater, string sortOrder, string titleFilter) => 
            await ExecuteActionInContext(() => InnerGetMoviesForMarketAsync(market, theater, sortOrder, titleFilter));
        private async Task<IEnumerable<Movie>> InnerGetMoviesForMarketAsync(Market market, Theater theater, string sortOrder, string titleFilter)
        {
            IQueryable<Movie> query = _dbContext.Movies
                .Include(m => m.ShowTimes);
            switch (sortOrder)
            {
                case null:
                case "title":
                    query = query.OrderBy(m => m.Title);
                    break;
                case "title_desc":
                    query = query.OrderByDescending(m => m.Title);
                    break;
            }

            query = query.Where(m =>
                m.ShowTimes.Any(st =>
                    st.Date >= DateTime.UtcNow &&
                    st.SeatsLeft > 0
                ));
            
            if (market != null)
            {
                query = query.Where(m =>
                    m.ShowTimes.Any(st =>
                        st.Theater.Market == market));
            }

            if (theater != null)
            {
                query = query.Where(m =>
                    m.ShowTimes.Any(st =>
                        st.Theater == theater));
            }

            IEnumerable<Movie> movies;
            IEnumerable<Movie> moviesPreFilter = await query.ToListAsync();
            if (!string.IsNullOrEmpty(titleFilter))
            {
                movies = moviesPreFilter.Where(m => m.Title.Contains(titleFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
            else
            {
                movies = moviesPreFilter;
            }
            return movies;
        }

        public async Task<IEnumerable<ShowTime>> GetShowTimesAsync(string movieTitle, string marketName, string theaterName) =>
            await ExecuteActionInContext(() => InnerGetShowTimesAsync(movieTitle, marketName, theaterName));
        private async Task<IEnumerable<ShowTime>> InnerGetShowTimesAsync(string movieTitle, string marketName, string theaterName)
        {
            IQueryable<ShowTime> query =
                _dbContext.ShowTimes
                .Include(st => st.Theater)
                    .ThenInclude(t => t.Market);
            query = query
                .Where(st => st.MovieTitle == movieTitle)
                .Where(st => st.Date >= DateTime.UtcNow);
            
            if (!string.IsNullOrEmpty(marketName))
            {
                query = query.Where(st => st.Theater.Market.Name == marketName);
            }

            if (!string.IsNullOrEmpty(theaterName))
            {
                query = query.Where(st => st.Theater.Name == theaterName);
            }

            var showTimes = await query.ToListAsync();
            return showTimes;
        }

        public async Task<User> GetUserAsync(string userName) =>
            await ExecuteActionInContext(() => InnerGetUserAsync(userName));
        private async Task<User> InnerGetUserAsync(string userName)
        {
            User user = new User(userName);
            var dbUser = await _dbContext.Users
                    .Include(u => u.DeviceNicknames)
                    .Include(u => u.MovieTitlesToWatch)
                .Where(u => u.EMail == user.EMail)
                .FirstOrDefaultAsync();

            if (dbUser == null)
            {
                dbUser = user;
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            return dbUser;
        }

        public async Task<int> Save() =>
            await ExecuteActionInContext(() => InnerSave());
        private async Task<int> InnerSave()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
