using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;
namespace SummonersTable
{
    [Serializable] public sealed class LobbyBots { public List<LobbyMember> members=new List<LobbyMember>(); }
    public sealed partial class SteamSession
    {
        BotDirector botDirector;
        public static List<LobbyMember> ReadBots(string json,IList<LobbyMember> humans,int capacity,Catalog catalog,MatchOptions options,string matchId)
        {
            var result=new List<LobbyMember>();if(string.IsNullOrEmpty(json))return result;
            var saved=JsonUtility.FromJson<LobbyBots>(json);if(saved?.members==null)return result;
            foreach(var m in saved.members.Take(3))
            {
                if(result.Count>=Math.Min(4,capacity)-humans.Count)break;
                if(m==null||m.id==null||!m.id.StartsWith("bot-")||humans.Any(h=>h.id==m.id)||result.Any(h=>h.id==m.id)||catalog.Deck(m.deckId)==null||!HeroOptions.Valid(m.heroId))continue;
                m.isBot=true;m.name=Clean(m.name,28);m.outfit=HeroOptions.Outfit(m.outfit);m.palette=HeroOptions.Palette(m.palette);
                m.ready=true;m.readyRules=options.Signature;m.readyMatch=matchId??"lobby";result.Add(m);
            }
            return result;
        }
        void AppendBots(List<LobbyMember> humans)
        {
            try{humans.AddRange(ReadBots(SteamMatchmaking.GetLobbyData(lobby,"bots"),humans,Capacity,catalog,Options,View?.matchId));}
            catch(Exception e){Debug.LogWarning("Ignored invalid lobby bots: "+e.Message);}
        }
        public void ToggleBot(int index)
        {
            if(!IsHost||View!=null&&View.phase!="matchEnd"||index<0||index>=Capacity)return;
            var bots=Members.Where(m=>m.isBot).ToList();
            if(index<Members.Count){if(!Members[index].isBot)return;bots.RemoveAll(b=>b.id==Members[index].id);}
            else if(Members.Count<Math.Min(Capacity,Math.Min(4,catalog.world.playerRange.y)))
            {
                string id="bot-"+Guid.NewGuid().ToString("N");int n=bots.Count;
                bots.Add(new LobbyMember{id=id,isBot=true,name="Бот "+(n+1),deckId=catalog.decks[n%catalog.decks.Count].id,heroId=HeroOptions.Ids[n%HeroOptions.Ids.Length],ready=true});
            }
            SaveBots(bots);
        }
        public void SetBotAppearance(string id,string deck,string hero,int outfit,int palette)
        {
            if(!IsHost||View!=null&&View.phase!="matchEnd"||catalog.Deck(deck)==null||!HeroOptions.Valid(hero))return;
            var bots=Members.Where(m=>m.isBot).ToList();var m=bots.Find(b=>b.id==id);if(m==null)return;
            m.deckId=deck;m.heroId=hero;m.outfit=HeroOptions.Outfit(outfit);m.palette=HeroOptions.Palette(palette);SaveBots(bots);
        }
        void SaveBots(List<LobbyMember> bots)
        {
            if(!SteamMatchmaking.SetLobbyData(lobby,"bots",JsonUtility.ToJson(new LobbyBots{members=bots})))Error="Не удалось сохранить ботов в лобби.";
            RefreshMembers();
        }
    }
}
