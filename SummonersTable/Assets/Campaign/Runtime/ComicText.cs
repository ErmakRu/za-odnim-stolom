using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace SummonersTable
{
    public static class ComicText
    {
        // Keep sentence punctuation with its sentence. Long sentences split at a word
        // boundary; an unbroken token is the only case split inside a word.
        public static string[] Split(string text,int limit=100)
        {
            if(limit<10)throw new ArgumentOutOfRangeException(nameof(limit));
            var pages=new List<string>();text=Regex.Replace(text??"",@"\s+"," ").Trim();
            foreach(Match match in Regex.Matches(text,@".+?(?:[.!?…]+[»”\""')\]]*(?=\s|$)|$)"))
            {
                string rest=match.Value.Trim();
                while(rest.Length>limit)
                {
                    int cut=rest.LastIndexOf(' ',limit-1,limit);
                    if(cut<1)cut=limit;
                    // Closing punctuation is never orphaned onto the next page.
                    while(cut<rest.Length&&".,!?…:;»”\"')]}".IndexOf(rest[cut])>=0)cut++;
                    pages.Add(rest.Substring(0,cut).Trim());rest=rest.Substring(cut).TrimStart();
                }
                if(rest.Length>0)pages.Add(rest);
            }
            return pages.Count==0?new[]{""}:pages.ToArray();
        }
    }
}
