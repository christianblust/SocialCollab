using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuisBot
{
    public class Tv
    {
        public string status { get; set; }
        public string channel { get; set; }
        public string volume { get; set; }

        public Tv(string Status, string Channel, string Volume)
        {
            this.status = Status;
            this.channel = Channel;
            this.volume = Volume;
        }
    }
}