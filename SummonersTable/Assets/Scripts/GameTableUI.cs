using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed partial class GameApp
    {
        TableBoard board;
        bool captureMode,draggingCard,mouseHeld,previewAim,previewLobby,unitPointerHeld,unitDragMoved;
        string hoverHandUid="",shakeHand="";
        float shakeUntil;
        double localReactionStarted;
        Vector2 handPress,aimStart,previewAimEnd,unitPress;
        Vector2? previewPointer;

        void UpdateSession()
        {
            steam.Update();
            if(steam.View!=null)
            {
                if(!online||page!="game"){online=true;local=null;page="game";handoff=false;ClearSelection();seenTurn=-1;}
                state=steam.View;seat=steam.Seat;
            }
            else if(online){online=false;page="steam";state=null;}
            if(page=="game"&&local!=null)
            {
                var s=local.State;int next=s.activeSeat;
                // A shared keyboard cannot accept simultaneous private inputs: pause the caster's
                // clock while the other local players privately choose one reaction or pass.
                if(s.phase=="qte"&&s.cast!=null&&s.pending!=null)
                {
                    var responder=s.players.FirstOrDefault(p=>p.alive&&p.connected&&!s.pending.responded.Contains(p.seat));
                    if(responder!=null)next=responder.seat;
                }
                if(s.phase!="roundEnd"&&s.phase!="matchEnd"&&next!=seat)
                {seat=next;handoff=true;localReactionStarted=0;ClearSelection();}
                bool localResponse=s.phase=="qte"&&s.cast!=null&&seat!=s.cast.owner;
                if(!handoff&&modal==""&&!quitConfirm)
                {
                    if(localResponse)
                    {
                        if(localReactionStarted==0)localReactionStarted=Time.realtimeSinceStartupAsDouble;
                        if(Time.realtimeSinceStartupAsDouble-localReactionStarted>=8)
                            Send(new GameCommand{kind="pass",phaseId=s.cast.id});
                    }
                    else{localTime+=Time.unscaledDeltaTime;localReactionStarted=0;}
                }
                local.Tick(localTime);state=local.View(seat,localTime);
            }
            if(state==null||page!="game")return;
            if(seenTurn!=state.turnNumber){ClearSelection();seenTurn=state.turnNumber;}
            if(seenPhase!=state.phase)
            {seenPhase=state.phase;if(state.phase!="action")ClearSelection();}
        }
        void ReadQteKeys()
        {
            var q=state.qte;
            if(q==null){lastQte="";return;}
            if(lastQte!=q.id){lastQte=q.id;lastProgress=q.index;lastMistakes=q.mistakes;}
            if(q.index>lastProgress)audioSource.PlayOneShot(board.CameraRig.settings?.qteSuccess??tickTone);
            if(q.mistakes>lastMistakes)audioSource.PlayOneShot(board.CameraRig.settings?.qteError??failTone);
            lastProgress=q.index;lastMistakes=q.mistakes;
            if(q.owner!=seat||modal!=""||quitConfirm)return;
            foreach(char key in "ASDFGHJ")
                if(Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode),key.ToString())))
                {Send(new GameCommand{kind="key",key=key.ToString(),phaseId=q.id});break;}
        }
        bool MyAction {get{return state!=null&&state.phase=="action"&&state.activeSeat==seat&&state.players[seat].alive;}}
        bool Casting {get{return state?.cast!=null&&(state.phase=="reveal"||state.phase=="qte");}}
        void Game()
        {
            if(state==null)return;
            if(handoff){Handoff();return;}
            var me=state.players[seat];
            Box(new Rect(0,0,W,100),panel);
            Box(new Rect(0,100,W,10),ink);
            Text(new Rect(28,19,310,33),"ЗА ОДНИМ СТОЛОМ",22,teal,true);
            Text(new Rect(28, 61,320,27),"Раунд "+state.round+" / 3  ·  Ход "+state.turnNumber,18,muted);
            string turn=state.players[state.activeSeat].name;
            string phase=state.phase=="action"?(MyAction?"ВАШ ХОД":"ХОД: "+turn):state.phase=="reveal"?"КАРТА ОБЪЯВЛЕНА":state.phase=="qte"?"РОЗЫГРЫШ: "+turn:state.phase=="combat"?"АТАКИ СУЩЕСТВ":"РАУНД ЗАВЕРШЁН";
            Text(new Rect(385,17,790,34),phase,26,MyAction?gold:Color.white,true,TextAnchor.MiddleCenter);
            string sub=state.phase=="action"?"Решение: "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с":state.phase=="reveal"?"Все видят карту ещё "+Math.Max(0,Math.Ceiling(state.cast.revealUntil-Clock))+" с":state.phase=="qte"?(state.qte!=null?"QTE видно только вам":"Прогресс ритуала виден на столе. Буквы и таймер скрыты."):"Счёт сохраняется между раундами";
            Text(new Rect(380,58,810,28),sub,18,muted,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(1310,27,120,44),"Правила",muted))modal="rules";
            if(Button(new Rect(1446,27,126,44),"Выйти",muted))quitConfirm=true;
            WorldLabels();
            Box(new Rect(0,760,W,240),new Color(.025f,.055f,.075f));
            Text(new Rect(24,924,260,33),me.name,23,TableBoard.SeatColors[seat],true);
            Text(new Rect(24,960,272,29),"HP "+me.hp+"  ·  Очки "+me.score+"  ·  Колода "+me.deckCount,17,Color.white);
            Text(new Rect(24,777,285,45),"Колесо — вид камеры\nПКМ — поворот взгляда",16,muted);
            bool ready=MatchRules.ReadyToEnd(catalog,state,seat);
            if(ready)Frame(new Rect(1315,897,260,70),Color.Lerp(gold,Color.white,.25f+.2f*Mathf.Sin(Time.unscaledTime*4)),3);
            if(Button(new Rect(1320,902,250,60),"Закончить ход",ready?gold:muted,MyAction))Send(new GameCommand{kind="end"});
            Text(new Rect(1318,970,260,23),MyAction?"Заклинаний: "+state.spellsPlayed+" / 3":"Наблюдаем за столом",16,muted,false,TextAnchor.MiddleCenter);
            if(Casting)CastBanner();
            DrawHand();
            if(MyAction&&(selectedCard!=""||selectedUnit!=""))
            {
                bool placing=selectedCard!=""&&catalog.Card(me.hand.Find(h=>h.uid==selectedCard)?.cardId)?.kind=="creature";
                string targetHint=selectedUnit!=""?"Отпустите стрелку над целью":placing?"Выберите свободный слот перед собой":"Укажите цель заклинания";
                Box(new Rect(410,704,780,44),panel);Text(new Rect(425,711,750,32),targetHint+(placing?"":" • центр стола = случайная цель"),19,gold,false,TextAnchor.MiddleCenter);
                var start=selectedUnit!=""?board.Project(board.TargetPosition(seat,selectedUnit,state),scale,offset):aimStart;
                if(!placing&&(selectedUnit==""||unitDragMoved||previewAim))DrawAim(start,previewAim?previewAimEnd:Event.current.mousePosition,gold);
                if(Button(new Rect(24,827,240,48),"Отменить выбор",muted))ClearSelection();
            }
            if(!online&&Casting&&state.cast.owner!=seat)
            {
                if(Button(new Rect(1318,826,250,50),"Пропустить реакцию",muted))Send(new GameCommand{kind="pass",phaseId=state.cast.id});
                Text(new Rect(1290,780,292,40),"Локальный ответ: "+Math.Max(0,8-(Time.realtimeSinceStartupAsDouble-localReactionStarted)).ToString("0")+" с",17,muted);
            }
            string feedback=online&&steam.Error!=""?steam.Error:error;
            if(feedback!=""){Box(new Rect(400,108,800,58),panel);Text(new Rect(416,117,768,49),feedback,19,red,false,TextAnchor.MiddleCenter);}
            else if(!Casting)Text(new Rect(400,112,800,46),state.lastEvent,18,Color.white,false,TextAnchor.MiddleCenter);
            if(state.phase=="roundEnd"||state.phase=="matchEnd")ScoreOverlay();
            else if(GUI.enabled&&modal==""&&!quitConfirm)WorldInput();
            DrawJournal();
        }

        void Handoff()
        {
            Dim();Text(new Rect(330,215,940,70),"Передайте клавиатуру",42,muted,true,TextAnchor.MiddleCenter);
            Text(new Rect(250,322,1100,90),state.players[seat].name,58,teal,true,TextAnchor.MiddleCenter);
            bool responder=state.cast!=null&&state.cast.owner!=seat;
            Text(new Rect(320,457,960,135),responder?"Выберите реакцию или пропустите.\nВ локальной игре QTE владельца на это время приостановлен.":"Сейчас появится ваша рука.\nОстальные игроки, отвернитесь от экрана.",25,null,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(530,640,540,70),"Я за столом — показать карты")){handoff=false;localReactionStarted=0;error="";}
            if(Button(new Rect(650,751,300,48),"В главное меню",muted))quitConfirm=true;
        }
        void WorldLabels()
        {
            Vector2 screenPointer=captureMode?previewPointer??new Vector2(-100,-100):(Vector2)Input.mousePosition;
            var hovered=!JournalCoversScreen(screenPointer)&&!cardCanvas.Covers(screenPointer)?board.Pick(screenPointer):null;
            foreach(var player in state.players)
            {
                if(hovered?.kind=="hero"&&hovered.seat==player.seat)
                {
                var rect=new Rect(1275,117,297,97);
                Box(rect,panel);Frame(rect,player.alive?TableBoard.SeatColors[player.seat]:muted,2);
                Text(new Rect(rect.x+8,rect.y+7,281,30),player.name+(player.seat==seat?" (вы)":""),20,null,true,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x+8,rect.y+42,281,24),"HP "+player.hp+"  ·  Очки "+player.score+"  ·  Рука "+player.handCount,17,player.alive?TableBoard.SeatColors[player.seat]:muted,false,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x+8,rect.y+69,281,20),player.alive?"Герой":"Выбыл из раунда",14,muted,false,TextAnchor.MiddleCenter);
                Box(new Rect(rect.x+3,rect.yMax-5,(rect.width-6)*board.DisplayHp("hero-"+player.seat,player.hp)/catalog.rules.heroHp,3),TableBoard.SeatColors[player.seat]);
                }
                foreach(var unit in player.units)
                {
                    var p=board.Project(TableBoard.SlotPosition(player.seat,unit.slot,state.players.Count)+Vector3.up*.1f,scale,offset);
                    if(cardCanvas.Covers(new Vector2(p.x*scale+offset.x,Screen.height-(p.y*scale+offset.y))))continue;
                    var c=catalog.Card(unit.cardId);int atk=c.attack+Math.Min(2,player.units.Where(u=>!u.deploying&&u.uid!=unit.uid&&catalog.Card(u.cardId).effect=="attackAura").Sum(u=>catalog.Card(u.cardId).value));
                    var stat=new Rect(p.x-21,p.y+22,42,22);Box(stat,panel);Text(stat,atk+"/"+unit.hp,14,null,true,TextAnchor.MiddleCenter);
                    Box(new Rect(p.x-20,p.y+44,40,4),RoleColor(c));
                }
            }
            if(!Casting)
            {
                var center=board.Project(new Vector3(0,TableBoard.TableTop+.1f,0),scale,offset);
                Text(new Rect(center.x-90,center.y-15,180,49),"СЛУЧАЙНАЯ\nЦЕЛЬ",16,new Color(.12f,.16f,.19f),true,TextAnchor.MiddleCenter);
            }
        }
        void CastBanner()
        {
            var cast=state.cast;var c=catalog.Card(cast.cardId);

            Box(new Rect(440,108,720,83),panel);
            Text(new Rect(457,120,686, 30),c.name+"  ·  "+TypeName(c),26,TypeColor(c),true,TextAnchor.MiddleCenter);
            string target=c.kind=="creature"?"можно назначить на столе после QTE":c.effect=="areaDamage"?"все противники":c.target=="self"||c.target=="none"?"на себя":cast.randomTarget?"случайная цель в центре":cast.targetSeat<0?"на себя":state.players[cast.targetSeat].name+(cast.targetUnit!=""?" / существо":"");
            Text(new Rect(457,159,686,26),"Цель: "+target+"  •  Реакции открыты",18,muted,false,TextAnchor.MiddleCenter);
            if(cast.owner!=seat)
            {
                Box(new Rect(410,704,780,42),panel);Text(new Rect(425,710,750,32),CanAnyReact()?"Подходящие реакции подсвечены в вашей руке":"Наблюдаем за розыгрышем",19,new Color(.80f,.71f,1),true,TextAnchor.MiddleCenter);
            }
        }

        bool CanReact(CardDef card,TargetRef target)
        {
            return MatchRules.CanReact(catalog,state,seat,card,target);
        }
        TargetRef ReactionTarget(CardDef c)
        {return state.pending?.targets.Where(t=>CanReact(c,t)).OrderBy(t=>t.seat==seat?0:1).ThenBy(t=>t.seat).FirstOrDefault();}
        bool CanAnyReact(){return state.players[seat].hand.Any(h=>ReactionTarget(catalog.Card(h.cardId))!=null);}
        void ChooseHand(HandCard hand,Vector2 origin)
        {
            var card=catalog.Card(hand.cardId);error="";unitPointerHeld=false;unitDragMoved=false;
            if(card.kind=="reaction")
            {
                var target=ReactionTarget(card);
                if(target!=null){Send(new GameCommand{kind="react",phaseId=state.cast.id,cardUid=hand.uid,targetSeat=target.seat,targetUnit=target.unit});ClearSelection();}
                else InvalidCard(hand.uid,"Эта реакция сейчас не подходит. Она играется только на чужой розыгрыш.");
                return;
            }
            if(!MatchRules.CanPlay(catalog,state,seat,card)){InvalidCard(hand.uid,"Сейчас эту карту разыграть нельзя.");return;}
            selectedCard=hand.uid;selectedUnit="";aimStart=origin;
            if(card.kind=="creature")
            {
                var me=state.players[seat];var free=Enumerable.Range(0,5).Where(slot=>!me.units.Any(u=>u.slot==slot)).ToList();
                if(free.Count==0){error="На столе нет свободных слотов.";selectedCard="";return;}
                selectedSlot=-1;
            }
        }
        Rect HandRect(int i,int count,bool lifted)
        {
            float midpoint=(count-1)/2f;float spread=Math.Min(132,830f/Math.Max(1,count-1));float x=800+(i-midpoint)*spread;
            float y=754+Mathf.Pow(Mathf.Abs(i-midpoint)/Math.Max(1,midpoint),2)*18;
            float size=board.CameraRig.Data.hoverScale;return lifted?new Rect(x-75*size,y-35,150*size,210*size):new Rect(x-75,y,150,210);
        }

        void DrawHand()
        {
            var hand=state.players[seat].hand;Vector2 pointer=Event.current.mousePosition;int hot=-1;
            for(int i=0;i<hand.Count;i++)
            {
                var r=HandRect(i,hand.Count,selectedCard==hand[i].uid);float angle=selectedCard==hand[i].uid?0:(i-(hand.Count-1)/2f)*3.2f;
                Vector2 local=Rotate(pointer,r.center,-angle);
                if(r.Contains(local))hot=i;
            }
            int previous=hand.FindIndex(h=>h.uid==hoverHandUid);
            if(previous>=0&&HandRect(previous,hand.Count,true).Contains(pointer))hot=previous;
            string nextHover=hot>=0?hand[hot].uid:"";
            if(nextHover!=""&&nextHover!=hoverHandUid&&Event.current.type==EventType.Repaint)
            {audioSource.pitch=UnityEngine.Random.Range(.92f,1.08f);audioSource.PlayOneShot(board.CameraRig.settings?.cardHover??tickTone);}
            hoverHandUid=nextHover;
            bool blocked=(state.phase=="roundEnd"||state.phase=="matchEnd"||state.qte!=null||!GUI.enabled);
            // Draw the selected/hovered card last so it rises above the fan.
            int raised=hot>=0?hot:hand.FindIndex(h=>h.uid==selectedCard);
            var order=Enumerable.Range(0,hand.Count).Where(i=>i!=raised).Concat(raised<0?Enumerable.Empty<int>():new[]{raised}).ToList();
            foreach(int i in order)
            {
                var h=hand[i];var c=catalog.Card(h.cardId);bool lift=i==raised&&!blocked;
                var rect=HandRect(i,hand.Count,lift);var matrix=GUI.matrix;
                if(h.uid==shakeHand&&Time.unscaledTime<shakeUntil)rect.x+=Mathf.Sin(Time.unscaledTime*70)*9;
                float tilt=lift?Mathf.Clamp((rect.center.x-pointer.x)/rect.width,-1,1)*board.CameraRig.Data.hoverTilt:(i-(hand.Count-1)/2f)*3.2f;
                GUIUtility.RotateAroundPivot(tilt,rect.center);
                if(MatchRules.CanUse(catalog,state,seat,c))
                {
                    var glow=c.kind=="reaction"?new Color(.85f,.67f,1):new Color(.39f,.94f,.67f);
                    Frame(new Rect(rect.x-7,rect.y-7,rect.width+14,rect.height+14),new Color(glow.r,glow.g,glow.b,.24f),7);
                    Frame(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),glow,3);
                }
                MiniCard(c,rect,selectedCard==h.uid);GUI.matrix=matrix;
                if(selectedCard==h.uid)aimStart=new Vector2(rect.center.x,rect.y+18);
            }
            if(blocked)return;
            if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&hot>=0)
            {
                ChooseHand(hand[hot],new Vector2(HandRect(hot,hand.Count,false).center.x,760));
                mouseHeld=selectedCard!="";handPress=pointer;draggingCard=false;Event.current.Use();
            }
            if(mouseHeld&&Event.current.type==EventType.MouseDrag)
            {draggingCard|=Vector2.Distance(pointer,handPress)>18;Event.current.Use();}
            if(Event.current.type==EventType.MouseUp&&Event.current.button==0&&mouseHeld)
            {
                mouseHeld=false;
                if(draggingCard)
                {
                    var hit=board.Pick(Input.mousePosition);if(hit!=null)ApplyWorldTarget(hit.kind,hit.seat,hit.uid,hit.slot);
                    draggingCard=false;Event.current.Use();
                }
                else if(hot>=0)Event.current.Use();
            }
        }
        static Vector2 Rotate(Vector2 point,Vector2 pivot,float angle)
        {float radians=angle*Mathf.Deg2Rad;var v=point-pivot;return pivot+new Vector2(v.x*Mathf.Cos(radians)-v.y*Mathf.Sin(radians),v.x*Mathf.Sin(radians)+v.y*Mathf.Cos(radians));}
        string InspectionAt(Vector2 screenPoint)
        {
            if(page!="game"||state==null||handoff||modal!=""||quitConfirm)return "";
            float factor=Mathf.Min(Screen.width/W,Screen.height/H);
            var padding=new Vector2((Screen.width-W*factor)/2,(Screen.height-H*factor)/2);
            var pointer=new Vector2((screenPoint.x-padding.x)/factor,(Screen.height-screenPoint.y-padding.y)/factor);
            if(JournalCovers(pointer))return JournalInspection(screenPoint);
            string canvasCard=cardCanvas.InspectAt(screenPoint);if(canvasCard!="")return canvasCard;
            if(cardCanvas.Covers(screenPoint))return "";
            var hand=state.players[seat].hand;
            int previous=hand.FindIndex(h=>h.uid==hoverHandUid);
            if(previous>=0&&HandRect(previous,hand.Count,true).Contains(pointer))return hand[previous].cardId;
            for(int i=hand.Count-1;i>=0;i--)
            {
                var r=HandRect(i,hand.Count,selectedCard==hand[i].uid);
                float angle=selectedCard==hand[i].uid?0:(i-(hand.Count-1)/2f)*3.2f;
                if(r.Contains(Rotate(pointer,r.center,-angle)))return hand[i].cardId;
            }
            if(pointer.y<105||pointer.y>751)return "";
            var target=board.Pick(screenPoint);
            if(target?.kind=="unit")return state.players[target.seat].units.Find(u=>u.uid==target.uid)?.cardId??"";
            return "";
        }
        void BeginUnitDrag(string uid,Vector2 pointer)
        {
            if(!MyAction||!state.players[seat].units.Any(u=>u.uid==uid))return;
            ClearSelection();selectedUnit=uid;unitPointerHeld=true;unitPress=pointer;
        }
        void MoveUnitDrag(Vector2 pointer)
        {if(unitPointerHeld)unitDragMoved|=Vector2.Distance(pointer,unitPress)>8;}
        void FinishUnitDrag(string kind,int owner,string uid,int slot)
        {
            if(unitPointerHeld&&unitDragMoved&&kind!="")ApplyWorldTarget(kind,owner,uid,slot);
            // Dropping on empty space or an illegal target preserves the previous assignment.
            selectedUnit="";unitPointerHeld=false;unitDragMoved=false;
        }
        void WorldInput()
        {
            var e=Event.current;var pointer=e.mousePosition;
            if(unitPointerHeld&&e.type==EventType.MouseDrag){MoveUnitDrag(pointer);e.Use();return;}
            bool available=pointer.y>=105&&pointer.y<=751&&!cardCanvas.Covers(Input.mousePosition)&&!JournalCovers(pointer);
            if(unitPointerHeld&&e.type==EventType.MouseUp&&e.button==0)
            {
                MoveUnitDrag(pointer);var drop=available?board.Pick(Input.mousePosition):null;
                FinishUnitDrag(drop?.kind??"",drop?.seat??-1,drop?.uid??"",drop?.slot??-1);e.Use();return;
            }
            if(!available)return;
            var target=board.Pick(Input.mousePosition);
            if(target==null)return;
            if(e.type==EventType.MouseDown&&e.button==0&&target.kind=="unit"&&target.seat==seat&&MyAction&&selectedCard=="")
            {
                BeginUnitDrag(target.uid,pointer);e.Use();return;
            }
            if(e.type==EventType.MouseUp&&e.button==0)
            {ApplyWorldTarget(target.kind,target.seat,target.uid,target.slot);e.Use();}
        }
        void ApplyWorldTarget(string kind,int owner,string uid,int slot)
        {
            if(kind=="unit"&&selectedCard==""&&selectedUnit==""&&(!MyAction||owner!=seat))
            {board.Invalid(uid);InvalidCard("","Это существо нельзя использовать сейчас.");return;}
            if(!MyAction)return;
            if(kind=="slot"&&owner==seat&&selectedCard!=""&&catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard).cardId).kind=="creature")
            {
                selectedSlot=slot;string cardUid=selectedCard;
                Send(new GameCommand{kind="play",cardUid=cardUid,slot=slot});
                if(error=="")ClearSelection();else InvalidCard(cardUid,error);return;
            }
            if(selectedCard!="")
            {
                if(catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard)?.cardId)?.kind=="creature")
                {InvalidCard(selectedCard,"Сначала выберите свободный слот на своей стороне стола.");return;}
                if(kind=="slot"){error="Укажите героя, существо или центр стола.";return;}
                Send(new GameCommand{kind="play",cardUid=selectedCard,slot=selectedSlot,targetSeat=kind=="center"?-1:owner,targetUnit=kind=="unit"?uid:""});
                if(error==""){ClearSelection();mouseHeld=false;draggingCard=false;}return;
            }
            if(selectedUnit!="")
            {
                if(kind!="center"&&owner==seat){error="Выберите вражескую цель или центр стола.";return;}
                if(kind=="slot")return;
                Send(new GameCommand{kind="target",unitUid=selectedUnit,targetSeat=kind=="center"?-1:owner,targetUnit=kind=="unit"?uid:""});
                if(error=="")ClearSelection();return;
            }
        }
        void DrawAim(Vector2 start,Vector2 end,Color color)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var path=TargetArrowGeometry.Build(start,end);
            if(path.Length==0)return;
            for(int i=0;i<path.Length-2;i+=2)ScreenLine(path[i],path[i+1],color,7);
            var dir=(end-path[path.Length-2]).normalized;var side=new Vector2(-dir.y,dir.x);
            ScreenLine(end-dir*22+side*12,end,color,7);ScreenLine(end-dir*22-side*12,end,color,7);
        }
        void ScreenLine(Vector2 from,Vector2 to,Color color,float width)
        {
            var old=GUI.matrix;
            GUI.matrix=old*Matrix4x4.TRS(from,Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg),Vector3.one);
            Box(new Rect(0,-width/2,Vector2.Distance(from,to),width),color);GUI.matrix=old;
        }
        void InvalidCard(string uid,string message)
        {error=message;shakeHand=uid;shakeUntil=Time.unscaledTime+.35f;audioSource.pitch=1;audioSource.PlayOneShot(board.CameraRig.settings?.invalidAction??failTone);}

        // Offline deterministic render fixtures. No Steam traffic, external UI control or AI player.
        IEnumerator CaptureLabPreview()
        {
            yield return new WaitForSeconds(.5f);
            var lab=FindFirstObjectByType<PresentationLab>();if(lab==null||state==null)throw new Exception("PresentationLab startup failed");
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");
            string directory=index>=0&&index+1<args.Length?args[index+1]:Path.GetFullPath("captures-lab");Directory.CreateDirectory(directory);
            string json=JsonUtility.ToJson(board.CameraRig.Data,true);File.WriteAllText(Path.Combine(directory,"presentation-roundtrip.json"),json);
            board.CameraRig.Apply(JsonUtility.FromJson<PresentationData>(File.ReadAllText(Path.Combine(directory,"presentation-roundtrip.json"))));
            if(FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length!=1)throw new Exception("Duplicate active EventSystems");
            yield return Shot(directory,"lab-camera-and-settings");
            LabAttack();localTime+=.41;local.Tick(localTime);state=local.View(seat,localTime);
            yield return Shot(directory,"lab-combat-effects");
            var actor=board.Actor(1);
            foreach(string clip in new[]{"Idle_Normal","Defend","AttackCombo04","Die","AttackCombo05","DashForward","DashBackward","DieRecover","Dizzy","GetHit","Sliding"})
            {
                actor.Play(clip,2);yield return new WaitForSeconds(.25f);
                var current=actor.Animator.GetCurrentAnimatorStateInfo(0);var next=actor.Animator.GetNextAnimatorStateInfo(0);
                if(!current.IsName(clip)&&!next.IsName(clip))throw new Exception("Hero clip not playing: "+clip);
            }
            actor.Die();yield return new WaitForSeconds(1.4f);if(actor.AnimationName!="AttackCombo05")throw new Exception("Death sequence incomplete");
            actor.Revive();yield return Shot(directory,"lab-hero-revive");
            announcements.Preview("ВАШ ХОД");yield return new WaitForSeconds(1.4f);yield return Shot(directory,"lab-motion-titles");
            if(!announcements.style.IsPlaying)throw new Exception("Motion Titles did not start");
            File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS PresentationLab boot without Steam, one EventSystem, JSON export/import roundtrip, combat effects and settings panel, all 11 humanoid Animator states, Die to AttackCombo05 then DieRecover, Cyrillic Motion Titles.\n");Application.Quit();
        }
        IEnumerator CapturePreview()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");
            string directory=index>=0&&index+1<args.Length?args[index+1]:Path.GetFullPath("captures");Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(.4f);
            if(FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=1||FindObjectsByType<CardTableCanvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=1)throw new Exception("Duplicate match presentation objects after scene load");
            yield return Shot(directory,"01-menu");
            menuCanvas.buttons[1].onClick.Invoke();if(page!="local")throw new Exception("Main menu local button is not connected");
            previewLobby=true;page="local";localCount=4;menuCanvas.Visible(false);lobbyCanvas.Visible(true);
            lobbyCanvas.PresentLobby(LocalLobbyMembers(),"local-0",false,true,true,catalog,"ТЕСТОВЫЙ СТОЛ · 4 ИГРОКА","Предпросмотр без подключения к Steam");
            yield return Shot(directory,"01a-lobby-layout");
            foreach(var portrait in lobbyCanvas.portraits)
            {
                if(portrait.actor.Head==null||portrait.actor.Animator==null)throw new Exception("Lobby hero rig missing");
                if(portrait.actor.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Weapon"&&t.gameObject.activeSelf))throw new Exception("Hero weapon visible");
            }
            string oldHero=localHeroes[0];int oldDeck=localDecks[0];
            FrontEndAction("hero-next:0");FrontEndAction("deck-next:0");FrontEndAction("customize:0");FrontEndAction("outfit-next");FrontEndAction("palette:2");
            if(localHeroes[0]==oldHero||localDecks[0]==oldDeck||localOutfits[0]!=1||localPalettes[0]!=2)throw new Exception("Lobby choices not connected");
            lobbyCanvas.PresentLobby(LocalLobbyMembers(),"",true,true,true,catalog,"ЛОКАЛЬНЫЙ СТОЛ","");yield return Shot(directory,"01b-appearance-controls");FrontEndAction("customize-close");
            localHeroes=new[]{"lizard","owl","rabbit","rat"};
            lobbyCanvas.PresentLobby(LocalLobbyMembers(),"",true,true,true,catalog,"ЛОКАЛЬНЫЙ СТОЛ","");yield return Shot(directory,"01c-more-animal-heroes");
            localCount=2;lobbyCanvas.PresentLobby(LocalLobbyMembers(),"",true,true,true,catalog,"ЛОКАЛЬНЫЙ СТОЛ · 2 ИГРОКА","");yield return Shot(directory,"01d-two-player-lobby");
            localCount=4;localHeroes=new[]{"badger","deer","dog","lion"};localOutfits=new[]{0,1,2,3};localPalettes=new[]{0,1,2,3};previewLobby=false;
            StartLocal(4);handoff=false;localTime=0;
            foreach(var p in local.State.players)
            {
                p.hand.Clear();p.units.Clear();
                for(int i=0;i<3;i++)p.units.Add(new UnitState{uid="preview-unit-"+p.seat+"-"+i,cardId="C"+(1+p.seat*3+i).ToString("00"),hp=4,slot=i,plannedSeat=i==2?-1:(p.seat+1)%4,targetAssigned=true});
            }
            foreach(string id in new[]{"C02","S01","S02","S06","C03","R01"})local.State.players[0].hand.Add(new HandCard{uid="preview-"+id,cardId=id});
            local.State.players[1].hand.Add(new HandCard{uid="preview-reaction",cardId="R01"});
            local.State.players[1].hand.Add(new HandCard{uid="preview-other",cardId="C07"});
            seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,true);
            yield return Shot(directory,"02-own-turn-table");
            board.CameraRig.SetMode(0);board.CameraRig.Sync(0,4,false,true);
            yield return Shot(directory,"02a-first-person");
            board.CameraRig.SetMode(1);board.CameraRig.Sync(0,4,false,true);
            yield return Shot(directory,"02b-above-head");
            if(state.players[0].heroId!=localHeroes[0]||board.Actor(0)?.Head==null)throw new Exception("Lobby appearance lost entering match");
            board.Actor(1).SetLook(new Vector2(45,-20));board.Actor(1).Play("Sliding",2);
            yield return Shot(directory,"02c-hero-animation");
            board.CameraRig.SetMode(2);board.CameraRig.Sync(0,4,false,true);
            foreach(var player in state.players)
            {
                var hit=board.Pick(board.ViewCamera.WorldToScreenPoint(TableBoard.HeroPosition(player.seat,4)));
                if(hit==null||hit.kind!="hero"||hit.seat!=player.seat)throw new Exception("Hero picking failed: "+player.seat);
                foreach(var unit in player.units)
                {
                    hit=board.Pick(board.ViewCamera.WorldToScreenPoint(TableBoard.SlotPosition(player.seat,unit.slot,4)+Vector3.up*.07f));
                    if(hit==null||hit.kind!="unit"||hit.uid!=unit.uid)throw new Exception("Unit picking failed: "+unit.uid);
                }
            }
            ChooseHand(state.players[0].hand.Find(h=>h.uid=="preview-C02"),new Vector2(600,790));
            if(selectedSlot!=-1||local.State.phase!="action")throw new Exception("Creature requires explicit slot confirmation");
            ApplyWorldTarget("hero",1,"",-1);
            if(local.State.phase!="action")throw new Exception("Creature played without slot confirmation");
            ClearSelection();
            var arrowStart=board.Project(board.TargetPosition(0,"preview-unit-0-0",state),scale,offset);
            BeginUnitDrag("preview-unit-0-0",arrowStart);
            FinishUnitDrag("center",-1,"",-1);
            if(local.State.players[0].units[0].plannedSeat!=1||selectedUnit!="")throw new Exception("A click assigned a creature target");
            BeginUnitDrag("preview-unit-0-0",arrowStart);MoveUnitDrag(arrowStart+Vector2.right*80);
            FinishUnitDrag("unit",0,"preview-unit-0-1",1);
            if(local.State.players[0].units[0].plannedSeat!=1||selectedUnit!="")throw new Exception("Illegal drop changed the planned target");
            BeginUnitDrag("preview-unit-0-0",arrowStart);MoveUnitDrag(arrowStart+Vector2.right*80);
            FinishUnitDrag("",-1,"",-1);
            if(local.State.players[0].units[0].plannedSeat!=1||selectedUnit!="")throw new Exception("Empty drop changed the planned target");
            BeginUnitDrag("preview-unit-0-0",arrowStart);MoveUnitDrag(arrowStart+Vector2.right*60);
            previewAim=true;
            previewAimEnd=arrowStart+Vector2.right*60;
            yield return Shot(directory,"03a-short-target-arrow");
            previewAimEnd=board.Project(TableBoard.HeroPosition(1,4),scale,offset);
            yield return Shot(directory,"03-target-arrow");previewAim=false;ClearSelection();
            seat=1;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,false);
            yield return Shot(directory,"04-observer-camera");
            seat=0;state=local.View(seat,localTime);
            ChooseHand(state.players[0].hand.Find(h=>h.uid=="preview-S01"),new Vector2(600,790));
            ApplyWorldTarget("hero",1,"",-1);
            if(local.State.phase!="reveal")throw new Exception("Target selection did not announce card");
            seat=1;localTime+=.65;local.Tick(localTime);
            state=local.View(seat,localTime);yield return Shot(directory,"05-two-second-reveal");
            local.Submit(1,new GameCommand{seq=1,kind="react",cardUid="preview-reaction",phaseId=local.State.cast.id,targetSeat=1},localTime+.5);
            state=local.View(seat,localTime);yield return Shot(directory,"05a-reaction-over-reveal");CheckReactionOverlays(1,"R01");
            localTime=local.State.cast.revealUntil;local.Tick(localTime);state=local.View(seat,localTime);
            if(state.qte!=null||state.deadline!=0)throw new Exception("Observer QTE leaked in preview");
            yield return Shot(directory,"06-observer-private-qte-hidden");
            CheckCardPanels("S01",false);
            seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,true);
            if(state.qte==null)throw new Exception("Caster QTE missing in preview");
            yield return Shot(directory,"07-caster-private-qte");
            CheckCardPanels("S01",true);
            CheckReactionOverlays(1,"R01");
            previewPointer=RectTransformUtility.WorldToScreenPoint(null,cardCanvas.reactionSlots[0].transform.position);
            yield return Shot(directory,"07c-reaction-hover");
            if(cardCanvas.rightSlot.CardId!="R01")throw new Exception("Reaction full inspection missing");
            previewPointer=null;historyOpen=true;yield return Shot(directory,"07d-history-during-qte");
            if(cardCanvas.qtePanel.gameObject.activeSelf||cardCanvas.keyButtons.Any(b=>b.interactable))throw new Exception("Journal permits QTE button click-through");
            historyOpen=false;
            previewPointer=new Vector2(HandRect(0,state.players[0].hand.Count,false).center.x*scale+offset.x,Screen.height-(900*scale+offset.y));
            yield return Shot(directory,"07a-hover-inspection");
            if(!cardCanvas.rightSlot.gameObject.activeSelf||cardCanvas.rightSlot.CardId!="C02")throw new Exception("Hand hover did not inspect C02");
            previewPointer=null;yield return Shot(directory,"07b-hover-cleared");
            CheckCardPanels("S01",true);
            var ritual=local.State.qte;
            local.Submit(0,new GameCommand{seq=++seq[0],kind="key",phaseId=ritual.id,key=ritual.sequence[0].ToString()},localTime);
            state=local.View(1,localTime);seat=1;board.SnapCamera(1,false);
            if(state.qte!=null||state.cast.qteProgress!=1)throw new Exception("Public progress/private letters contract failed");
            yield return Shot(directory,"08-public-qte-orbs");
            seat=0;LabFinishQte();ClearSelection();board.SnapCamera(0,true);
            for(int owner=1;owner<4;owner++)local.State.players[owner].hand.Add(new HandCard{uid="copy-reaction-"+owner,cardId="R02"});
            ChooseHand(state.players[0].hand.Find(h=>h.uid=="preview-C02"),new Vector2(600,790));ApplyWorldTarget("slot",0,"",3);
            if(local.State.cast?.slot!=3)throw new Exception("Click-to-slot summon failed");
            for(int owner=1;owner<4;owner++)
            {
                string uid="copy-reaction-"+owner;
                if(!local.Submit(owner,new GameCommand{seq=100+owner,kind="react",cardUid=uid,phaseId=local.State.cast.id,targetSeat=0},localTime).ok)throw new Exception("Copy reaction fixture rejected");
            }
            state=local.View(seat,localTime);yield return Shot(directory,"08b-three-reactions-over-summon");CheckReactionOverlays(3,"R02");
            localTime=local.State.cast.revealUntil;local.Tick(localTime);state=local.View(seat,localTime);
            yield return Shot(directory,"08a-summon-private-qte");CheckCardPanels("C02",true);
            CheckReactionOverlays(3,"R02");
            LabFinishQte();
            yield return Shot(directory,"09-summon-rests-on-table");
            CheckSummonPreviewCleared();
            seat=1;state=local.View(seat,localTime);yield return Shot(directory,"09b-observer-summon-cleared");CheckSummonPreviewCleared();
            seat=0;state=local.View(seat,localTime);
            if(selectedUnit!=""||unitPointerHeld||cardCanvas.rightSlot.gameObject.activeSelf)throw new Exception("Summon was automatically selected or inspected");
            var summoned=board.GetComponentsInChildren<BoardTarget>().First(t=>t.uid=="preview-C02");
            if(Mathf.Abs(summoned.transform.position.y-TableBoard.SlotPosition(0,3,4).y-.065f)>.01f)throw new Exception("Summon still floats above its slot");
            previewPointer=board.ViewCamera.WorldToScreenPoint(summoned.transform.position);
            yield return Shot(directory,"09a-world-hover-inspection");
            if(!cardCanvas.rightSlot.gameObject.activeSelf||cardCanvas.rightSlot.CardId!="C02")throw new Exception("World hover did not inspect summoned creature");
            previewPointer=null;
            BeginUnitDrag("preview-C02",board.Project(summoned.transform.position,scale,offset));
            MoveUnitDrag(unitPress+Vector2.up*100);FinishUnitDrag("center",-1,"",-1);
            if(!local.State.players[0].units.Find(u=>u.uid=="preview-C02").targetAssigned||local.State.players[0].units.Find(u=>u.uid=="preview-C02").plannedSeat!=-1)throw new Exception("Drag to random center failed");
            BeginUnitDrag("preview-C02",board.Project(summoned.transform.position,scale,offset));
            MoveUnitDrag(unitPress+Vector2.up*100);FinishUnitDrag("hero",1,"",-1);
            if(local.State.players[0].units.Find(u=>u.uid=="preview-C02").plannedSeat!=1)throw new Exception("Drag to enemy hero failed");
            Send(new GameCommand{kind="target",unitUid="preview-unit-0-0",targetSeat=1,targetUnit="preview-unit-1-0"});
            Send(new GameCommand{kind="end"});localTime+=.41;local.Tick(localTime);state=local.View(seat,localTime);
            yield return Shot(directory,"10-sequential-attack-impact");
            yield return CaptureFeedbackDetails(directory);
            if(board.CameraRig.Mode!=2)throw new Exception("Turn changed manual camera mode");
            ClearSelection();StartLocal(2);handoff=false;state=local.View(0,0);board.Sync(state,0,0);board.SnapCamera(0,true);
            yield return Shot(directory,"11-duel-opposite-seats");
            CheckHandBackCounts();
            StartLocal(3);handoff=false;state=local.View(0,0);board.Sync(state,0,0);board.SnapCamera(0,true);
            yield return Shot(directory,"12-three-player-layout");
            CheckHandBackCounts();yield return CaptureFailedSummon(directory);
            int captured=Directory.GetFiles(directory,"*.png").Length;
            if(captured<36)throw new Exception("Runtime screenshots are missing");
            File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS "+captured+" nonblank runtime screenshots without missing shaders or duplicate worlds/Canvas. Tavern lobby, all eight imported hero rigs, Weapon disabled, deck/hero/armor/palette callbacks, appearance preserved in match. Three manual cameras, 2/3/4 seating and exact public 3D hand counts. Revealed card goes left for caster and observer; right slot follows hand/world/reaction/history hover and clears on leave. One/three UI reaction overlays tilt about 20 degrees and follow the cast. Summon previews clear on success and failure for caster and observer. Private QTE and public progress. Hero HP/name only on hover. History source and target inspection, input coverage, damage and retaliation ownership. Journal suppresses QTE click-through. Four hero and 12 unit raycasts. Summon lands on table without automatic selection. Short/long segmented arrows without aim VFX; explicit drag to hero/center, click and invalid/empty drop preserve old target. Spell target before QTE; deferred combat; camera unchanged by turn.\n");
            Debug.Log("PREVIEW_CAPTURE_COMPLETE "+directory);Application.Quit();
        }
        void CheckCardPanels(string id,bool ownQte)
        {
            if(!cardCanvas.leftSlot.gameObject.activeSelf||cardCanvas.leftSlot.CardId!=id||cardCanvas.rightSlot.gameObject.activeSelf||cardCanvas.qtePanel.gameObject.activeSelf!=ownQte)
                throw new Exception("Card panels violated left announcement / hover-only right / private QTE contract");
        }
        void CheckSummonPreviewCleared()
        {
            if(cardCanvas.leftSlot.gameObject.activeSelf||cardCanvas.centerSlot.gameObject.activeSelf||cardCanvas.reactionSlots.Any(s=>s.gameObject.activeSelf))throw new Exception("Summon preview survived QTE");
        }
        void CheckReactionOverlays(int count,string id)
        {
            if(cardCanvas.reactionSlots.Count(s=>s.gameObject.activeSelf)!=count)throw new Exception("Wrong reaction overlay count");
            foreach(var slot in cardCanvas.reactionSlots.Where(s=>s.gameObject.activeSelf))
                if(slot.CardId!=id||Mathf.Abs(Mathf.DeltaAngle(slot.transform.localEulerAngles.z,-20))>3)throw new Exception("Wrong reaction card or tilt");
            var top=cardCanvas.reactionSlots[count-1];var pointer=RectTransformUtility.WorldToScreenPoint(null,top.transform.position);
            if(cardCanvas.InspectAt(pointer)!=id||!cardCanvas.Covers(pointer))throw new Exception("Reaction hover/input coverage failed");
        }
        IEnumerator Shot(string directory,string name)
        {
            yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();
            var capture=ScreenCapture.CaptureScreenshotAsTexture();
            if(capture==null){Debug.LogError("Frame capture failed: "+name+" "+Screen.width+"x"+Screen.height);Application.Quit(2);yield break;}
            var pixels=capture.GetPixels32();bool visible=false;int magenta=0,samples=0;
            for(int i=0;i<pixels.Length;i+=137){samples++;if(pixels[i].r>235&&pixels[i].g<20&&pixels[i].b>235)magenta++;}
            if(magenta>samples/100){Destroy(capture);Debug.LogError("Missing shader in captured frame: "+name);Application.Quit(4);yield break;}
            for(int i=0;i<pixels.Length;i+=137)if(pixels[i].r>35||pixels[i].g>35||pixels[i].b>35){visible=true;break;}
            if(!visible){Destroy(capture);Debug.LogError("Frame is blank: "+name);Application.Quit(3);yield break;}
            File.WriteAllBytes(Path.Combine(directory,name+".png"),capture.EncodeToPNG());Destroy(capture);
            yield return null;
        }
    }
}
