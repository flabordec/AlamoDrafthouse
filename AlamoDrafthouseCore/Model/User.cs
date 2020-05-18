using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class ShowTimeNotification
    {
        public string ShowTimeTicketsUrl { get; set; }
        public ShowTime ShowTime { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public ShowTimeNotification(ShowTime showTime, User user)
        {
            ShowTime = showTime;
            User = user;
        }

        public ShowTimeNotification() { }
    }

    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "You must specify a user name")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "You must specify a password")]
        public string PasswordHash { get; set; }

        public HashSet<ShowTimeNotification> Notifications { get; set; }

        public HashSet<MovieTitleToWatch> MovieTitlesToWatch { get; set; }

        public HashSet<DeviceNickname> DeviceNicknames { get; set; }

        public string PushbulletApiKey { get; set; }

        public Market HomeMarket { get; set; }

        public User(string userName, string password)
        {
            UserName = userName;
            PasswordHash = CalculatePasswordHash(password);
            Notifications = new HashSet<ShowTimeNotification>();
            MovieTitlesToWatch = new HashSet<MovieTitleToWatch>();
            DeviceNicknames = new HashSet<DeviceNickname>();
        }

        public User() { }

        private static string CalculatePasswordHash(string password)
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashValue = mySHA256.ComputeHash(passwordBytes);
                return BitConverter.ToString(hashValue);
            }
        }
    }

    public class MovieTitleToWatch
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Value { get; set; }

        public MovieTitleToWatch(string value)
        {
            Value = value;
        }
    }

    public class DeviceNickname
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Value { get; set; }

        public DeviceNickname(string value)
        {
            Value = value;
        }
    }
}
