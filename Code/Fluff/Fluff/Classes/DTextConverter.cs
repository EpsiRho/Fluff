using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fluff.Classes
{
    public static class DTextConverter
    {
        public static string ToMarkdown(string dtext)
        {
            // Replace Bold Tags
            dtext = dtext.Replace("[b]", "**");
            dtext = dtext.Replace("[/b]", "**");

            // Replace Italics
            dtext = dtext.Replace("[i]", "*");
            dtext = dtext.Replace("[/i]", "*");

            // Replace Underline (this may not work)
            dtext = dtext.Replace("[u]", "<u>");
            dtext = dtext.Replace("[/u]", "</u>");

            // Replace Strikethrough
            dtext = dtext.Replace("[s]", "~~");
            dtext = dtext.Replace("[/s]", "~~");

            // Subscript / Superscript broken

            // Username

            // Spoiler

            // Code Block
            dtext = dtext.Replace("[code]", "```");
            dtext = dtext.Replace("[/code]", "```");


            // Color

            // Quotes
            dtext = dtext.Replace("[quote]", ">").Replace("[/quote]", "");

            // Links
            dtext = dtext.Replace("\":", "](").Replace("\"", "[").Replace(" said:", ") said:");

            // Post Thumbnails

            // Block formatting

            // Headers
            dtext = dtext.Replace("h1.", "#");
            dtext = dtext.Replace("h2.", "##");
            dtext = dtext.Replace("h3.", "###");
            dtext = dtext.Replace("h4.", "####");
            dtext = dtext.Replace("h5.", "#####");
            dtext = dtext.Replace("h6.", "######");

            // Lists are the same

            // Sections
            dtext = dtext.Replace("[section]", ">");
            dtext = dtext.Replace("[/section]", "");
            Regex rx = new Regex(@"(\[section(.*)=)",
             RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Matches(dtext);

            foreach (var match in matches)
            {
                string BlowMe = match.ToString();
                dtext = dtext.Replace(BlowMe, ">");
                var idx = dtext.IndexOf(']');
                dtext = dtext.Remove(idx, 1);
            }


            // Tables will be a problem

            return dtext;
        }
    }
}
