using GalaSoft.MvvmLight;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.magusoft.drafthouse.Model
{
	public class Movie : ObservableObject
	{
		public string Title { get; }
		
		public Theater Theater { get; }

		private readonly List<ShowTime> mShowTimes;
		public IEnumerable<ShowTime> ShowTimes
		{
			get { return mShowTimes; }
		}

		public Movie(Theater theater, string title, IEnumerable<ShowTime> showTimes)
		{
			Theater = theater;
			Title = title;
			mShowTimes = new List<ShowTime>(showTimes);
		}
	}
}
