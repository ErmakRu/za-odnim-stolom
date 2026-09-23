using System.Collections.Generic;
using UnityEngine;
namespace SummonersTable
{
    // Explicit renderer lists keep card art, particles, target indicators and UI untouched.
    public sealed class ShaderStyleTarget:MonoBehaviour
    {
        public ShaderStyleLibrary library;
        public Renderer[] targets;
        Material[][] originals;
        readonly Dictionary<Material,Material[]> variants=new Dictionary<Material,Material[]>();
        public int AppliedMode{get;private set;}=-1;
        void OnEnable(){ShaderSettings.Changed+=Refresh;Refresh();}
        void OnDisable(){ShaderSettings.Changed-=Refresh;Restore();}
        public void Configure(Renderer[] renderers){Restore();targets=renderers;originals=null;Refresh();}
        public void Refresh()
        {
            if(targets==null||targets.Length==0)return;
            if(library==null)library=Resources.Load<ShaderStyleLibrary>("ShaderStyles");
            if(originals==null){originals=new Material[targets.Length][];for(int i=0;i<targets.Length;i++)originals[i]=targets[i]!=null?targets[i].sharedMaterials:new Material[0];}
            int mode=ShaderSettings.Mode;
            if(mode==0||library==null||library.Template(mode)==null){Restore();return;}
            for(int i=0;i<targets.Length;i++)
            {
                if(targets[i]==null)continue;var materials=new Material[originals[i].Length];
                for(int j=0;j<materials.Length;j++)materials[j]=Variant(originals[i][j],mode);
                targets[i].sharedMaterials=materials;
            }
            AppliedMode=mode;
        }
        Material Variant(Material original,int mode)
        {
            if(original==null)return null;
            if(!variants.TryGetValue(original,out var pair)){pair=new Material[2];variants.Add(original,pair);}
            if(pair[mode-1]!=null)return pair[mode-1];
            var material=new Material(library.Template(mode)){name=original.name+" / "+ShaderSettings.Names[mode]};
            bool armor=original.HasProperty("_PolyArtAlbedo");material.SetFloat("_UseMasks",armor?1:0);
            string albedo=armor?"_PolyArtAlbedo":original.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
            CopyTexture(original,material,albedo,"_MainTex");
            material.SetColor("_Color",original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);
            if(armor)
            {
                CopyTexture(original,material,"_Mask01","_Mask01");CopyTexture(original,material,"_Mask02","_Mask02");
                for(int n=1;n<=6;n++){string key="_Color0"+n;material.SetColor(key,original.GetColor(key));material.SetFloat(key+"Power",original.GetFloat(key+"Power"));}
            }
            pair[mode-1]=material;return material;
        }
        static void CopyTexture(Material from,Material to,string source,string dest)
        {if(!from.HasProperty(source))return;to.SetTexture(dest,from.GetTexture(source));to.SetTextureScale(dest,from.GetTextureScale(source));to.SetTextureOffset(dest,from.GetTextureOffset(source));}
        void Restore(){if(originals!=null)for(int i=0;i<targets.Length;i++)if(targets[i]!=null)targets[i].sharedMaterials=originals[i];AppliedMode=0;}
        void OnDestroy(){foreach(var pair in variants.Values)foreach(var material in pair)if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}variants.Clear();}
    }
}
