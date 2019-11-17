using HtmlAgilityPack;
using MaguSoft.ComeAndTicket.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using PushbulletDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class ComeAndTicketContext : DbContext
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public DbSet<Market> Markets { get; set; }
        public DbSet<Theater> Theaters { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<ShowTime> ShowTimes { get; set; }
        public DbSet<Configuration> Configuration { get; set; }
        public DbSet<Target> Targets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
            .UseNpgsql("Host=raspberrypi;Database=come_and_ticket;Username=come_and_ticket_user;Password=comeandticket")
            .EnableSensitiveDataLogging(true);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>()
                .HasIndex(b => b.Name)
                .IsUnique();

            modelBuilder.Entity<ShowTimeTarget>()
                .HasKey(st => new { st.ShowTimeTicketsUrl, st.TargetId });

            modelBuilder.Entity<ShowTimeTarget>()
                .HasOne(st => st.ShowTime)
                .WithMany(s => s.TargetsUpdated)
                .HasForeignKey(s => s.ShowTimeTicketsUrl);

            modelBuilder.Entity<ShowTimeTarget>()
                .HasOne(st => st.Target)
                .WithMany(t => t.ShowTimes)
                .HasForeignKey(t => t.TargetId);
        }

        public static async Task UpdateDatabaseFromWebAsync(ComeAndTicketContext db)
        {
            await db.Database.MigrateAsync();

            await db.Markets
                .Include(m => m.Theaters)
                    .ThenInclude(t => t.ShowTimes)
                        .ThenInclude(s => s.TargetsUpdated)
                .LoadAsync();
            await db.Movies
                .LoadAsync();

            IEnumerable<Market> marketsFromWeb = await ReadMarketsFromWebAsync();
            await UpdateMarketsAsync(db, marketsFromWeb);

            var theatersByMarket = await Task.WhenAll(
                from market in db.Markets
                select ReadTheatersFromWebAsync(market));
            var theaters = theatersByMarket.SelectMany(t => t);
            await UpdateTheatersAsync(db, theaters);

            var showTimesByTheater = await Task.WhenAll(
                from theater in db.Theaters
                select ReadShowTimesFromWebAsync(theater));
            var showTimes = showTimesByTheater.SelectMany(s => s).ToArray();
            await UpdateShowTimesAsync(db, showTimes);
        }

        private static async ValueTask<IEnumerable<Market>> ReadMarketsFromWebAsync()
        {
            logger.Info($"Reading markets from web");

            HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync("https://drafthouse.com/markets");
            var marketsFromWeb =
                from node in marketsDocument.DocumentNode.Descendants("a")
                where node.Attributes["id"]?.Value == "markets-page"
                let url = node.Attributes["href"].Value
                select new Market(url, node.InnerText);

            return marketsFromWeb;
        }

        private static async ValueTask UpdateMarketsAsync(ComeAndTicketContext db, IEnumerable<Market> marketsFromWeb)
        {
            await db.Markets.LoadAsync();
            var marketsByName = await db.Markets.ToDictionaryAsync(
                m => m.Name,
                m => m,
                StringComparer.CurrentCultureIgnoreCase);
            foreach (var marketFromWeb in marketsFromWeb)
            {
                if (marketsByName.TryGetValue(marketFromWeb.Name, out Market marketFromDb))
                {
                    marketFromDb.Name = marketFromWeb.Name;
                }
                else
                {
                    db.Markets.Add(marketFromWeb);
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task<IEnumerable<Theater>> ReadTheatersFromWebAsync(Market marketFromDb)
        {
            logger.Info($"Reading theaters for {marketFromDb.Name} from web");

            HtmlDocument marketDocument = await InternetHelpers.GetPageHtmlDocumentAsync(marketFromDb.Url);

            var theatersFromWeb =
                from node in marketDocument.DocumentNode.Descendants("a")
                where node.Attributes["class"] != null && node.Attributes["class"].Value == "button small secondary Showtimes-time"
                let theaterUrl = node.Attributes["href"].Value
                select new Theater(marketFromDb, theaterUrl, node.InnerText);

            return theatersFromWeb;
        }

        private static async Task UpdateTheatersAsync(ComeAndTicketContext db, IEnumerable<Theater> theatersFromWeb)
        {
            await db.Theaters.LoadAsync();
            var theatersByUrl = await db.Theaters.ToDictionaryAsync(
                t => t.Url,
                t => t,
                StringComparer.OrdinalIgnoreCase);

            foreach (var theaterFromWeb in theatersFromWeb)
            {
                if (theatersByUrl.TryGetValue(theaterFromWeb.Url, out Theater theaterFromDb))
                {
                    theaterFromDb.Name = theaterFromWeb.Name;
                }
                else
                {
                    db.Theaters.Add(theaterFromWeb);
                    
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task<IEnumerable<IGrouping<string, ShowTime>>> ReadShowTimesFromWebAsync(Theater theaterFromDb)
        {
            logger.Info($"Loading movies for {theaterFromDb.Name}");

            // Sometimes the browser will return the page source before the page is fully loaded, in those 
            // cases just retry until you get something. 
            int retryCount = 0;
            while (retryCount < 5)
            {
                retryCount++;

                HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync(theaterFromDb.CalendarUrl);
                HtmlNode showTimeControllerNode = marketsDocument.DocumentNode.SelectSingleNode("//div[@ng-controller='ShowtimeController']");
                Regex showTimesRegex = new Regex(@"initCalendar\('([^']+)','([^']+)'\)");
                Match showTimesMatch = showTimesRegex.Match(showTimeControllerNode.Attributes["ng-init"].Value);
                if (!showTimesMatch.Success)
                    continue;

                string showTimesUrlBase = showTimesMatch.Groups[1].Value;
                string showTimesUrlCode = showTimesMatch.Groups[2].Value;
                string ajaxUrl = $"{showTimesUrlBase}calendar/{showTimesUrlCode}";


                string jsonContent = await InternetHelpers.GetPageContentAsync(ajaxUrl);
                JToken json = JToken.Parse(jsonContent, new JsonLoadSettings());

                // https://drafthouse.com/austin/tickets/showtime/0002/29212
                //IEnumerable<IGrouping<string, ShowTime>> movies =

                if (json["Calendar"]["Cinemas"] == null)
                {
                    break;
                }

                IEnumerable<IGrouping<string, ShowTime>> showTimesByMovie =
                    from cinemaToken in json["Calendar"]["Cinemas"]
                    from monthsNode in cinemaToken["Months"]
                    from weeksNode in monthsNode["Weeks"]
                    from daysNode in weeksNode["Days"]
                    where daysNode["Films"] != null
                    from filmsNode in daysNode["Films"]
                    from seriesNode in filmsNode["Series"]
                    from formatsNode in seriesNode["Formats"]
                    from sessionsNode in formatsNode["Sessions"]
                    let cinemaSlug = cinemaToken["MarketSlug"]?.Value<string>()
                    let cinemaId = cinemaToken["CinemaId"]?.Value<string>()
                    let title = filmsNode["FilmName"]?.Value<string>()
                    let movieTitle = title
                    let showTimeDateTime = sessionsNode["SessionDateTime"]?.Value<DateTime>()
                    let showTimeId = sessionsNode["SessionId"]?.Value<string>()
                    let showTimeStatus = sessionsNode["SessionStatus"]?.Value<string>()
                    let seatsLeft = sessionsNode["SeatsLeft"]?.Value<int>()
                    let showTimeUrl = $"https://drafthouse.com/{cinemaSlug}/tickets/showtime/{cinemaId}/{showTimeId}"
                    let showTime = new ShowTime(theaterFromDb, showTimeUrl, showTimeDateTime, showTimeStatus, seatsLeft)
                    group showTime by movieTitle
                    into movieGroup
                    select movieGroup;

                return showTimesByMovie;
            }
            return Enumerable.Empty<IGrouping<string, ShowTime>>();
        }

        private static async Task UpdateShowTimesAsync(ComeAndTicketContext db, IEnumerable<IGrouping<string, ShowTime>> showTimesByMovie)
        {
            await db.Movies.LoadAsync();
            var moviesByTitle = await db.Movies.ToDictionaryAsync(
                m => m.Title,
                m => m,
                StringComparer.CurrentCultureIgnoreCase);
            foreach (var showTimesFromWeb in showTimesByMovie)
            {
                string movieTitle = showTimesFromWeb.Key;
                if (!moviesByTitle.ContainsKey(movieTitle))
                {
                    logger.Info($"Adding movie: {movieTitle}");
                    var movie = new Movie(movieTitle);
                    db.Movies.Add(movie);
                    moviesByTitle.Add(movieTitle, movie);
                }
            }

            await db.ShowTimes.LoadAsync();
            var showTimesByTicketsUrl = await db.ShowTimes.ToDictionaryAsync(
                s => s.TicketsUrl,
                s => s,
                StringComparer.OrdinalIgnoreCase);
            foreach (IGrouping<string, ShowTime> showTimesFromWeb in showTimesByMovie)
            {
                string movieTitle = showTimesFromWeb.Key;
                IEnumerable<ShowTime> deDupedShowTimesFromWeb = showTimesFromWeb.GroupBy(s => s.TicketsUrl).Select(g => g.First());
                
                var movieFromDb = moviesByTitle[movieTitle];
                foreach (ShowTime showTimeFromWeb in deDupedShowTimesFromWeb)
                {
                    showTimeFromWeb.Movie = movieFromDb;

                    if (showTimesByTicketsUrl.TryGetValue(showTimeFromWeb.TicketsUrl, out ShowTime showTimeInDb))
                    {
                        showTimeInDb.SeatsLeft = showTimeFromWeb.SeatsLeft;
                        showTimeInDb.Date = showTimeFromWeb.Date;
                        showTimeInDb.TicketsStatus = showTimeFromWeb.TicketsStatus;
                    }
                    else
                    {
                        db.ShowTimes.Add(showTimeFromWeb);
                    }
                }
            }
        }

        public static async Task UpdateDevicesAsync(ComeAndTicketContext db, IEnumerable<IDevice> devicesFromPushbulletApi)
        {
            await db.Targets.LoadAsync();
            var targetsById = await db.Targets.ToDictionaryAsync(
                d => d.Id,
                d => d,
                StringComparer.OrdinalIgnoreCase);

            foreach (var deviceFromPushbulletApi in devicesFromPushbulletApi)
            {
                if (targetsById.TryGetValue(deviceFromPushbulletApi.Id, out Target targetFromDb))
                {
                    targetFromDb.Nickname = deviceFromPushbulletApi.Nickname;
                }
                else
                {
                    var newTarget = new Target(deviceFromPushbulletApi.Id, deviceFromPushbulletApi.Nickname);
                    db.Targets.Add(newTarget);

                }
            }
            await db.SaveChangesAsync();
        }
    }
}
