using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerFinishing
    {
        static void ApplyFan()
        {
            string path=ConfigAuthoring.Root+"UI/GameInterface.prefab";var root=PrefabUtility.LoadPrefabContents(path);var hand=root.GetComponent<PrefabInterface>().hand;((RectTransform)hand.transform).anchoredPosition=new Vector2(0,-310);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            var config=ConfigJson.Read<InterfaceConfig>(System.IO.File.ReadAllText(ConfigAuthoring.Folder+"/interface.json"));if(config.fan.uiUnitsPerMetre==155)config.fan.uiUnitsPerMetre=240;
            ManagerStorage.SaveSection("interface.json",JsonUtility.ToJson(config,true));
            path="Assets/Prefabs/Managers/InterfaceManager.prefab";root=PrefabUtility.LoadPrefabContents(path);root.GetComponent<AuthoringManager>().ui=config;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=PrefabUtility.LoadPrefabContents(ConfigAuthoring.WorldPath);
            var old=root.GetComponent<ShaderStyleTarget>();if(old!=null)Object.DestroyImmediate(old);
            PrefabUtility.SaveAsPrefabAsset(root,ConfigAuthoring.WorldPath);PrefabUtility.UnloadPrefabContents(root);
            string uiPath=ConfigAuthoring.Root+"UI/GameInterface.prefab";root=PrefabUtility.LoadPrefabContents(uiPath);var ui=root.GetComponent<PrefabInterface>();
            foreach(var status in ui.playerStatus)if(status!=null&&!status.seatOwned)Object.DestroyImmediate(status.gameObject);
            ui.playerStatus=new PlayerStatusView[4];PrefabUtility.SaveAsPrefabAsset(root,uiPath);PrefabUtility.UnloadPrefabContents(root);
            ApplyFan();PlayerSettings.bundleVersion="0.8.0";
            string audioPath="Assets/Resources/AudioRig.prefab";root=PrefabUtility.LoadPrefabContents(audioPath);if(root.GetComponent<AudioListener>()==null)root.AddComponent<AudioListener>();PrefabUtility.SaveAsPrefabAsset(root,audioPath);PrefabUtility.UnloadPrefabContents(root);
            root=PrefabUtility.LoadPrefabContents(ConfigAuthoring.WorldPath);foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))Object.DestroyImmediate(listener);PrefabUtility.SaveAsPrefabAsset(root,ConfigAuthoring.WorldPath);PrefabUtility.UnloadPrefabContents(root);
            ConfigAuthoring.BuildAssetRegistry();ConfigAuthoring.CopyDefaults();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            ManagerEditorTests.Run();
        }
    }
}
