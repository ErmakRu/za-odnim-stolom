using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        [MenuItem("Summoners Table/Upgrade lobby rules and card depth (0.6)")]
        public static void UpgradeRules()
        {
            Ensure();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string path=Root+"UI/MatchOptions.prefab";
            if(!File.Exists(path))
            {
                var r=R("Match options",null,new Rect(0,0,830,114));I("Wood backing",r,new Rect(-8,-6,848,118),new Color(.1f,.055f,.025f,.96f));
                var v=r.gameObject.AddComponent<MatchOptionsView>();
                v.wizards=OptionButton(r,"Волшебники",new Rect(0,5,205,41));v.commanders=OptionButton(r,"Полководцы",new Rect(214,5,205,41));
                v.power=OptionToggle(r,"Увеличение силы",new Rect(436,5,213,41));v.depth=OptionToggle(r,"3D карточки",new Rect(662,5,168,41));
                v.explanation=T("Rule details",r,new Rect(8,54,814,54),"",19);Save(r.gameObject,path);
            }
            string file="Assets/Prefabs/LobbyCanvas.prefab";var root=PrefabUtility.LoadPrefabContents(file);
            if(root.GetComponentInChildren<MatchOptionsView>(true)==null)
            {
                var options=Nest(Load(path).GetComponent<MatchOptionsView>(),root.transform);var rect=(RectTransform)options.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(-750,324);
                var shader=root.GetComponentInChildren<ShaderChoiceView>(true);var sr=(RectTransform)shader.transform;sr.anchoredPosition=new Vector2(130,316);sr.localScale=Vector3.one*.86f;
                foreach(var portrait in root.GetComponent<FrontEndCanvas>().portraits){var p=(RectTransform)portrait.transform;p.anchoredPosition=new Vector2(0,43);p.sizeDelta=new Vector2(345,372);}
                PrefabUtility.SaveAsPrefabAsset(root,file);
            }
            PrefabUtility.UnloadPrefabContents(root);
            path=Root+"UI/TurnBudget.prefab";
            if(!File.Exists(path))
            {
                var r=R("Turn budget",null,new Rect(0,0,288,154));var v=r.gameObject.AddComponent<TurnBudgetView>();
                v.mode=T("Mode",r,new Rect(0,0,288,27),"Волшебники",21,TextAnchor.MiddleCenter);
                v.spellPanel=I("Three spells route",r,new Rect(0,32,288,39),panel);v.spellRoute=T("Count",v.spellPanel.transform,new Rect(5,0,278,39),"0 / 3 заклинаний",20,TextAnchor.MiddleCenter);
                v.mixedPanel=I("Mixed route",r,new Rect(0,76,288,64),panel);v.mixedRoute=T("Count",v.mixedPanel.transform,new Rect(5,0,278,64),"0 / 1 заклинаний\n0 / 1 существ",19,TextAnchor.MiddleCenter);
                v.remaining=T("QTE budget",r,new Rect(0,33,288,107),"",25,TextAnchor.MiddleCenter);Save(r.gameObject,path);
            }
            file=Root+"UI/MatchHUD.prefab";root=PrefabUtility.LoadPrefabContents(file);
            if(root.GetComponentInChildren<TurnBudgetView>(true)==null)
            {
                var view=Nest(Load(path).GetComponent<TurnBudgetView>(),root.transform);((RectTransform)view.transform).anchoredPosition=new Vector2(1301,-730);
                // A reaction button only exists while casting; place it above the budget.
                var hud=root.GetComponent<WidgetScreen>();((RectTransform)hud.Item("pass").transform).anchoredPosition=new Vector2(1318,-673);
                PrefabUtility.SaveAsPrefabAsset(root,file);
            }
            PrefabUtility.UnloadPrefabContents(root);
            file=Root+"UI/HandFan.prefab";root=PrefabUtility.LoadPrefabContents(file);var fan=root.GetComponent<HandFan>();
            fan.cardSize=new Vector2(178,248);fan.spacing=126;fan.maxSpread=740;fan.arc=38;fan.angle=7.5f;fan.hoverLift=55;
            ((RectTransform)root.transform).anchoredPosition=new Vector2(-40,-310);PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
            file="Assets/Prefabs/TableWorld.prefab";root=PrefabUtility.LoadPrefabContents(file);if(root.GetComponent<WorldHandFanSettings>()==null)root.AddComponent<WorldHandFanSettings>();PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
            file=Root+"Cards/CardBase.prefab";root=PrefabUtility.LoadPrefabContents(file);
            var depth=root.GetComponent<CardDepthVisual>()??root.AddComponent<CardDepthVisual>();depth.uiMaterial=DepthMaterial("CardWindowUI");depth.worldMaterial=DepthMaterial("CardWindowWorld");PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
            // Longer uncapped rituals wrap inside the existing QTE panel.
            file="Assets/Prefabs/CardTableCanvas.prefab";root=PrefabUtility.LoadPrefabContents(file);var qte=root.GetComponent<CardTableCanvas>();qte.qteKeys.resizeTextForBestFit=true;qte.qteKeys.resizeTextMinSize=18;qte.qteKeys.resizeTextMaxSize=qte.qteKeys.fontSize;PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
            // The user's explicit five-bear example sets the limited guard total to two.
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Data/catalog").text);catalog.version="0.6.0";
            foreach(var card in catalog.cards.Where(c=>c.effect=="guard"))
            {
                card.rules=card.rules.Replace("не выше 3","не выше 2");file=Root+"Cards/Instances/"+card.id+".prefab";root=PrefabUtility.LoadPrefabContents(file);root.GetComponent<CardView>().Import(card,catalog);PrefabUtility.SaveAsPrefabAsset(root,file);PrefabUtility.UnloadPrefabContents(root);
            }
            File.WriteAllText("Assets/Resources/Data/catalog.json",JsonUtility.ToJson(catalog,true));AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            RuleOptionsTests.Run();
        }
        static Button OptionButton(Transform parent,string text,Rect rect)=>SharedButton.TopLeft(parent,text,text,rect);
        static Toggle OptionToggle(Transform parent,string label,Rect rect)
        {
            var r=R(label,parent,rect);var toggle=r.gameObject.AddComponent<Toggle>();var bg=I("Box",r,new Rect(0,8,26,26),new Color(.43f,.3f,.14f),true);var check=I("Checked",bg.transform,new Rect(5,5,16,16),teal);toggle.targetGraphic=bg;toggle.graphic=check;toggle.isOn=true;
            var text=T("Label",r,new Rect(34,0,rect.width-34,rect.height),label,18);text.raycastTarget=true;return toggle;
        }
        static Material DepthMaterial(string name)
        {
            string path="Assets/Resources/Styles/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Resources.Load<Shader>("Styles/"+name));AssetDatabase.CreateAsset(material,path);}return material;
        }
        public static void UpgradeRulesAndBuild(){UpgradeRules();BuildTools.BuildWindows();}
        public static void PolishRulePanelsAndBuild()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string path=Root+"UI/GameInterface.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            var fan=root.GetComponent<PrefabInterface>().hand;var serialized=new SerializedObject(fan.transform);
            PrefabUtility.RevertPropertyOverride(serialized.FindProperty("m_AnchoredPosition"),InteractionMode.AutomatedAction);serialized.Dispose();
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            path=Root+"UI/TurnBudget.prefab";root=PrefabUtility.LoadPrefabContents(path);var budget=root.GetComponent<TurnBudgetView>();
            if(budget.either==null)budget.either=T("Either route",root.transform,new Rect(0,71,288,15),"ИЛИ",12,TextAnchor.MiddleCenter);
            ((RectTransform)budget.mixedPanel.transform).anchoredPosition=new Vector2(0,-86);
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();BuildTools.BuildWindows();
        }
    }
}
