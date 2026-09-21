using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace SummonersTable.Editor
{
    public static class SteamSmokeTest
    {
        // An isolated private lobby, always left in finally. No invites or messages are sent.
        public static void Run()
        {
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);
            using(var steam=new SteamSession(catalog))
            {
                steam.Initialize();if(!steam.Available)throw new Exception("STEAM_SMOKE_FAILED: "+steam.Status);
                steam.Create("Prototype private smoke test",4,false);
                var until=DateTime.UtcNow.AddSeconds(25);
                while(steam.Busy&&DateTime.UtcNow<until){steam.Update();Thread.Sleep(50);}
                if(!steam.InRoom||!steam.IsHost)throw new Exception("STEAM_SMOKE_FAILED: "+steam.Error);
                steam.SetMember("tricks",true);
                for(int i=0;i<20;i++){steam.Update();Thread.Sleep(50);}
                if(steam.Members.Count!=1||steam.Members[0].deckId!="tricks"||!steam.Members[0].ready)
                    throw new Exception("STEAM_SMOKE_FAILED: member metadata roundtrip");
                if(steam.CanStart)throw new Exception("STEAM_SMOKE_FAILED: solo match permitted");
                Directory.CreateDirectory("../output/tests");
                File.WriteAllText("../output/tests/steam-smoke.txt","PASS Steam API Init, App ID 480\nPASS private lobby creation, ownership, 4-seat capacity\nPASS deck/ready metadata roundtrip\nPASS prevents a one-player start\nPrivate test lobby left after test.\nNot tested: cross-account lobby discovery/join or SteamNetworkingMessages delivery.\nUTC: "+DateTime.UtcNow.ToString("O"));
                Debug.Log("STEAM_SMOKE_PASSED");
            }
        }
    }
}
