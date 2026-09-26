using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SummonersTable
{
    // A standalone catalogue, using the exact same face and materials as the game.
    public sealed class LayeredCardsGallery : MonoBehaviour
    {
        public LayeredCardView[] slots;
        public Text[] identifiers;
        public Text pageLabel, statusLabel;
        public Button previous, next, rotation, reset, reload;
        public Button[] filters, modes;
        public bool autoRotate=true;
        public Vector2 look;
        ConfigBundle bundle;
        CardDef[] visible=Array.Empty<CardDef>();
        int page, filter, mode;
        float phase;
        bool dragging;
        static readonly string[] Kinds={"", "creature", "spell", "reaction"};
        static readonly string[] ModeNames={"КАРТОЧКА", "ТОЛЬКО ФОН", "ПЕРСОНАЖ И ЭФФЕКТЫ", "ПЛОСКИЙ ОРИГИНАЛ"};
        public int Page=>page;
        public int PageCount=>Mathf.Max(1,(visible.Length+slots.Length-1)/slots.Length);
        public int FilteredCount=>visible.Length;
        public int Mode=>mode;

        void Start()
        {
            previous.onClick.AddListener(()=>ShowPage(page-1));
            next.onClick.AddListener(()=>ShowPage(page+1));
            rotation.onClick.AddListener(ToggleRotation);
            reset.onClick.AddListener(ResetPose);
            reload.onClick.AddListener(Reload);
            for(int i=0;i<filters.Length;i++){int index=i;filters[i].onClick.AddListener(()=>SetFilter(index));}
            for(int i=0;i<modes.Length;i++){int index=i;modes[i].onClick.AddListener(()=>SetMode(index));}
            foreach(var button in GetComponentsInChildren<Button>())button.onClick.AddListener(()=>EventSystem.current?.SetSelectedGameObject(null));
            Reload();
        }
        public void Initialize(ConfigBundle data)
        {
            bundle=data;SetFilter(filter);
        }
        public void Reload()
        {
            try {LayeredCardData.Reload();Initialize(ConfigBundle.Read(ConfigRuntime.DirectoryPath));}
            catch(Exception e){statusLabel.text="Не удалось перечитать данные: "+e.Message;Debug.LogException(e);}
        }
        public void SetFilter(int index)
        {
            filter=Mathf.Clamp(index,0,Kinds.Length-1);
            if(bundle==null)return;
            visible=bundle.Catalog().cards.Where(c=>filter==0||c.kind==Kinds[filter]).ToArray();
            ShowPage(0);
        }
        public void ShowPage(int index)
        {
            if(bundle==null)return;
            page=Mathf.Clamp(index,0,PageCount-1);
            var catalog=bundle.Catalog();
            for(int i=0;i<slots.Length;i++)
            {
                int at=page*slots.Length+i;bool active=at<visible.Length;
                slots[i].gameObject.SetActive(active);identifiers[i].gameObject.SetActive(active);
                if(!active)continue;
                var card=visible[at];slots[i].runtimeMode=true;slots[i].cardName=card.name;
                slots[i].ApplyCard(card,catalog,bundle.layeredCards.Find(card.name),bundle.layeredCards.Frame(card.name));
                identifiers[i].text=card.id+"   /   "+(at+1).ToString("00");
            }
            SetMode(mode);
            previous.interactable=page>0;next.interactable=page<PageCount-1;
            pageLabel.text=$"{page+1} / {PageCount}     ·     {visible.Length} карт";
            TintButtons(filters,filter);ApplyLook();
        }
        public void SetMode(int index)
        {
            mode=Mathf.Clamp(index,0,3);
            foreach(var face in slots.Where(c=>c.gameObject.activeSelf))
            {
                face.SetGameArt(mode!=3,look,null);
                face.SetPreviewLayer(mode==3?0:mode);
            }
            TintButtons(modes,mode);RefreshStatus();
        }
        public void Pose(float time)
        {
            look=new Vector2(Mathf.Sin(time*.83f),Mathf.Sin(time*1.17f)*.7f);ApplyLook();
        }
        public void ApplyLook()
        {
            foreach(var face in slots.Where(c=>c.gameObject.activeSelf))
            {
                face.transform.localRotation=Quaternion.Euler(-look.y*17,look.x*23,-look.x*2);
                face.SetLook(look);
            }
            RefreshStatus();
        }
        public void ToggleRotation(){autoRotate=!autoRotate;RefreshStatus();}
        public void ResetPose(){autoRotate=false;look=Vector2.zero;phase=0;ApplyLook();}
        void RefreshStatus()
        {
            if(statusLabel!=null)statusLabel.text=$"{ModeNames[mode]}     ·     X {look.x*23:+0;-0;0}°   Y {look.y*17:+0;-0;0}°     ·     ЛКМ: наклон   /   ← →: страницы   /   1–4: слои   /   Пробел: вращение";
            if(rotation!=null)rotation.GetComponentInChildren<Text>().text=autoRotate?"Пауза вращения":"Автоповорот";
        }
        static void TintButtons(Button[] buttons,int selected)
        {
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].GetComponent<Image>().color=i==selected?new Color(.2f,.61f,.55f):new Color(.085f,.14f,.18f);
                buttons[i].GetComponentInChildren<Text>().color=i==selected?new Color(.025f,.05f,.06f):new Color(.85f,.9f,.92f);
            }
        }
        void Update()
        {
            if(bundle==null)return;
            if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.PageDown))ShowPage(page+1);
            if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.PageUp))ShowPage(page-1);
            if(Input.GetKeyDown(KeyCode.Home))ShowPage(0);
            if(Input.GetKeyDown(KeyCode.End))ShowPage(PageCount-1);
            if(Input.GetKeyDown(KeyCode.Space))ToggleRotation();
            if(Input.GetKeyDown(KeyCode.Alpha1))SetMode(0);
            if(Input.GetKeyDown(KeyCode.Alpha2))SetMode(1);
            if(Input.GetKeyDown(KeyCode.Alpha3))SetMode(2);
            if(Input.GetKeyDown(KeyCode.Alpha4))SetMode(3);
            if(Input.GetKeyDown(KeyCode.R))ResetPose();
            if(Input.GetMouseButtonDown(0))dragging=EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject();
            if(Input.GetMouseButtonUp(0))dragging=false;
            if(dragging&&Input.GetMouseButton(0))
            {
                autoRotate=false;
                look=new Vector2(Mathf.Clamp((Input.mousePosition.x/Screen.width-.5f)*2,-1,1),Mathf.Clamp((Input.mousePosition.y/Screen.height-.5f)*2,-1,1));ApplyLook();
            }
            else if(autoRotate){phase+=Time.unscaledDeltaTime;Pose(phase);}
        }
    }
}
