using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    [CustomEditor(typeof(AuthoringManager))]
    public sealed class AuthoringManagerInspector:UnityEditor.Editor
    {
        static readonly string[] Titles={"Локация","Звуки по событиям","Визуальные эффекты","Анимации персонажа","События локации","Карты","Колоды","Правила","Камера и параллакс","Интерфейс и стрелки"};
        static readonly string[] Fields={"world","audio","vfx","playeranimations","events","cards","decks","rules","presentation","ui"};
        string status="";bool details;
        public override void OnInspectorGUI()
        {
            var m=(AuthoringManager)target;serializedObject.Update();
            EditorGUILayout.LabelField(Titles[(int)m.section]+" · Manager",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Поля меняются здесь. Предпросмотр не требует Play. «Сохранить» обновляет этот раздел main.json и рабочий "+m.FileName+". Копия и откат затрагивают только этот Manager.",MessageType.Info);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Сохранить"))Action(()=>{serializedObject.ApplyModifiedProperties();ManagerStorage.Save(m);EditorUtility.SetDirty(m);PrefabUtility.RecordPrefabInstancePropertyModifications(m);AssetDatabase.Refresh();return "Сохранено в main.json и "+m.FileName;});
                if(GUILayout.Button("Создать копию"))Action(()=>{serializedObject.ApplyModifiedProperties();return "Копия: "+ManagerStorage.Copy(m);});
                if(GUILayout.Button("Откатиться"))Action(()=>{Undo.RecordObject(m,"Откат Manager");string file=ManagerStorage.Latest(m);EditorUtility.SetDirty(m);serializedObject.Update();ManagerPreview.Refresh(m);return "Загружена копия: "+Path.GetFileName(file)+". Для записи нажмите «Сохранить».";});
            }
            if(GUILayout.Button("Загрузить сохранённые параметры"))Action(()=>{Undo.RecordObject(m,"Load Manager");m.Import(File.ReadAllText(Path.Combine(ConfigAuthoring.Folder,m.FileName)));serializedObject.Update();return "Параметры загружены.";});
            if(m.Data==null){EditorGUILayout.HelpBox("Нажмите «Загрузить сохранённые параметры».",MessageType.Warning);return;}
            var data=serializedObject.FindProperty(Fields[(int)m.section]);
            EditorGUI.BeginChangeCheck();int previousMax=m.world?.playerRange.y??4;
            if(m.section==ManagerSection.World)World(data);
            else if(m.section==ManagerSection.Cards)Cards(m,data);
            else EditorGUILayout.PropertyField(data,new GUIContent(Titles[(int)m.section]),true);
            if(EditorGUI.EndChangeCheck()){serializedObject.ApplyModifiedProperties();if(m.section==ManagerSection.World&&previousMax!=m.world.playerRange.y)m.previewPlayers=m.world.playerRange.y;EditorUtility.SetDirty(m);ManagerPreview.Refresh(m);}
            Preview(m);
            details=EditorGUILayout.Foldout(details,"Объекты и точки предпросмотра");
            if(details){EditorGUILayout.PropertyField(serializedObject.FindProperty("table"));EditorGUILayout.PropertyField(serializedObject.FindProperty("previewAnchor"));EditorGUILayout.PropertyField(serializedObject.FindProperty("previewHero"));}
            serializedObject.ApplyModifiedProperties();
            if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
        }
        void World(SerializedProperty data)
        {
            foreach(string name in new[]{"id","name","timeChanges","playersTurnTime","qte","revealTime","gameTimer"})EditorGUILayout.PropertyField(data.FindPropertyRelative(name));
            var range=data.FindPropertyRelative("playerRange");float min=range.vector2IntValue.x,max=range.vector2IntValue.y;
            EditorGUILayout.MinMaxSlider(new GUIContent("Игроки: "+min+" – "+max),ref min,ref max,1,8);range.vector2IntValue=new Vector2Int(Mathf.RoundToInt(min),Mathf.RoundToInt(max));
            EditorGUILayout.HelpBox("Предпросмотр: 1–8 мест. Настоящий матч: пересечение диапазона локации с 2–4. Таймер -1 оставляет правила игры.",MessageType.None);
            foreach(string name in new[]{"scale","scene","events"})EditorGUILayout.PropertyField(data.FindPropertyRelative(name),true);
        }
        void Cards(AuthoringManager m,SerializedProperty data)
        {
            var entries=data.FindPropertyRelative("cards");
            m.previewIndex=Mathf.Clamp(m.previewIndex,0,entries.arraySize-1);
            m.previewIndex=EditorGUILayout.Popup("Карта",m.previewIndex,m.cards.cards.Select(c=>c.id+" · "+c.name).ToArray());
            EditorGUILayout.PropertyField(entries.GetArrayElementAtIndex(m.previewIndex),new GUIContent("Параметры карты"),true);
            EditorGUILayout.PropertyField(data.FindPropertyRelative("typeColors"),true);EditorGUILayout.PropertyField(data.FindPropertyRelative("roleColors"),true);
            if(GUILayout.Button("Открыть префаб выбранной карты"))AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(ConfigAuthoring.Root+"Cards/Instances/"+m.cards.cards[m.previewIndex].id+".prefab"));
        }
        void Preview(AuthoringManager m)
        {
            EditorGUILayout.Space();EditorGUILayout.LabelField("Предпросмотр",EditorStyles.boldLabel);
            if(m.section==ManagerSection.World)
            {
                EditorGUI.BeginChangeCheck();int count=EditorGUILayout.IntSlider("Количество мест",m.previewPlayers,1,8);
                if(EditorGUI.EndChangeCheck()){Undo.RecordObject(m,"Preview players");m.previewPlayers=count;ManagerPreview.Refresh(m);}
            }
            if(m.section==ManagerSection.Interface)m.previewCards=EditorGUILayout.IntSlider("Карты в веере",m.previewCards,1,20);
            string[] labels=m.section switch
            {
                ManagerSection.Audio=>m.audio.cues.Select(c=>c.action).ToArray(),
                ManagerSection.Vfx=>m.vfx.effects.Select(e=>e.id).Concat(new[]{"Атака существа","Попадание","Смерть","QTE: огонь","QTE: дым","QTE: попытка"}).ToArray(),
                ManagerSection.PlayerAnimations=>m.playeranimations.states.Select(s=>s.id).ToArray(),
                ManagerSection.Events=>m.events.events.Select(e=>e.name).ToArray(),
                _=>Array.Empty<string>()
            };
            if(labels.Length>0)m.previewIndex=EditorGUILayout.Popup("Проверить",Mathf.Clamp(m.previewIndex,0,labels.Length-1),labels);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Показать / воспроизвести"))Action(()=>{serializedObject.ApplyModifiedProperties();ManagerPreview.Play(m);return "Предпросмотр запущен.";});
                if(GUILayout.Button("Остановить и очистить")){ManagerPreview.Stop(m);status="Предпросмотр очищен.";}
            }
            if(Application.isPlaying&&GUILayout.Button("Применить сохранённое к игре")){UnityEngine.Object.FindFirstObjectByType<GameApp>()?.ReloadConfiguration();}
        }
        void Action(Func<string> action){try{status=action();}catch(Exception e){status=e.Message;Debug.LogWarning(e.Message);}}
    }
}
