using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class ShowTimeTarget
    {
        public string ShowTimeTicketsUrl { get; set; }
        public ShowTime ShowTime { get; set; }

        public string TargetId { get; set; }
        public Target Target { get; set; }

        public ShowTimeTarget(ShowTime showTime, Target target)
        {
            ShowTime = showTime;
            Target = target;
        }

        public ShowTimeTarget() { }
    }

    public class Target
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }
        public string Nickname { get; set; }

        public HashSet<ShowTimeTarget> ShowTimes { get; set; }

        public Target(string id, string nickname)
        {
            Id = id;
            Nickname = nickname;
            ShowTimes = new HashSet<ShowTimeTarget>();
        }
        public Target() { }
    }
}
