using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public sealed class ConfigJsonInspector:UnityEditor.Editor
    {
        string text,original,status="";Vector2 scroll;bool assets;string filter="";
        readonly LayeredCardsJsonEditor layered=new LayeredCardsJsonEditor();
        [UnityEditor.Callbacks.OnOpenAsset(0)]
        static bool OpenJson(int id,int line){var item=EditorUtility.InstanceIDToObject(id);string path=AssetDatabase.GetAssetPath(item);if(!path.StartsWith(ConfigAuthoring.Folder+"/")||!path.EndsWith(".json"))return false;Selection.activeObject=item;EditorApplication.ExecuteMenuItem("Window/General/Inspector");return true;}
        void OnEnable(){string path=AssetDatabase.GetAssetPath(target);if(path.StartsWith(ConfigAuthoring.Folder+"/")&&path.EndsWith(".json")&&File.Exists(path))text=original=File.ReadAllText(path);}
        public override void OnInspectorGUI()
        {
            string path=AssetDatabase.GetAssetPath(target);if(!path.StartsWith(ConfigAuthoring.Folder+"/")||!path.EndsWith(".json")){DrawDefaultInspector();return;}
            string file=Path.GetFileName(path);
            if(file==LayeredCardsJsonEditor.FileName){layered.Draw(path);return;}
            EditorGUILayout.LabelField(file,EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(file=="main.json"?"Общий снимок настроек. Изменяйте раздел через его Manager; «Сохранить» обновит main.json автоматически.":file=="assets.json"?"Справочник импортированных ассетов. Обновляется из Unity; для выбора скопируйте ID.":"Редактируйте JSON здесь. Сохранение проверит весь набор Config. Во время Play нажмите «Перезагрузить Config», чтобы обновить звук и оформление. Баланс - со следующего матча.",MessageType.Info);
            using(new EditorGUI.DisabledScope(file=="assets.json"||file=="main.json"||path.Contains("/History/")))
            {
                scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.MinHeight(300),GUILayout.MaxHeight(650));text=EditorGUILayout.TextArea(text,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("Сохранить"))Save(path,file);
                    if(GUILayout.Button("Отменить изменения")){text=original=File.ReadAllText(path);status="Изменения отменены";}
                }
            }
            if(file=="assets.json"&&GUILayout.Button("Обновить список ассетов")){ConfigAuthoring.BuildAssetRegistry();AssetDatabase.Refresh();text=original=File.ReadAllText(path);}
            if(Application.isPlaying&&GUILayout.Button("Перезагрузить Config")){var app=Object.FindFirstObjectByType<GameApp>();app?.ReloadConfiguration();status=ConfigRuntime.Message;}
            if(file=="world.json"&&!Application.isPlaying)
            {
                if(GUILayout.Button("Применить world.json к открытой сцене"))try{var config=ConfigBundle.Read(ConfigAuthoring.Folder);foreach(var board in Object.FindObjectsByType<TableBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Undo.RegisterFullObjectHierarchyUndo(board.gameObject,"Apply world.json");ConfigRuntime.ApplyWorld(board,config.world);EditorSceneManager.MarkSceneDirty(board.gameObject.scene);}status="Применено к открытой сцене. Сохраните сцену при необходимости.";}catch(Exception e){status=e.Message;}
            }
            if(file=="audio.json"||file=="vfx.json"||file=="cards.json")
            {
                assets=EditorGUILayout.Foldout(assets,"ID ассетов из Unity");if(assets){filter=EditorGUILayout.TextField("Поиск",filter);var registry=Resources.Load<ConfigAssets>("ConfigAssets");int count=0;foreach(var entry in registry.entries){if(!entry.path.ToLowerInvariant().Contains(filter.ToLowerInvariant()))continue;if(file=="audio.json"&&entry.kind!="audio"||file=="vfx.json"&&entry.kind!="prefab"||file=="cards.json"&&entry.kind!="texture")continue;EditorGUILayout.LabelField(entry.path,EditorStyles.wordWrappedMiniLabel);using(new EditorGUILayout.HorizontalScope()){EditorGUILayout.SelectableLabel(entry.id,GUILayout.Height(18));if(GUILayout.Button("Копировать",GUILayout.Width(82)))EditorGUIUtility.systemCopyBuffer=entry.id;}if(++count>=40){EditorGUILayout.LabelField("Уточните поиск, чтобы увидеть другие ассеты.");break;}}}
            }
            if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
        }
        void Save(string path,string file)
        {
            try{if(File.ReadAllText(path)!=original)throw new IOException("Файл изменился на диске. Отмените изменения, чтобы перечитать его.");ManagerStorage.SaveSection(file,text);original=text;status="Сохранено. Проверка Config пройдена.";AssetDatabase.ImportAsset(path);}
            catch(Exception e){status="Не сохранено: "+e.Message;}
        }
    }
}
