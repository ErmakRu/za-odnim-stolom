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
    public static class PresentationCorrectionsAuthoring
    {
        static T Ensure<T>(GameObject o) where T:Component {var c=o.GetComponent<T>();return c!=null?c:o.AddComponent<T>();}
        static void Animate(GameObject o,float offset=18){Ensure<CanvasGroup>(o);Ensure<UiEntrance>(o).offset=offset;}
        static void Edit(string path,Action<GameObject> action){var root=PrefabUtility.LoadPrefabContents(path);try{action(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        static void Labels(PlayerStatusView status)
        {
            status.healthNumber.color=Color.black;
            foreach(var graphic in status.nameAnchor.GetComponentsInChildren<Graphic>(true))if(!(graphic is Text))graphic.enabled=false;
            var outline=Ensure<Outline>(status.nickname.gameObject);outline.effectColor=new Color(.08f,.035f,.012f,1);outline.effectDistance=new Vector2(2,-2);
        }
        static void Seat(PlayerSeatView seat)
        {
            if(seat.body==null||seat.avatar==null)return;
            var group=seat.body.Find("Character placement");
            if(group==null)
            {
                group=new GameObject("Character placement").transform;group.SetParent(seat.body,false);
                foreach(var item in new[]{seat.chair,seat.avatar.transform,seat.heroTarget,seat.nameAnchor,seat.healthAnchor})if(item!=null)item.SetParent(group,true);
                group.localPosition=new Vector3(0,0,-4);
            }
            if(seat.status!=null)Labels(seat.status);
        }
        static void Polish(GameObject root)
        {
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))Ensure<CanvasRenderer>(graphic.gameObject);
            foreach(var status in root.GetComponentsInChildren<PlayerStatusView>(true))Labels(status);
            foreach(var local in root.GetComponentsInChildren<LocalHeroHud>(true))
            {
                local.healthText.color=Color.black;
                var hud=local.GetComponent<WidgetScreen>();var phase=hud.Get<RectTransform>("phase");
                phase.anchorMin=phase.anchorMax=phase.pivot=new Vector2(0,1);phase.anchoredPosition=new Vector2(385,-17);phase.sizeDelta=new Vector2(790,34);
            }
            foreach(var seat in root.GetComponentsInChildren<PlayerSeatView>(true))Seat(seat);
            foreach(var front in root.GetComponentsInChildren<FrontEndCanvas>(true))Animate(front.gameObject);
            foreach(var screen in root.GetComponentsInChildren<WidgetScreen>(true))Animate(screen.gameObject);
            foreach(var comic in root.GetComponentsInChildren<CampaignComicView>(true)){Animate(comic.comic);Animate(comic.hub);Animate(comic.dialoguePanel.gameObject,0);}
            foreach(var tutorial in root.GetComponentsInChildren<TutorialPanel>(true)){Animate(tutorial.callout.gameObject,0);Animate(tutorial.secondaryCallout.gameObject,0);}
            var sound=typeof(RequestedGamePolish).GetMethod("Sound",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);sound.Invoke(null,new object[]{root});
        }
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            var dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/ThirdParty/VFX/Vefects/Trails VFX URP/VFX/Materials/M_VFX_URP_Trail_Dark_02.mat");
            foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(id);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab.GetComponentInChildren<WorldArrowView>(true)!=null)Edit(path,root=>{foreach(var arrow in root.GetComponentsInChildren<WorldArrowView>(true))arrow.magicMaterial=dark;});
            }
            foreach(var path in new[]{"Assets/Prefabs/Editable/World/PlayerSeat.prefab","Assets/Prefabs/Editable/UI/PlayerStatus.prefab","Assets/Prefabs/Editable/UI/LobbyPlayerPanel.prefab","Assets/Prefabs/LobbyCanvas.prefab","Assets/Prefabs/MainMenuCanvas.prefab","Assets/Prefabs/Editable/UI/SettingsPanel.prefab","Assets/Prefabs/Editable/UI/MatchHUD.prefab"})Edit(path,Polish);
            var previous=SceneManager.GetActiveScene();
            foreach(var name in new[]{"MainMenu","Lobby","Match","PresentationLab"})
            {
                string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())Polish(root);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return "Saved character placement, labels and interface animations";
        }
    }
}
