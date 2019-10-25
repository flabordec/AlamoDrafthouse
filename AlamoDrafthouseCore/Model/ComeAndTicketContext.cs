using HtmlAgilityPack;
using MaguSoft.ComeAndTicket.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
            .UseNpgsql("Host=raspberrypi;Database=come_and_ticket;Username=come_and_ticket_user;Password=comeandticket")
            .EnableSensitiveDataLogging(true);

        public static async Task UpdateDatabaseFromWeb(ComeAndTicketContext db)
        {
            await db.Markets
                .Include(m => m.Theaters)
                    .ThenInclude(t => t.ShowTimes)
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

        public static async Task<IEnumerable<Market>> ReadMarketsFromWebAsync()
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

        public static async Task UpdateMarketsAsync(ComeAndTicketContext db, IEnumerable<Market> marketsFromWeb)
        {
            foreach (var marketFromWeb in marketsFromWeb)
            {
                var marketFromDb = db.Markets.SingleOrDefault(m => m.Name == marketFromWeb.Name);
                if (marketFromDb == null)
                {
                    db.Markets.Add(marketFromWeb);
                }
                else
                {
                    marketFromDb.Name = marketFromWeb.Name;
                }
            }
            await db.SaveChangesAsync();
        }

        public static async Task<IEnumerable<Theater>> ReadTheatersFromWebAsync(Market marketFromDb)
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

        public static async Task UpdateTheatersAsync(ComeAndTicketContext db, IEnumerable<Theater> theatersFromWeb)
        {
            foreach (var theaterFromWeb in theatersFromWeb)
            {
                Theater theaterFromDb = db.Theaters.SingleOrDefault(t => t.Url == theaterFromWeb.Url);
                if (theaterFromDb == null)
                {
                    db.Theaters.Add(theaterFromWeb);
                }
                else
                {
                    theaterFromDb.Name = theaterFromWeb.Name;
                }
            }
            await db.SaveChangesAsync();
        }

        public static async Task<IEnumerable<IGrouping<string, ShowTime>>> ReadShowTimesFromWebAsync(Theater theaterFromDb)
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

        public static async Task UpdateShowTimesAsync(ComeAndTicketContext db, IEnumerable<IGrouping<string, ShowTime>> showTimesByMovie)
        {
            foreach (var showTimesFromWeb in showTimesByMovie)
            {
                string movieTitle = showTimesFromWeb.Key;
                Movie movieFromDb = db.Movies.SingleOrDefault(m => m.Title == movieTitle);
                if (movieFromDb == null)
                {
                    logger.Info($"Adding movie: {movieTitle}");
                    db.Movies.Add(new Movie(movieTitle));

                    await db.SaveChangesAsync();
                }
            }
            

            foreach (IGrouping<string, ShowTime> showTimesFromWeb in showTimesByMovie)
            {
                string movieTitle = showTimesFromWeb.Key;
                IEnumerable<ShowTime> deDupedShowTimesFromWeb = showTimesFromWeb.GroupBy(s => s.TicketsUrl).Select(g => g.First());
                var movieFromDb = db.Movies.Single(m => m.Title == movieTitle);
                foreach (ShowTime showTimeFromWeb in deDupedShowTimesFromWeb)
                {
                    showTimeFromWeb.Movie = movieFromDb;

                    var showTimeInDb = db.ShowTimes.SingleOrDefault(s => showTimeFromWeb.TicketsUrl == s.TicketsUrl);
                    if (showTimeInDb == null)
                    {
                        db.ShowTimes.Add(showTimeFromWeb);
                    }
                    else
                    {
                        showTimeInDb.SeatsLeft = showTimeFromWeb.SeatsLeft;
                        showTimeInDb.Date = showTimeFromWeb.Date;
                        showTimeInDb.TicketsStatus = showTimeFromWeb.TicketsStatus;
                    }
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
