using System;
using System.Linq;
using UnityEngine;
using SummonersTable.Story;
namespace SummonersTable
{
    public static class ApprovedOpening
    {
        public static void Apply(CampaignBook book)
        {
            var asset=Resources.Load<TextAsset>("Campaign/approved-opening");
            if(asset==null)throw new InvalidOperationException("Missing approved campaign opening");
            var story=JsonUtility.FromJson<StoryConfig>(asset.text);
            ComicFrame Frame(StoryStep step)=>new ComicFrame{id=step.id,title="",background=Background(step.background),scriptedStaging=true,sprites=step.sprites.ToArray(),lines=new[]{new ComicLine{
                sourceId=step.id,speaker=step.speaker,text=step.text,kind=step.speaker=="РАССКАЗЧИК"?"narration":"dialogue",
                sfx=step.audio.sfx,ambience=step.audio.bgm,actionType=step.action.actionType,actionTarget=step.action.target,actionParameter=step.action.parameter}}};
            book.introduction=story.scenes.Take(3).SelectMany(s=>s.steps).Select(Frame).ToArray();
            book.tutorial=story.scenes[1].steps.Select(Frame).ToArray();
            for(int i=0;i<2;i++)
            {
                var steps=story.scenes[i+3].steps;int battle=steps.FindIndex(s=>s.action.actionType=="START_BATTLE");
                book.chapters[i].before=steps.Take(battle+1).Select(Frame).ToArray();book.chapters[i].after=steps.Skip(battle+1).Select(Frame).ToArray();
            }
            book.textPageLength=200;
        }
        // Existing backgrounds are used until dedicated illustrations are authored.
        static string Background(string key)
        {
            if(Resources.Load<Texture2D>("Art/"+key)!=null)return "Art/"+key;
            if(key=="bg_black")return "";
            return key=="bg_throne_hall"?"Art/menu":"Art/board";
        }
    }
}
