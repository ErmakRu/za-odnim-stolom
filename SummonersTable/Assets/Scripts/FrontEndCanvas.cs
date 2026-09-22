using System;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable
{
    [RequireComponent(typeof(Canvas),typeof(CanvasScaler))]
    public sealed class FrontEndCanvas : MonoBehaviour
    {
        public bool lobby;
        public Text title,subtitle,status;
        public Text[] members;
        public Button[] buttons;
        public string[] actions;
        public void Bind(Font font,Action<string> click)
        {
            GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            foreach(var t in GetComponentsInChildren<Text>(true))t.font=font;
            for(int i=0;i<buttons.Length;i++){string action=actions[i];buttons[i].onClick.RemoveAllListeners();buttons[i].onClick.AddListener(()=>click(action));}
        }
        public void Visible(bool visible){gameObject.SetActive(visible);}
        public void Build(bool isLobby)
        {
            lobby=isLobby;var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5;
            var scale=GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,1000);scale.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var bg=CardTableCanvas.Rect("Background",transform,Vector2.zero,new Vector2(1600,1000)).gameObject.AddComponent<RawImage>();bg.texture=Resources.Load<Texture2D>(lobby?"Art/board":"Art/menu");bg.raycastTarget=false;
            var shade=CardTableCanvas.Rect("Tint",transform,Vector2.zero,new Vector2(1600,1000)).gameObject.AddComponent<Image>();shade.color=new Color(.02f,.055f,.075f,lobby?.94f:.75f);shade.raycastTarget=false;
            title=Label("Title",new Vector2(0,350),new Vector2(1400,95),lobby?"Собираемся за столом":"За одним столом",62);
            subtitle=Label("Subtitle",new Vector2(0,254),new Vector2(1300,76),lobby?"Выберите колоду и подтвердите готовность":"Призывай нелепых героев. Порти планы друзьям. Жми в ритм.",25);
            status=Label("Connection status",new Vector2(0,-424),new Vector2(1320,58),"",18);
            if(!lobby)
            {
                actions=new[]{"steam","local","cards","rules","quit"};string[] labels={"Играть через Steam","За одним ПК · 2–4 игрока","Колоды и карты","Как играть","Выход"};buttons=new Button[5];
                for(int i=0;i<5;i++)buttons[i]=MakeButton(labels[i],new Vector2(0,118-i*91),new Vector2(520,68));
            }
            else
            {
                members=new Text[4];for(int i=0;i<4;i++)members[i]=Label("Seat "+i,new Vector2(0,140-i*86),new Vector2(1350,64),"Свободное место",24);
                actions=new[]{"deck0","deck1","deck2","ready","start","leave","copy"};buttons=new Button[7];
                for(int i=0;i<3;i++)buttons[i]=MakeButton("Колода "+(i+1),new Vector2(-455+i*455,-206),new Vector2(420,60));
                buttons[3]=MakeButton("Я готов!",new Vector2(-475,-310),new Vector2(300,65));buttons[4]=MakeButton("Начать матч",new Vector2(-155,-310),new Vector2(300,65));
                buttons[5]=MakeButton("Выйти",new Vector2(165,-310),new Vector2(300,65));buttons[6]=MakeButton("Копировать код",new Vector2(485,-310),new Vector2(300,65));
            }
        }
        Text Label(string name,Vector2 position,Vector2 size,string text,int fontSize)
        {var t=CardTableCanvas.Rect(name,transform,position,size).gameObject.AddComponent<Text>();t.text=text;t.fontSize=fontSize;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button MakeButton(string label,Vector2 position,Vector2 size)
        {
            var r=CardTableCanvas.Rect(label,transform,position,size);r.gameObject.AddComponent<Image>().color=new Color(.25f,.78f,.68f);
            var button=r.gameObject.AddComponent<Button>();var text=CardTableCanvas.Rect("Label",r,Vector2.zero,size).gameObject.AddComponent<Text>();text.text=label;text.fontSize=23;text.color=new Color(.025f,.08f,.1f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return button;
        }
    }
}
