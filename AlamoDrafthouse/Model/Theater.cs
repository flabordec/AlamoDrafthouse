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
			this.LoadMoviesCommand = DelegateCommand.FromAsyncHandler(OnLoadMoviesAsync);

			this.mMoviesLoaded = false;
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

				HtmlDocument marketsDocument = await WebDriverHelper.GetPageHtmlDocumentAsync(this.CalendarUrl);
				HtmlNode commentNode = marketsDocument.DocumentNode.SelectSingleNode("//main/comment()[contains(., 'ANGULAR ajax:')]");
				if (commentNode == null)
					continue;

				Regex ajaxRegex = new Regex(@"<!--ANGULAR ajax: ([^ ]+) -->");
				Match ajaxMatch = ajaxRegex.Match(commentNode.InnerText);
				if (!ajaxMatch.Success)
					continue;

				string ajaxUrl = ajaxMatch.Groups[1].Value;

				HtmlDocument ajaxDocument = await WebDriverHelper.GetPageHtmlDocumentAsync(ajaxUrl);

				var dateNodes =
					from dateNode in ajaxDocument.DocumentNode.Descendants("div")
					where dateNode.AttributeExistsAndHasValue("class", "Calendar-day")
					select dateNode;

				IEnumerable<IGrouping<string, ShowTime>> movies =
					from dateNode in dateNodes
					from node in dateNode.Descendants("p")
					where node.AttributeExistsAndHasValue("class", "clearfix")
					let title = node.Element("strong").Element("a").InnerText
					from showTime in ParseShowTimes(node, dateNode)
					group showTime by title into movieGroup
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

		private Dictionary<string, int> Months { get; } = new Dictionary<string, int>()
		{
			{ "Jan", 1 },
			{ "Feb", 2 },
			{ "Mar", 3 },
			{ "Apr", 4 },
			{ "May", 5 },
			{ "Jun", 6 },
			{ "Jul", 7 },
			{ "Agu", 8 },
			{ "Sep", 9 },
			{ "Oct", 10 },
			{ "Nov", 11 },
			{ "Dec", 12 },
		};

		private IEnumerable<ShowTime> ParseShowTimes(HtmlNode node, HtmlNode dayNode)
		{
			var dateNode = (
				from dateDiv in dayNode.Descendants("div")
				where dateDiv.AttributeExistsAndHasValue("class", "Calendar-date")
				select dateDiv
				).Single();
			string dateString = dateNode.InnerText.Trim();

			// Parse date of the format: 
			// Sun, Sep 4
			Regex dateRegex = new Regex(@"\w+, (\w+) (\d+)");
			Match dateMatch = dateRegex.Match(dateString);

			string monthString = dateMatch.Groups[1].Value;
			int month = Months[monthString];

			// If we are in November or December and they already have January or February on their 
			// calendar then it's the next year. This is a bit kludgy, but the data does not include 
			// years
			int year = DateTime.Today.Year;
			if (DateTime.Today.Month > month)
				year++;

			int day = int.Parse(dateMatch.Groups[2].Value);

			Regex timeRegex = new Regex(@"(\d+):(\d+)(am|pm)");
			foreach (HtmlNode showTimeNode in node.Elements("a"))
			{
				string url = showTimeNode.Attributes["href"]?.Value;
				string timeString = showTimeNode.InnerText.Trim();
				Match timeMatch = timeRegex.Match(timeString);
				int hours = int.Parse(timeMatch.Groups[1].Value);
				int minutes = int.Parse(timeMatch.Groups[2].Value);
				if (hours != 12 && timeMatch.Groups[3].Value.Equals("pm", StringComparison.OrdinalIgnoreCase))
					hours += 12;

				DateTime date = new DateTime(year, month, day, hours, minutes, 0, DateTimeKind.Local);
				yield return new ShowTime(date, url);
			}
		}
	}
}