using System;
using System.Collections.Generic;
using UnityEngine;

namespace SummonersTable.Story
{
    [Serializable]
    public class StoryConfig
    {
        public int schemaVersion = 1;
        public string title = "За одним столом: Пир Хохота";
        public List<StoryScene> scenes = new List<StoryScene>();
    }

    [Serializable]
    public class StoryScene
    {
        public string sceneId;         // e.g. "scene_00_prologue", "scene_01_tutorial", "scene_02_tavern"
        public string title;           // e.g. "Сцена 0. Пролог: Смертельный номер"
        public string defaultBackground;
        public List<StoryStep> steps = new List<StoryStep>();
    }

    [Serializable]
    public class StoryStep
    {
        public string id;              // e.g. "0.1", "1.5"
        public string background;      // e.g. "bg_throne_hall", "bg_barn_sepia", "bg_tavern_day"
        public string speaker;         // e.g. "РАССКАЗЧИК", "МАЛХОРАТ", "ШУТ"
        public string text;            // Текст реплики
        public List<StorySpriteSlot> sprites = new List<StorySpriteSlot>();
        public StoryAudio audio = new StoryAudio();
        public StoryAction action = new StoryAction();
    }

    [Serializable]
    public class StorySpriteSlot
    {
        public string position;        // "L" (Left), "C" (Center), "R" (Right)
        public string character;       // "Йорик", "Малхорат", "Бальтазар", "ЖабаДушэс", "Собачка"
        public string emotion;         // "пафос", "паника", "раздут", "скепсис", "с_молотком"
    }

    [Serializable]
    public class StoryAudio
    {
        public string bgm;             // e.g. "Ambience_Dungeon.wav", "Ambience_Pasture.wav"
        public string sfx;             // e.g. "Pop_Big_01.wav", "Bash_Metal_01.wav"
    }

    [Serializable]
    public class StoryAction
    {
        public string actionType;      // "FADE_IN", "FADE_OUT", "CAMERA_SHAKE", "SPRITE_EFFECT", "UI_TUTORIAL_TRIGGER", "UI_SHOW_DECK_SELECT", "START_BATTLE", "WALL_OF_SHAME_WRITE"
        public string target;          // Цель (имя персонажа, слот, id битвы)
        public string parameter;       // Доп. параметр ("1.0", "SCALE_UP", "0-3", "Жаба Задушенная")
    }
}
