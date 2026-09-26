using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    public sealed partial class FrontEndCanvas
    {
        public LobbyPortrait[] portraits;
        public Text[] deckNames,heroNames,readyLabels;
        public RawImage[] deckCards;
        public Button[] seatControls;
        public GameObject appearancePanel;public Text appearanceTitle;
        public Button readyButton,startButton,copyButton;
        public GameObject localCountControls;
        int appearanceSeat=-1;
        readonly List<Button> builtButtons=new List<Button>();readonly List<string> builtActions=new List<string>();
        static readonly Color[] banners={new Color(.51f,.10f,.08f),new Color(.19f,.35f,.11f),new Color(.12f,.26f,.48f),new Color(.55f,.29f,.06f)};
        static readonly Color cream=new Color(1,.89f,.68f),wood=new Color(.19f,.085f,.035f,.97f);
        public void BuildTavern()
        {
            lobby=true;var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5;
            var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(GetComponent<GraphicRaycaster>()==null)gameObject.AddComponent<GraphicRaycaster>();
            var background=CardTableCanvas.Rect("Painted tavern backdrop",transform,Vector2.zero,new Vector2(2000,1125)).gameObject.AddComponent<RawImage>();background.texture=Resources.Load<Texture2D>("UI/LobbyBackdrop");background.raycastTarget=false;
            Panel("Warm shade",transform,Vector2.zero,new Vector2(2000,1125),new Color(.10f,.04f,.01f,.17f),false);
            Panel("Carved title plaque",transform,new Vector2(0,378),new Vector2(870,88),wood);
            title=TText("Title",transform,new Vector2(0,381),new Vector2(810,67),"ПОДГОТОВКА К ИГРЕ",42,true);
            subtitle=TText("Room code",transform,new Vector2(0,307),new Vector2(1330,40),"",20);
            portraits=new LobbyPortrait[4];members=new Text[4];deckNames=new Text[4];heroNames=new Text[4];readyLabels=new Text[4];deckCards=new RawImage[12];seatControls=new Button[20];
            for(int i=0;i<4;i++)
            {
                float x=-558+i*372;var column=CardTableCanvas.Rect("Player place "+(i+1),transform,new Vector2(x,0),new Vector2(348,900));
                var image=CardTableCanvas.Rect("Live hero portrait",column,new Vector2(0,79),new Vector2(345,430)).gameObject.AddComponent<RawImage>();image.raycastTarget=false;
                var portrait=image.gameObject.AddComponent<LobbyPortrait>();portrait.image=image;portraits[i]=portrait;
                Panel("Wooden player panel",column,new Vector2(0,-259),new Vector2(345,250),wood);
                for(int c=0;c<3;c++)
                {
                    var card=CardTableCanvas.Rect("Deck card "+c,column,new Vector2(-64+c*64,-94+(c==1?12:0)),new Vector2(105,135));card.localRotation=Quaternion.Euler(0,0,(1-c)*13);
                    Panel("Card frame",card,Vector2.zero,card.sizeDelta,new Color(.30f,.17f,.07f));
                    var art=CardTableCanvas.Rect("Card art",card,Vector2.zero,new Vector2(95,122)).gameObject.AddComponent<RawImage>();art.raycastTarget=false;deckCards[i*3+c]=art;
                }
                Panel("Player ribbon",column,new Vector2(0,-159),new Vector2(362,53),banners[i]);
                members[i]=TText("Name",column,new Vector2(0,-159),new Vector2(330,46),"Свободное место",24,true);
                deckNames[i]=TText("Deck",column,new Vector2(0,-216),new Vector2(244,42),"",18,true);
                heroNames[i]=TText("Hero",column,new Vector2(0,-265),new Vector2(244,42),"",20);
                seatControls[i*5]=TButton("deck-prev:"+i,"‹",column,new Vector2(-143,-216),new Vector2(43,40));
                seatControls[i*5+1]=TButton("deck-next:"+i,"›",column,new Vector2(143,-216),new Vector2(43,40));
                seatControls[i*5+2]=TButton("hero-prev:"+i,"‹",column,new Vector2(-143,-265),new Vector2(43,40));
                seatControls[i*5+3]=TButton("hero-next:"+i,"›",column,new Vector2(143,-265),new Vector2(43,40));
                seatControls[i*5+4]=TButton("customize:"+i,"НАСТРОИТЬ ОБЛИК",column,new Vector2(0,-311),new Vector2(296,39),banners[i]);
                readyLabels[i]=TText("Ready",column,new Vector2(0,-358),new Vector2(325,30),"ЖДЁМ ИГРОКА",18,true);
            }
            TButton("leave","← ВЫЙТИ",transform,new Vector2(-655,-419),new Vector2(208,48));
            copyButton=TButton("copy","КОД ЛОББИ",transform,new Vector2(-420,-419),new Vector2(230,48));
            readyButton=TButton("ready","Я ГОТОВ",transform,new Vector2(100,-419),new Vector2(245,50),new Color(.22f,.39f,.16f));
            startButton=TButton("start","НАЧАТЬ МАТЧ",transform,new Vector2(412,-419),new Vector2(315,50),new Color(.56f,.28f,.05f));
            status=TText("Status",transform,new Vector2(0,269),new Vector2(1320,31),"",17);
            localCountControls=CardTableCanvas.Rect("Local player count",transform,new Vector2(-391,-419),new Vector2(280,50)).gameObject;
            for(int n=2;n<=4;n++)TButton("players:"+n,n+" игрока",localCountControls.transform,new Vector2((n-3)*99,0),new Vector2(95,44));
            appearancePanel=CardTableCanvas.Rect("Appearance controls",transform,new Vector2(-558,-263),new Vector2(345,170)).gameObject;
            appearanceTitle=TText("Wardrobe title",appearancePanel.transform,new Vector2(0,56),new Vector2(325,28),"ОБЛИК ГЕРОЯ",18,true);
            TButton("outfit-prev","‹",appearancePanel.transform,new Vector2(-124,12),new Vector2(80,35));
            TButton("outfit-next","›",appearancePanel.transform,new Vector2(124,12),new Vector2(80,35));
            for(int c=0;c<4;c++)TButton("palette:"+c,"●",appearancePanel.transform,new Vector2(-102+c*68,-30),new Vector2(61,30),banners[c]);
            TButton("customize-close","ПОДТВЕРДИТЬ ОБЛИК",appearancePanel.transform,new Vector2(0,-71),new Vector2(296,32),banners[1]);
            appearancePanel.SetActive(false);buttons=builtButtons.ToArray();actions=builtActions.ToArray();
        }
        public void OpenAppearance(int seat){appearanceSeat=seat;((RectTransform)appearancePanel.transform).anchoredPosition=new Vector2(-558+372*seat,-263);appearancePanel.SetActive(true);}
        public int AppearanceSeat {get{return appearanceSeat;}}
        public void CloseAppearance(){appearancePanel.SetActive(false);appearanceSeat=-1;}
        public void PresentLobby(IList<LobbyMember> list,string ownId,bool local,bool host,bool canStart,Catalog catalog,string room,string message)
        {
            title.text="ПОДГОТОВКА К ИГРЕ";subtitle.text=room;status.text=message;
            localCountControls.SetActive(local);copyButton.gameObject.SetActive(!local);readyButton.gameObject.SetActive(!local);
            startButton.interactable=canStart;startButton.GetComponentInChildren<Text>().text=host?"НАЧАТЬ МАТЧ":"ЖДЁМ ХОСТА";
            for(int i=0;i<4;i++)
            {
                var m=i<list.Count?list[i]:null;bool editable=m!=null&&(local||m.id==ownId||host&&m.isBot);
                members[i].text=m==null?"СВОБОДНО":m.name+(m.id==ownId&&!local?" · ВЫ":"");
                deckNames[i].text=m==null?"Присоединяйтесь":catalog.Deck(m.deckId).name;
                heroNames[i].text=m==null?"Герой ещё не выбран":HeroOptions.Names[Array.IndexOf(HeroOptions.Ids,HeroOptions.Normalize(m.heroId))];
                readyLabels[i].text=m==null?"ЖДЁМ ИГРОКА":m.ready?"✓ ГОТОВ":"ВЫБИРАЕТ КАРТЫ";readyLabels[i].color=m?.ready==true?new Color(.66f,.91f,.40f):cream;
                for(int b=0;b<5;b++)seatControls[i*5+b].interactable=editable;
                bool wardrobe=appearancePanel.activeSelf&&appearanceSeat==i;
                deckNames[i].gameObject.SetActive(!wardrobe);heroNames[i].gameObject.SetActive(!wardrobe);
                for(int b=0;b<5;b++)seatControls[i*5+b].gameObject.SetActive(!wardrobe);
                var cards=m==null?new string[0]:catalog.Deck(m.deckId).entries.Select(e=>e.cardId).Take(3).ToArray();
                for(int c=0;c<3;c++){var art=deckCards[i*3+c];art.transform.parent.gameObject.SetActive(m!=null);if(m!=null)art.texture=Resources.Load<Texture2D>("Art/"+cards[c]);}
                portraits[i].Present(m);
            }
            var own=list.FirstOrDefault(m=>m.id==ownId);readyButton.interactable=own!=null;readyButton.GetComponentInChildren<Text>().text=own?.ready==true?"СНЯТЬ ГОТОВНОСТЬ":"Я ГОТОВ";
            if(appearanceSeat>=0)
            {
                if(appearanceSeat>=list.Count||!local&&list[appearanceSeat].id!=ownId&&!(host&&list[appearanceSeat].isBot))CloseAppearance();
                else{var m=list[appearanceSeat];appearanceTitle.text="ДОСПЕХ "+(m.outfit+1)+" / 8";}
            }
        }
        Button TButton(string action,string text,Transform parent,Vector2 position,Vector2 size,Color? tint=null)
        {
            var button=SharedButton.Create(parent,action,text,position,size);builtButtons.Add(button);builtActions.Add(action);return button;
        }
        static Graphic Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color,bool decorated=true)
        {
            var r=CardTableCanvas.Rect(name,parent,position,size);Graphic g=decorated?(Graphic)r.gameObject.AddComponent<TavernPanel>():r.gameObject.AddComponent<Image>();g.color=color;g.raycastTarget=false;return g;
        }
        static Text TText(string name,Transform parent,Vector2 position,Vector2 size,string text,int fontSize,bool bold=false)
        {
            var t=CardTableCanvas.Rect(name,parent,position,size).gameObject.AddComponent<Text>();t.text=text;t.fontSize=fontSize;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.color=cream;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
            t.resizeTextForBestFit=true;t.resizeTextMaxSize=fontSize;t.resizeTextMinSize=14;var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.05f,.015f,0,.9f);shadow.effectDistance=new Vector2(1,-2);return t;
        }
    }
}
