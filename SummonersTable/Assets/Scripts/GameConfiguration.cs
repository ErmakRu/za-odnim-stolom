using UnityEngine;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public void ReloadConfiguration()
        {
            if(ConfigRuntime.Reload()){RefreshConfigBetweenMatches();browserKey="";}error=ConfigRuntime.Message;
        }
        void RefreshConfigBetweenMatches()
        {
            if(!ConfigRuntime.Available||state!=null&&state.phase!="matchEnd"&&page=="game")return;
            if(ConfigRuntime.ActivateGameplay())
            {
                catalog=ConfigRuntime.ActiveCatalog;board.SetCatalog(catalog);steam.SetCatalog(catalog);browserKey="";
                for(int i=0;i<localDecks.Length;i++)localDecks[i]=Mathf.Clamp(localDecks[i],0,catalog.decks.Count-1);
            }
        }
    }
}
