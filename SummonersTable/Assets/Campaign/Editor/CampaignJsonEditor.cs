using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    public sealed class CampaignJsonEditor
    {
        public const string PathName="Assets/Campaign/Resources/Campaign/campaign.json";
        CampaignBook book;int part,frame,line,character;bool after,raw,showCharacters;string original,rawText,status="";Vector2 scroll;
        public void Draw()
        {
            if(book==null)Read();if(book==null){EditorGUILayout.HelpBox(status,MessageType.Error);return;}
            EditorGUILayout.LabelField("Комиксы кампании · единый JSON",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Кадр задаёт фон и персонажей. Внутри кадра переключаются реплики. Сохранение проверяет картинки, колоды и все части кампании.",MessageType.Info);
            var parts=new[]{"Пролог и обучение"}.Concat(book.chapters.Select((c,i)=>(i+1)+". "+c.title)).Concat(new[]{"Эпилог"}).ToArray();
            int selected=EditorGUILayout.Popup("Часть",part,parts);if(selected!=part){part=selected;frame=line=0;}
            EditorGUI.BeginChangeCheck();
            book.charactersPerSecond=EditorGUILayout.Slider("Символов в секунду",book.charactersPerSecond,1,150);
            book.textPageLength=EditorGUILayout.IntSlider("Символов во фрагменте",book.textPageLength,20,100);
            showCharacters=EditorGUILayout.Foldout(showCharacters,"Персонажи и цвета диалога");
            if(showCharacters&&book.characters.Length>0)
            {
                character=Mathf.Clamp(character,0,book.characters.Length-1);character=EditorGUILayout.Popup("Персонаж",character,book.characters.Select(c=>c.name).ToArray());var c=book.characters[character];
                c.name=EditorGUILayout.TextField("Имя на плашке",c.name);
                string[] kinds={"main","npc","important"};c.type=kinds[EditorGUILayout.Popup("Тип персонажа",Math.Max(0,Array.IndexOf(kinds,c.type)),new[]{"Главный герой · слева","Второстепенный · справа","Важный · справа"})];
                c.art=Texture("Арт персонажа",c.art);c.scale=EditorGUILayout.Slider("Масштаб бюста",c.scale,.2f,3);c.offsetX=EditorGUILayout.Slider("Смещение X",c.offsetX,-800,800);c.offsetY=EditorGUILayout.Slider("Смещение Y",c.offsetY,-600,600);
                foreach(var palette in book.dialogueStyles){EditorGUILayout.LabelField(palette.type,EditorStyles.boldLabel);palette.body=ColorField("Подложка текста",palette.body);palette.border=ColorField("Рамка",palette.border);palette.name=ColorField("Подложка имени",palette.name);}
            }
            if(part>0&&part<=book.chapters.Length)
            {
                var chapter=book.chapters[part-1];chapter.title=EditorGUILayout.TextField("Название главы",chapter.title);chapter.opponentName=EditorGUILayout.TextField("Имя противника",chapter.opponentName);
                var catalog=ConfigBundle.Read(ConfigAuthoring.Folder).Catalog();int d=catalog.decks.FindIndex(x=>x.id==chapter.opponentDeck);
                chapter.opponentDeck=catalog.decks[EditorGUILayout.Popup("Колода противника",Math.Max(0,d),catalog.decks.Select(x=>x.name).ToArray())].id;
                chapter.mode=EditorGUILayout.Popup("Режим",chapter.mode=="commanders"?1:0,new[]{"Волшебники","Полководцы"})==1?"commanders":"wizards";
                after=EditorGUILayout.Toggle("После победы",after);
            }
            var frames=Frames();frame=Mathf.Clamp(frame,0,Math.Max(0,frames.Length-1));
            if(frames.Length>0)
            {
                int next=EditorGUILayout.Popup("Кадр",frame,frames.Select(f=>f.id).ToArray());if(next!=frame){frame=next;line=0;}
                var f=frames[frame];scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.MaxHeight(630));
                f.id=EditorGUILayout.TextField("ID кадра",f.id);f.title=EditorGUILayout.TextField("Заголовок",f.title);f.background=Texture("Фон",f.background);
                EditorGUILayout.HelpBox("Показывается только говорящий из справочника персонажей. Его рисунок размещается за подложкой текста.",MessageType.Info);
                line=Mathf.Clamp(line,0,f.lines.Length-1);line=EditorGUILayout.Popup("Реплика",line,f.lines.Select((l,i)=>(i+1)+" · "+l.speaker).ToArray());var l=f.lines[line];
                string[] lineTypes={"dialogue","thought","narration"};l.kind=lineTypes[EditorGUILayout.Popup("Тип реплики",Math.Max(0,Array.IndexOf(lineTypes,l.kind)),new[]{"Речь","Мысли · курсив","Рассказчик · без имени"})];
                var characters=new[]{"Рассказчик"}.Concat(book.characters.Select(c=>c.name)).ToArray();int speakerIndex=Array.FindIndex(book.characters,c=>c.id==l.characterId)+1;int chosen=EditorGUILayout.Popup("Говорящий",speakerIndex,characters);
                if(chosen!=speakerIndex){l.characterId=chosen==0?"":book.characters[chosen-1].id;l.speaker=chosen==0?"РАССКАЗЧИК":book.characters[chosen-1].name;l.kind=chosen==0?"narration":"dialogue";}
                EditorGUILayout.LabelField("Текст");l.text=EditorGUILayout.TextArea(l.text,GUILayout.MinHeight(105));
                EditorGUILayout.LabelField("Фрагментов: "+ComicText.Split(l.text,book.textPageLength).Length);
                l.rewardIcon=Texture("Полученный предмет",l.rewardIcon);l.rewardLabel=EditorGUILayout.TextField("Название предмета",l.rewardLabel);
                l.artOverride=Texture("Арт / эмоция говорящего",l.artOverride);
                l.leftArt=Texture("Эмоция Шута",l.leftArt);l.sfx=EditorGUILayout.TextField("SFX / action:id",l.sfx);l.ambience=EditorGUILayout.TextField("Эмбиент / Mute",l.ambience);
                if(GUILayout.Button("Добавить реплику")){f.lines=f.lines.Concat(new[]{new ComicLine{speaker="ШУТ",text="Новая реплика"}}).ToArray();line=f.lines.Length-1;}
                EditorGUILayout.EndScrollView();
            }
            if(EditorGUI.EndChangeCheck())rawText=JsonUtility.ToJson(book,true);
            if(GUILayout.Button("Добавить кадр"))
            {SetFrames(frames.Concat(new[]{new ComicFrame{id="frame-"+Guid.NewGuid().ToString("N").Substring(0,8),left=new ComicActor{art="Art/Jester/jester_basic",label="Йорик"},lines=new[]{new ComicLine{speaker="РАССКАЗЧИК",text="Новый кадр"}}}}).ToArray());frame=frames.Length;line=0;rawText=JsonUtility.ToJson(book,true);}
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Сохранить"))try
                {
                    if(File.ReadAllText(PathName)!=original)throw new IOException("JSON изменился на диске. Перечитайте его перед сохранением.");
                    book.Validate(ConfigBundle.Read(ConfigAuthoring.Folder).Catalog());var json=JsonUtility.ToJson(book,true)+"\n";
                    string temp=PathName+".tmp";File.WriteAllText(temp,json);File.Replace(temp,PathName,null);original=rawText=json;AssetDatabase.Refresh();status="Сохранено. Новые данные применятся при открытии кампании.";
                }catch(Exception e){status=e.Message;}
                if(GUILayout.Button("Перечитать / отменить"))Read();
            }
            raw=EditorGUILayout.Foldout(raw,"Исходный JSON (все параметры)");
            if(raw){rawText=EditorGUILayout.TextArea(rawText,GUILayout.Height(200));if(GUILayout.Button("Применить JSON к полям"))try{var value=JsonUtility.FromJson<CampaignBook>(rawText);value.Validate(ConfigBundle.Read(ConfigAuthoring.Folder).Catalog());book=value;part=frame=line=0;status="Черновик применён. Нажмите «Сохранить».";}catch(Exception e){status=e.Message;}}
            if(GUILayout.Button("Открыть кампанию · затем Play"))CampaignAuthoring.Open();
            if(status!="")EditorGUILayout.HelpBox(status,MessageType.Info);
        }
        ComicFrame[] Frames()=>part==0?book.introduction:part>book.chapters.Length?book.ending:after?book.chapters[part-1].after:book.chapters[part-1].before;
        void SetFrames(ComicFrame[] frames){if(part==0)book.introduction=frames;else if(part>book.chapters.Length)book.ending=frames;else if(after)book.chapters[part-1].after=frames;else book.chapters[part-1].before=frames;}
        void Read(){try{original=rawText=File.ReadAllText(PathName);book=JsonUtility.FromJson<CampaignBook>(original);status="";}catch(Exception e){status=e.Message;}}
        string ColorField(string label,string hex){ColorUtility.TryParseHtmlString(hex,out var color);return "#"+ColorUtility.ToHtmlStringRGBA(EditorGUILayout.ColorField(label,color));}
        string Texture(string label,string path)
        {
            var old=Resources.Load<Texture2D>(path);var chosen=(Texture2D)EditorGUILayout.ObjectField(label,old,typeof(Texture2D),false);
            if(chosen==old)return path;if(chosen==null)return "";string asset=AssetDatabase.GetAssetPath(chosen);int at=asset.LastIndexOf("/Resources/",StringComparison.Ordinal);
            if(at<0){status="Добавьте PNG в любую папку Resources проекта.";return path;}return Path.ChangeExtension(asset.Substring(at+11),null);
        }
        void Actor(string title,ComicActor a)
        {EditorGUILayout.Space();EditorGUILayout.LabelField(title,EditorStyles.boldLabel);a.art=Texture("Картинка",a.art);a.label=EditorGUILayout.TextField("Подпись",a.label);a.portrait=EditorGUILayout.Toggle("Портрет в рамке",a.portrait);a.scale=EditorGUILayout.Slider("Масштаб",a.scale,.2f,3);a.offsetX=EditorGUILayout.Slider("Смещение X",a.offsetX,-800,800);a.offsetY=EditorGUILayout.Slider("Смещение Y",a.offsetY,-600,600);}
    }
}
