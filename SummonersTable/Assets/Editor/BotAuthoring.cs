using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class BotAuthoring
    {
        [MenuItem("Summoners Table/Bots/Open Bots Manager")]
        public static void Open(){AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/BotsManager.prefab"));}
        public static void SetupAndTest()
        {
            Create();BotTests.Run();AssetDatabase.SaveAssets();Debug.Log("BOT_SETUP_AND_TEST_OK");
        }
        public static void Create()
        {
            const string path="Assets/Prefabs/Editable/UI/BotLobbyControls.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)
            {
                var root=new GameObject("Bot lobby controls",typeof(RectTransform),typeof(BotLobbyControls));var view=root.GetComponent<BotLobbyControls>();
                ((RectTransform)root.transform).sizeDelta=new Vector2(1600,1000);view.seats=new Button[4];view.labels=new Text[4];
                for(int i=0;i<4;i++)
                {
                    view.seats[i]=SharedButton.Create(root.transform,"Seat "+i,"БОТ / ИГРОК",new Vector2(-558+372*i,-358),new Vector2(325,32));
                    view.labels[i]=view.seats[i].GetComponent<SharedButton>().label;view.labels[i].fontSize=16;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
            }
            var lobby=PrefabUtility.LoadPrefabContents("Assets/Prefabs/LobbyCanvas.prefab");
            if(lobby.GetComponentInChildren<BotLobbyControls>(true)==null)
            {var ui=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),lobby.transform);((RectTransform)ui.transform).anchoredPosition=Vector2.zero;PrefabUtility.SaveAsPrefabAsset(lobby,"Assets/Prefabs/LobbyCanvas.prefab");}
            PrefabUtility.UnloadPrefabContents(lobby);
            const string managerPath="Assets/Prefabs/Managers/BotsManager.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(managerPath)==null)
            {
                var root=new GameObject("Bots Manager");var manager=root.AddComponent<AuthoringManager>();manager.section=ManagerSection.Bots;manager.Import(File.ReadAllText(ConfigAuthoring.Folder+"/bots.json"));PrefabUtility.SaveAsPrefabAsset(root,managerPath);Object.DestroyImmediate(root);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Authoring/LocationLab.unity");
            bool found=false;foreach(var manager in Object.FindObjectsByType<AuthoringManager>(FindObjectsSortMode.None))if(manager.section==ManagerSection.Bots)found=true;
            if(!found){PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(managerPath));EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
            ConfigAuthoring.CopyDefaults();EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
    }
}
