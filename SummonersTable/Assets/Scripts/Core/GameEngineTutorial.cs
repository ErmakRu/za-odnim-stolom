using System;
using System.Linq;
namespace SummonersTable
{
    public sealed partial class GameEngine
    {
        bool tutorialMode;
        public static GameEngine Tutorial(Catalog catalog,string deck)
        {
            var engine=new GameEngine(catalog,new[]{new LobbyMember{id="dream-jester",name="Шут-малыш",deckId=deck,heroId="deer"},new LobbyMember{id="dream-loskut",name="Лоскут",deckId=deck,heroId="badger",isBot=true}},1701,0,new MatchOptions{cards3D=true});
            engine.tutorialMode=true;engine.State.deadline=3600;return engine;
        }
        public void TutorialDraw()
        {
            if(!tutorialMode)throw new InvalidOperationException("Draw demonstration is available only in the tutorial.");
            Draw(0,1);State.revision++;
        }
    }
}
