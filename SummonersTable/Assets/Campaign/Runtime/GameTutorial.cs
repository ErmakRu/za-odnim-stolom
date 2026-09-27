using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        bool standaloneTutorial,tutorialComplete;
        string tutorialLesson="";
        TutorialPanel tutorialPanel;
        void StartTutorialLesson(string lesson)
        {
            tutorialPanel=FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
            if(tutorialPanel==null)throw new InvalidOperationException("Place the authored TutorialPanel in the entry scene.");
            comic.Hide();ClearCampaignMatch();tutorialLesson=lesson;tutorialComplete=false;
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
            if(tutorialLesson=="STEP_BOARD_SLOTS"&&state.cast!=null)tutorialComplete=true;
            if(tutorialLesson=="STEP_QTE_RITUAL"&&state.players[0].units.Any())tutorialComplete=true;
            if(tutorialLesson=="STEP_SPELL_CAST"&&state.players[1].hp<30)tutorialComplete=true;
            if(tutorialLesson=="STEP_COMBAT_PHASE"&&state.players[1].hp<30)tutorialComplete=true;
            string text=tutorialLesson switch{
                "STEP_HP_BARS"=>"У вас с Лоскутом по 30 жизней. Ваша плашка — внизу слева, жизни соперника — над ним. Найдите обе плашки.",
                "STEP_DRAW_CARD"=>"В начале хода приходит одна карта. Нажмите «Взять карту» и посмотрите, как она появится в руке.",
                "STEP_BOARD_SLOTS"=>"Выберите Медведя-обнимателя в руке. Стрелкой укажите один из пяти подсвеченных свободных пазов и нажмите на него.",
                "STEP_QTE_RITUAL"=>"Повторите буквы ритуала клавишами или кнопками на экране. После третьей ошибки карта уйдёт Лоскуту. Не получилось — нажмите «Повторить».",
                "STEP_SPELL_CAST"=>"Выберите «Огненный чих», наведите стрелку на Лоскута и подтвердите цель. Затем выполните ритуал: жизни соперника уменьшатся.",
                "STEP_COMBAT_PHASE"=>"Медведь готов к бою. Нажмите «Завершить ход»: он сам атакует Лоскута. Существа атакуют по порядку слева направо.",
                _=>"В руке уже 8 карт. Нажмите «Взять девятую»: новая карта сгорит, а восемь останутся. Событие появится в журнале."};
            tutorialPanel.instruction.text=tutorialComplete?(tutorialLesson=="STEP_HAND_LIMIT_8"?"Девятая карта сгорела. В руке осталось восемь. Продолжим сон.":"Получилось! Продолжим сон Шута."):text;
            tutorialPanel.proceed.interactable=tutorialComplete||tutorialLesson=="STEP_HP_BARS"||tutorialLesson=="STEP_DRAW_CARD"||tutorialLesson=="STEP_HAND_LIMIT_8";
            tutorialPanel.proceed.GetComponentInChildren<Text>().text=tutorialComplete?"Продолжить":tutorialLesson=="STEP_HP_BARS"?"Нашёл":tutorialLesson=="STEP_HAND_LIMIT_8"?"Взять девятую":"Взять карту";
            tutorialPanel.highlight.gameObject.SetActive(tutorialLesson=="STEP_HP_BARS"&&!tutorialComplete);
        }
    }
}
