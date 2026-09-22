using System;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler))]
    public sealed class CardTableCanvas : MonoBehaviour
    {
        public CardDisplaySlot leftSlot,centerSlot,rightSlot;
        public RectTransform qtePanel;
        public Text qteTitle,qteKeys,qteStatus;
        public Button[] keyButtons;
        Vector2 centerRest;
        string phaseId;
        Action<string> sendKey;
        Font font;
        public void Initialize(Font typeface,Action<string> input)
        {
            if(centerSlot==null)Build();font=typeface;sendKey=input;
            centerRest=((RectTransform)centerSlot.transform).anchoredPosition;
            GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            for(int i=0;i<keyButtons.Length;i++)
            {string key="ASDFGHJ"[i].ToString();var b=keyButtons[i];b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>sendKey(key));b.GetComponentInChildren<Text>().font=font;}
            qteTitle.font=qteKeys.font=qteStatus.font=font;
        }
        public bool Covers(Vector2 screenPoint)
        {
            foreach(var r in new[]{leftSlot.transform as RectTransform,centerSlot.transform as RectTransform,rightSlot.transform as RectTransform,qtePanel})
                if(r.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(r,screenPoint))return true;
            return false;
        }
        public void Present(MatchState state,int seat,double now,string inspection,Catalog catalog,TableBoard board,bool visible)
        {
            gameObject.SetActive(visible);if(!visible||state==null)return;
            var cast=state.cast;bool reveal=cast!=null&&state.phase=="reveal",qte=cast!=null&&state.phase=="qte";
            var card=cast==null?null:catalog.Card(cast.cardId);
            leftSlot.Show(qte&&cast.owner!=seat?card:null,catalog,font);
            rightSlot.Show(qte&&cast.owner==seat?card:catalog.Card(inspection),catalog,font);
            centerSlot.Show(reveal?card:null,catalog,font);
            if(reveal)
            {
                float duration=board.CameraRig.Data.cardFlightSeconds;
                float elapsed=(float)(now-cast.startedAt),t=duration<=0?1:Mathf.Clamp01(elapsed/duration);
                Vector3 source=cast.owner==seat?new Vector3(Screen.width*.5f,Screen.height*.08f,1):board.ViewCamera.WorldToScreenPoint(TableBoard.HeroPosition(cast.owner,state.players.Count));
                Vector3 table=board.ViewCamera.WorldToScreenPoint(new Vector3(0,TableBoard.TableTop+.5f,0));
                var rt=(RectTransform)transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,source,null,out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,table,null,out var b);
                var center=(RectTransform)centerSlot.transform;
                center.anchoredPosition=(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*centerRest;
                center.localScale=Vector3.one*Mathf.Lerp(.18f,1,t);
            }
            bool own=qte&&state.qte!=null&&cast.owner==seat;qtePanel.gameObject.SetActive(own);
            if(own)
            {
                var q=state.qte;phaseId=q.id;qteTitle.text="ВАШ РИТУАЛ";
                string keys="";for(int i=0;i<q.sequence.Length;i++)keys+="<color="+(i<q.index?"#3FC5AD":i==q.index?"#F0B354":"#80939A")+">"+q.sequence[i]+"</color> ";
                qteKeys.text=keys;qteStatus.text=Math.Max(0,q.deadline-now).ToString("0.0")+" с · Попытки: "+(3-q.mistakes)+" / 3\nПри срыве → "+state.players[q.recipient].name;
            }
        }
        // Used by the editor scaffolder. Every generated widget is saved in an editable prefab.
        public void Build()
        {
            var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=.5f;
            if(GetComponent<GraphicRaycaster>()==null)gameObject.AddComponent<GraphicRaycaster>();
            leftSlot=MakeSlot("Left — announced card",new Vector2(-642,35),new Vector2(270,480));
            rightSlot=MakeSlot("Right — inspection",new Vector2(642,35),new Vector2(270,480));
            centerSlot=MakeSlot("Center — reveal",new Vector2(0,15),new Vector2(310,535));
            qtePanel=Rect("Private QTE",transform,new Vector2(0,10),new Vector2(700,255));
            qtePanel.gameObject.AddComponent<Image>().color=new Color(.035f,.08f,.1f,.98f);
            qteTitle=Label("Title",qtePanel,new Rect(20,16,660,35),24);qteKeys=Label("Private letters",qtePanel,new Rect(20,61,660,57),35);qteKeys.supportRichText=true;
            qteStatus=Label("Private timer",qtePanel,new Rect(20,122,660,57),20);
            keyButtons=new Button[7];
            for(int i=0;i<7;i++)
            {
                var r=Rect("Key "+"ASDFGHJ"[i],qtePanel,new Vector2(-264+i*88,-89),new Vector2(70,46));
                r.gameObject.AddComponent<Image>().color=new Color(.94f,.72f,.35f);var button=r.gameObject.AddComponent<Button>();
                var text=Label("Key",r,new Rect(0,0,70,46),24);text.text="ASDFGHJ"[i].ToString();text.color=Color.black;keyButtons[i]=button;
            }
        }
        CardDisplaySlot MakeSlot(string name,Vector2 position,Vector2 size)
        {
            var rect=Rect(name,transform,position,size);var s=rect.gameObject.AddComponent<CardDisplaySlot>();
            s.background=rect.gameObject.AddComponent<Image>();s.background.color=new Color(.035f,.085f,.11f,.98f);
            s.typeBand=Panel("Type color",rect,new Rect(0,0,size.x,32),Color.white);
            s.typeLabel=Label("Type",rect,new Rect(9,0,size.x-18,32),16);s.typeLabel.color=new Color(.03f,.08f,.1f);
            var art=TopRect("Artwork",rect,new Rect(8,40,size.x-16,155));s.artwork=art.gameObject.AddComponent<RawImage>();s.artwork.raycastTarget=false;
            s.roleBand=Panel("Creature role color",rect,new Rect(8,199,size.x-16,25),Color.white);
            s.roleLabel=Label("Faction and role",rect,new Rect(12,199,size.x-24,25),14);s.roleLabel.color=Color.black;
            s.nameLabel=Label("Name",rect,new Rect(12,232,size.x-24,54),22);
            s.statsLabel=Label("Stats",rect,new Rect(12,290,size.x-24,32),16);
            s.rulesLabel=Label("Rules",rect,new Rect(14,329,size.x-28,size.y-341),18);s.rulesLabel.alignment=TextAnchor.UpperLeft;s.rulesLabel.resizeTextForBestFit=true;s.rulesLabel.resizeTextMinSize=13;s.rulesLabel.resizeTextMaxSize=18;
            return s;
        }
        public static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        static RectTransform TopRect(string name,Transform parent,Rect bounds)
        {var r=Rect(name,parent,Vector2.zero,bounds.size);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(bounds.x,-bounds.y);return r;}
        static Image Panel(string name,Transform parent,Rect bounds,Color color)
        {var i=TopRect(name,parent,bounds).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
        static Text Label(string name,Transform parent,Rect bounds,int size)
        {var t=TopRect(name,parent,bounds).gameObject.AddComponent<Text>();t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
    }
}
