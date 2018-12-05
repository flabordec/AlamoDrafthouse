using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.magusoft.drafthouse.ExtensionMethods;
using com.magusoft.drafthouse.ViewModel;
using HtmlAgilityPack;
using mvvm.magusoft.com;
using Prism.Commands;
using Prism.Mvvm;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com.magusoft.drafthouse.Model
{
    public class Theater : ValidatableBindableBase
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Theater));

        public string Name { get; }
        public string TheaterUrl { get; }

        private bool mMoviesLoaded;
        public bool MoviesLoaded
        {
            get { return mMoviesLoaded; }
            set { SetProperty(ref mMoviesLoaded, value); }
        }

        public string CalendarUrl
        {
            get { return TheaterUrl.Replace("theater", "calendar"); }
        }

        public ObservableCollection<Movie> Movies { get; }

        public DelegateCommand LoadMoviesCommand { get; }

        private bool mLoadingMovies;
        public bool LoadingMovies
        {
            get { return mLoadingMovies; }
            private set { SetProperty(ref mLoadingMovies, value); }
        }

        public Theater(string name, string theaterUrl)
        {
            this.Name = name;
            this.TheaterUrl = theaterUrl;
            this.Movies = new ObservableCollection<Movie>();
            this.mMoviesLoaded = false;

            this.LoadMoviesCommand = new DelegateCommand(async () => await OnLoadMoviesAsync());
        }

        public async Task OnLoadMoviesAsync()
        {
            try
            {
                LoadingMovies = true;
                logger.InfoFormat("Reading movies for {0}", this.Name);
                await InnerOnLoadMoviesAsync();
            }
            finally
            {
                LoadingMovies = false;
            }
        }

        private async Task InnerOnLoadMoviesAsync()
        {
            lock (this.Movies)
            {
                if (this.MoviesLoaded)
                    return;
            }

            // Sometimes the browser will return the page source before the page is fully loaded, in those 
            // cases just retry until you get something. 
            int retryCount = 0;
            while (retryCount < 5)
            {
                retryCount++;

                HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync(this.CalendarUrl);
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
                    this.ErrorsContainer.SetErrors(() => this.Name, new[] { "Error loading movies" });
                    this.MoviesLoaded = true;
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
                    lock (this.Movies)
                    {
                        if (this.Movies.Any())
                            return;

                        foreach (IGrouping<string, ShowTime> showTimes in movies)
                        {
                            string title = showTimes.Key;
                            Movie movie = new Movie(this, title, showTimes);

                            this.Movies.Add(movie);
                        }

                        this.MoviesLoaded = true;
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
                        this.ErrorsContainer.SetErrors(() => this.Name, new[] { "Error loading movies" });
                        this.MoviesLoaded = true;
                        break;
                    }
                }
            }
        }

    }
}