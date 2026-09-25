using System;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    // Read back the actual UI shader: two source landmarks must remain a rigid pair.
    public static class LayeredMotionTests
    {
        public static void Run()
        {
            const int size=512;var mat=LayeredArtMaterial.Create();
            Texture2D Texture(Func<int,int,Color> pixel){var t=new Texture2D(size,size,TextureFormat.RGBA32,false,true);var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=pixel(x,y);t.SetPixels(pixels);t.Apply();t.wrapMode=TextureWrapMode.Clamp;return t;}
            var background=Texture((x,y)=>x>236&&x<276?Color.blue:Color.black);
            var rear=Texture((x,y)=>x>116&&x<156&&y>170&&y<210?Color.red:Color.clear);
            var foreground=Texture((x,y)=>x>310&&x<350&&y>342&&y<382?Color.green:Color.clear);
            var rt=new RenderTexture(size,size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);rt.Create();
            var readback=new Texture2D(size,size,TextureFormat.RGB24,false,true);var previous=RenderTexture.active;
            try
            {
                mat.SetFloat("_WindowAspect",1);mat.SetFloat("_ResponsePower",1.5f);mat.SetFloat("_SubjectFoil",0);
                mat.SetVector("_Subject",new Vector4(0,0,1,.006f));
                void Set(string name,Texture2D texture,float zoom,float depth){mat.SetTexture("_"+name+"Tex",texture);mat.SetVector("_"+name,new Vector4(0,0,zoom,depth));mat.SetVector("_"+name+"Info",new Vector4(1,0,0,0));}
                Set("Background",background,1.3f,.085f);Set("Rear",rear,1,0);Set("Foreground",foreground,1,0);
                Vector2[] Read(Vector2 look)
                {
                    LayeredArtMaterial.View(mat,look);Graphics.Blit(Texture2D.whiteTexture,rt,mat);RenderTexture.active=rt;
                    readback.ReadPixels(new Rect(0,0,size,size),0,0);readback.Apply();var sums=new Vector2[3];var counts=new int[3];var pixels=readback.GetPixels();
                    for(int i=0;i<pixels.Length;i++){var c=pixels[i];int k=c.r>.7f&&c.g<.1f&&c.b<.1f?0:c.g>.7f&&c.r<.1f&&c.b<.1f?1:c.b>.7f&&c.r<.1f&&c.g<.1f?2:-1;if(k<0)continue;sums[k]+=new Vector2(i%size,i/size);counts[k]++;}
                    for(int k=0;k<3;k++){if(counts[k]<100)throw new Exception("Motion GPU test: missing landmark "+k);sums[k]/=counts[k];}return sums;
                }
                var neutral=Read(Vector2.zero);var small=Read(new Vector2(.2f,.15f));
                if(Vector2.Distance(neutral[0],small[0])>1||Vector2.Distance(neutral[1],small[1])>1)throw new Exception("Subject moves too far at a small tilt");
                foreach(var look in new[]{new Vector2(1,-1),new Vector2(-1,1),new Vector2(.35f,.5f)})
                {
                    var tilted=Read(look);
                    if(Vector2.Distance(tilted[1]-tilted[0],neutral[1]-neutral[0])>1)throw new Exception("Subject source alignment drifted");
                    if(Mathf.Abs(look.x)==1&&Mathf.Abs(tilted[2].x-neutral[2].x)<35)throw new Exception("Background depth is not visible");
                    if(Vector2.Distance(tilted[0],neutral[0])>5)throw new Exception("Combined subject movement exceeds intended bound");
                }
                Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/layered-motion.txt","PASS actual GPU shader readback: red/green source landmarks keep their relative position within 1px at multiple tilts; small-tilt subject shift <=1px at 512px; full-tilt subject shift <5px while background moves >35px.\n");
                Debug.Log("LAYERED_MOTION_GPU_PASS");
            }
            finally{RenderTexture.active=previous;rt.Release();foreach(var item in new Object[]{mat,background,rear,foreground,rt,readback})Object.DestroyImmediate(item);}
        }
    }
}
