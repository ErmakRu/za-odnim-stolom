using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        // One-time authoring pass for the new 0.5 assets; deliberately not run by normal builds.
        public static void PolishAndBuild()
        {
            string cardPath=Root+"Cards/CardBase.prefab";var cardRoot=PrefabUtility.LoadPrefabContents(cardPath);var cardName=cardRoot.GetComponent<CardView>().compactName;cardName.resizeTextForBestFit=true;cardName.resizeTextMinSize=12;cardName.resizeTextMaxSize=16;PrefabUtility.SaveAsPrefabAsset(cardRoot,cardPath);PrefabUtility.UnloadPrefabContents(cardRoot);
            foreach(string file in new[]{"SettingsPanel","LabControls"})
            {
                string path=Root+"UI/"+file+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                foreach(var slider in root.GetComponentsInChildren<Slider>(true))FixSlider(slider);
                PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            foreach(string path in Directory.GetFiles("Assets/Presentation/Effects","*.prefab"))
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                foreach(var source in root.GetComponentsInChildren<AudioSource>(true))Object.DestroyImmediate(source);
                bool purple=path.Contains("purple trail"),fire=path.Contains("Attack trail");
                if(purple||fire)foreach(var trail in root.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.widthMultiplier=.10f;trail.time=.16f;
                    foreach(var material in trail.sharedMaterials){material.color=purple?new Color(.65f,.25f,1,.45f):new Color(1,.35f,.035f,.6f);material.SetFloat("_DstBlend",1);EditorUtility.SetDirty(material);}
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            string spellPath=Root+"Spells/S02.prefab";var heal=PrefabUtility.LoadPrefabContents(spellPath);heal.GetComponent<SpellEffect>().impactScale=.22f;PrefabUtility.SaveAsPrefabAsset(heal,spellPath);PrefabUtility.UnloadPrefabContents(heal);
            foreach(string file in new[]{"Arrow","Outline"}){var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"World/"+file+".mat");material.shader=Resources.Load<Shader>("ImportedParticle");EditorUtility.SetDirty(material);}
            string spritePath=Root+"UI/Solid.png";var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();File.WriteAllBytes(spritePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(spritePath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(spritePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.SaveAndReimport();
            string hpPath=Root+"UI/PlayerStatus.prefab";var hp=PrefabUtility.LoadPrefabContents(hpPath);var status=hp.GetComponent<PlayerStatusView>();status.healthFill.sprite=status.healthTrail.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            PrefabUtility.SaveAsPrefabAsset(hp,hpPath);PrefabUtility.UnloadPrefabContents(hp);
            AssetDatabase.SaveAssets();BuildTools.BuildWindows();
        }
        static void FixSlider(Slider slider)
        {
            if(slider.transform.Find("Fill area")!=null){slider.handleRect.sizeDelta=new Vector2(slider.handleRect.sizeDelta.x,0);return;}
            var rect=(RectTransform)slider.transform;float w=rect.sizeDelta.x,h=rect.sizeDelta.y;
            var fill=R("Fill area",rect,new Rect(10,h*.35f,w-20,h*.3f));slider.fillRect.SetParent(fill,false);slider.fillRect.anchorMin=Vector2.zero;slider.fillRect.anchorMax=Vector2.one;slider.fillRect.sizeDelta=Vector2.zero;slider.fillRect.anchoredPosition=Vector2.zero;slider.fillRect.pivot=new Vector2(.5f,.5f);
            var handle=R("Handle area",rect,new Rect(10,0,w-20,h));slider.handleRect.SetParent(handle,false);slider.handleRect.anchorMin=slider.handleRect.anchorMax=new Vector2(0,.5f);slider.handleRect.pivot=new Vector2(.5f,.5f);slider.handleRect.sizeDelta=new Vector2(18,0);slider.handleRect.anchoredPosition=Vector2.zero;
        }
    }
}
