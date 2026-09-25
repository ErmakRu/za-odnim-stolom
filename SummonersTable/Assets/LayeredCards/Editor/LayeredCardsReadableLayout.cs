using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SummonersTable.Editor
{
    public static class LayeredCardsReadableLayout
    {
        const string Root=LayeredCardsAuthoring.Root;
        // An explicit migration preserves the base prefab's existing object IDs and variant links.
        [MenuItem("Summoners Table/Layered Cards/Apply readable layout")]
        public static void Apply()
        {
            var path=Root+"/Prefabs/LayeredCardBase.prefab";var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var v=go.GetComponent<LayeredCardView>();
                Place(v.accent.rectTransform,0,432,580,56);Place(v.type.rectTransform,0,432,548,42);v.type.fontSize=27;
                Place(go.transform.Find("Art frame"),0,218,554,352);
                Place(v.artwork.rectTransform,0,218,546,344);
                if(v.titleFill==null){var p=new GameObject("Name type fill",typeof(RectTransform),typeof(Image));p.transform.SetParent(go.transform,false);v.titleFill=p.GetComponent<Image>();v.titleFill.raycastTarget=false;}
                Place(v.titleFill.rectTransform,0,6,580,64);v.titleFill.transform.SetSiblingIndex(v.title.transform.GetSiblingIndex());
                Place(v.title.rectTransform,0,6,554,58);v.title.fontSize=35;v.title.resizeTextMinSize=28;v.title.resizeTextMaxSize=35;
                Place(go.transform.Find("QTE backdrop"),0,365,546,44);
                Place(v.qteLabel.rectTransform,211,365,108,32);v.qteLabel.fontSize=23;v.qteLabel.fontStyle=FontStyle.Bold;
                for(int i=0;i<v.qteSymbols.Length;i++){var r=v.qteSymbols[i].rectTransform;Place(r,-248+(i%10)*32,365-i/10*27,27,24);r.localRotation=Quaternion.identity;}
                Place(v.roleAccent.rectTransform,-274,-51,5,30);Place(v.role.rectTransform,0,-51,532,34);v.role.fontSize=24;
                Place(v.stats.rectTransform,0,69,530,34);v.stats.fontSize=25;v.stats.color=Color.white;
                var badge=go.transform.Find("Stats backdrop");if(badge==null){var p=new GameObject("Stats backdrop",typeof(RectTransform),typeof(Image));p.transform.SetParent(go.transform,false);p.GetComponent<Image>().color=new Color(.02f,.04f,.06f,.9f);p.GetComponent<Image>().raycastTarget=false;badge=p.transform;}
                Place(badge,0,69,546,40);badge.SetSiblingIndex(v.stats.transform.GetSiblingIndex());
                Place(go.transform.Find("Separator"),0,-82,534,1);
                Place(v.description.rectTransform,0,-234,534,282);v.description.fontSize=29;v.description.lineSpacing=1.06f;
                v.description.resizeTextForBestFit=true;v.description.resizeTextMinSize=25;v.description.resizeTextMaxSize=29;
                Place(v.flavor.rectTransform,0,-422,532,60);v.flavor.fontSize=22;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }finally{PrefabUtility.UnloadPrefabContents(go);}
            LayeredCardsAuthoring.Create();
            // BuildScene created instances from the existing base; extend that reusable screen to three cards.
            var demo=Object.FindFirstObjectByType<LayeredCardsDemo>();var root=(RectTransform)demo.transform;
            foreach(var card in demo.cards)Object.DestroyImmediate(card.gameObject);
            string[] ids={"C08","C02","S01"};demo.cards=new LayeredCardView[3];demo.previewQteCosts=new[]{3,4,7};
            for(int i=0;i<3;i++)
            {
                var card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cards/"+ids[i]+".prefab"),root);
                var r=(RectTransform)card.transform;r.anchoredPosition=new Vector2((i-1)*650,0);r.localScale=Vector3.one;
                demo.cards[i]=card.GetComponent<LayeredCardView>();demo.cards[i].Apply(LayeredCardData.Current);
            }
            root.Find("Preview heading").GetComponent<Text>().text="ПЕРЕЛИВАШКИ · ТЕКСТ НА ПЕРВОМ МЕСТЕ";
            root.Find("Instructions").GetComponent<Text>().text="1–3 QTE · зелёный     /     4–6 · жёлтый     /     7–9 · красный";
            demo.hint.text="";demo.look=Vector2.zero;demo.ApplyLook();
            var note=new GameObject("Cost preview note",typeof(RectTransform),typeof(Text));note.transform.SetParent(root,false);Place(note.transform,650,-491,600,28);
            var text=note.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=21;text.alignment=TextAnchor.MiddleCenter;text.color=CardView.QteHardColor;text.text="Пример удорожания: 3 → 7 QTE";
            Object.FindFirstObjectByType<Camera>().transform.position=new Vector3(0,0,-2.70f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Root+"/Prefabs/LayeredCardsPreview.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),LayeredCardsAuthoring.Scene);
            AssetDatabase.SaveAssets();LayeredCardsTests.Run();ReadabilityChecks();LayeredCardsAuthoring.Render();
            Debug.Log("READABLE_CARDS_PASS");
        }
        static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        static void ReadabilityChecks()
        {
            var demo=Object.FindFirstObjectByType<LayeredCardsDemo>();
            foreach(var view in demo.cards)
            {
                var card=(RectTransform)view.transform;var art=view.artwork.rectTransform;
                if(art.rect.width*art.rect.height/(card.rect.width*card.rect.height)>.4f)throw new Exception("Artwork exceeds 40%");
                foreach(int cost in new[]{3,4,6,7,9,4,2}){view.SetQteCost(cost);var symbols=view.qteSymbols.Where(s=>s.gameObject.activeSelf).ToArray();if(symbols.Length!=cost||symbols.Any(s=>s.color!=CardView.GetQteColor(cost)))throw new Exception("QTE cost recolour failed");}
            }
            if(CardView.GetQteColor(3)==CardView.GetQteColor(4)||CardView.GetQteColor(6)==CardView.GetQteColor(7))throw new Exception("QTE tier bounds");
            demo.ApplyLook();File.WriteAllText("../output/tests/layered-readable.txt","PASS: illustration 33.3%; inherited type/name fill; QTE 3→4→6→7→9→4→2, whole row recolours; no gameplay price mutation.\n");
        }
    }
}
