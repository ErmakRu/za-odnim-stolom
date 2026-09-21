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
        bool captureMode,draggingCard,mouseHeld,previewAim;
        string hoverCard="",hoverHandUid="";
        double localReactionStarted;
        Vector2 handPress,aimStart,previewAimEnd;

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
            {seenPhase=state.phase;if(state.phase!="action"){selectedCard="";selectedUnit="";mouseHeld=false;draggingCard=false;}}
        }
        void ReadQteKeys()
        {
            var q=state.qte;
            if(q==null){lastQte="";return;}
            if(lastQte!=q.id){lastQte=q.id;lastProgress=q.index;lastMistakes=q.mistakes;}
            if(q.index>lastProgress)audioSource.PlayOneShot(tickTone);
            if(q.mistakes>lastMistakes)audioSource.PlayOneShot(failTone);
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
            string phase=state.phase=="action"?(MyAction?"ВАШ ХОД":"ХОД: "+turn):state.phase=="reveal"?"КАРТА ОБЪЯВЛЕНА":state.phase=="qte"?"РОЗЫГРЫШ: "+turn:"РАУНД ЗАВЕРШЁН";
            Text(new Rect(385,17,790,34),phase,26,MyAction?gold:Color.white,true,TextAnchor.MiddleCenter);
            string sub=state.phase=="action"?"Решение: "+Math.Max(0,Math.Ceiling(state.deadline-Clock))+" с":state.phase=="reveal"?"Все видят карту ещё "+Math.Max(0,Math.Ceiling(state.cast.revealUntil-Clock))+" с":state.phase=="qte"?(state.qte!=null?"QTE видно только вам":"Чужой QTE скрыт. Можно сыграть подходящую реакцию."):"Счёт сохраняется между раундами";
            Text(new Rect(380,58,810,28),sub,18,muted,false,TextAnchor.MiddleCenter);
            if(Button(new Rect(1310,27,120,44),"Правила",muted))modal="rules";
            if(Button(new Rect(1446,27,126,44),"Выйти",muted))quitConfirm=true;
            WorldLabels();
            Box(new Rect(0,760,W,240),new Color(.025f,.055f,.075f));
            Text(new Rect(24,924,260,33),me.name,23,TableBoard.SeatColors[seat],true);
            Text(new Rect(24,960,272,29),"HP "+me.hp+"  ·  Очки "+me.score+"  ·  Колода "+me.deckCount,17,Color.white);
            if(Button(new Rect(1320,902,250, 60),"Завершить ход",gold,MyAction))Send(new GameCommand{kind="end"});
            Text(new Rect(1318,970,260,23),MyAction?"Заклинаний: "+state.spellsPlayed+" / 3":"Наблюдаем за столом",16,muted,false,TextAnchor.MiddleCenter);
            if(Casting)CastBanner();
            DrawHand();
            if(MyAction&&(selectedCard!=""||selectedUnit!=""))
            {
                string targetHint=selectedUnit!=""?"Переназначьте стрелку существа":"Укажите цель картой";
                Box(new Rect(410,704,780,44),panel);Text(new Rect(425,711,750,32),targetHint+" • центр стола = случайная цель",19,gold,false,TextAnchor.MiddleCenter);
                var start=selectedUnit!=""?board.Project(board.TargetPosition(seat,selectedUnit,state),scale,offset):aimStart;
                DrawAim(start,previewAim?previewAimEnd:Event.current.mousePosition,gold);
                if(Button(new Rect(24,827,240,48),"Отменить выбор",muted))ClearSelection();
            }
            var selected=me.hand.Find(h=>h.uid==selectedCard);
            string show=hoverCard!=""?hoverCard:selected?.cardId??detailId;
            if(show!=""&&catalog.Card(show)!=null&&!Casting)
                CardDetail(catalog.Card(show),new Rect(22,151,272,490));
            if(state.phase=="qte"&&state.qte!=null)PrivateQte();
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
            foreach(var player in state.players)
            {
                Vector2 pos=board.Project(TableBoard.HeroPosition(player.seat,state.players.Count)+Vector3.up*1.25f,scale,offset);
                var rect=new Rect(Mathf.Clamp(pos.x-116,300,1318),Mathf.Clamp(pos.y-36,174,760),232,67);
                if(player.seat==seat&&state.activeSeat!=seat)rect=new Rect(1318,676,250,67);
                Box(rect,panel);Frame(rect,player.alive?TableBoard.SeatColors[player.seat]:muted,2);
                Text(new Rect(rect.x+8,rect.y+7,216,24),player.name+(player.seat==seat?" (вы)":""),18,null,true,TextAnchor.MiddleCenter);
                Text(new Rect(rect.x+8,rect.y+36,216,24),"HP "+player.hp+"  ·  Очки "+player.score+"  ·  Рука "+player.handCount,16,player.alive?TableBoard.SeatColors[player.seat]:muted,false,TextAnchor.MiddleCenter);
                if(GUI.enabled&&Event.current.type==EventType.MouseUp&&Event.current.button==0&&rect.Contains(Event.current.mousePosition))
                {ApplyWorldTarget("hero",player.seat,"",-1);Event.current.Use();}
                foreach(var unit in player.units)
                {
                    var p=board.Project(TableBoard.SlotPosition(player.seat,unit.slot,state.players.Count)+Vector3.up*.1f,scale,offset);
                    var c=catalog.Card(unit.cardId);int atk=c.attack+Math.Min(2,player.units.Where(u=>u.uid!=unit.uid&&catalog.Card(u.cardId).effect=="attackAura").Sum(u=>catalog.Card(u.cardId).value));
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
            CardDetail(c,new Rect(22,214,272,490));
            Box(new Rect(440,108,720,83),panel);
            Text(new Rect(457,120,686, 30),c.name+"  ·  "+TypeName(c),26,TypeColor(c),true,TextAnchor.MiddleCenter);
            string target=c.effect=="areaDamage"?"все противники":c.target=="self"||c.target=="none"?"на себя":cast.randomTarget?"случайная цель в центре":cast.targetSeat<0?"на себя":state.players[cast.targetSeat].name+(cast.targetUnit!=""?" / существо":"");
            Text(new Rect(457,159,686,26),"Цель: "+target+"  •  Реакции открыты",18,muted,false,TextAnchor.MiddleCenter);
            if(cast.owner!=seat)
            {
                Text(new Rect(475,654,650,32),CanAnyReact()?"Подходящие реакции подсвечены в вашей руке":"Наблюдаем за розыгрышем",20,new Color(.80f,.71f,1),true,TextAnchor.MiddleCenter);
            }
            foreach(var reaction in state.tableReactions.Where(r=>r.castId==cast.id))
            {
                var p=board.Project(TableBoard.Away(reaction.owner,state.players.Count)*2.1f+Vector3.up*1.1f,scale,offset);
                Box(new Rect(p.x-83,p.y+28,166,44),panel);Text(new Rect(p.x-77,p.y+33,154,35),catalog.Card(reaction.cardId).name,14,new Color(.8f,.72f,1),true,TextAnchor.MiddleCenter);
            }
        }

        bool CanReact(CardDef card,TargetRef target)
        {
            var a=state.pending;
            if(!Casting||a==null||card.kind!="reaction"||a.source==seat||!state.players[seat].alive||a.responded.Contains(seat))return false;
            bool damage=new[]{"opening","damage","areaDamage"}.Contains(a.kind);
            switch(card.effect)
            {
                case "reduce":case "reflect":return damage&&target.seat==seat;
                case "rescue":return damage;
                case "deny":return target.seat==seat&&catalog.Card(a.cardId).kind=="spell"&&(damage||new[]{"stun","swap","bounce","heal"}.Contains(a.kind));
                default:return false;
            }
        }
        TargetRef ReactionTarget(CardDef c)
        {return state.pending?.targets.Where(t=>CanReact(c,t)).OrderBy(t=>t.seat==seat?0:1).ThenBy(t=>t.seat).FirstOrDefault();}
        bool CanAnyReact(){return state.players[seat].hand.Any(h=>ReactionTarget(catalog.Card(h.cardId))!=null);}
        void ChooseHand(HandCard hand,Vector2 origin)
        {
            var card=catalog.Card(hand.cardId);detailId=card.id;error="";
            if(card.kind=="reaction")
            {
                var target=ReactionTarget(card);
                if(target!=null){Send(new GameCommand{kind="react",phaseId=state.cast.id,cardUid=hand.uid,targetSeat=target.seat,targetUnit=target.unit});ClearSelection();}
                else error="Эта реакция сейчас не подходит. Она играется только на чужой розыгрыш.";
                return;
            }
            if(!MyAction)return;
            selectedCard=hand.uid;selectedUnit="";aimStart=origin;
            if(card.kind=="creature")
            {
                var me=state.players[seat];var free=Enumerable.Range(0,5).Where(slot=>!me.units.Any(u=>u.slot==slot)).ToList();
                if(free.Count==0){error="На столе нет свободных слотов.";selectedCard="";return;}
                if(!free.Contains(selectedSlot))selectedSlot=free[0];
            }
        }
        Rect HandRect(int i,int count,bool lifted)
        {
            float midpoint=(count-1)/2f;float spread=Math.Min(132,830f/Math.Max(1,count-1));float x=800+(i-midpoint)*spread;
            float y=754+Mathf.Pow(Mathf.Abs(i-midpoint)/Math.Max(1,midpoint),2)*18;
            return lifted?new Rect(x-96,y-90,192,276):new Rect(x-75,y,150,210);
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
            hoverCard=hot>=0?hand[hot].cardId:"";hoverHandUid=hot>=0?hand[hot].uid:"";
            bool blocked=(state.phase=="roundEnd"||state.phase=="matchEnd"||state.qte!=null||!GUI.enabled);
            // Draw the selected/hovered card last so it rises above the fan.
            int raised=hot>=0?hot:hand.FindIndex(h=>h.uid==selectedCard);
            var order=Enumerable.Range(0,hand.Count).Where(i=>i!=raised).Concat(raised<0?Enumerable.Empty<int>():new[]{raised}).ToList();
            foreach(int i in order)
            {
                var h=hand[i];var c=catalog.Card(h.cardId);bool lift=i==raised&&!blocked;
                var rect=HandRect(i,hand.Count,lift);var matrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(lift?0:(i-(hand.Count-1)/2f)*3.2f,rect.center);
                if(c.kind=="reaction"&&ReactionTarget(c)!=null)Frame(new Rect(rect.x-5,rect.y-5,rect.width+10,rect.height+10),new Color(.85f,.67f,1),4);
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
        void WorldInput()
        {
            var e=Event.current;var pointer=e.mousePosition;
            if(e.type==EventType.MouseDown&&e.button==1){ClearSelection();mouseHeld=false;draggingCard=false;e.Use();return;}
            if(pointer.y<105||pointer.y>751||new Rect(22,151,272,490).Contains(pointer)&&(detailId!=""||hoverCard!=""))return;
            var target=board.Pick(Input.mousePosition);
            if(target==null)return;
            if(target.kind=="unit"&&selectedCard==""&&selectedUnit=="")
            {
                var unit=state.players[target.seat].units.Find(u=>u.uid==target.uid);if(unit!=null)detailId=unit.cardId;
            }
            if(e.type==EventType.MouseUp&&e.button==0)
            {ApplyWorldTarget(target.kind,target.seat,target.uid,target.slot);e.Use();}
        }
        void ApplyWorldTarget(string kind,int owner,string uid,int slot)
        {
            if(!MyAction)return;
            if(kind=="slot"&&owner==seat&&selectedCard!=""&&catalog.Card(state.players[seat].hand.Find(h=>h.uid==selectedCard).cardId).kind=="creature")
            {selectedSlot=slot;error="";return;}
            if(selectedCard!="")
            {
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
            if(kind=="unit"&&owner==seat){selectedUnit=uid;detailId=state.players[seat].units.Find(u=>u.uid==uid)?.cardId??"";error="";}
        }
        void DrawAim(Vector2 start,Vector2 end,Color color)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Vector2 previous=start,control=(start+end)/2+Vector2.up*80;
            for(int i=1;i<=28;i++)
            {float t=i/28f;var next=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;ScreenLine(previous,next,color,5);previous=next;}
            var dir=(end-control).normalized;var side=new Vector2(-dir.y,dir.x);
            ScreenLine(end-dir*25+side*13,end,color,6);ScreenLine(end-dir*25-side*13,end,color,6);
        }
        void ScreenLine(Vector2 from,Vector2 to,Color color,float width)
        {
            var old=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
            Box(new Rect(from.x,from.y-width/2,Vector2.Distance(from,to),width),color);GUI.matrix=old;
        }
        void PrivateQte()
        {
            var q=state.qte;if(q==null||q.owner!=seat)return;
            var rect=new Rect(342,562,916,315);Box(rect,panel);Frame(rect,teal,2);
            Text(new Rect(362,578,876,32),"ВАШ QTE  ·  остальные видят только карту",23,teal,true,TextAnchor.MiddleCenter);
            float step=Math.Min(63,830f/q.sequence.Length),left=800-q.sequence.Length*step/2;
            for(int i=0;i<q.sequence.Length;i++)
            {var r=new Rect(left+i*step,630,step-9,62);Box(r,i<q.index?teal:i==q.index?gold:muted);Text(r,q.sequence[i].ToString(),34,ink,true,TextAnchor.MiddleCenter);}
            double seconds=Math.Max(0,q.deadline-Clock);
            Text(new Rect(363,714,875, 30),seconds.ToString("0.0")+" с  ·  Ошибки "+q.mistakes+" / 3  ·  При срыве → "+state.players[q.recipient].name,21,seconds<4?red:Color.white,false,TextAnchor.MiddleCenter);
            for(int i=0;i<7;i++)if(Button(new Rect(484+i*91,772,76,54),"ASDFGHJ"[i].ToString(),gold))Send(new GameCommand{kind="key",phaseId=q.id,key="ASDFGHJ"[i].ToString()});
        }

        // Offline deterministic render fixtures. No Steam traffic, external UI control or AI player.
        IEnumerator CapturePreview()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");
            string directory=index>=0&&index+1<args.Length?args[index+1]:Path.GetFullPath("captures");Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(.4f);
            yield return Shot(directory,"01-menu");
            StartLocal(4);handoff=false;localTime=0;
            foreach(var p in local.State.players)
            {
                p.hand.Clear();p.units.Clear();
                for(int i=0;i<3;i++)p.units.Add(new UnitState{uid="preview-unit-"+p.seat+"-"+i,cardId="C"+(1+p.seat*3+i).ToString("00"),hp=4,slot=i,plannedSeat=i==2?-1:(p.seat+1)%4});
            }
            foreach(string id in new[]{"C02","S01","S02","S06","C03","R01"})local.State.players[0].hand.Add(new HandCard{uid="preview-"+id,cardId=id});
            local.State.players[1].hand.Add(new HandCard{uid="preview-reaction",cardId="R01"});
            local.State.players[1].hand.Add(new HandCard{uid="preview-other",cardId="C07"});
            seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,true);
            yield return Shot(directory,"02-own-turn-table");
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
            if(selectedSlot!=3)throw new Exception("First free slot selection failed");
            previewAim=true;
            previewAimEnd=board.Project(TableBoard.HeroPosition(1,4),Mathf.Min(Screen.width/W,Screen.height/H),new Vector2((Screen.width-W*Mathf.Min(Screen.width/W,Screen.height/H))/2,0));
            yield return Shot(directory,"03-target-arrow");previewAim=false;ClearSelection();
            seat=1;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,false);
            yield return Shot(directory,"04-observer-camera");
            seat=0;state=local.View(seat,localTime);
            ChooseHand(state.players[0].hand.Find(h=>h.uid=="preview-C02"),new Vector2(600,790));
            ApplyWorldTarget("hero",1,"",-1);
            if(local.State.phase!="reveal")throw new Exception("Target selection did not announce card");
            seat=1;
            state=local.View(seat,localTime);yield return Shot(directory,"05-three-second-reveal");
            local.Submit(1,new GameCommand{seq=1,kind="react",cardUid="preview-reaction",phaseId=local.State.cast.id,targetSeat=1},localTime+.5);
            localTime=3;local.Tick(localTime);state=local.View(seat,localTime);
            if(state.qte!=null||state.deadline!=0)throw new Exception("Observer QTE leaked in preview");
            yield return Shot(directory,"06-observer-private-qte-hidden");
            seat=0;state=local.View(seat,localTime);board.Sync(state,seat,Clock);board.SnapCamera(seat,true);
            if(state.qte==null)throw new Exception("Caster QTE missing in preview");
            yield return Shot(directory,"07-caster-private-qte");
            if(Directory.GetFiles(directory,"*.png").Length<7)throw new Exception("Runtime screenshots are missing");
            File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS 7 nonblank runtime screenshots. Observer QTE absent; caster QTE present. Four hero and 12 unit raycasts. UI picks free slot and announces target before QTE. 4 seats, 5 creature slots each. Gray cube placeholders only.\n");
            Debug.Log("PREVIEW_CAPTURE_COMPLETE "+directory);Application.Quit();
        }
        IEnumerator Shot(string directory,string name)
        {
            yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();
            var capture=ScreenCapture.CaptureScreenshotAsTexture();
            if(capture==null){Debug.LogError("Frame capture failed: "+name+" "+Screen.width+"x"+Screen.height);Application.Quit(2);yield break;}
            var pixels=capture.GetPixels32();bool visible=false;
            for(int i=0;i<pixels.Length;i+=137)if(pixels[i].r>35||pixels[i].g>35||pixels[i].b>35){visible=true;break;}
            if(!visible){Destroy(capture);Debug.LogError("Frame is blank: "+name);Application.Quit(3);yield break;}
            File.WriteAllBytes(Path.Combine(directory,name+".png"),capture.EncodeToPNG());Destroy(capture);
            yield return null;
        }
    }
}
