using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class ManagerLabCapture
    {
        static readonly string DirectoryPath="../output/editor-v0.8";
        public static void Run()
        {
            Directory.CreateDirectory(DirectoryPath);
            EditorSceneManager.OpenScene("Assets/Scenes/Authoring/LocationLab.unity");
            var board=Object.FindFirstObjectByType<TableBoard>();var manager=board.GetComponentInChildren<AuthoringManager>();
            manager.Import(File.ReadAllText(ConfigAuthoring.Folder+"/world.json"));
            var camera=board.tableCamera;camera.gameObject.SetActive(true);
            RenderSettings.ambientLight=new Color(.5f,.53f,.59f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            foreach(int count in new[]{5,8})
            {
                manager.previewPlayers=count;ManagerPreview.Refresh(manager);
                var data=ConfigBundle.Read(ConfigAuthoring.Folder).presentation.camera;data.initialCameraMode=2;board.PreviewCamera(data,0,count);
                Save(camera,"lab-"+count+"-seats");
                if(board.seating.Seats.Length!=count)throw new Exception("Edit preview seat count mismatch");
            }
            var ui=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/InterfaceManager.prefab"));
            var uiManager=ui.GetComponent<AuthoringManager>();uiManager.previewAnchor=new GameObject("Capture anchor").transform;uiManager.previewAnchor.position=new Vector3(0,8,0);
            uiManager.Import(File.ReadAllText(ConfigAuthoring.Folder+"/interface.json"));ManagerPreview.Play(uiManager);
            board.seating.Clear();board.authoredEnvironment.gameObject.SetActive(false);
            camera.transform.SetPositionAndRotation(new Vector3(0,12,-17),Quaternion.identity);camera.fieldOfView=44;
            Save(camera,"lab-interface-preview");ManagerPreview.StopAll();
            Object.DestroyImmediate(uiManager.previewAnchor.gameObject);Object.DestroyImmediate(ui);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("EDITOR_LAB_CAPTURE_PASSED");
        }
        static void Save(Camera camera,string name)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;
            var rt=new RenderTexture(1200,900,24);rt.Create();
            try
            {
                camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
                var frame=new Texture2D(1200,900,TextureFormat.RGB24,false);frame.ReadPixels(new Rect(0,0,1200,900),0,0);frame.Apply();
                File.WriteAllBytes(Path.Combine(DirectoryPath,name+".png"),frame.EncodeToPNG());Object.DestroyImmediate(frame);
            }
            finally{camera.targetTexture=previous;camera.rect=rect;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
