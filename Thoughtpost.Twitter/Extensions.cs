using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Thoughtpost.Twitter
{
    public static class Extensions
    {
        public static Dictionary<string, string> GetParams(this Uri uri)
        {
            try
            {
                MatchCollection matches = Regex.Matches(uri.AbsoluteUri, @"[\?&](([^&=]+)=([^&=#]*))", RegexOptions.Compiled);
                Dictionary<string, string> keyValues = new Dictionary<string, string>(matches.Count);
                foreach (Match m in matches)
                {
                    keyValues.Add(Uri.UnescapeDataString(m.Groups[2].Value), Uri.UnescapeDataString(m.Groups[3].Value));
                }

                return keyValues;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return new Dictionary<string, string>();
            }
        }
    }
}
