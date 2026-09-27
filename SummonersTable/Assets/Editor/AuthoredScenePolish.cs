using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace SummonersTable.Editor
{
    public static class AuthoredScenePolish
    {
        static void Edit(string path,Action<GameObject> work){var root=PrefabUtility.LoadPrefabContents(path);try{work(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        public static void Apply()
        {
            Edit("Assets/Prefabs/MainMenuCanvas.prefab",root=>
            {
                foreach(string name in new[]{"Background","Tint"}){var r=root.transform.Find(name) as RectTransform;if(r==null)continue;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
                var controls=root.GetComponentInChildren<ConfigControlsView>(true);if(controls!=null)controls.gameObject.SetActive(false);
            });
            const string borderPath="Assets/Settings/Board zone frame.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(borderPath);
            if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Editable/World/Outline.mat"));material.name="Board zone frame";material.color=new Color(.78f,.64f,.34f);AssetDatabase.CreateAsset(material,borderPath);}
            Edit("Assets/Prefabs/TableWorld.prefab",root=>
            {
                var board=root.GetComponent<TableBoard>();
                foreach(var layout in board.seating.authoredLayouts)foreach(var anchor in layout.slotAnchors)
                {
                    if(anchor.Find("Gold frame — editable")!=null)continue;
                    var seat=anchor.GetComponentInParent<PlayerSeatView>(true);var tangent=seat.transform.right;var relative=anchor.position-layout.transform.position;
                    anchor.position+=tangent*(Vector3.Dot(relative,tangent)*(1/.84f-1));
                    var frame=new GameObject("Gold frame — editable",typeof(MeshFilter),typeof(MeshRenderer));frame.transform.SetParent(anchor,false);
                    frame.GetComponent<MeshFilter>().sharedMesh=anchor.GetComponent<MeshFilter>().sharedMesh;frame.GetComponent<MeshRenderer>().sharedMaterial=material;
                    frame.transform.localPosition=new Vector3(0,-.1f,0);frame.transform.localScale=new Vector3(1.05f,.75f,1.05f);
                }
            });
            var previous=SceneManager.GetActiveScene();
            foreach(string name in new[]{"Match","PresentationLab"})
            {
                var scene=SceneManager.GetSceneByPath("Assets/Scenes/"+name+".unity");bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity",OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                if(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignComicView>(true)).Any())
                {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Campaign/Resources/Campaign/CampaignComic.prefab");var comic=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);comic.name="CampaignComic — editable screen";comic.SetActive(false);}
                if(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ConfigAudio>(true)).Any())
                {var audio=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/AudioRig.prefab"),scene);audio.name="AudioRig — scene audio buses";}
                EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            AssetDatabase.SaveAssets();
        }
    }
}
