using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    [Serializable] public sealed class ComicActor
    {
        public string art="",label="";
        public bool portrait;
        public float scale=1,offsetX,offsetY;
    }
    [Serializable] public sealed class ComicLine
    {
        public string sourceId="",speaker="",text="",leftArt="",sfx="",ambience="";
    }
    [Serializable] public sealed class ComicFrame
    {
        public string id="",title="",background="Art/board";
        public ComicActor left=new ComicActor(),right=new ComicActor();
        public ComicLine[] lines=Array.Empty<ComicLine>();
    }
    [Serializable] public sealed class CampaignChapter
    {
        public string id,title,opponentName,opponentDeck="noise",opponentHero="badger",mode="wizards";
        public int seed=1701;
        public ComicFrame[] before=Array.Empty<ComicFrame>(),after=Array.Empty<ComicFrame>();
    }
    [Serializable] public sealed class CampaignBook
    {
        public int schemaVersion=1;
        public string title="Пир Хохота",defaultDeck="noise";
        public float charactersPerSecond=60,ambienceVolume=.25f,sfxVolume=.6f;
        public ComicFrame[] introduction=Array.Empty<ComicFrame>(),ending=Array.Empty<ComicFrame>();
        public CampaignChapter[] chapters=Array.Empty<CampaignChapter>();
        public static string FilePath=>Application.isEditor?Path.Combine(Application.dataPath,"Campaign/Resources/Campaign/campaign.json"):Path.Combine(ConfigRuntime.DirectoryPath,"campaign.json");
        public static CampaignBook Load(Catalog catalog)
        {
            string json=File.Exists(FilePath)?File.ReadAllText(FilePath):Resources.Load<TextAsset>("Campaign/campaign")?.text;
            if(string.IsNullOrEmpty(json))throw new FormatException("campaign.json отсутствует");
            var book=JsonUtility.FromJson<CampaignBook>(json);book.Validate(catalog);return book;
        }
        public void Validate(Catalog catalog)
        {
            void Check(bool ok,string message){if(!ok)throw new FormatException("campaign.json: "+message);}
            Check(schemaVersion==1&&chapters!=null&&chapters.Length>0,"нужны schemaVersion 1 и главы");
            Check(chapters.All(c=>c!=null&&!string.IsNullOrWhiteSpace(c.id))&&chapters.Select(c=>c.id).Distinct().Count()==chapters.Length,"уникальные id глав");
            Check(catalog.Deck(defaultDeck)!=null,"неизвестная стартовая колода");
            Check(float.IsFinite(charactersPerSecond)&&charactersPerSecond>=0&&charactersPerSecond<=500,"charactersPerSecond: 0…500");
            Check(float.IsFinite(ambienceVolume)&&ambienceVolume>=0&&ambienceVolume<=1&&float.IsFinite(sfxVolume)&&sfxVolume>=0&&sfxVolume<=1,"громкость: 0…1");
            void Frames(ComicFrame[] frames,bool required)
            {
                Check(frames!=null&&(!required||frames.Length>0),"отсутствуют кадры");
                Check(frames.All(f=>f!=null&&!string.IsNullOrWhiteSpace(f.id))&&frames.Select(f=>f.id).Distinct().Count()==frames.Length,"уникальные id кадров внутри части");
                foreach(var f in frames)
                {
                    Check(f.lines!=null&&f.lines.Length>0&&f.lines.All(l=>l!=null&&!string.IsNullOrWhiteSpace(l.text)),"в кадре нужны реплики");
                    Texture(f.background,false);
                    foreach(var actor in new[]{f.left,f.right}){Check(actor!=null,"отсутствует слот персонажа");Texture(actor.art,true);Check(float.IsFinite(actor.scale)&&actor.scale>=.2f&&actor.scale<=3&&float.IsFinite(actor.offsetX)&&Math.Abs(actor.offsetX)<=800&&float.IsFinite(actor.offsetY)&&Math.Abs(actor.offsetY)<=600,"размер/смещение персонажа");}
                    foreach(var line in f.lines)Texture(line.leftArt,true);
                }
            }
            void Texture(string id,bool optional){if(optional&&string.IsNullOrEmpty(id))return;Check(!string.IsNullOrEmpty(id)&&!id.Contains("..")&&Resources.Load<Texture2D>(id)!=null,"не найдена картинка "+id);}
            Frames(introduction,false);Frames(ending,false);
            foreach(var c in chapters){Check(catalog.Deck(c.opponentDeck)!=null,"неизвестная колода "+c.opponentDeck);Check(c.mode==MatchOptions.Wizards||c.mode==MatchOptions.Commanders,"неизвестный режим");Check(HeroOptions.Ids.Contains(c.opponentHero),"неизвестный аватар");Frames(c.before,true);Frames(c.after,false);}
        }
    }
    [Serializable] public sealed class CampaignProgress
    {
        public int schemaVersion=1,chapter,frame,line;
        public string phase="intro",deck="noise";
        public static string TestPath;
        public static string SavePath=>TestPath??Path.Combine(Application.persistentDataPath,"campaign-progress.json");
        public static CampaignProgress Read(CampaignBook book)
        {
            try{var p=JsonUtility.FromJson<CampaignProgress>(File.ReadAllText(SavePath));if(p.schemaVersion!=1||p.chapter<0||p.chapter>=book.chapters.Length||!new[]{"intro","before","battle","after","ending","done"}.Contains(p.phase))throw new FormatException();p.frame=Math.Max(0,p.frame);p.line=Math.Max(0,p.line);return p;}
            catch{return new CampaignProgress{deck=book.defaultDeck};}
        }
        public void Save()
        {
            try{Directory.CreateDirectory(Path.GetDirectoryName(SavePath));string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(this,true));if(File.Exists(SavePath))File.Replace(temp,SavePath,null);else File.Move(temp,SavePath);}
            catch(Exception e){Debug.LogWarning("Не удалось сохранить кампанию: "+e.Message);}
        }
    }
}
