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

namespace com.magusoft.drafthouse.Model
{
	public class Theater : ValidatableBindableBase
	{
		private readonly string mName;
		public string Name
		{
			get { return mName; }
		}

		private readonly string mTheaterUrl;
		public string TheaterUrl
		{
			get { return mTheaterUrl; }
		}

		private bool mMoviesLoaded;
		public bool MoviesLoaded
		{
			get { return mMoviesLoaded; }
			set { SetProperty(ref mMoviesLoaded, value); }
		}

		public string CalendarUrl
		{
			get { return mTheaterUrl.Replace("theater", "calendar"); }
        }

		private readonly ObservableCollection<Movie> mMovies;
        public ObservableCollection<Movie> Movies { get { return mMovies; } }

		private readonly DelegateCommand mLoadMoviesCommand;
		public DelegateCommand LoadMoviesCommand
		{
			get { return mLoadMoviesCommand; }
		}

		private bool mLoadingMovies;
		public bool LoadingMovies
		{
			get { return mLoadingMovies; }
			private set { SetProperty(ref mLoadingMovies, value); }
		}

		public Theater(string name, string theaterUrl)
		{
			this.mName = name;
			this.mTheaterUrl = theaterUrl;
			this.mMovies = new ObservableCollection<Movie>();
			this.mLoadMoviesCommand = DelegateCommand.FromAsyncHandler(OnLoadMoviesAsync);

			this.mMoviesLoaded = false;
		}

		public async Task OnLoadMoviesAsync()
		{
			try
			{
				LoadingMovies = true;
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

		private IEnumerable<ShowTime> ParseShowTimes(HtmlNode node, HtmlNode dayNode)
		{
			var dateNode = (
				from dateDiv in dayNode.Descendants("div")
				where dateDiv.AttributeExistsAndHasValue("class", "Calendar-date")
				select dateDiv
				).Single();
			string date = dateNode.InnerText.Trim();

			foreach (HtmlNode showTimeNode in node.Elements("a"))
			{
				string url = showTimeNode.Attributes["href"]?.Value;
				string time = showTimeNode.InnerText.Trim();

				string showTimeString = string.Format("{0} {1}", date, time);

				yield return new ShowTime(showTimeString, url);
			}
		}
	}
}