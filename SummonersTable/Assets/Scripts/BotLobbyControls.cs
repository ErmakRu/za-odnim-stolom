using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class BotLobbyControls:MonoBehaviour
    {
        public Button[] seats;public Text[] labels;
        [NonSerialized] public Action<int> changed;
        void Awake(){for(int i=0;i<seats.Length;i++){int index=i;seats[i].onClick.AddListener(()=>changed?.Invoke(index));}}
        public void Present(IList<LobbyMember> members,bool local,bool host,int capacity)
        {
            var front=GetComponentInParent<FrontEndCanvas>();if(front!=null)foreach(var label in front.readyLabels)label.gameObject.SetActive(false);
            for(int i=0;i<seats.Length;i++)
            {
                var member=i<members.Count?members[i]:null;bool available=i<capacity;
                seats[i].gameObject.SetActive(available);bool bot=member?.isBot==true;
                labels[i].text=local?(i==0?"ВЫ · ЧЕЛОВЕК":bot?"БОТ · СМЕНИТЬ НА ИГРОКА":"ИГРОК · СМЕНИТЬ НА БОТА"):
                    member==null?(host?"+ ДОБАВИТЬ БОТА":"СВОБОДНО"):bot?(host?"БОТ · УБРАТЬ":"БОТ · ГОТОВ"):member.ready?"✓ ИГРОК ГОТОВ":"ИГРОК ВЫБИРАЕТ КАРТЫ";
                seats[i].interactable=host&&(local?i>0&&member!=null:member==null||bot);
            }
        }
    }
}
