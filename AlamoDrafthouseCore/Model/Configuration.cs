using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class Configuration
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Name { get; set; }
        public string Value { get; set; }

        public Configuration(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public Configuration()
        {
        }
    }
}
