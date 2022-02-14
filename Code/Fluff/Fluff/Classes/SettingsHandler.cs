using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fluff.Classes
{
    /// <summary>
    /// This contains the current app settings.
    /// </summary>
    public static class SettingsHandler
    {
        public static string Username { get; set; }
        public static string ApiKey { get; set; }
        public static string Rating { get; set; }
        public static bool ShowComments { get; set; }
        public static bool MuteVolume { get; set; }
        public static double PostCount { get; set; }
        public static Dictionary<int, bool> VotedPosts { get; set; }

    }
}
