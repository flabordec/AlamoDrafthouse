using HtmlAgilityPack;
using MaguSoft.ComeAndTicket.Core.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using PushbulletDotNet;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class ComeAndTicketContext : DbContext
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public string DbConnectionString { get; }
        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }

        public ComeAndTicketContext(bool useInMemoryDatabase)
        {
            if (useInMemoryDatabase)
            {
                DbConnectionString = "DataSource=file::memory:?cache=shared";
            }
            else
            {
                Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
                string path = Environment.GetFolderPath(folder);
                string dbPath = System.IO.Path.Join(path, "ComeAndTicket.db");
                DbConnectionString = $"Data Source={dbPath}";
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
            .UseSqlite(DbConnectionString)
            .EnableSensitiveDataLogging(true);

        public async Task<User?> GetUserFromDbAsync(string usernName) =>
            await Users
                .Include(u => u.SessionsNotified)
                .Where(u => u.UserName.Equals(usernName))
                .SingleOrDefaultAsync();

        public async Task<List<Session>> GetSessionsFromDbAsync() => await Sessions.ToListAsync();

        public async Task<IEnumerable<Market>> GetMarketsFromWebAsync(IEnumerable<string> marketNamesToUpdate)
        {
            IEnumerable<Market> marketsFromWeb = await GetMarketsFromWebAsync();

            // Calling ToList becuase we are returning the value, so the filtering only runs once.
            var marketNamesToUpdateSet = new HashSet<string>(marketNamesToUpdate, StringComparer.OrdinalIgnoreCase);
            IEnumerable<Market> marketsToUpdate = (
                from market in marketsFromWeb
                where marketNamesToUpdateSet.Contains(market.Name)
                select market
                ).ToList();

            var cinemasByMarket = await Task.WhenAll(
                from market in marketsToUpdate
                select GetCinemasFromWebAsync(market));
            var cinemas = cinemasByMarket.SelectMany(t => t);

            return marketsToUpdate;
        }

        public async Task<IEnumerable<Market>> GetMarketsFromWebAsync()
        {
            _logger.Info("Reading markets from web");
            try
            {
                JsonElement results = await InternetHelpers.GetPageJsonAsync<JsonElement>("https://drafthouse.com/s/mother/v1/page/cclamp");
                JsonElement marketSummaries = results.GetProperty("data").GetProperty("marketSummaries");
                var markets = marketSummaries.Deserialize<Market[]>();
                if (markets == null)
                    throw new InvalidOperationException("Could not deserialize markets from Alamo");
                return markets;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Retry limit exceeded when reading markets");
                return Enumerable.Empty<Market>();
            }
        }

        public async Task<IEnumerable<Cinema>> GetCinemasFromWebAsync(Market marketFromDb)
        {
            try
            {
                return await InnerGetCinemasFromWebAsync(marketFromDb);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Retry limit exceeded when reading cinemas for {Market}", marketFromDb.Name);
                return Enumerable.Empty<Cinema>();
            }

        }

        private async Task<IEnumerable<Cinema>> InnerGetCinemasFromWebAsync(Market marketFromDb)
        {
            _logger.Info("Reading cinemas for {Market} from web", marketFromDb.Name);

            JsonElement resultsJson = await InternetHelpers.GetPageJsonAsync<JsonElement>(marketFromDb.MarketUrl);
            JsonElement dataJson = resultsJson.GetProperty("data");
            JsonElement marketJson = dataJson.GetProperty("market");
            JsonElement cinemasJson = marketJson[0].GetProperty("cinemas");
            var cinemas = cinemasJson.Deserialize<Cinema[]>();
            if (cinemas == null)
                throw new InvalidOperationException("Could not deserialize cinemas from Alamo");
            foreach (var cinema in cinemas)
            {
                marketFromDb.Cinemas.Add(cinema);
                cinema.Market = marketFromDb;
            }
            var cinemasById = cinemas.ToDictionary(c => c.Id);


            JsonElement presentationsJson = dataJson.GetProperty("presentations");
            var presentations = presentationsJson.Deserialize<Presentation[]>();
            if (presentations == null)
                throw new InvalidOperationException("Could not deserialize presentations from Alamo");
            foreach (var presentation in presentations)
            {
                marketFromDb.Presentations.Add(presentation);
                presentation.Market = marketFromDb;
            }
            var presentationsBySlug = presentations.ToDictionary(p => p.Slug);

            var sessionsInDb = await GetSessionsFromDbAsync();
            var sessionsInDbById = sessionsInDb.ToDictionary(s => s.Id);

            JsonElement sessionJson = dataJson.GetProperty("sessions");
            var sessions = sessionJson.Deserialize<Session[]>();
            if (sessions == null)
                throw new InvalidOperationException("Could not deserialize sessions from Alamo");
            foreach (var sessionInWeb in sessions)
            {
                Session session;
                if (sessionsInDbById.TryGetValue(sessionInWeb.Id, out Session sessionInDb))
                {
                    session = sessionInDb;
                }
                else
                {
                    session = sessionInWeb;
                }

                var cinema = cinemasById[session.CinemaId];
                cinema.Sessions.Add(session);
                session.Cinema = cinema;

                var presentation = presentationsBySlug[session.PresentationSlug];
                presentation.Sessions.Add(session);
                session.Presentation = presentation;

                cinema.Presentations.Add(presentation);
            }

            return cinemas;
        }
    }
}
