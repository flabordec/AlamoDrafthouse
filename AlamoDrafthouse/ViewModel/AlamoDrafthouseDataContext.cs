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

namespace com.magusoft.drafthouse.ViewModel
{
	class AlamoDrafthouseDataContext : BindableBase
	{
		#region Data
		private string mTitleFilter;
		public string TitleFilter
		{
			get { return mTitleFilter; }
			set
			{
				SetProperty(ref mTitleFilter, value);
				foreach (Market market in this.Markets)
					foreach (Theater theater in market.Theaters)
						CollectionViewSource.GetDefaultView(theater.Movies).Refresh();
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
			this.mTitleFilter = "Master Pancake";

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
			if (e.NewItems != null)
			{
				foreach (Theater theater in e.NewItems)
					CollectionViewSource.GetDefaultView(theater.Movies).Filter = FilterMovies;
			}
		}

		private bool FilterMovies(object obj)
		{
			if (obj is Movie)
			{
				Movie currentMovie = (Movie)obj;
				return currentMovie.Title.ToLowerInvariant().Contains(
					this.TitleFilter.ToLowerInvariant());
			}

			return true;
		}
		
		internal async Task InitializeAsync(string marketName, string movieTitle)
		{
			try
			{
				this.Status = string.Format("Initializing");

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
				}

				this.TitleFilter = movieTitle;
			}
			finally
			{
				this.Status = string.Format("Finished Initializing");
			}
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
	}
}
