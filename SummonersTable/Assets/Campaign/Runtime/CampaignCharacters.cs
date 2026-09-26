using System;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    [Serializable] public sealed class ComicCharacter
    {
        public string id="",name="",type="npc",art="";
        public string[] aliases=Array.Empty<string>();
        public bool portrait;
        public float scale=1,offsetX,offsetY;
        public ComicActor Actor()=>new ComicActor{art=art,label=name,portrait=portrait,scale=scale,offsetX=offsetX,offsetY=offsetY};
    }
    [Serializable] public sealed class ComicPalette
    {
        public string type="npc",body="#35241CFB",border="#B98B51",name="#714B2B";
    }
    public sealed partial class CampaignBook
    {
        public int textPageLength=100;
        public ComicCharacter[] characters=Array.Empty<ComicCharacter>();
        public ComicPalette[] dialogueStyles={
            new ComicPalette{type="main",body="#163A35FB",border="#68D4AC",name="#267361"},
            new ComicPalette{type="npc"},
            new ComicPalette{type="important",body="#30223DFB",border="#D7AD64",name="#6B4086"},
            new ComicPalette{type="narration",body="#1A2028FB",border="#7D8999",name="#1A2028"}};
        public bool IsNarrator(ComicLine line)=>line.kind=="narration"||string.Equals(line.speaker,"РАССКАЗЧИК",StringComparison.OrdinalIgnoreCase);
        public ComicCharacter Character(ComicLine line)
        {
            if(IsNarrator(line))return null;
            return characters.FirstOrDefault(c=>c.id==line.characterId&&!string.IsNullOrEmpty(line.characterId))??characters.FirstOrDefault(c=>string.Equals(c.name,line.speaker,StringComparison.OrdinalIgnoreCase)||c.aliases.Any(a=>string.Equals(a,line.speaker,StringComparison.OrdinalIgnoreCase)));
        }
        public ComicPalette Palette(string type)=>dialogueStyles.FirstOrDefault(p=>p.type==type)??dialogueStyles[0];
        void ValidatePresentation()
        {
            void Check(bool ok,string why){if(!ok)throw new FormatException("campaign.json: "+why);}
            Check(textPageLength>=20&&textPageLength<=100,"textPageLength: 20…100");
            Check(characters!=null&&characters.All(c=>c!=null&&!string.IsNullOrWhiteSpace(c.id))&&characters.Select(c=>c.id).Distinct().Count()==characters.Length,"уникальные персонажи");
            foreach(var c in characters){Check(new[]{"main","npc","important"}.Contains(c.type),"тип персонажа");Check(c.aliases!=null&&!string.IsNullOrWhiteSpace(c.name),"имя и aliases персонажа "+c.id);Check(Resources.Load<Texture2D>(c.art)!=null,"арт персонажа "+c.id);Check(float.IsFinite(c.scale)&&c.scale>=.2f&&c.scale<=3,"масштаб персонажа");Check(float.IsFinite(c.offsetX)&&float.IsFinite(c.offsetY),"смещение персонажа "+c.id);}
            Check(dialogueStyles!=null&&new[]{"main","npc","important","narration"}.All(t=>dialogueStyles.Count(p=>p.type==t)==1),"четыре палитры диалога");
            foreach(var p in dialogueStyles)Check(ColorUtility.TryParseHtmlString(p.body,out _)&&ColorUtility.TryParseHtmlString(p.border,out _)&&ColorUtility.TryParseHtmlString(p.name,out _),"цвет диалога");
        }
    }
}
