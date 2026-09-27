using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace SummonersTable.Editor
{
    public static class BattleHudReference
    {
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var g=new GameObject(name,typeof(RectTransform));var r=(RectTransform)g.transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static string Apply()
        {
            const string path="Assets/Prefabs/Editable/UI/MatchHUD.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var screen=root.GetComponent<WidgetScreen>();
                screen.Get<RectTransform>("journal").anchoredPosition=new Vector2(24,-27);
                screen.Get<RectTransform>("turn").anchoredPosition=new Vector2(28,-79);
                foreach(var n in new[]{"title","camera","personal","endhint"})screen.Item(n).SetActive(false);
                root.transform.Find("Hand backdrop").gameObject.SetActive(false);
                root.transform.Find("Header").gameObject.SetActive(false);
                root.GetComponentInChildren<TurnBudgetView>(true).gameObject.SetActive(false);
                screen.Get<RectTransform>("cancel").anchoredPosition=new Vector2(28,-755);
                var name=screen.Get<RectTransform>("name");name.anchoredPosition=new Vector2(205,-899);name.sizeDelta=new Vector2(180,40);
                var hud=root.GetComponent<LocalHeroHud>();if(hud==null)hud=root.AddComponent<LocalHeroHud>();
                if(hud.portrait==null)
                {
                    hud.portrait=Rect("Hero portrait",root.transform,28,816,165,150).gameObject.AddComponent<RawImage>();
                    hud.portrait.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/Jester/jester_basic.png");hud.portrait.raycastTarget=false;
                    var frame=Rect("Hero health frame",root.transform,28,950,332,43).gameObject.AddComponent<TavernPanel>();frame.color=new Color(.27f,.13f,.05f);frame.raycastTarget=false;
                    hud.healthFill=Rect("Health fill",frame.transform,5,5,322,33).gameObject.AddComponent<Image>();hud.healthFill.color=new Color(.18f,.68f,.62f);hud.healthFill.type=Image.Type.Filled;hud.healthFill.fillMethod=Image.FillMethod.Horizontal;hud.healthFill.raycastTarget=false;
                    hud.healthText=Object.Instantiate(screen.Get<Text>("personal"),frame.transform);hud.healthText.name="HP value";hud.healthText.gameObject.SetActive(true);var r=(RectTransform)hud.healthText.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;hud.healthText.alignment=TextAnchor.MiddleCenter;hud.healthText.text="HP 30 / 30";
                    hud.scoreText=Object.Instantiate(screen.Get<Text>("name"),root.transform);hud.scoreText.name="Score points";var p=(RectTransform)hud.scoreText.transform;p.anchoredPosition=new Vector2(205,-861);p.sizeDelta=new Vector2(180,30);hud.scoreText.color=new Color(1,.8f,.15f);hud.scoreText.text="● ● ●";
                }
                hud.portrait.uvRect=new Rect(.08f,.52f,.84f,.48f);
                var portraitRect=(RectTransform)hud.portrait.transform;portraitRect.anchoredPosition=new Vector2(28,-842);portraitRect.sizeDelta=new Vector2(170,108);
                var healthFrame=hud.healthText.transform.parent;
                var maskTransform=healthFrame.Find("Octagonal health mask") as RectTransform;
                if(maskTransform==null)
                {
                    maskTransform=Rect("Octagonal health mask",healthFrame,4,4,324,35);
                    var shape=maskTransform.gameObject.AddComponent<TavernPanel>();shape.inset=0;shape.corner=11;shape.color=shape.border=Color.white;shape.raycastTarget=false;
                    var mask=maskTransform.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;
                }
                hud.healthFill.transform.SetParent(maskTransform,false);var fillRect=hud.healthFill.rectTransform;fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=fillRect.offsetMax=Vector2.zero;
                hud.healthText.transform.SetAsLastSibling();
                hud.healthFill.sprite=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/World/Player.prefab").GetComponentInChildren<PlayerStatusView>(true).healthFill.sprite;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            return "HUD composition saved";
        }
    }
}
