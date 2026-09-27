using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class TavernEnvironmentAuthoring
    {
        const string Base="Assets/ThirdParty/3dModels/LowPolyFantasyVillage/Prefabs/";
        const string Destination="Assets/Prefabs/Editable/World/TavernEnvironment.prefab";
        static Bounds Bounds(GameObject root){var renderers=root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds;}
        static GameObject Prop(Transform parent,string asset,string name,Vector3 pos,Vector3 size,float yaw=0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+asset+".prefab");if(prefab==null)throw new InvalidOperationException(asset);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.transform.localPosition=Vector3.zero;var b=Bounds(g);
            g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));b=Bounds(g);
            g.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);g.transform.RotateAround(Vector3.zero,Vector3.up,yaw);g.transform.position+=pos;
            foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var t in g.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
            return g;
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Destination)!=null)throw new InvalidOperationException("Tavern already authored; edit the prefab manually.");
            var root=new GameObject("Tavern — authored props");
            try
            {
                var floor=new GameObject("Oak plank floor");floor.transform.SetParent(root.transform,false);
                for(int row=0;row<30;row++)for(int col=0;col<8;col++)Prop(floor.transform,"Props/Board_01","Plank "+row+"-"+col,new Vector3(-15.75f+col*4.5f,-.25f,-16+row*1.1f),new Vector3(4.48f,.20f,1.08f));
                var walls=new GameObject("Walls and beams");walls.transform.SetParent(root.transform,false);
                for(int i=0;i<4;i++)
                {
                    Prop(walls.transform,"HouseParts/Wall_03_Window","Back window bay "+i,new Vector3(-13.5f+i*9,0,18),new Vector3(9,12,.45f));
                    foreach(int side in new[]{-1,1})Prop(walls.transform,"HouseParts/Wall_01","Side bay "+side+"-"+i,new Vector3(side*18,0,-13.5f+i*9),new Vector3(9,12,.45f),90);
                }
                for(int i=0;i<5;i++)Prop(walls.transform,"Props/Board_01","Back timber beam "+i,new Vector3(-18+i*9,0,17.6f),new Vector3(.6f,12,.65f));
                var bar=new GameObject("Bar and back shelves");bar.transform.SetParent(root.transform,false);
                Prop(bar.transform,"Props/Table_01","Oak bar counter",new Vector3(8,0,13.1f),new Vector3(13,5.5f,3));
                Prop(bar.transform,"Props/Shelving_01","Bottle shelves",new Vector3(8,0,16.3f),new Vector3(11,8.5f,1.5f));
                for(int i=0;i<8;i++)Prop(bar.transform,"Props/Bottle_0"+(i%4+1),"Bottle "+i,new Vector3(3+i*1.4f,5.5f,13.2f),new Vector3(.38f,1.0f,.38f),i*39);
                for(int i=0;i<6;i++)Prop(root.transform,"Props/Barrel_0"+(i%2+1),"Ale barrel "+i,new Vector3(-15+(i%3)*2.4f,0,12+(i/3)*2.8f),new Vector3(2.1f,3.1f,2.1f),i*31);
                foreach(int side in new[]{-1,1})
                {
                    Prop(root.transform,"Props/Table_01","Side guest table "+side,new Vector3(side*14,0,1),new Vector3(4,4.2f,3));
                    Prop(root.transform,"Props/Bench_01","Guest bench "+side,new Vector3(side*14,0,-2),new Vector3(4,2.5f,1));
                    Prop(root.transform,"Props/Candle_01","Guest table candle "+side,new Vector3(side*14,4.2f,1),new Vector3(.6f,1.2f,.6f));
                    var glow=new GameObject("Warm candle light "+side,typeof(Light));glow.transform.SetParent(root.transform,false);glow.transform.localPosition=new Vector3(side*13,7,4);var light=glow.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.66f,.30f);light.intensity=4;light.range=17;light.shadows=LightShadows.None;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Destination);
            }
            finally{Object.DestroyImmediate(root);}
            var previous=SceneManager.GetActiveScene();
            foreach(string name in new[]{"MainMenu","Lobby","Match"})
            {
                string path="Assets/Scenes/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                if(scene.isDirty)throw new InvalidOperationException("Unsaved scene "+path);
                var board=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TableBoard>(true)).Single();
                var tavern=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Destination),board.authoredEnvironment);tavern.transform.localPosition=Vector3.zero;
                var old=board.authoredEnvironment.Find("Floor");if(old!=null)old.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();Debug.Log("TAVERN_AUTHORED_FROM_EXISTING_TEXTURED_PROPS");
        }
    }
}
