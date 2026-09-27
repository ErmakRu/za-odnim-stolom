using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace SummonersTable.Editor
{
    public static class CountdownAuthoring
    {
        static void ApplyRoot(GameObject root)
        {
            foreach(var hud in root.GetComponentsInChildren<LocalHeroHud>(true))
            {
                var screen=hud.GetComponent<WidgetScreen>();
                var timer=screen.Get<Text>("timer");var phase=screen.Get<Text>("phase");
                timer.alignment=TextAnchor.MiddleLeft;timer.rectTransform.anchoredPosition=new Vector2(phase.rectTransform.anchoredPosition.x,-58);timer.rectTransform.sizeDelta=new Vector2(360,28);EditorUtility.SetDirty(timer);
                if(hud.countdown!=null)continue;
                var go=new GameObject("Last seconds",typeof(RectTransform),typeof(UrgentCountdown));go.layer=hud.gameObject.layer;
                var rect=(RectTransform)go.transform;rect.SetParent(hud.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-125);rect.sizeDelta=new Vector2(140,130);
                var digit=new GameObject("Animated number",typeof(RectTransform),typeof(Text),typeof(Outline));digit.layer=go.layer;var dr=(RectTransform)digit.transform;dr.SetParent(rect,false);dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.sizeDelta=Vector2.zero;
                var text=digit.GetComponent<Text>();text.font=hud.healthText.font;text.fontSize=100;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.text="9";text.enabled=false;
                var outline=digit.GetComponent<Outline>();outline.effectColor=new Color(.15f,.055f,.015f,.95f);outline.effectDistance=new Vector2(3,-3);
                var countdown=go.GetComponent<UrgentCountdown>();countdown.number=text;countdown.animatedDigit=dr;hud.countdown=countdown;EditorUtility.SetDirty(hud);
            }
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene: "+SceneManager.GetSceneAt(i).path);
            const string path="Assets/Prefabs/Editable/UI/MatchHUD.prefab";
            AssetDatabase.CopyAsset(path,AssetDatabase.GenerateUniqueAssetPath("Assets/EditorSnapshots/MatchHUD-before-countdown.prefab"));
            var prefab=PrefabUtility.LoadPrefabContents(path);try{ApplyRoot(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var previous=SceneManager.GetActiveScene();
            foreach(var name in new[]{"MainMenu","Lobby","Match","PresentationLab"})
            {
                string scenePath="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
                if(!EditorSceneManager.SaveScene(scene,AssetDatabase.GenerateUniqueAssetPath("Assets/EditorSnapshots/"+name+"-before-countdown.unity"),true))throw new Exception("Backup failed");
                foreach(var root in scene.GetRootGameObjects())ApplyRoot(root);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return "Saved countdown in HUD and four scenes";
        }
    }
}
