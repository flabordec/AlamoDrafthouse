using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using com.magusoft.drafthouse.Model;
using HtmlAgilityPack;
using NDesk.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.PhantomJS;
using Prism.Commands;
using Prism.Mvvm;
using System.Net.Mail;
using System.Net;
using System.Windows;
using log4net;
using System.Xml.Linq;

namespace com.magusoft.drafthouse.ViewModel
{
	class AlamoDrafthouseDataContext : BindableBase
	{
		private static readonly ILog logger = LogManager.GetLogger(typeof(AlamoDrafthouseDataContext));

		#region Data
		private string mTitleFilter;
		public string TitleFilter
		{
			get { return mTitleFilter; }
			set
			{
				SetProperty(ref mTitleFilter, value);
				RefreshFilters();
			}
		}

		private DateTime? mDateFilter;
		public DateTime? DateFilter
		{
			get { return mDateFilter; }
			set
			{
				SetProperty(ref mDateFilter, value);
				RefreshFilters();
			}
		}

		private readonly ObservableCollection<Market> mMarkets;
		public ObservableCollection<Market> Markets
		{
			get
			{
				return mMarkets;
			}
		}
		#endregion

		#region State
		private string mStatus;
		public string Status
		{
			get { return mStatus; }
			set { SetProperty(ref mStatus, value); }
		}
		#endregion

		public AlamoDrafthouseDataContext()
		{
			this.mTitleFilter = string.Empty;
			this.mDateFilter = null;

			this.mMarkets = new ObservableCollection<Market>();
			this.Markets.CollectionChanged += Markets_CollectionChanged;
		}

		private void Markets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (Market market in e.OldItems)
					market.Theaters.CollectionChanged -= Theaters_CollectionChanged;
			}

