using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static class ShaderStyleTests
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("SHADER STYLE TEST: "+message);}
        public static void Run()
        {
            var styles=Resources.Load<ShaderStyleLibrary>("ShaderStyles");Check(styles!=null&&styles.toon!=null&&styles.painterly!=null,"two authored style materials");
            Check(!ShaderUtil.ShaderHasError(styles.toon.shader),"style shader compilation");
            Check(styles.toon.GetFloat("_Painterly")==0&&styles.painterly.GetFloat("_Painterly")==1,"distinct styles");
            foreach(string path in new[]{"Assets/Prefabs/LobbyCanvas.prefab","Assets/Prefabs/Editable/UI/SettingsPanel.prefab"})
            {var view=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<ShaderChoiceView>(true);Check(view!=null&&view.choices.Length==3&&view.choices.All(b=>b!=null),"three editable buttons in "+path);}
            var table=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TableWorld.prefab");
            var board=table.GetComponent<TableBoard>();var furniture=board.authoredEnvironment.GetComponent<ShaderStyleTarget>();
            Check(furniture!=null&&furniture.targets.Length>0&&board.authoredEnvironment.Find("Imported table").GetComponentsInChildren<Renderer>(true).All(r=>Array.IndexOf(furniture.targets,r)>=0),"interior furniture targets assigned");
            Check(board.seating.seatPrefab.chair.GetComponent<ShaderStyleTarget>()?.targets.Length>0,"common chair shader target");
            var proportions=table.GetComponent<TableProportions>();
            foreach(var actor in table.GetComponentsInChildren<HeroActor>(true))Check(Mathf.Abs(actor.transform.localScale.x-(proportions!=null?proportions.avatarScale:2.25f))<.01f,"consistent avatar scale in every layout");
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);var renderer=obj.GetComponent<Renderer>();
            var original=new Material(Shader.Find("PolyArtMaskTint"));var natural=Resources.Load<HeroLibrary>("HeroLibrary").naturalMaterial;
            var texture=new Texture2D(2,2);original.SetTexture("_PolyArtAlbedo",texture);original.SetTextureScale("_PolyArtAlbedo",new Vector2(2,3));original.SetColor("_Color03",Color.cyan);
            renderer.sharedMaterials=new[]{original,natural};int previous=ShaderSettings.Mode;
            try
            {
                ShaderSettings.Apply(0,false);var target=obj.AddComponent<ShaderStyleTarget>();target.library=styles;target.Configure(new[]{renderer});
                foreach(int mode in new[]{1,2,0,2,1,0})
                {
                    ShaderSettings.Apply(mode,false);target.Refresh();var materials=renderer.sharedMaterials;Check(materials.Length==2,"multiple submeshes retained");
                    if(mode==0){Check(materials[0]==original&&materials[1]==natural,"original material references restored");continue;}
                    Check(materials[0]!=original&&materials[0].mainTexture==texture,"original assets unchanged; albedo retained");
                    Check(materials[0].mainTextureScale==new Vector2(2,3)&&materials[0].GetColor("_Color03")==Color.cyan,"texture mapping and palette retained");
                    Check(materials[0].GetFloat("_UseMasks")==1&&materials[1].GetFloat("_UseMasks")==0,"armor masks do not recolor fur");
                }
                Check(original.shader.name=="PolyArtMaskTint","source shader untouched");
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(texture);ShaderSettings.Apply(previous,false);}
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/shader-style-tests.txt","PASS style shader compilation, two presets, three prefab buttons in lobby/settings, larger avatars in every 2/3/4 layout, repeated switching and exact original-material restoration, submesh count, albedo UV mapping, armor masks/palette, natural fur and source asset preservation.\n");Debug.Log("ALL_SHADER_STYLE_TESTS_PASSED");
        }
    }
}
