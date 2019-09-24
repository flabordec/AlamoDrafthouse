using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HtmlAgilityPack;
using MaguSoft.ComeAndTicket.Core.ExtensionMethods;
using MaguSoft.ComeAndTicket.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class Theater : ObservableObject
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string Name { get; }
        public string TheaterUrl { get; }

        private bool _moviesLoaded;
        public bool MoviesLoaded
        {
            get { return _moviesLoaded; }
            set { Set(ref _moviesLoaded, value); }
        }

        public string CalendarUrl
        {
            get { return TheaterUrl.Replace("theater", "calendar"); }
        }

        public ObservableCollection<Movie> Movies { get; }

        public RelayCommand LoadMoviesCommand { get; }

        private bool mLoadingMovies;
        public bool LoadingMovies
        {
            get { return mLoadingMovies; }
            private set { Set(ref mLoadingMovies, value); }
        }

        public Theater(string name, string theaterUrl)
        {
            Name = name;
            TheaterUrl = theaterUrl;
            Movies = new ObservableCollection<Movie>();
            _moviesLoaded = false;

            LoadMoviesCommand = new RelayCommand(async () => await OnLoadMoviesAsync());
        }

        public async Task OnLoadMoviesAsync()
        {
            try
            {
                LoadingMovies = true;
                logger.Info("Reading movies for {0}", Name);
                await InnerOnLoadMoviesAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Could not load movies {Name}");
                _moviesLoaded = false;
            }
            finally
            {
                LoadingMovies = false;
            }
        }

        private async Task InnerOnLoadMoviesAsync()
        {
            lock (Movies)
            {
                if (MoviesLoaded)
                    return;
            }

            // Sometimes the browser will return the page source before the page is fully loaded, in those 
            // cases just retry until you get something. 
            int retryCount = 0;
            while (retryCount < 5)
            {
                retryCount++;

                HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync(CalendarUrl);
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
                    // This is a bit weird, but the property that is bound is the name, so the 
                    // name is the property that must have errors. 
                    // TODO magus figure this out
                    // ErrorsContainer.SetErrors(() => Name, new[] { "Error loading movies" });
                    MoviesLoaded = true;
                    break;
                }

                var movies =
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
                    let showTimeDateTime = sessionsNode["SessionDateTime"]?.Value<DateTime>()
                    let showTimeId = sessionsNode["SessionId"]?.Value<string>()
                    let showTimeStatus = sessionsNode["SessionStatus"]?.Value<string>()
                    let seatsLeft = sessionsNode["SeatsLeft"]?.Value<int>()
                    let movieUrl = $"https://drafthouse.com/{cinemaSlug}/tickets/showtime/{cinemaId}/{showTimeId}"
                    let showTime = new ShowTime(showTimeDateTime, movieUrl, showTimeStatus, seatsLeft)
                    group showTime by title
                    into movieGroup
                    select movieGroup;

                if (movies.Any())
                {
                    lock (Movies)
                    {
                        if (Movies.Any())
                            return;

                        foreach (IGrouping<string, ShowTime> showTimes in movies)
                        {
                            string title = showTimes.Key;
                            Movie movie = new Movie(this, title, showTimes);

                            Movies.Add(movie);
                        }

                        MoviesLoaded = true;
                    }
                }
                else
                {
                    var errorNode = (
                        from node in marketsDocument.DocumentNode.Descendants("i")
                        where node.AttributeExistsAndHasValue("class", "fa", "fa-exclamation-triangle")
                        select node
                        ).SingleOrDefault();

                    if (errorNode != null)
                    {
                        // This is a bit weird, but the property that is bound is the name, so the 
                        // name is the property that must have errors. 
                        // TODO magus Figure this out
                        // ErrorsContainer.SetErrors(() => Name, new[] { "Error loading movies" });
                        MoviesLoaded = true;
                        break;
                    }
                }
            }
        }

    }
}