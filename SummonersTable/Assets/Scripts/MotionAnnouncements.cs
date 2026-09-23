using System.Linq;
using Michsky.UI.MTP;
using UnityEngine;
namespace SummonersTable
{
    public sealed class MotionAnnouncements : MonoBehaviour
    {
        public StyleManager style;string last="",configStyle="";
        public void Configure(string id)
        {
            if(configStyle==id)return;var prefab=ConfigRuntime.Assets.Get<GameObject>(id);if(prefab==null)return;
            var previous=style.transform;while(previous.parent!=transform&&previous.parent!=null)previous=previous.parent;
            var font=style.GetComponentInChildren<TMPro.TMP_Text>(true)?.font;var created=Instantiate(prefab,transform);created.transform.localPosition=previous.localPosition;created.transform.localRotation=previous.localRotation;created.transform.localScale=previous.localScale;
            foreach(var text in created.GetComponentsInChildren<TMPro.TMP_Text>(true))if(font!=null)text.font=font;
            style=created.GetComponentInChildren<StyleManager>(true);style.playOnEnable=false;style.loopAnimations=false;style.disableOnOut=true;style.Stop();foreach(var item in style.textItems.Where(t=>t!=null)){item.text="";item.UpdateText();}previous.gameObject.SetActive(false);Destroy(previous.gameObject);configStyle=id;last="";
        }
        public void Present(string page,MatchState state,int members)
        {
            string key="",text="";
            if(page=="local"||page=="steam"&&members>0){key="lobby-"+members;text="СОБИРАЕМСЯ ЗА СТОЛОМ";}
            else if(page=="game"&&state!=null)
            {
                if(state.phase=="matchEnd"){key="finish-"+state.matchId;text="ИГРА ЗАВЕРШЕНА";}
                else if(state.phase=="roundEnd"){key="roundEnd-"+state.matchId+state.round;text="РАУНД ЗАВЕРШЁН";}
                else{key="turn-"+state.matchId+state.turnNumber;text=(state.turnNumber==1?"ИГРА НАЧИНАЕТСЯ":"ХОД "+state.players[state.activeSeat].name).ToUpperInvariant();}
            }
            if(key==last)return;last=key;if(key==""){style.Stop();return;}
            foreach(var item in style.textItems.Where(t=>t!=null)){item.text=text;item.UpdateText();}
            style.Play();
        }
        public void Preview(string text){foreach(var item in style.textItems){item.text=text;item.UpdateText();}style.Play();}
    }
}
