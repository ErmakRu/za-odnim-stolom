using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        PrefabInterface ui;bool settingsOpen,postMatchLobby;string interfaceMatch="",browserKey="",roomsKey="";int localVoter;
        readonly Dictionary<string,WidgetScreen> unitBadges=new Dictionary<string,WidgetScreen>();
        readonly List<RectTransform> nameBlockers=new List<RectTransform>();
        readonly List<GameObject> browserCards=new List<GameObject>(),roomRows=new List<GameObject>();
        Vector2 Pointer=>captureMode?previewPointer??new Vector2(-100,-100):(Vector2)Input.mousePosition;
        bool InputBlocked=>settingsOpen||modal!=""||quitConfirm||handoff||state==null||state.phase=="matchEnd";
        void BindPrefabInterface()
        {
            ui=FindFirstObjectByType<PrefabInterface>(FindObjectsInactive.Include);if(ui==null)throw new InvalidOperationException("GameInterface prefab is missing from the scene.");
            audioSource=ui.GetComponent<AudioSource>();AudioSettings.Initialize();
            foreach(var label in ui.playerStatus)if(label!=null&&!label.seatOwned)label.gameObject.SetActive(false);
            ui.hud.Click("end",()=>Send(new GameCommand{kind="end"}));ui.hud.Click("cancel",ClearSelection);
            ui.hud.Click("rules",()=>modal="rules");ui.hud.Click("settings",()=>{settingsOpen=true;ClearSelection();});ui.hud.Click("journal",()=>{historyOpen=!historyOpen;ClearSelection();});
            ui.hud.Click("pass",()=>{if(state.cast!=null)Send(new GameCommand{kind="pass",phaseId=state.cast.id});});
            ui.history.close.onClick.AddListener(()=>historyOpen=false);
            ui.settings.Click("resume",()=>settingsOpen=false);ui.settings.Click("leave",()=>quitConfirm=true);
            ui.settings.Get<Slider>("master").SetValueWithoutNotify(AudioSettings.Master);ui.settings.Get<Slider>("effects").SetValueWithoutNotify(AudioSettings.Effects);
            ui.settings.Get<Slider>("master").onValueChanged.AddListener(v=>AudioSettings.Apply(v,AudioSettings.Effects));ui.settings.Get<Slider>("effects").onValueChanged.AddListener(v=>AudioSettings.Apply(AudioSettings.Master,v));
            ui.results.Click("again",()=>ChoosePostMatch("again"));ui.results.Click("deck",()=>ChoosePostMatch("deck"));ui.results.Click("menu",()=>ChoosePostMatch("menu"));
            ui.handoff.Click("continue",()=>{handoff=false;localReactionStarted=0;error="";});ui.handoff.Click("menu",()=>quitConfirm=true);
            ui.rules.Click("close",()=>modal="");ui.quit.Click("stay",()=>quitConfirm=false);ui.quit.Click("leave",ExitMatch);
            ui.search.Click("back",()=>{steam.Leave();page="menu";});ui.search.Click("retry",steam.Initialize);ui.search.Click("quick",()=>steam.Search(true));ui.search.Click("refresh",()=>steam.Search());
            ui.search.Click("join",()=>{if(ulong.TryParse(ui.search.Get<InputField>("code").text.Trim(),out var id))steam.Join(id);else steam.Error="Код лобби должен состоять из цифр.";});
            for(int n=2;n<=4;n++){int c=n;ui.search.Click("capacity"+n,()=>capacity=c);}
            ui.search.Click("create",()=>steam.Create(ui.search.Get<InputField>("room").text,capacity,true));ui.search.Click("cancel",steam.CancelSearch);
            ui.browser.Click("back",()=>page=returnPage);for(int d=-1;d<3;d++){int selected=d;ui.browser.Click("deck"+d,()=>{catalogDeck=selected;browserKey="";});}
            ui.hand.onPress=(h,e)=>{if(InputBlocked||historyOpen||state.qte!=null)return;ChooseHand(h,LogicalPointer(e.position));mouseHeld=selectedCard!="";handPress=LogicalPointer(e.position);draggingCard=false;};
            ui.hand.onDrag=(h,e)=>{if(mouseHeld)draggingCard|=Vector2.Distance(handPress,LogicalPointer(e.position))>18;};
            ui.hand.onRelease=(h,e)=>
            {
                if(mouseHeld&&draggingCard&&!InputBlocked&&!OverInterface(e.position)){var target=board.Pick(e.position);if(target!=null)ApplyWorldTarget(target.kind,target.seat,target.uid,target.slot);}
                mouseHeld=false;draggingCard=false;
            };
        }
        void UpdateAuthoredInterface()
        {
            if(ui==null)return;
            scale=Mathf.Min(Screen.width/W,Screen.height/H);offset=new Vector2((Screen.width-W*scale)/2,(Screen.height-H*scale)/2);
            bool game=page=="game"&&state!=null&&!handoff;bool ended=game&&state.phase=="matchEnd";
            if(interfaceMatch!=(state?.matchId??"")){interfaceMatch=state?.matchId??"";localVoter=0;postMatchLobby=false;settingsOpen=false;}
            ui.hud.Show(game&&!ended);ui.results.Show(ended);ui.handoff.Show(page=="game"&&handoff);
            ui.settings.Show(settingsOpen);ui.rules.Show(modal=="rules");ui.quit.Show(quitConfirm);
            ui.search.Show(page=="steam"&&!steam.InRoom);ui.browser.Show(page=="cards");
            ui.history.gameObject.SetActive(game&&!ended&&historyOpen&&!settingsOpen&&modal==""&&!quitConfirm);
            if(ui.history.gameObject.activeSelf)ui.history.Present(journal,state,catalog,font);
            ui.settings.Text("masterLabel","Общая громкость: "+Mathf.RoundToInt(AudioSettings.Master*100)+"%");ui.settings.Text("effectsLabel","Звуковые эффекты: "+Mathf.RoundToInt(AudioSettings.Effects*100)+"%");
            if(ui.rules.gameObject.activeSelf)
            {
                var options=CardPresentationContext.Options;
                ui.rules.Text("body",ConfigRuntime.GameplaySummary(catalog.rules)+(options.limitPower?"\n\nПределы усилений включены.":"\n\nПределы сложения усилений отключены."));
            }
            ui.quit.Text("note",online&&steam.IsHost?"Вы хост. Ваш выход завершит игру для остальных участников.":"Вернуться в главное меню?");
            if(ui.search.gameObject.activeSelf)PresentSearch();if(ui.browser.gameObject.activeSelf)PresentBrowser();
            if(ui.handoff.gameObject.activeSelf){ui.handoff.Text("name",state.players[seat].name);ui.handoff.Text("hint",Casting&&state.cast.owner!=seat?"Выберите реакцию или пропустите.\nQTE владельца приостановлен на время локального ответа.":"Сейчас появится ваша рука.\nОстальные игроки, отвернитесь от экрана.");}
            if(ended){PresentResults();return;}if(!game)return;
            board.ViewCamera.rect=new Rect(offset.x/Screen.width,(Screen.height-offset.y-760*scale)/Screen.height,W*scale/Screen.width,650*scale/Screen.height);
            var me=state.players[seat];var hud=ui.hud;string turn=state.players[state.activeSeat].name;
            hud.Text("turn","Раунд "+state.round+" / "+catalog.rules.rounds+" · Ход "+state.turnNumber);
            hud.Text("phase",state.phase=="action"?(MyAction?"ВАШ ХОД":"ХОД: "+turn):state.phase=="reveal"?"КАРТА ОБЪЯВЛЕНА":state.phase=="qte"?"РОЗЫГРЫШ: "+turn:state.phase=="combat"?"АТАКИ СУЩЕСТВ":"МАТЧ ЗАВЕРШЁН");
            hud.Text("timer",state.phase=="action"?"Решение: "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с":state.phase=="reveal"?"Общий показ: "+Math.Max(0,Math.Ceiling(state.cast.revealUntil-Clock))+" с":state.phase=="qte"?(state.qte!=null?"QTE видно только вам":"Буквы и таймер скрыты, прогресс виден на столе."):"");
            hud.Text("name",me.name);hud.Text("personal","HP "+me.hp+" · Очки "+me.score+" · Колода "+me.deckCount);
            hud.Enabled("end",MyAction&&!InputBlocked);hud.Get<Button>("end").targetGraphic.color=MatchRules.ReadyToEnd(catalog,state,seat)?new Color(.50f,.32f,.09f):new Color(.27f,.13f,.05f);
            hud.Text("endhint",MyAction?"Можно закончить ход раньше":"Наблюдаем за столом");hud.GetComponentInChildren<TurnBudgetView>(true).Present(state,seat);hud.Visible("pass",!online&&Casting&&state.cast.owner!=seat);hud.Visible("cancel",selectedCard!=""||selectedUnit!="");
            hud.Text("journal",historyOpen?"▲ Скрыть журнал":"▼ Журнал действий");
            string feedback=online&&steam.Error!=""?steam.Error:error;hud.Text("feedback",feedback);
            hud.Text("cast",Casting?catalog.Card(state.cast.cardId).name+"\nРеакции открыты":string.Join("\n",state.worldEvents.Select(e=>(catalog.events.events.FirstOrDefault(d=>d.id==e.eventId)?.name??"Событие")+" · "+(e.expiresTurn-state.turnNumber)+" хода"))+(state.matchDeadline>0?"\nДо конца партии: "+Math.Max(0,Math.Ceiling(state.matchDeadline-Clock))+" с":""));
            var chosen=catalog.Card(me.hand.Find(h=>h.uid==selectedCard)?.cardId);bool placing=chosen?.kind=="creature";
            hud.Text("hint",selectedUnit!=""?"Отпустите стрелку над целью · центр = случайный противник":selectedCard!=""?(placing?"Выберите свободный слот перед собой":"Укажите цель заклинания"):Casting&&state.cast.owner!=seat?CanAnyReact()?"Подходящие реакции подсвечены":"Наблюдаем за розыгрышем":"");
            string hot=!InputBlocked&&!historyOpen?ui.hand.Hit(Pointer):"";
            if(hot!=""&&hot!=hoverHandUid&&!captureMode){audioSource.pitch=UnityEngine.Random.Range(.92f,1.08f);ConfigAudio.Play("card.hover");}
            hoverHandUid=hot;ui.hand.Present(state,seat,catalog,font,selectedCard,hot,shakeHand,shakeUntil,board.CameraRig.Data);
            ui.arrow.gameObject.SetActive(!InputBlocked&&!placing&&(selectedCard!=""||selectedUnit!=""&&(unitDragMoved||previewAim)));
            if(ui.arrow.gameObject.activeSelf){var from=selectedUnit!=""?board.ViewCamera.WorldToScreenPoint(board.TargetPosition(seat,selectedUnit,state)):new Vector3(aimStart.x*scale+offset.x,Screen.height-aimStart.y*scale-offset.y);var to=previewAim?new Vector2(previewAimEnd.x*scale+offset.x,Screen.height-previewAimEnd.y*scale-offset.y):Pointer;ui.arrow.Set(from,to);}
            PresentWorldLabels();if(!captureMode)PrefabWorldInput();
        }
        void PresentWorldLabels()
        {
            for(int i=0;i<ui.playerStatus.Length;i++){var owned=board.Seat(i)?.status;if(owned!=null)ui.playerStatus[i]=owned;if(ui.playerStatus[i]==null)continue;bool visible=i<state.players.Count&&!(i==seat&&board.CameraRig.Mode==0);ui.playerStatus[i].gameObject.SetActive(visible);if(visible)ui.playerStatus[i].Present(state.players[i],state,board,catalog.rules.heroHp);}
            var active=new HashSet<string>();
            foreach(var p in state.players)foreach(var unit in p.units)
            {
                active.Add(unit.uid);if(!unitBadges.TryGetValue(unit.uid,out var badge)){badge=Instantiate(ui.unitBadgePrefab,ui.worldOverlay,false);unitBadges[unit.uid]=badge;}
                badge.Show(true);var d=catalog.Card(unit.cardId);int attack=d.attack+MatchRules.Stack(state,p.units.Where(u=>!u.deploying&&u.uid!=unit.uid&&catalog.Card(u.cardId).effect=="attackAura").Sum(u=>catalog.Card(u.cardId).value),state.rules.Limit("attackAura",2));badge.Text("stats",attack+"/"+unit.hp);badge.Get<Image>("group").color=RoleColor(d);
                SetWorldRect((RectTransform)badge.transform,TableBoard.SlotPosition(p.seat,unit.slot,state.players.Count)+Vector3.up*.1f,new Vector2(-24,-35));
            }
            foreach(var k in unitBadges.Keys.Where(k=>!active.Contains(k)).ToList()){Destroy(unitBadges[k].gameObject);unitBadges.Remove(k);}
            ui.hud.Visible("center",!Casting);SetWorldRect(ui.hud.Get<RectTransform>("center"),new Vector3(0,TableBoard.TableTop+.1f,0),new Vector2(-90,24));
            nameBlockers.Clear();nameBlockers.Add(ui.hud.Get<RectTransform>("journal"));
            foreach(var badge in unitBadges.Values)nameBlockers.Add((RectTransform)badge.transform);
            foreach(var status in ui.playerStatus)if(status!=null&&status.gameObject.activeInHierarchy)nameBlockers.Add(status.healthAnchor);
            foreach(var status in ui.playerStatus)if(status!=null&&status.gameObject.activeInHierarchy)status.AvoidNameOverlap(nameBlockers,board.ViewCamera);
        }
        void SetWorldRect(RectTransform r,Vector3 world,Vector2 padding)
        {var p=board.ViewCamera.WorldToScreenPoint(world);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)r.parent,p,null,out var local);r.position=((RectTransform)r.parent).TransformPoint(local+padding);}
        bool OverInterface(Vector2 point)
        {
            if(EventSystem.current==null)return false;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);return hits.Count>0;
        }
        string PrefabInspection(Vector2 point)
        {
            if(InputBlocked||page!="game")return "";
            if(historyOpen)return ui.history.Inspect(point);
            string announced=cardCanvas.InspectAt(point);if(announced!="")return announced;
            if(cardCanvas.Covers(point))return "";
            string hand=ui.hand.Hit(point);if(hand!="")return state.players[seat].hand.Find(h=>h.uid==hand)?.cardId??"";
            if(OverInterface(point))return "";var target=board.Pick(point);
            return target?.kind=="unit"?state.players[target.seat].units.Find(u=>u.uid==target.uid)?.cardId??"":"";
        }
        void PrefabWorldInput()
        {
            if(InputBlocked||historyOpen)return;var pointer=LogicalPointer(Input.mousePosition);bool available=!OverInterface(Input.mousePosition)&&pointer.y>=105&&pointer.y<=751;
            if(unitPointerHeld){if(Input.GetMouseButton(0))MoveUnitDrag(pointer);if(Input.GetMouseButtonUp(0)){MoveUnitDrag(pointer);var target=available?board.Pick(Input.mousePosition):null;FinishUnitDrag(target?.kind??"",target?.seat??-1,target?.uid??"",target?.slot??-1);}return;}
            if(!available||mouseHeld)return;var hit=board.Pick(Input.mousePosition);if(hit==null)return;
            if(Input.GetMouseButtonDown(0)&&hit.kind=="unit"&&hit.seat==seat&&MyAction&&selectedCard==""){BeginUnitDrag(hit.uid,pointer);return;}
            if(Input.GetMouseButtonUp(0))ApplyWorldTarget(hit.kind,hit.seat,hit.uid,hit.slot);
        }
        void ChoosePostMatch(string choice)
        {
            if(state?.phase!="matchEnd")return;
            if(CampaignResultChoice(choice))return;
            if(choice=="menu"){ExitMatch();return;}
            if(online){steam.ChooseAfterMatch(choice);if(choice=="deck"){postMatchLobby=true;page="steam";lobbyCanvas.CloseAppearance();}return;}
            if(choice=="deck"){postMatchLobby=true;page="local";lobbyCanvas.CloseAppearance();return;}
            var p=local.State.players.FirstOrDefault(p=>p.connected&&!p.isBot&&p.postMatchChoice!="again");
            if(p!=null)local.Submit(p.seat,new GameCommand{kind="postMatch",choice="again",seq=++seq[p.seat]},localTime);
            if(local.State.players.Where(p=>p.connected).All(p=>p.postMatchChoice=="again"))StartLocal(local.State.players.Count);else state=local.View(seat,localTime);
        }
        void PresentResults()
        {
            if(CampaignResults())return;
            var ready=online?steam.Members.Where(m=>RematchRules.Ready(state.players.Find(p=>p.id==m.id),m,state.matchId)).Select(m=>m.id).ToList():state.players.Where(p=>p.postMatchChoice=="again").Select(p=>p.id).ToList();
            ui.results.Text("result",state.result);ui.results.Text("scores",string.Join("\n\n",state.players.OrderByDescending(p=>p.score).Select(p=>p.name+" — "+p.score+" оч.  "+(ready.Contains(p.id)?"✓ Ещё раз":p.postMatchChoice=="deck"?"Выбирает колоду":""))));
            string voter=online?"":state.players.FirstOrDefault(p=>p.connected&&!p.isBot&&p.postMatchChoice!="again")?.name;
            ui.results.Text("votes","Готовы к новой игре: "+ready.Count+" / "+(online?steam.Members.Count:state.players.Count)+(voter!=""&&voter!=null?"\nПодтверждает: "+voter:""));
            ui.results.Enabled("again",!online||state.players[seat].postMatchChoice!="again");
        }
        void PresentSearch()
        {
            var screen=ui.search;screen.Text("status",steam.Status);screen.Text("error",steam.Error);screen.Visible("unavailable",!steam.Available);screen.Visible("retry",!steam.Available);
            foreach(string id in new[]{"quick","refresh","joinlabel","code","join","createLabel","room","capacity2","capacity3","capacity4","create","roomsLabel","rooms","empty","cancel"})screen.Visible(id,steam.Available);
            foreach(string id in new[]{"quick","refresh","join","create"})screen.Enabled(id,!steam.Busy);
            for(int n=2;n<=4;n++)screen.Get<Button>("capacity"+n).targetGraphic.color=n==capacity?new Color(.50f,.32f,.09f):new Color(.27f,.13f,.05f);
            screen.Text("empty",steam.Rooms.Count==0?(steam.Busy?"Ищем подходящие лобби…":"Открытых столов пока нет. Создайте свой и передайте друзьям код."):"");
            var sc=screen.Get<ScrollRect>("rooms");string key=string.Join("|",steam.Rooms.Select(r=>r.id+":"+r.name+":"+r.count))+steam.Busy;
            if(key==roomsKey)return;roomsKey=key;foreach(var r in roomRows)Destroy(r);roomRows.Clear();int index=0;
            foreach(var room in steam.Rooms){var row=Instantiate(ui.roomRowPrefab,sc.content,false);var rect=(RectTransform)row.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(0,-index++*74);row.Text("name",room.name+"   "+room.count+" / "+room.capacity);row.Click("join",()=>steam.Join(room.id));row.Enabled("join",!steam.Busy);roomRows.Add(row.gameObject);}sc.content.sizeDelta=new Vector2(sc.content.sizeDelta.x,Mathf.Max(sc.viewport.rect.height,index*74));
        }
        void PresentBrowser()
        {
            var screen=ui.browser;for(int d=0;d<3;d++)screen.Get<Button>("deck"+d).GetComponentInChildren<Text>().text=catalog.decks[d].name;
            string key=catalogDeck.ToString();if(key==browserKey)return;browserKey=key;foreach(var item in browserCards)Destroy(item);browserCards.Clear();
            var cards=catalogDeck<0?catalog.cards:catalog.decks[catalogDeck].entries.Select(e=>catalog.Card(e.cardId)).ToList();var sc=screen.Get<ScrollRect>("cards");
            screen.Text("guide",catalogDeck<0?"Бирюзовый — существо · Золотой — заклинание · Сиреневый — реакция":catalog.decks[catalogDeck].guide);
            for(int i=0;i<cards.Count;i++){var slot=Instantiate(ui.browserCardPrefab,sc.content,false);var r=(RectTransform)slot.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(i%5*290,-i/5*505);r.sizeDelta=new Vector2(270,480);slot.Show(cards[i],catalog,font);browserCards.Add(slot.gameObject);}sc.content.sizeDelta=new Vector2(1450,Mathf.Ceil(cards.Count/5f)*505);sc.content.anchoredPosition=Vector2.zero;
        }
    }
}