			if (e.NewItems != null)
			{
				foreach (Market market in e.NewItems)
					market.Theaters.CollectionChanged += Theaters_CollectionChanged;
			}
		}

		private void Theaters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (Theater theater in e.OldItems)
					theater.Movies.CollectionChanged -= Movies_CollectionChanged;
			}

			if (e.NewItems != null)
			{
				foreach (Theater theater in e.NewItems)
				{
					theater.Movies.CollectionChanged += Movies_CollectionChanged;
					CollectionViewSource.GetDefaultView(theater.Movies).Filter = FilterMovie;
				}
			}
		}

		private void Movies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (Movie movie in e.NewItems)
				{
					CollectionViewSource.GetDefaultView(movie.ShowTimes).Filter = FilterShowTimes;
				}
			}
		}

		private bool FilterShowTimes(object obj)
		{
			Debug.Assert(obj is ShowTime);
			ShowTime showTime = (ShowTime)obj;

			return !DateFilter.HasValue || showTime.MyShowTime.Date.Equals(DateFilter.Value.Date);
		}

		private bool MovieTitleContains(Movie movie, string wantedTitle)
		{
			return movie.Title.ToLowerInvariant().Contains(
				wantedTitle.ToLowerInvariant());
		}

		private bool FilterMovie(object obj)
		{
			Debug.Assert(obj is Movie);
			Movie currentMovie = (Movie)obj;
			bool titleFilter = MovieTitleContains(currentMovie, this.TitleFilter);
			// No date time filter or...
			// any showtime matches the filter date
			bool showTimesFilter =
				!DateFilter.HasValue ||
				currentMovie.ShowTimes.Any(s => s.MyShowTime.Date.Equals(DateFilter.Value.Date));
			return titleFilter && showTimesFilter;
		}

		internal async Task InitializeAsync(
			string marketName, string movieTitle,
			string eMailAddress, string eMailPassword, string toAddress, bool isService)
		{
			try
			{
				this.TitleFilter = movieTitle;

				this.Status = string.Format("Initializing");

				logger.Info("Reading markets");
				await OnReloadMarketsAsync();

				var market = (
					from m in this.Markets
					where m.Name.ToLowerInvariant() == marketName
					select m
					).SingleOrDefault();

				if (market != null)
				{

					await market.OnLoadTheatersAsync();

					await Task.WhenAll(
						from t in market.Theaters
						select t.OnLoadMoviesAsync()
						);

					await Task.WhenAll(
						from t in market.Theaters
						from m in t.Movies
						where MovieTitleContains(m, movieTitle)
						from s in m.ShowTimes
						select s.OnCheckTicketsOnSaleAsync()
						);

					if (isService)
					{
						logger.Info($"Sending e-mail to {toAddress}");
						await SendEmail(market, movieTitle, eMailAddress, eMailPassword, new[] { toAddress });
						Application.Current.Shutdown();
					}
				}
			}
			catch (Exception ex)
			{
				logger.Error("Exception while initializing", ex);
				throw;
			}
			finally
			{
				this.Status = string.Format("Finished Initializing");
			}
		}

		private async Task SendEmail(
			Market market,
			string movieTitle,
			string address,
			string password,
			IEnumerable<string> toAddresses)
		{
			var moviesOnSale =
				from t in market.Theaters
				from m in t.Movies
				from s in m.ShowTimes
				where MovieTitleContains(m, movieTitle)
				where !MovieAlreadySent(t, m, s)
				group s by new { Theater = t, Movie = m } into showtimes
				select showtimes;

			if (!moviesOnSale.Any())
				return;

			StringBuilder messageBuilder = new StringBuilder();
			foreach (var movieOnSale in moviesOnSale)
			{
				Theater t = movieOnSale.Key.Theater;
				Movie m = movieOnSale.Key.Movie;
				messageBuilder.AppendLine($"{t.Name} has {m.Title} on the following showtimes:");
				foreach (ShowTime s in movieOnSale)
				{
					if (s.MyTicketsState == TicketsState.OnSale)
						messageBuilder.AppendLine($" - {s.MyShowTime} (Buy: {s.TicketsUrl})");
					else
						messageBuilder.AppendLine($" - {s.MyShowTime} (Tickets not yet on sale)");

					MarkMovieSent(t, m, s);
				}
			}
			logger.Info(messageBuilder.ToString());

			var fromAddress = new MailAddress(address);
			var smtp = new SmtpClient("smtp.gmail.com", 587)
			{
				EnableSsl = true,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(fromAddress.Address, password)
			};

			using (var message = new MailMessage())
			{
				message.From = fromAddress;
				foreach (string toAddress in toAddresses)
					message.To.Add(toAddress);
				message.Subject = "Movies on Sale";
				string bodyContent = messageBuilder.ToString();
				message.Body = bodyContent;
				message.BodyEncoding = Encoding.UTF8;

				await smtp.SendMailAsync(message);
			}
		}

		private XDocument GetConfigurationFile()
		{
			if (File.Exists("output.xml"))
			{
				return XDocument.Load("output.xml");
			}
			else
			{
				return new XDocument(
					new XElement("SentMovies"));
			}
		}

		private bool MovieAlreadySent(Theater t, Movie m, ShowTime s)
		{
			XDocument doc = GetConfigurationFile();
			return (
				from movie in doc.Descendants("Movie")
				where movie.Attribute("Theater").Value == t.Name
				where movie.Attribute("Title").Value == m.Title
				where movie.Attribute("TicketsURL").Value == s.TicketsUrl
				where movie.Attribute("TicketState").Value == s.MyTicketsState.ToString()
				select movie
				).Any();
		}

		private void MarkMovieSent(Theater t, Movie m, ShowTime s)
		{
			XDocument doc = GetConfigurationFile();
			doc.Root.Add(
				new XElement("Movie",
					new XAttribute("Theater", t.Name),
					new XAttribute("Title", m.Title),
					new XAttribute("TicketsURL", s.TicketsUrl),
					new XAttribute("TicketState", s.MyTicketsState)
					)
				);
			doc.Save("output.xml");
		}

		private async Task OnReloadMarketsAsync()
		{
			try
			{
				this.Status = string.Format("Reloading markets");

				HtmlDocument marketsDocument = await WebDriverHelper.GetPageHtmlDocumentAsync("https://drafthouse.com/markets");

				var markets =
					from node in marketsDocument.DocumentNode.Descendants("a")
					where node.Attributes["id"]?.Value == "markets-page"
					let url = node.Attributes["href"].Value
					select new Market(url, node.InnerText);

				this.Markets.AddRange(markets);
			}
			finally
			{
				this.Status = string.Format("Reloaded markets");
			}
		}

		private void RefreshFilters()
		{
			foreach (Market market in this.Markets)
			{
				foreach (Theater theater in market.Theaters)
				{
					CollectionViewSource.GetDefaultView(theater.Movies).Refresh();
					foreach (Movie movie in theater.Movies)
					{
						CollectionViewSource.GetDefaultView(movie.ShowTimes).Refresh();
					}
				}
			}
		}
	}
}
