using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    // URP base-camera requests clear colour even with CameraClearFlags.Depth.
    // Render the table to one target, then show that target behind the actual UI canvases.
    public static class CampaignFrameCapture
    {
        public static void Save(string path)
        {
            var previousTarget=RenderTexture.active;
            const int width=1600,height=1000;
            var world=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);world.Create();
            var final=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);final.Create();
            var overlays=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.isActiveAndEnabled&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var layers=new Dictionary<GameObject,int>();var cameraObject=new GameObject("Campaign capture UI");var camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.nearClipPlane=.01f;camera.farClipPlane=50;camera.targetTexture=final;
            camera.transform.position=new Vector3(0,0,-10);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var backdrop=new GameObject("Captured world",typeof(RectTransform),typeof(Canvas));backdrop.layer=31;
            var canvas=backdrop.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;canvas.sortingOrder=-30000;
            var image=new GameObject("World image",typeof(RectTransform),typeof(RawImage));image.layer=31;image.transform.SetParent(backdrop.transform,false);
            var r=(RectTransform)image.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;image.GetComponent<RawImage>().texture=world;
            try
            {
                RenderTexture.active=world;GL.Clear(true,true,new Color(.035f,.05f,.06f));
                var table=Object.FindFirstObjectByType<TableBoard>();
                if(table!=null&&table.gameObject.activeInHierarchy)
                {
                    var c=table.ViewCamera;var old=c.targetTexture;c.targetTexture=world;
                    try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=world});}finally{c.targetTexture=old;}
                }
                foreach(var overlay in overlays)
                {
                    foreach(var obj in overlay.GetComponentsInChildren<Graphic>(true).Select(g=>g.gameObject).Concat(overlay.GetComponentsInChildren<Canvas>(true).Select(c=>c.gameObject)))
                        if(!layers.ContainsKey(obj)){layers.Add(obj,obj.layer);obj.layer=31;}
                    overlay.renderMode=RenderMode.ScreenSpaceCamera;overlay.worldCamera=camera;overlay.planeDistance=1;
                }
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=final});RenderTexture.active=final;
                var frame=new Texture2D(width,height,TextureFormat.RGB24,false);frame.ReadPixels(new Rect(0,0,width,height),0,0);frame.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,frame.EncodeToPNG());Object.DestroyImmediate(frame);
            }
            finally
            {
                foreach(var overlay in overlays){overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.worldCamera=null;}
                foreach(var layer in layers)if(layer.Key!=null)layer.Key.layer=layer.Value;
                camera.targetTexture=null;image.GetComponent<RawImage>().texture=null;
                Object.DestroyImmediate(backdrop);Object.DestroyImmediate(cameraObject);Canvas.ForceUpdateCanvases();RenderTexture.active=null;
                world.Release();RenderTexture.active=null;final.Release();RenderTexture.active=null;Object.DestroyImmediate(world);Object.DestroyImmediate(final);RenderTexture.active=previousTarget;
            }
        }
    }
}
