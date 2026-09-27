using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace SummonersTable.Editor
{
    public static class HumanEditableScenes
    {
        const string World="Assets/Prefabs/TableWorld.prefab";
        const string UI="Assets/Prefabs/Editable/UI/";
        static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();if(c==null)c=go.AddComponent<T>();return c;}
        static void EditPrefab(string path,Action<GameObject> change)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Buttons(GameObject root)
        {
            foreach(var b in root.GetComponentsInChildren<Button>(true))
            {
                var feel=Ensure<ButtonFeel>(b.gameObject);var audio=Ensure<AudioSource>(b.gameObject);audio.playOnAwake=false;audio.spatialBlend=0;
                feel.hoverSound=ConfigRuntime.Clip("card.hover");feel.clickSound=ConfigRuntime.Clip("qte.correct");feel.soundVolume=.18f;
            }
        }
        [MenuItem("Summoners Table/Authoring/Apply saved scenes upgrade (once)")]
        public static void Apply()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/World/Player.prefab")!=null)throw new InvalidOperationException("Already migrated to player prefab; do not regenerate scenes.");
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(World).GetComponent<TableBoard>().seating.HasAuthoredLayouts)throw new InvalidOperationException("Already migrated. Edit the saved scenes/prefabs directly; do not regenerate them.");
            ConfigRuntime.LoadInitial();
            EditPrefab("Assets/Resources/UI/CommonButton.prefab",Buttons);
            foreach(var name in new[]{"MatchHUD","HistoryPanel","SettingsPanel","MatchResults","LocalHandoff","RulesPanel","ExitConfirmation","SteamSearch","CardBrowser"})
                EditPrefab(UI+name+".prefab",root=>{Ensure<Canvas>(root);Ensure<CanvasScaler>(root).referenceResolution=new Vector2(1600,1000);Ensure<GraphicRaycaster>(root);Buttons(root);});
            EditPrefab(UI+"HandFan.prefab",root=>{var r=(RectTransform)root.transform;r.anchoredPosition+=Vector2.down*45;});
            EditPrefab(UI+"GameInterface.prefab",root=>{Ensure<StartupCanvasGate>(root);Buttons(root);});
            foreach(var path in new[]{"Assets/Prefabs/MainMenuCanvas.prefab","Assets/Prefabs/LobbyCanvas.prefab"})
                EditPrefab(path,root=>
                {
                    Buttons(root);
                    foreach(var portrait in root.GetComponentsInChildren<LobbyPortrait>(true))
                    {
                        portrait.actor.Configure("badger",0,0,false);portrait.actor.Animator.Rebind();portrait.actor.Animator.Update(0);portrait.actor.EditorPose();
                    }
                });
            EditPrefab(World,root=>
            {
                var board=root.GetComponent<TableBoard>();var seating=board.seating;
                if(!seating.HasAuthoredLayouts)
                {
                    seating.Clear();var layouts=new TableLayout[3];
                    for(int n=2;n<=4;n++)
                    {
                        var go=new GameObject(n+" players — editable positions");go.transform.SetParent(seating.transform,false);
                        var builder=go.AddComponent<SeatingLayout>();builder.seatPrefab=seating.seatPrefab;builder.radius=seating.radius;builder.startAngle=seating.startAngle;builder.clockwise=seating.clockwise;builder.bodyScale=seating.bodyScale;
                        var layout=builder.Build(n,seating.bodyScale,true);layouts[n-2]=layout;
                        foreach(var t in go.GetComponentsInChildren<Transform>(true)){t.gameObject.hideFlags=HideFlags.None;foreach(var c in t.GetComponents<Component>())if(c!=null)c.hideFlags=HideFlags.None;}
                        foreach(var seat in builder.Seats)
                        {
                            for(int i=0;i<seat.slots.Length;i++)
                            {
                                var anchor=seat.slots[i];var p=anchor.position;var center=new Vector3(board.transform.position.x,p.y,board.transform.position.z);
                                // Move the existing zones inward, preserving seat angles and slot order.
                                anchor.position=Vector3.Lerp(center,p,.84f);
                            }
                        }
                        UnityEngine.Object.DestroyImmediate(builder);go.SetActive(n==2);
                    }
                    seating.authoredLayouts=layouts;seating.previewCount=2;seating.layout=layouts[0];board.layouts=layouts;
                    seating.name="Saved seating layouts — choose previewCount";
                }
            });
            var previous=SceneManager.GetActiveScene();
            foreach(var name in new[]{"MainMenu","Lobby","Match","PresentationLab"})
            {
                string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
                if(!opened&&scene.isDirty)throw new InvalidOperationException("Save user edits first: "+path);
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var app=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameApp>(true)).FirstOrDefault();
                if(app==null)app=new GameObject("Управление игрой").AddComponent<GameApp>();
                app.entryScreen=name=="Lobby"?GameApp.EntryScreen.Lobby:name=="Match"?GameApp.EntryScreen.Match:GameApp.EntryScreen.Menu;
                foreach(var binding in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrefabConfigBinding>(true)))binding.applyJsonOverrides=false;
                EditorSceneManager.SaveScene(scene);
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            AssetDatabase.SaveAssets();Debug.Log("HUMAN_EDITABLE_SCENES_SAVED");
        }
        public static string Validate()
        {
            var board=AssetDatabase.LoadAssetAtPath<GameObject>(World).GetComponent<TableBoard>();
            if(board.seating.previewInEditor)throw new Exception("Player preview should default to off");
            if(board.GetComponentsInChildren<PlayerSeatView>(true).Length!=0)throw new Exception("Players must not be saved in TableWorld");
            var player=board.seating.seatPrefab;
            if(player==null||player.avatar==null||player.slots.Length!=5)throw new Exception("Incomplete player prefab");
            foreach(var name in new[]{"MatchHUD","HistoryPanel","SettingsPanel","MatchResults","LocalHandoff","RulesPanel","ExitConfirmation","SteamSearch","CardBrowser"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>(UI+name+".prefab").GetComponent<Canvas>()==null)throw new Exception("Missing screen Canvas: "+name);
            if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/CommonButton.prefab").GetComponent<ButtonFeel>()==null)throw new Exception("Missing button response");
            return "PASS: player prefab, no saved players, optional preview, 9 Canvas screens, button feel";
        }
    }
}
