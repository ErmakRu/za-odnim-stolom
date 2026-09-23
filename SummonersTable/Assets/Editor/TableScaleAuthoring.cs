using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static class TableScaleAuthoring
    {
        public static Bounds VisibleBounds(GameObject root)
        {
            var bounds=new Bounds();bool first=true;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
            {
                if(renderer is SkinnedMeshRenderer skin)
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    foreach(var vertex in mesh.vertices){var p=skin.transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                    Object.DestroyImmediate(mesh);
                }
                else {if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
            }
            return bounds;
        }
        public static GameObject Hero(HeroDefinition definition)
        {
            var root=Object.Instantiate(definition.prefab);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {if(t.name=="Weapon"||t.name=="Shield")t.gameObject.SetActive(false);if(t.name.StartsWith("Body")&&int.TryParse(t.name.Substring(4),out int n))t.gameObject.SetActive(n==1);}
            root.GetComponentInChildren<Animator>().Rebind();return root;
        }
        public static void Inspect()
        {
            var report=new StringBuilder();var library=Resources.Load<HeroLibrary>("HeroLibrary");
            foreach(var definition in library.heroes)
            {
                var root=Hero(definition);var animator=root.GetComponentInChildren<Animator>();var bounds=VisibleBounds(root);
                report.AppendLine(definition.id+" bounds="+bounds+" head="+animator.GetBoneTransform(HumanBodyBones.Head).position+" hips="+animator.GetBoneTransform(HumanBodyBones.Hips).position+" foot="+animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
                Object.DestroyImmediate(root);
            }
            foreach(var prefab in new[]{library.table,library.chair}){var root=Object.Instantiate(prefab);report.AppendLine(prefab.name+" "+VisibleBounds(root));Object.DestroyImmediate(root);}
            var world=PrefabUtility.LoadPrefabContents("Assets/Prefabs/TableWorld.prefab");
            foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Imported table"||t.name=="Armchair from stuff"||t.name.StartsWith("Hero avatar")))report.AppendLine(t.name+" pos="+t.position+" scale="+t.localScale);
            PrefabUtility.UnloadPrefabContents(world);Directory.CreateDirectory("../tmp");File.WriteAllText("../tmp/table-scale-inventory.txt",report.ToString());Debug.Log(report);
        }
        [MenuItem("Summoners Table/Apply human and table proportions (0.5.2)")]
        public static void Apply()
        {
            string path="Assets/Prefabs/TableWorld.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var board=root.GetComponent<TableBoard>();var p=root.GetComponent<TableProportions>()??root.AddComponent<TableProportions>();float m=p.unitsPerMetre;
                var library=Resources.Load<HeroLibrary>("HeroLibrary");var reference=Hero(library.Find("badger"));var animator=reference.GetComponentInChildren<Animator>();
                p.avatarScale=(p.seatedCrownHeight-p.hipHeight)*m/(VisibleBounds(reference).max.y-animator.GetBoneTransform(HumanBodyBones.Hips).position.y);Object.DestroyImmediate(reference);
                foreach(var definition in library.heroes){var hero=Hero(definition);definition.crownAboveHead=VisibleBounds(hero).max.y-hero.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head).position.y;Object.DestroyImmediate(hero);}
                var table=board.authoredEnvironment.Find("Imported table");Fit(table,new Vector3(p.tableDiameter*m,p.tableHeight*m,p.tableDiameter*m),Vector3.zero,Quaternion.identity);
                var center=board.authoredEnvironment.GetComponentsInChildren<BoardTarget>(true).First(t=>t.kind=="center");var c=center.transform.localPosition;c.y=p.tableHeight*m+.015f;center.transform.localPosition=c;
                var chairMeshes=ChairMeshes(library.chair,p);
                foreach(var layout in board.layouts)for(int seat=0;seat<layout.playerCount;seat++)
                {
                    var away=TableBoard.Away(seat,layout.playerCount);var actor=layout.avatars[seat].GetComponent<HeroActor>();
                    actor.transform.localPosition=away*p.playerRadius*m;actor.transform.localScale=Vector3.one*p.avatarScale;
                    actor.seatedHipHeight=p.hipHeight*m;actor.seatedThighLength=.34f*m;actor.seatedFootHeight=.08f*m;
                    var hero=layout.heroes[seat];hero.localPosition=away*p.playerRadius*m+Vector3.up*8.2f;hero.localScale=new Vector3(3.8f,5.6f,3.6f);
                    var chair=actor.transform.parent.Find("Armchair from stuff");var filters=chair.GetComponentsInChildren<MeshFilter>();
                    for(int i=0;i<filters.Length;i++)filters[i].sharedMesh=chairMeshes[i];
                    Fit(chair,new Vector3(p.chairWidth*m,p.chairBackHeight*m,p.chairDepth*m),away*p.chairRadius*m,Quaternion.LookRotation(-away));
                    for(int slot=0;slot<5;slot++){var anchor=layout.slotAnchors[seat*5+slot];var position=anchor.localPosition;position.y=p.tableHeight*m;anchor.localPosition=position;}
                }
                library.heroScale=p.avatarScale;EditorUtility.SetDirty(library);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var settings=Resources.Load<PresentationSettings>("PresentationSettings");var cameras=settings.data.cameraModes;
            SetCamera(cameras[0],8,9.6f,7.2f,74);SetCamera(cameras[1],12,24,8,60);SetCamera(cameras[2],.3f,34,6,57);EditorUtility.SetDirty(settings);
            path="Assets/Prefabs/Editable/UI/PlayerStatus.prefab";root=PrefabUtility.LoadPrefabContents(path);var status=root.GetComponent<PlayerStatusView>();status.healthHeight=TableBoard.TableTop+.28f;status.healthBetweenHeroAndSlots=.5f;status.nameScreenPadding=70;status.minimumNameGap=20;status.healthAnchor.localScale=Vector3.one*.8f;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            path="Assets/Prefabs/Editable/UI/LabControls.prefab";root=PrefabUtility.LoadPrefabContents(path);var controls=root.GetComponent<WidgetScreen>();controls.Get<UnityEngine.UI.Slider>("height").maxValue=42;controls.Get<UnityEngine.UI.Slider>("distance").maxValue=26;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();Validate();
        }
        static void SetCamera(CameraModeSettings c,float distance,float height,float focus,float fov){c.distance=distance;c.height=height;c.focusHeight=focus;c.fieldOfView=fov;}
        static Mesh[] ChairMeshes(GameObject source,TableProportions p)
        {
            // Preserve the seat and floor contact while shortening this pack's very tall back.
            // Store editable derived meshes; the imported FBX and its vertices remain unchanged.
            var root=Object.Instantiate(source);var bounds=VisibleBounds(root);float seat=SeatSurface(root,bounds);var filters=root.GetComponentsInChildren<MeshFilter>();var meshes=new Mesh[filters.Length];
            Directory.CreateDirectory("Assets/Presentation/Meshes");AssetDatabase.Refresh();
            for(int i=0;i<filters.Length;i++)
            {
                var filter=filters[i];var mesh=Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;
                for(int v=0;v<vertices.Length;v++)
                {
                    var point=filter.transform.TransformPoint(vertices[v]);point.x=(point.x-bounds.center.x)/bounds.size.x*p.chairWidth;point.z=(point.z-bounds.center.z)/bounds.size.z*p.chairDepth;
                    point.y=point.y<=seat?(point.y-bounds.min.y)/(seat-bounds.min.y)*p.seatHeight:p.seatHeight+(point.y-seat)/(bounds.max.y-seat)*(p.chairBackHeight-p.seatHeight);
                    vertices[v]=filter.transform.InverseTransformPoint(point);
                }
                mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.name="Armchair proportions "+i;
                string path="Assets/Presentation/Meshes/Armchair proportions "+i+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null){AssetDatabase.CreateAsset(mesh,path);meshes[i]=mesh;}else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);meshes[i]=existing;EditorUtility.SetDirty(existing);}
            }
            Object.DestroyImmediate(root);return meshes;
        }
        static void Fit(Transform item,Vector3 size,Vector3 position,Quaternion rotation)
        {
            item.SetPositionAndRotation(Vector3.zero,Quaternion.identity);item.localScale=Vector3.one;var bounds=VisibleBounds(item.gameObject);
            item.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
            var offset=-Vector3.Scale(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),item.localScale);
            item.SetPositionAndRotation(position+rotation*offset,rotation);
        }
        static float SeatSurface(GameObject chair,Bounds bounds)
        {
            float surface=float.NegativeInfinity;var ray=new Ray(new Vector3(bounds.center.x,bounds.max.y+2,bounds.center.z),Vector3.down);
            foreach(var filter in chair.GetComponentsInChildren<MeshFilter>())
            {
                var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;
                Physics.SyncTransforms();
                if(collider.Raycast(ray,out var hit,bounds.size.y+3))surface=Mathf.Max(surface,hit.point.y);Object.DestroyImmediate(collider);
            }
            if(!float.IsFinite(surface)||surface<=bounds.min.y+.1f)throw new Exception("Cannot locate the armchair seat surface.");return surface;
        }
        public static void Validate()
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/TableWorld.prefab");var report=new StringBuilder();
            try
            {
                var p=root.GetComponent<TableProportions>();var board=root.GetComponent<TableBoard>();var table=VisibleBounds(board.authoredEnvironment.Find("Imported table").gameObject);if(board.seating!=null)board.layouts=new[]{board.seating.Build(4,1,true)};
                if(Mathf.Abs(table.size.x/p.unitsPerMetre-p.tableDiameter)>.01f||Mathf.Abs(table.size.z/p.unitsPerMetre-p.tableDiameter)>.01f||Mathf.Abs(table.max.y/p.unitsPerMetre-p.tableHeight)>.01f)throw new Exception("Table does not match its authored diameter and height reference.");
                foreach(var layout in board.layouts)foreach(var avatar in layout.avatars)
                {var actor=avatar.GetComponent<HeroActor>();if(Mathf.Abs(actor.transform.localScale.x-p.avatarScale)>.001f||Mathf.Abs(actor.seatedHipHeight-p.hipHeight*p.unitsPerMetre)>.001f)throw new Exception("Inconsistent seated actor proportions.");}
                foreach(var layout in board.layouts)foreach(var slot in layout.slotAnchors)if(Mathf.Abs(slot.position.y-table.max.y)>.02f)throw new Exception("Slot is detached from tabletop.");
                foreach(var layout in board.layouts)foreach(var avatar in layout.avatars)
                {
                    // Inactive 2/3-player layouts have no physics shapes in the prefab preview scene.
                    // Inspect a temporary active copy so the seat raycast measures every layout.
                    var chair=Object.Instantiate(avatar.transform.parent.Find("Armchair from stuff").gameObject);chair.SetActive(true);
                    try
                    {
                        var bounds=VisibleBounds(chair);float seat=SeatSurface(chair,bounds)/p.unitsPerMetre;
                        if(Mathf.Abs(bounds.max.y/p.unitsPerMetre-p.chairBackHeight)>.01f||Mathf.Abs(seat-p.seatHeight)>.015f)throw new Exception("Armchair height or seat contact is wrong: top="+bounds.max.y/p.unitsPerMetre+", seat="+seat);
                    }
                    finally{Object.DestroyImmediate(chair);}
                }
                report.AppendLine("PASS 1.5 m circular tabletop, 0.75 m table height, 0.46 m seat reference; consistent seated rig/feet and raised slots across all 2/3/4 layouts.");
                report.AppendLine("Reference: 1.8 m human; seated crown 1.35 m. Stylized animal ears and antlers preserve their additional height.");
                report.AppendLine("Scene units per metre: "+p.unitsPerMetre+"; avatar scale: "+p.avatarScale.ToString("F3"));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/table-proportions.txt",report.ToString());Debug.Log("TABLE_PROPORTIONS_PASSED\n"+report);
        }
        public static void ApplyAndBuild(){Apply();BuildTools.BuildWindows();}
    }
}
