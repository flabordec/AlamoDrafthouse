using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
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
    public class TheaterComparer
    {
        public static IEqualityComparer<Cinema> TheaterNameComparer => new TheaterNameComparer(StringComparer.CurrentCultureIgnoreCase);
    }

    class TheaterNameComparer : IEqualityComparer<Cinema>
    {
        private readonly StringComparer _comparer;

        public TheaterNameComparer(StringComparer comparer)
        {
            _comparer = comparer;
        }

        public bool Equals([AllowNull] Cinema x, [AllowNull] Cinema y)
        {
            if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
                return true;
            if (ReferenceEquals(null, x))
                return false;
            if (ReferenceEquals(null, y))
                return false;
            if (ReferenceEquals(x, y))
                return true;
            return _comparer.Equals(x.Name, y.Name);
        }

        public int GetHashCode([DisallowNull] Cinema obj)
        {
            return _comparer.GetHashCode(obj.Name);
        }
    }

    public class Cinema
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Key, Required(ErrorMessage = "You must specify an ID"), JsonPropertyName("id")]
        public string Id { get; set; }
        [Required(ErrorMessage = "You must specify a name"), JsonPropertyName("name")]
        public string Name { get; set; }
        [Required(ErrorMessage = "You must specify a slug"), JsonPropertyName("slug")]
        public string Slug { get; set; }



        public Market Market { get; set; }
        public HashSet<Presentation> Presentations { get; } = new ();
        public HashSet<Session> Sessions { get; } = new ();

        public Cinema() { }
    }
}