using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SummonersTable.Editor
{
    // Shared by the JSON Inspector and the scene Manager: changes stay a draft until Save.
    public sealed class LayeredCardsJsonEditor
    {
        public const string FileName="layered-cards.json";
        string path,original,status="";LayeredCardsConfig data;int selected;bool raw,sources;string rawText;Vector2 scroll;
        public void Draw(string filePath)
        {
            if(path!=filePath||data==null){path=filePath;Read();}
            if(data==null){EditorGUILayout.HelpBox(status,MessageType.Error);return;}
            EditorGUILayout.LabelField("Фон и единый план",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Персонаж и передний план соединены: их взаимное положение не меняется при наклоне. Основное движение — у фона. Текст и характеристики по-прежнему берутся из cards.json.",MessageType.Info);
            selected=Mathf.Clamp(selected,0,Math.Max(0,data.cards.Length-1));
            if(data.cards.Length>0)
            {
                selected=EditorGUILayout.Popup("Карточка",selected,data.cards.Select(c=>c.cardName).ToArray());
                var art=data.cards[selected];EditorGUI.BeginChangeCheck();
                art.cardName=EditorGUILayout.TextField("Название из cards.json",art.cardName);
                scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.MaxHeight(580));
                art.responsePower=EditorGUILayout.Slider("Плавность у центра",art.responsePower,1,3);
                Layer("Фон",art.background);art.background.depth=EditorGUILayout.Slider("Движение фона",art.background.depth,0,.2f);art.background.foil=0;
                EditorGUILayout.Space();EditorGUILayout.LabelField("Единый план · персонаж и огонь",EditorStyles.boldLabel);
                art.subject.offsetX=EditorGUILayout.Slider("По горизонтали",art.subject.offsetX,-1,1);
                art.subject.offsetY=EditorGUILayout.Slider("По вертикали",art.subject.offsetY,-1,1);
                art.subject.zoom=EditorGUILayout.Slider("Масштаб плана",art.subject.zoom,.25f,3);
                art.subject.depth=EditorGUILayout.Slider("Движение плана",art.subject.depth,0,.03f);
                art.subject.foil=EditorGUILayout.Slider("Перелив плана",art.subject.foil,0,1);
                sources=EditorGUILayout.Foldout(sources,"Исходные изображения единого плана");
                if(sources){Layer("Персонаж",art.subject.rear);Layer("Огонь / передний объект",art.subject.foreground);}
                EditorGUILayout.EndScrollView();
                if(EditorGUI.EndChangeCheck()){rawText=JsonUtility.ToJson(data,true);Preview();}
            }
            if(GUILayout.Button("Добавить оформление карты")){data.cards=data.cards.Concat(new[]{new LayeredCardArt()}).ToArray();selected=data.cards.Length-1;rawText=JsonUtility.ToJson(data,true);}
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Сохранить"))try
                {
                    if(File.ReadAllText(path)!=original)throw new IOException("JSON изменился на диске. Перечитайте его перед сохранением.");
                    string json=JsonUtility.ToJson(data,true)+"\n";ManagerStorage.SaveSection(FileName,json);original=rawText=json;
                    File.WriteAllText("Assets/Resources/ConfigDefaults/"+FileName,json);AssetDatabase.Refresh();LayeredCardData.Reload();
                    if(Application.isPlaying)ConfigRuntime.Reload();Preview();status="Сохранено в JSON, main.json и резервный набор.";
                }catch(Exception e){status=e.Message;}
                if(GUILayout.Button("Перечитать / отменить")){Read();Preview();}
            }
            raw=EditorGUILayout.Foldout(raw,"Исходный JSON");
            if(raw){rawText=EditorGUILayout.TextArea(rawText,GUILayout.Height(180));if(GUILayout.Button("Применить текст к ползункам"))try{var next=ConfigJson.Read<LayeredCardsConfig>(rawText);next.Validate(LayeredCardData.Current.Catalog());data=next;Preview();status="Черновик применён. Для записи нажмите «Сохранить».";}catch(Exception e){status=e.Message;}}
            if(GUILayout.Button("Открыть сцену примеров"))LayeredCardsAuthoring.Open();
            if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
        }
        void Read(){try{original=rawText=File.ReadAllText(path);data=ConfigJson.Read<LayeredCardsConfig>(original);status="";}catch(Exception e){status=e.Message;data=null;}}
        void Layer(string title,CardArtLayer layer)
        {
            EditorGUILayout.Space();EditorGUILayout.LabelField(title,EditorStyles.boldLabel);
            var texture=Resources.Load<Texture2D>(layer.texture);
            var chosen=(Texture2D)EditorGUILayout.ObjectField("PNG из проекта",texture,typeof(Texture2D),false);
            if(chosen!=texture&&chosen!=null){string asset=AssetDatabase.GetAssetPath(chosen);int at=asset.LastIndexOf("/Resources/",StringComparison.Ordinal);if(at>=0)layer.texture=Path.ChangeExtension(asset.Substring(at+11),null);else status="Переместите PNG в LayeredCards/Resources/LayeredCards/Layers/.";}
            EditorGUILayout.LabelField(layer.texture,EditorStyles.wordWrappedMiniLabel);
            layer.fit=EditorGUILayout.Popup("Вписывание",layer.fit=="cover"?0:1,new[]{"Заполнить с обрезкой","Вписать целиком"})==0?"cover":"contain";
            layer.offsetX=EditorGUILayout.Slider("По горизонтали",layer.offsetX,-1,1);
            layer.offsetY=EditorGUILayout.Slider("По вертикали",layer.offsetY,-1,1);
            layer.zoom=EditorGUILayout.Slider("Масштаб",layer.zoom,.25f,3);
        }
        void Preview()
        {
            try{data.Validate(LayeredCardData.Current.Catalog());foreach(var v in UnityEngine.Object.FindObjectsByType<LayeredCardView>(FindObjectsInactive.Include,FindObjectsSortMode.None))v.Apply(LayeredCardData.Current,data);SceneView.RepaintAll();}
            catch(Exception e){status=e.Message;}
        }
    }
    [CustomEditor(typeof(LayeredCardsManager))]
    public sealed class LayeredCardsManagerInspector:UnityEditor.Editor
    {
        readonly LayeredCardsJsonEditor editor=new LayeredCardsJsonEditor();
        public override void OnInspectorGUI()=>editor.Draw(ConfigAuthoring.Folder+"/"+LayeredCardsJsonEditor.FileName);
    }
    [CustomEditor(typeof(LayeredCardView))]
    public sealed class LayeredCardViewInspector:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if(GUILayout.Button("Заполнить из JSON по названию"))try{LayeredCardData.Reload();((LayeredCardView)target).Apply(LayeredCardData.Current);SceneView.RepaintAll();}catch(Exception e){Debug.LogError(e.Message);}
            if(GUILayout.Button("Настроить слои в JSON"))Selection.activeObject=AssetDatabase.LoadMainAssetAtPath(ConfigAuthoring.Folder+"/layered-cards.json");
        }
    }
}
