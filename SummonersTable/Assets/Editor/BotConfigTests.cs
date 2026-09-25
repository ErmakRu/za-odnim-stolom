using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class BotConfigTests
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("BOT CONFIG: "+message);}
        public static void Run()
        {
            string folder=Path.GetFullPath("../tmp/config-work/bots-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            foreach(string file in ConfigBundle.Files)File.Copy(ConfigAuthoring.Folder+"/"+file,folder+"/"+file);
            var go=new GameObject("Bots configuration test");var manager=go.AddComponent<AuthoringManager>();manager.section=ManagerSection.Bots;
            try
            {
                manager.Import(File.ReadAllText(folder+"/bots.json"));Check(manager.FileName=="bots.json"&&manager.Data==manager.bots,"section mapping");
                string cards=File.ReadAllText(folder+"/cards.json");manager.bots.profiles[0].weights.attack=4;
                string saved=ManagerStorage.Copy(manager,folder,DateTime.UtcNow.AddSeconds(-2));manager.bots.profiles[0].weights.attack=7;
                Check(ManagerStorage.Latest(manager,folder)==saved&&manager.bots.profiles[0].weights.attack==4,"section rollback");
                ManagerStorage.Save(manager,folder);var loaded=ConfigBundle.Read(folder);Check(loaded.bots.profiles[0].weights.attack==4,"typed save/read");
                Check(File.ReadAllText(folder+"/main.json").Contains("\"bots\"")&&File.ReadAllText(folder+"/cards.json")==cards,"snapshot and isolation");
                string before=File.ReadAllText(folder+"/bots.json");manager.bots.profiles[0].nodes[0].children=new[]{"root"};bool failed=false;
                try{ManagerStorage.Save(manager,folder);}catch(FormatException){failed=true;}
                Check(failed&&File.ReadAllText(folder+"/bots.json")==before,"invalid tree saves nothing");
                var inspector=UnityEditor.Editor.CreateEditor(manager);Check(inspector is AuthoringManagerInspector,"typed inspector");Object.DestroyImmediate(inspector);
                var jsonAsset=AssetDatabase.LoadAssetAtPath<DefaultAsset>(ConfigAuthoring.Folder+"/bots.json");var jsonEditor=UnityEditor.Editor.CreateEditor(jsonAsset);Check(jsonEditor is ConfigJsonInspector,"JSON inspector");Object.DestroyImmediate(jsonEditor);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/BotsManager.prefab");Check(prefab.GetComponent<AuthoringManager>().section==ManagerSection.Bots,"manager prefab");
                var lobby=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyCanvas.prefab");Check(lobby.GetComponentInChildren<BotLobbyControls>(true).seats.Length==4,"four reusable lobby controls");
                Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/bots-config.txt","PASS Bots Manager and JSON Inspector; copy/rollback, save/main snapshot, section isolation, cyclic tree atomic rejection, authored manager/lobby prefab references.\n");Debug.Log("BOT_CONFIG_TEST_OK");
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}