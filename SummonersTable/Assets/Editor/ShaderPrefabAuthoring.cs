using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        [MenuItem("Summoners Table/Add shader choices and larger heroes (0.5.1)")]
        public static void UpgradeShaderPrefabs()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Directory.CreateDirectory("Assets/Resources/Styles");AssetDatabase.Refresh();
            var styles=Resources.Load<ShaderStyleLibrary>("ShaderStyles");
            if(styles==null){styles=ScriptableObject.CreateInstance<ShaderStyleLibrary>();AssetDatabase.CreateAsset(styles,"Assets/Resources/ShaderStyles.asset");}
            for(int mode=1;mode<=2;mode++)
            {
                string path="Assets/Resources/Styles/"+(mode==1?"Toon":"Painterly")+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Resources.Load<Shader>("Styles/TableStyle"));material.SetFloat("_Painterly",mode==2?1:0);AssetDatabase.CreateAsset(material,path);}
                if(mode==1)styles.toon=material;else styles.painterly=material;
            }
            EditorUtility.SetDirty(styles);
            string selector=Root+"UI/ShaderChoice.prefab";
            if(!File.Exists(selector))
            {
                var r=R("Shader choice",null,new Rect(0,0,660,95));var v=r.gameObject.AddComponent<ShaderChoiceView>();v.choices=new Button[3];
                T("Title",r,new Rect(0,0,170,24),"ШЕЙДЕР",19);
                v.description=T("Style description",r,new Rect(180,0,480,24),"",17,TextAnchor.MiddleRight);
                for(int i=0;i<3;i++){var image=I("Option "+i,r,new Rect(i*222,37,216,45),panel,true);v.choices[i]=image.gameObject.AddComponent<Button>();T("Label",image.transform,new Rect(0,0,216,45),ShaderSettings.Names[i],20,TextAnchor.MiddleCenter);}
                Save(r.gameObject,selector);
            }
            string file=Root+"UI/SettingsPanel.prefab";var root=PrefabUtility.LoadPrefabContents(file);
            if(root.GetComponentInChildren<ShaderChoiceView>(true)==null)
            {
                var view=Nest(Load(selector).GetComponent<ShaderChoiceView>(),root.transform);((RectTransform)view.transform).anchoredPosition=new Vector2(470,-605);
                var s=root.GetComponent<WidgetScreen>();((RectTransform)s.Get<Text>("note").transform).anchoredPosition=new Vector2(420,-710);
                ((RectTransform)s.Get<Button>("resume").transform).anchoredPosition=new Vector2(440,-780);((RectTransform)s.Get<Button>("leave").transform).anchoredPosition=new Vector2(815,-780);
                PrefabUtility.SaveAsPrefabAsset(root,file);
            }
            PrefabUtility.UnloadPrefabContents(root);
            file="Assets/Prefabs/LobbyCanvas.prefab";root=PrefabUtility.LoadPrefabContents(file);
            if(root.GetComponentInChildren<ShaderChoiceView>(true)==null)
            {
                var view=Nest(Load(selector).GetComponent<ShaderChoiceView>(),root.transform);var r=(RectTransform)view.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(-330,324);
                var ui=root.GetComponent<FrontEndCanvas>();
                ((RectTransform)ui.subtitle.transform).anchoredPosition=new Vector2(-635,375);((RectTransform)ui.subtitle.transform).sizeDelta=new Vector2(290,95);ui.subtitle.fontSize=17;ui.subtitle.resizeTextMaxSize=17;
                ((RectTransform)ui.status.transform).anchoredPosition=new Vector2(635,375);((RectTransform)ui.status.transform).sizeDelta=new Vector2(290,95);ui.status.fontSize=17;ui.status.resizeTextMaxSize=17;
                PrefabUtility.SaveAsPrefabAsset(root,file);
            }
            PrefabUtility.UnloadPrefabContents(root);
            file="Assets/Prefabs/TableWorld.prefab";root=PrefabUtility.LoadPrefabContents(file);
            if(root.GetComponent<ShaderStyleTarget>()==null)
            {
                var target=root.AddComponent<ShaderStyleTarget>();target.library=styles;
                var board=root.GetComponent<TableBoard>();target.targets=root.GetComponentsInChildren<Renderer>(true).Where(r=>(r.transform.IsChildOf(board.authoredEnvironment)&&r.GetComponentInParent<BoardTarget>()==null)||AncestorsContain(r.transform,"Armchair from stuff")).ToArray();
                foreach(var actor in root.GetComponentsInChildren<HeroActor>(true))actor.transform.localScale=Vector3.one*2.25f;
                PrefabUtility.SaveAsPrefabAsset(root,file);
            }
            PrefabUtility.UnloadPrefabContents(root);
            var heroes=Resources.Load<HeroLibrary>("HeroLibrary");var proportion=Load("Assets/Prefabs/TableWorld.prefab").GetComponent<TableProportions>();heroes.heroScale=proportion!=null?proportion.avatarScale:2.25f;EditorUtility.SetDirty(heroes);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();ShaderStyleTests.Run();
        }
        public static void UpgradeShadersAndBuild(){UpgradeShaderPrefabs();BuildTools.BuildWindows();}
        static bool AncestorsContain(Transform t,string name){while(t!=null){if(t.name==name)return true;t=t.parent;}return false;}
    }
}
