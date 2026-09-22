using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler))]
    public sealed class CardTableCanvas : MonoBehaviour
    {
        public CardDisplaySlot leftSlot,centerSlot,rightSlot;
        public CardDisplaySlot[] reactionSlots;
        public RectTransform qtePanel;
        public Text qteTitle,qteKeys,qteStatus;
        public Button[] keyButtons;
        Vector2 centerRest;
        string phaseId;
        string announcedCard="",announcedMatch="";int announcedRound;
        Action<string> sendKey;
        Font font;
        readonly string[] reactionIds=new string[3];
        readonly float[] reactionStarted=new float[3];
        public void Initialize(Font typeface,Action<string> input)
        {
            if(centerSlot==null)Build();EnsureReactionSlots();font=typeface;sendKey=input;
            centerRest=((RectTransform)centerSlot.transform).anchoredPosition;
            GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            for(int i=0;i<keyButtons.Length;i++)
            {string key="ASDFGHJ"[i].ToString();var b=keyButtons[i];b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>sendKey(key));if(b.GetComponentInChildren<Text>().font==null)b.GetComponentInChildren<Text>().font=font;}
            foreach(var label in new[]{qteTitle,qteKeys,qteStatus})if(label.font==null)label.font=font;
        }
        public bool Covers(Vector2 screenPoint)
        {
            if(ReactionAt(screenPoint)!=null)return true;
            foreach(var r in new[]{leftSlot.transform as RectTransform,centerSlot.transform as RectTransform,rightSlot.transform as RectTransform,qtePanel})
                if(r.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(r,screenPoint))return true;
            return false;
        }
        public string InspectAt(Vector2 screenPoint)
        {
            var reaction=ReactionAt(screenPoint);if(reaction!=null)return reaction.CardId;
            // The side preview must never inspect itself and keep itself open.
            foreach(var slot in new[]{leftSlot,centerSlot})
                if(slot.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)slot.transform,screenPoint))return slot.CardId;
            return "";
        }
        public void Present(MatchState state,int seat,double now,string inspection,Catalog catalog,TableBoard board,bool visible)
        {
            gameObject.SetActive(visible);
            if(state==null){announcedCard="";announcedMatch="";return;}
            if(announcedMatch!=state.matchId||announcedRound!=state.round)
            {announcedCard="";announcedMatch=state.matchId;announcedRound=state.round;}
            var cast=state.cast;bool reveal=cast!=null&&state.phase=="reveal",qte=cast!=null&&state.phase=="qte";
            var card=cast==null?null:catalog.Card(cast.cardId);
            if(card!=null)announcedCard=card.id;
            else announcedCard="";
            if(!visible)return;
            leftSlot.Show(reveal?null:catalog.Card(announcedCard),catalog,font);
            rightSlot.Show(catalog.Card(inspection),catalog,font);
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
            PresentReactions(state,cast,catalog,reveal);
            bool own=qte&&state.qte!=null&&cast.owner==seat;qtePanel.gameObject.SetActive(own);
            if(own)
            {
                var q=state.qte;phaseId=q.id;qteTitle.text="ВАШ РИТУАЛ";
                string keys="";for(int i=0;i<q.sequence.Length;i++)keys+="<color="+(i<q.index?"#3FC5AD":i==q.index?"#F0B354":"#80939A")+">"+q.sequence[i]+"</color> ";
                qteKeys.text=keys;qteStatus.text=Math.Max(0,q.deadline-now).ToString("0.0")+" с · Попытки: "+(3-q.mistakes)+" / 3\nПри срыве → "+state.players[q.recipient].name;
            }
        }
        CardDisplaySlot ReactionAt(Vector2 screenPoint)
        {
            if(reactionSlots!=null)for(int i=reactionSlots.Length-1;i>=0;i--)
            {var s=reactionSlots[i];if(s.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)s.transform,screenPoint))return s;}
            return null;
        }
        void PresentReactions(MatchState state,CastState cast,Catalog catalog,bool reveal)
        {
            var reactions=cast==null?null:state.tableReactions.Where(r=>r.castId==cast.id).ToList();
            var anchor=(RectTransform)(reveal?centerSlot:leftSlot).transform;
            for(int i=0;i<reactionSlots.Length;i++)
            {
                var slot=reactionSlots[i];var r=reactions!=null&&i<reactions.Count?reactions[i]:null;
                slot.Show(r==null?null:catalog.Card(r.cardId),catalog,font);
                if(r==null){reactionIds[i]=null;continue;}
                if(reactionIds[i]!=r.uid){reactionIds[i]=r.uid;reactionStarted[i]=Time.unscaledTime;}
                float t=Mathf.Clamp01((Time.unscaledTime-reactionStarted[i])/.2f);t=1-(1-t)*(1-t);
                var rect=(RectTransform)slot.transform;
                rect.anchoredPosition=anchor.anchoredPosition+new Vector2(65+i*31,-45-i*30)+new Vector2(40,95)*(1-t);
                rect.localRotation=Quaternion.Euler(0,0,-18-i*2);
                rect.localScale=anchor.localScale*Mathf.Lerp(.76f,.62f,t);
                slot.statsLabel.text="Реакция · "+state.players[r.owner].name;
            }
        }
        public void EnsureReactionSlots()
        {
            if(reactionSlots!=null&&reactionSlots.Length==3&&reactionSlots.All(s=>s!=null))return;
            reactionSlots=new CardDisplaySlot[3];
            for(int i=0;i<3;i++)
            {
                reactionSlots[i]=MakeSlot("Reaction overlay "+(i+1),Vector2.zero,new Vector2(270,480));
                reactionSlots[i].transform.SetSiblingIndex(qtePanel.GetSiblingIndex());
                reactionSlots[i].gameObject.SetActive(false);
            }
        }
        public void CoverWithJournal()
        {
            leftSlot.gameObject.SetActive(false);centerSlot.gameObject.SetActive(false);qtePanel.gameObject.SetActive(false);
            foreach(var slot in reactionSlots)slot.gameObject.SetActive(false);
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
        {var rect=Rect(name,transform,position,size);var slot=rect.gameObject.AddComponent<CardDisplaySlot>();slot.library=Resources.Load<CardLibrary>("CardLibrary");return slot;}
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
