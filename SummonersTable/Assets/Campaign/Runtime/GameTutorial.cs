using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        bool standaloneTutorial,tutorialComplete,dreamNarration,dreamScene;
        string tutorialLesson="";
        TutorialPanel tutorialPanel;
        void BeginDreamScene()
        {
            EnsureBattleWorld();dreamScene=true;
            board.GetComponent<DreamTableBackdrop>()?.Show(true);
            if(local==null){local=GameEngine.Tutorial(ConfigBundle.Clone(catalog),campaignSave.deck);localTime=0;seq=new int[4];seat=0;localBotDirector=null;state=local.View(0,0);}
            page="game";handoff=false;online=false;campaignActive=true;campaignMatch=false;
        }
        void EndDreamScene()
        {
            dreamNarration=false;dreamScene=false;board?.GetComponent<DreamTableBackdrop>()?.Show(false);
        }
        void StartTutorialLesson(string lesson)
        {
            EnsureBattleWorld();
            tutorialPanel=FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
            if(tutorialPanel==null)throw new InvalidOperationException("Place the authored TutorialPanel in the entry scene.");
            comic.Hide();ClearCampaignMatch();BeginDreamScene();dreamNarration=false;tutorialLesson=lesson;tutorialComplete=false;
            local=GameEngine.Tutorial(ConfigBundle.Clone(catalog),campaignSave.deck);localBotDirector=null;localTime=0;seq=new int[4];seat=0;
            foreach(var player in local.State.players){player.hand.Clear();player.units.Clear();}
            var me=local.State.players[0];
            if(lesson=="STEP_BOARD_SLOTS"||lesson=="STEP_QTE_RITUAL")me.hand.Add(new HandCard{uid="lesson-creature",cardId="C02"});
            if(lesson=="STEP_SPELL_CAST")me.hand.Add(new HandCard{uid="lesson-spell",cardId="S01"});
            if(lesson=="STEP_COMBAT_PHASE")me.units.Add(new UnitState{uid="lesson-attacker",cardId="C02",hp=catalog.Card("C02").health,slot=2,plannedSeat=1,targetAssigned=true});
            if(lesson=="STEP_HAND_LIMIT_8")for(int i=0;i<8;i++)me.hand.Add(new HandCard{uid="lesson-hand-"+i,cardId="C02"});
            me.handCount=me.hand.Count;state=local.View(0,0);page="game";seenTurn=-1;seenPhase="";campaignActive=true;campaignMatch=false;
            tutorialPanel.gameObject.SetActive(true);tutorialPanel.proceed.onClick.RemoveAllListeners();tutorialPanel.retry.onClick.RemoveAllListeners();
            tutorialPanel.retry.onClick.AddListener(()=>StartTutorialLesson(lesson));
            tutorialPanel.proceed.onClick.AddListener(()=>
            {
                if(tutorialComplete){tutorialLesson="";tutorialPanel.gameObject.SetActive(false);AdvanceCampaignLine();return;}
                if(lesson=="STEP_HP_BARS")tutorialComplete=true;
                else if(lesson=="STEP_DRAW_CARD"||lesson=="STEP_HAND_LIMIT_8"){local.TutorialDraw();state=local.View(0,localTime);tutorialComplete=true;}
            });
            if(lesson=="STEP_QTE_RITUAL")Send(new GameCommand{kind="play",cardUid="lesson-creature",slot=2});
            UpdateTutorial();
        }
        bool TutorialAllows(GameCommand command)
        {
            if(tutorialLesson==""||command.kind=="look")return true;
            if(tutorialComplete)return false;
            if(command.kind=="key")return tutorialLesson=="STEP_QTE_RITUAL"||tutorialLesson=="STEP_SPELL_CAST";
            if(command.kind=="play")return tutorialLesson=="STEP_BOARD_SLOTS"||tutorialLesson=="STEP_QTE_RITUAL"||tutorialLesson=="STEP_SPELL_CAST";
            if(command.kind=="end")return tutorialLesson=="STEP_COMBAT_PHASE";
            return false;
        }
        void UpdateTutorial()
        {
            if(tutorialLesson==""||tutorialPanel==null||page!="game")return;
            tutorialPanel.gameObject.SetActive(!settingsOpen&&modal==""&&!quitConfirm);
            if(tutorialLesson=="STEP_BOARD_SLOTS"&&state.cast!=null)tutorialComplete=true;
            if(tutorialLesson=="STEP_QTE_RITUAL"&&state.players[0].units.Any())tutorialComplete=true;
            if(tutorialLesson=="STEP_SPELL_CAST"&&state.players[1].hp<30)tutorialComplete=true;
            if(tutorialLesson=="STEP_COMBAT_PHASE"&&state.players[1].hp<30)tutorialComplete=true;
            string text=tutorialLesson switch{
                "STEP_HP_BARS"=>"Ваши жизни. В начале боя — 30. Если они закончатся, вы проиграете.",
                "STEP_DRAW_CARD"=>"Каждый ход вы получаете одну карту. Нажмите «Взять карту» — она появится здесь, в руке.",
                "STEP_BOARD_SLOTS"=>"Нажмите на Медведя-обнимателя. Затем стрелкой выберите свободное место на столе.",
                "STEP_QTE_RITUAL"=>"Нажимайте подсвеченные буквы по порядку — на клавиатуре или здесь. Три ошибки передадут карту Лоскуту.",
                "STEP_SPELL_CAST"=>"Нажмите «Огненный чих», затем выберите Лоскута целью. Успешный ритуал нанесёт ему урон.",
                "STEP_COMBAT_PHASE"=>"Завершите ход этой кнопкой. Медведь атакует сам. Существа сражаются по порядку слева направо.",
                _=>"Здесь восемь карт — это предел руки. Возьмите девятую: она сгорит, а остальные останутся."};
            tutorialPanel.instruction.text=tutorialComplete?(tutorialLesson=="STEP_HAND_LIMIT_8"?"Девятая карта сгорела. В руке осталось восемь. Продолжим сон.":"Получилось! Продолжим сон Шута."):text;
            tutorialPanel.proceed.interactable=tutorialComplete||tutorialLesson=="STEP_HP_BARS"||tutorialLesson=="STEP_DRAW_CARD"||tutorialLesson=="STEP_HAND_LIMIT_8";
            tutorialPanel.proceed.GetComponentInChildren<Text>().text=tutorialComplete?"Продолжить":tutorialLesson=="STEP_HP_BARS"?"Понятно":tutorialLesson=="STEP_HAND_LIMIT_8"?"Взять девятую":"Взять карту";

        }
        void LateUpdate(){
            // HeroActor has already applied its seated pose and head look.
            if(page=="game"&&state!=null&&!handoff&&board!=null&&ui!=null)
                for(int i=0;i<ui.playerStatus.Length&&i<state.players.Count;i++)
                {var status=ui.playerStatus[i];if(status!=null&&status.gameObject.activeInHierarchy)status.Present(state.players[i],state,board,catalog.rules.heroHp);}
            if(tutorialLesson!=""&&tutorialPanel!=null&&tutorialPanel.gameObject.activeInHierarchy){Canvas.ForceUpdateCanvases();UpdateTutorialFocus();}}
        static Rect ScreenArea(RectTransform rect,float padding=14)
        {
            var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?(canvas.worldCamera!=null?canvas.worldCamera:Camera.main):null;
            var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=min;
            foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
            return Rect.MinMaxRect(min.x-padding,min.y-padding,max.x+padding,max.y+padding);
        }
        Rect WorldArea(params Transform[] targets)
        {
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            foreach(var target in targets)
            {
                var renderers=target.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                foreach(var renderer in renderers)
                {var b=renderer.bounds;for(int i=0;i<8;i++){var point=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=board.ViewCamera.WorldToScreenPoint(point);if(p.z>0){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}}}
            }
            if(min.x==float.MaxValue){var p=board.ViewCamera.WorldToScreenPoint(targets[0].position);return new Rect(p.x-16,p.y-16,32,32);}
            return Rect.MinMaxRect(Mathf.Max(0,min.x-16),Mathf.Max(0,min.y-16),Mathf.Min(Screen.width,max.x+16),Mathf.Min(Screen.height,max.y+16));
        }
        void UpdateTutorialFocus()
        {
            if(board?.ViewCamera==null||board.Seat(0)==null||board.Seat(1)==null)return;
            Rect hand=ScreenArea((RectTransform)ui.hand.transform);
            var cards=ui.hand.Slots.Where(x=>x.gameObject.activeInHierarchy).ToArray();
            if(cards.Length>0){hand=ScreenArea((RectTransform)cards[0].transform);foreach(var card in cards.Skip(1)){var r=ScreenArea((RectTransform)card.transform);hand=Rect.MinMaxRect(Mathf.Min(hand.xMin,r.xMin),Mathf.Min(hand.yMin,r.yMin),Mathf.Max(hand.xMax,r.xMax),Mathf.Max(hand.yMax,r.yMax));}}
            if(tutorialLesson=="STEP_HP_BARS")
            {
                var own=ui.hud.GetComponent<LocalHeroHud>();var enemy=board.Seat(1).status;
                tutorialPanel.PointAt(ScreenArea(own.healthFill.rectTransform,18),ScreenArea(enemy.healthNumber.rectTransform,18),"Жизни Лоскута. Урон уменьшает их до нуля.");return;
            }
            if(state.qte!=null&&cardCanvas.qtePanel.gameObject.activeInHierarchy){tutorialPanel.PointAt(ScreenArea(cardCanvas.qtePanel));return;}
            if(state.phase=="reveal"&&cardCanvas.centerSlot.gameObject.activeInHierarchy){tutorialPanel.PointAt(ScreenArea((RectTransform)cardCanvas.centerSlot.transform));return;}
            if(tutorialLesson=="STEP_BOARD_SLOTS"&&(selectedCard!=""||tutorialComplete))
            {tutorialPanel.PointAt(WorldArea(board.Seat(0).slots));if(!tutorialComplete)tutorialPanel.instruction.text="Пять мест для существ. Нажмите на свободный паз, куда хотите поставить Медведя.";return;}
            if(tutorialLesson=="STEP_QTE_RITUAL"&&tutorialComplete){tutorialPanel.PointAt(WorldArea(board.Seat(0).slots[2]));return;}
            if(tutorialLesson=="STEP_SPELL_CAST"&&tutorialComplete){tutorialPanel.PointAt(ScreenArea(board.Seat(1).status.healthNumber.rectTransform,18));return;}
            if(tutorialLesson=="STEP_SPELL_CAST"&&selectedCard!="")
            {tutorialPanel.PointAt(WorldArea(board.Actor(1).transform));tutorialPanel.instruction.text="Лоскут — цель заклинания. Нажмите на него, затем повторите знаки ритуала.";return;}
            if(tutorialLesson=="STEP_COMBAT_PHASE")
            {tutorialPanel.PointAt(ScreenArea((RectTransform)ui.hud.Item("end").transform),WorldArea(board.Seat(0).slots[2]),"Медведь готов атаковать.");return;}
            tutorialPanel.PointAt(hand);
        }
    }
}
