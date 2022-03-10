using e6API;
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
        public static async Task<string> ToMarkdown(string dtext)
        {
            RequestHost host = new RequestHost(SettingsHandler.UserAgent);
            dtext = dtext.Replace("\r", "");
            dtext = dtext.Replace("\n", "\n\n");

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

            // TODO Username

            // TODO Spoiler

            // Code Block
            dtext = dtext.Replace("[code]", "```");
            dtext = dtext.Replace("[/code]", "```");


            // Color

            // Quotes
            dtext = dtext.Replace("[quote]", "> ").Replace("[/quote]", "");

            // Links
            Regex rgx = new Regex("(\":((?! )(?!\").)*\\/*)",
             RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var rgmatches = rgx.Matches(dtext);
            foreach (var match in rgmatches)
            {
                string BlowMe = match.ToString();
                var fix = BlowMe.Replace("\":", "](");
                fix += ")";
                dtext = dtext.Replace(BlowMe, fix);
            }
            dtext = dtext.Replace(") said:", ") said:").Replace("\"", "[");

            // Post Thumbnails
            //try
            //{
            //    bool fuck = true;
            //    while (fuck)
            //    {
            //        fuck = dtext.Contains("thumb #");
            //        if (fuck)
            //        {
            //            var str = dtext.Substring(dtext.IndexOf("thumb #"));
            //            var shit = str.Substring(7);
            //            int idx = 0;
            //            foreach (char c in shit)
            //            {
            //                if (Char.IsDigit(c))
            //                {
            //                    idx++;
            //                }
            //                else
            //                {
            //                    idx++;
            //                    break;
            //                }
            //            }

            //            if (idx != 0)
            //            {
            //                str = str.Substring(0, idx + 7);
            //                var id = shit.Substring(0, idx);
            //                var post = await host.GetPosts($"id:{id}");

            //                dtext = dtext.Replace(str, $"![]({post[0].preview.url})");
            //                dtext = dtext.Replace(str, $"![]({post[0].preview.url})");
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}


            // TODO Block formatting

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


            // Tables might be the same

            return dtext;
        }
    }
}
