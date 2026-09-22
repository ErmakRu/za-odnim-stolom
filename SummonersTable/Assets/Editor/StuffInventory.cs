using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SummonersTable.Editor
{
    public static class StuffInventory
    {
        public static void Run()
        {
            var report=new StringBuilder();
            foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("ModularAnimalKnightsPolyart/Prefab/")&&p.EndsWith("Standard.prefab")&&!Path.GetFileName(p).StartsWith("sd")&&!Path.GetFileName(p).StartsWith("sw")))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);report.AppendLine("MODEL "+path);
                var obj=Object.Instantiate(prefab);var renderers=obj.GetComponentsInChildren<Renderer>(true);
                Bounds bounds=new Bounds();bool first=true;foreach(var r in renderers.Where(r=>r.gameObject.activeInHierarchy)){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                report.AppendLine("BOUNDS "+bounds+" ROOT "+obj.transform.localScale);
                foreach(var t in obj.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Head")||t.name.Contains("Weapon")||t.name.Contains("Thigh")||t.name.Contains("Calf")||t.name.Contains("Spine")||t.name.Contains("Hips")))report.AppendLine("BONE "+t.name+" "+t.localPosition);
                report.AppendLine("MATERIALS "+string.Join(";",renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name+" / "+m.shader.name).Distinct()));
                var animator=obj.GetComponentInChildren<Animator>();report.AppendLine("ANIMATOR "+(animator==null?"NONE":animator.avatar?.name+" human="+animator.isHuman));
                Object.DestroyImmediate(obj);
            }
            foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("ModularAnimalKnightsPolyart/Animation/")&&p.EndsWith(".fbx")&&!p.Contains("RootMotion")))
                report.AppendLine("CLIPS "+Path.GetFileName(path)+": "+string.Join(";",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).Select(c=>c.name+" / "+c.length)));
            foreach(string name in new[]{"SM_Armchair","SM_Table"})
            {
                var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/3dModels/room_items/"+name+".fbx"));
                foreach(var r in obj.GetComponentsInChildren<Renderer>())report.AppendLine(name+" "+r.bounds+" material "+string.Join(",",r.sharedMaterials.Select(m=>m?.name+"/"+m?.shader?.name)));Object.DestroyImmediate(obj);
            }
            File.WriteAllText("../tmp/stuff-inventory-unity.txt",report.ToString());Debug.Log("STUFF_INVENTORY_COMPLETE");
        }
    }
}
