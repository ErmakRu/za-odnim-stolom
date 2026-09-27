using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace SummonersTable
{
    public static class ComicText
    {
        // Pack complete sentences into pages, preserving sentences longer than the soft limit.
        public static string[] Split(string text,int limit=200)
        {
            if(limit<10)throw new ArgumentOutOfRangeException(nameof(limit));
            var pages=new List<string>();text=Regex.Replace(text??"",@"\s+"," ").Trim();
            foreach(Match match in Regex.Matches(text,@".+?(?:[.!?…]+[»”\""')\]]*(?=\s|$)|$)"))
            {
                string rest=match.Value.Trim();
                if(rest.Length==0)continue;
                if(pages.Count>0&&pages[pages.Count-1].Length+1+rest.Length<=limit)
                    pages[pages.Count-1]+=" "+rest;
                else pages.Add(rest); // Preserve whole sentences beyond the soft limit.
            }
            return pages.Count==0?new[]{""}:pages.ToArray();
        }
    }
}
