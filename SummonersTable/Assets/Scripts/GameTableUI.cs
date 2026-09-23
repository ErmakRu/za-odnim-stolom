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
                if(state?.matchId!=steam.View.matchId)postMatchLobby=false;
                if(!online||page!="game"&&!postMatchLobby){online=true;local=null;page="game";handoff=false;ClearSelection();seenTurn=-1;}
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
                if(!handoff&&modal==""&&!quitConfirm&&!settingsOpen)
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
            if(seenTurn!=state.turnNumber){ClearSelection();seenTurn=state.turnNumber;if(!captureMode)ConfigAudio.Play("turn.start");}
            if(seenPhase!=state.phase)
            {seenPhase=state.phase;if(state.phase!="action")ClearSelection();}
        }
        void ReadQteKeys()
        {
            var q=state.qte;
            if(q==null){lastQte="";return;}
            if(lastQte!=q.id){lastQte=q.id;lastProgress=q.index;lastMistakes=q.mistakes;}
            if(q.index>lastProgress)ConfigAudio.Play("qte.correct");
            if(q.mistakes>lastMistakes)ConfigAudio.Play("qte.error");
            lastProgress=q.index;lastMistakes=q.mistakes;
            if(q.owner!=seat||modal!=""||quitConfirm)return;
            foreach(char key in "ASDFGHJ")
                if(Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode),key.ToString())))
                {Send(new GameCommand{kind="key",key=key.ToString(),phaseId=q.id});break;}
        }
        bool MyAction {get{return state!=null&&state.phase=="action"&&state.activeSeat==seat&&state.players[seat].alive;}}
        bool Casting {get{return state?.cast!=null&&(state.phase=="reveal"||state.phase=="qte");}}
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
        string InspectionAt(Vector2 screenPoint){return ui!=null?PrefabInspection(screenPoint):"";}
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
        void InvalidCard(string uid,string message)
        {error=message;shakeHand=uid;shakeUntil=Time.unscaledTime+.35f;audioSource.pitch=1;ConfigAudio.Play("action.invalid");}

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
            if(lab.controls.Get<UnityEngine.UI.Text>("heightLabel").text==""||lab.controls.Get<UnityEngine.UI.Text>("spellLabel").text=="")throw new Exception("Laboratory labels not bound");
            lab.controls.Get<UnityEngine.UI.Button>("nextSpell").onClick.Invoke();yield return null;
            if(lab.controls.Get<UnityEngine.UI.Text>("spellLabel").text!=catalog.Card("S02").name)throw new Exception("Laboratory spell selector not bound");
            lab.controls.Get<UnityEngine.UI.Button>("spell").onClick.Invoke();yield return Shot(directory,"lab-spell-preview");
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
            previewPointer=RectTransformUtility.WorldToScreenPoint(null,ui.hand.Slot("preview-C02").transform.position);
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
            yield return Shot(directory,"08c-spell-preview-cleared");CheckSummonPreviewCleared();
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
            yield return CaptureEditableFeatures(directory);
            int captured=editorSmokeNoCapture?editorSmokeFrames:Directory.GetFiles(directory,"*.png").Length;
            if(captured<36)throw new Exception("Runtime screenshots are missing");
            File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS "+captured+(editorSmokeNoCapture?" Editor Play checkpoints (offscreen capture when requested). ":" nonblank runtime screenshots without missing shaders or duplicate worlds/Canvas. ")+"Tavern lobby, all eight imported hero rigs, Weapon disabled, deck/hero/armor/palette callbacks, appearance preserved in match. Three manual cameras, 2/3/4 seating and exact public 3D hand counts. Revealed card goes left for caster and observer; right slot follows hand/world/reaction/history hover and clears on leave. One/three UI reaction overlays tilt about 20 degrees and follow the cast. Summon previews clear on success and failure for caster and observer. Private QTE and public progress. Persistent hero names and numeric HP bars. Editable UI prefabs, master/effects controls, eight spell VFX, unanimous rematch, lobby return and menu exit. History source and target inspection, input coverage, damage and retaliation ownership. Journal suppresses QTE click-through. Four hero and 12 unit raycasts. Summon lands on table without automatic selection. Short/long segmented arrows without aim VFX; explicit drag to hero/center, click and invalid/empty drop preserve old target. Spell target before QTE; deferred combat; camera unchanged by turn.\n");
            Debug.Log("PREVIEW_CAPTURE_COMPLETE "+directory);
#if UNITY_EDITOR
            if(editorSmokeNoCapture){EditorRegressionSmokeDone=true;yield break;}
#endif
            Application.Quit();
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
        bool editorSmokeNoCapture;int editorSmokeFrames;
        Vector2 CaptureHandPoint(string uid)
        {
            var rect=(RectTransform)ui.hand.Slot(uid).transform;
            foreach(float x in new[]{-.35f,-.2f,0,.2f,.35f})foreach(float y in new[]{0f,.2f,-.2f,.35f,-.35f})
            {
                var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(x*rect.rect.width,y*rect.rect.height)));
                if(ui.hand.Hit(point)==uid)return point;
            }
            throw new Exception("Hand card has no reachable inspection point: "+uid);
        }
        IEnumerator Shot(string directory,string name)
        {
            if(editorSmokeNoCapture){yield return new WaitForSeconds(.4f);yield return null;yield return null;editorSmokeFrames++;
#if UNITY_EDITOR
                EditorCapture?.Invoke(Path.Combine(directory,name+".png"));
#endif
                yield break;}
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
