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
        CampaignBook book;int part,frame,line;bool after,raw;string original,rawText,status="";Vector2 scroll;
        public void Draw()
        {
            if(book==null)Read();if(book==null){EditorGUILayout.HelpBox(status,MessageType.Error);return;}
            EditorGUILayout.LabelField("Комиксы кампании · единый JSON",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Кадр задаёт фон и персонажей. Внутри кадра переключаются реплики. Сохранение проверяет картинки, колоды и все части кампании.",MessageType.Info);
            var parts=new[]{"Пролог и обучение"}.Concat(book.chapters.Select((c,i)=>(i+1)+". "+c.title)).Concat(new[]{"Эпилог"}).ToArray();
            int selected=EditorGUILayout.Popup("Часть",part,parts);if(selected!=part){part=selected;frame=line=0;}
            EditorGUI.BeginChangeCheck();
            book.charactersPerSecond=EditorGUILayout.Slider("Символов в секунду",book.charactersPerSecond,0,150);
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
                Actor("Главный герой слева",f.left);Actor("Персонаж справа",f.right);
                line=Mathf.Clamp(line,0,f.lines.Length-1);line=EditorGUILayout.Popup("Реплика",line,f.lines.Select((l,i)=>(i+1)+" · "+l.speaker).ToArray());var l=f.lines[line];
                l.speaker=EditorGUILayout.TextField("Говорящий",l.speaker);EditorGUILayout.LabelField("Текст");l.text=EditorGUILayout.TextArea(l.text,GUILayout.MinHeight(105));
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
