using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class DreamAndHealthAuthoring
    {
        const string BarPath="Assets/Prefabs/Editable/UI/HeroHealthBar.prefab";
        static GameObject barPrefab;static Material sky;
        static void Edit(string path){var root=PrefabUtility.LoadPrefabContents(path);try{ApplyRoot(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        static HeroHealthBar Replace(RectTransform old)
        {
            var existing=old.GetComponent<HeroHealthBar>();if(existing!=null)return existing;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(barPrefab,old.parent);
            var rect=(RectTransform)root.transform;rect.SetSiblingIndex(old.GetSiblingIndex());
            rect.anchorMin=old.anchorMin;rect.anchorMax=old.anchorMax;rect.pivot=old.pivot;
            rect.localPosition=old.localPosition;rect.localRotation=old.localRotation;rect.localScale=old.localScale;rect.sizeDelta=new Vector2(332,43);
            root.layer=old.gameObject.layer;foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
            Object.DestroyImmediate(old.gameObject);return root.GetComponent<HeroHealthBar>();
        }
        static void ApplyRoot(GameObject root)
        {
            foreach(var local in root.GetComponentsInChildren<LocalHeroHud>(true))
            {var bar=Replace((RectTransform)local.healthText.transform.parent);local.healthFill=bar.fill;local.healthText=bar.value;EditorUtility.SetDirty(local);}
            foreach(var status in root.GetComponentsInChildren<PlayerStatusView>(true))
            {var bar=Replace(status.healthAnchor);status.healthAnchor=(RectTransform)bar.transform;status.healthFill=bar.fill;status.healthTrail=bar.trail;status.healthNumber=bar.value;EditorUtility.SetDirty(status);}
            foreach(var seat in root.GetComponentsInChildren<PlayerSeatView>(true))
            {var group=seat.body!=null?seat.body.Find("Character placement"):null;if(group!=null)group.localPosition=new Vector3(0,0,-2);}
            foreach(var board in root.GetComponentsInChildren<TableBoard>(true))
            {
                var backdrop=board.GetComponent<DreamTableBackdrop>();if(backdrop==null)backdrop=board.gameObject.AddComponent<DreamTableBackdrop>();
                backdrop.view=board.tableCamera;backdrop.dreamSky=sky;
                backdrop.surroundings=board.authoredEnvironment.Cast<Transform>().Where(t=>t.name=="Floor"||t.name.StartsWith("TavernEnvironment")).Select(t=>t.gameObject).ToArray();EditorUtility.SetDirty(backdrop);
            }
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
            barPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(BarPath);
            if(barPrefab==null)
            {
                var hud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/UI/MatchHUD.prefab").GetComponent<LocalHeroHud>();
                var clone=Object.Instantiate(hud.healthText.transform.parent.gameObject);clone.name="Hero health bar";
                var bar=clone.AddComponent<HeroHealthBar>();bar.fill=clone.GetComponentsInChildren<Image>(true).First(x=>x.name=="Health fill");bar.value=clone.GetComponentInChildren<Text>(true);
                var trail=Object.Instantiate(bar.fill,bar.fill.transform.parent);trail.name="Damage trail";trail.transform.SetAsFirstSibling();trail.color=new Color(.7f,.32f,.1f,1);bar.trail=trail;
                bar.value.color=Color.black;bar.value.text="HP  30 / 30";
                barPrefab=PrefabUtility.SaveAsPrefabAsset(clone,BarPath);Object.DestroyImmediate(clone);
            }
            const string skyPath="Assets/Resources/Styles/DreamSky.mat";
            sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(sky==null){sky=new Material(Shader.Find("SummonersTable/Dream Background"));AssetDatabase.CreateAsset(sky,skyPath);}sky.mainTexture=Resources.Load<Texture2D>("Art/bg_barn_sepia");EditorUtility.SetDirty(sky);
            foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
            {var path=AssetDatabase.GUIDToAssetPath(id);if(path==BarPath)continue;var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(p.GetComponentInChildren<LocalHeroHud>(true)!=null||p.GetComponentInChildren<PlayerStatusView>(true)!=null||p.GetComponentInChildren<TableBoard>(true)!=null)Edit(path);}
            var settings=Resources.Load<PresentationSettings>("PresentationSettings");settings.attackTrailWidthMultiplier=8;settings.attackDistortionWidthMultiplier=12;EditorUtility.SetDirty(settings);
            var previous=SceneManager.GetActiveScene();
            foreach(var name in new[]{"MainMenu","Lobby","Match","PresentationLab"})
            {
                string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                var backup=AssetDatabase.GenerateUniqueAssetPath("Assets/EditorSnapshots/"+name+"-before-dream-hp.unity");if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
                foreach(var root in scene.GetRootGameObjects())ApplyRoot(root);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return "Saved common HP prefab, dream sky, chair offset and attack ribbon widths";
        }
    }
}
