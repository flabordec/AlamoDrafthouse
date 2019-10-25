using GalaSoft.MvvmLight;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    public class Movie
    {
        [Required(ErrorMessage = "You must specify a title"), Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Title { get; set; }

        public HashSet<ShowTime> ShowTimes { get; set; }

        public Movie()
        {

        }

        public Movie(string title)
        {
            Title = title;
            ShowTimes = new HashSet<ShowTime>();
        }

        //public bool Equals([AllowNull] Movie other) => MovieComparer.TitleCurrentCultureIgnoreCase.Equals(this, other);

        //public override bool Equals(object obj) => Equals(obj as Movie);

        //public override int GetHashCode() => MovieComparer.TitleCurrentCultureIgnoreCase.GetHashCode(this);
    }
}
