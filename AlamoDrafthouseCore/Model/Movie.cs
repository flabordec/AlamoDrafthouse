using GalaSoft.MvvmLight;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class MovieComparer
    {
        public static IEqualityComparer<Movie> TitleCurrentCultureIgnoreCase => new MovieTitleComparer(StringComparer.CurrentCultureIgnoreCase);
    }

    class MovieTitleComparer : IEqualityComparer<Movie>
    {
        private readonly StringComparer _comparer;

        public MovieTitleComparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        public bool Equals([AllowNull] Movie x, [AllowNull] Movie y)
        {
            if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
                return true;
            if (ReferenceEquals(null, x))
                return false;
            if (ReferenceEquals(null, y))
                return false;
            if (ReferenceEquals(x, y))
                return true;
            return _comparer.Equals(x.Title, y.Title);
        }

        public int GetHashCode([DisallowNull] Movie obj)
        {
            return _comparer.GetHashCode(obj.Title);
        }
    }

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
