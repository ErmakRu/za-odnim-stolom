using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    // Explicit offscreen Editor rendering avoids black hidden-window ScreenCapture frames.
    public static class EditorFrameCapture
    {
        public static void Save(string path)
        {
            int width=Math.Max(800,Screen.width),height=Math.Max(500,Screen.height);
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
            var overlays=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.isActiveAndEnabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var layers=new Dictionary<GameObject,int>();var cameraObject=new GameObject("Editor capture UI");var ui=cameraObject.AddComponent<Camera>();
            ui.enabled=false;ui.clearFlags=CameraClearFlags.Depth;ui.cullingMask=1<<31;ui.nearClipPlane=.01f;ui.farClipPlane=50;ui.targetTexture=target;
            ui.transform.position=new Vector3(0,0,-10);var active=RenderTexture.active;
            try
            {
                RenderTexture.active=target;GL.Clear(true,true,new Color(.06f,.09f,.12f));
                foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c!=ui&&c.isActiveAndEnabled&&c.targetTexture==null).OrderBy(c=>c.depth))
                {camera.targetTexture=target;try{camera.Render();}finally{camera.targetTexture=null;}}
                foreach(var canvas in overlays)
                {
                    var objects=canvas.GetComponentsInChildren<Graphic>(true).Select(g=>g.gameObject).Concat(canvas.GetComponentsInChildren<Canvas>(true).Select(c=>c.gameObject));
                    foreach(var obj in objects)if(!layers.ContainsKey(obj)){layers.Add(obj,obj.layer);obj.layer=31;}
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=1;
                }
                Canvas.ForceUpdateCanvases();ui.Render();RenderTexture.active=target;
                var frame=new Texture2D(width,height,TextureFormat.RGB24,false);frame.ReadPixels(new Rect(0,0,width,height),0,0);frame.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,frame.EncodeToPNG());Object.DestroyImmediate(frame);
            }
            finally
            {
                foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
                foreach(var layer in layers)if(layer.Key!=null)layer.Key.layer=layer.Value;
                Canvas.ForceUpdateCanvases();RenderTexture.active=active;Object.DestroyImmediate(cameraObject);target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
