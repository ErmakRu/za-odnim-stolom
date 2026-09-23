using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    // Replace the visual prefabs without changing rules, slot anchors or hit targets.
    public sealed partial class TableBoard : MonoBehaviour
    {
        public GameObject tablePrefab,chairPrefab,avatarPrefab;
        public Transform authoredEnvironment;
        public TableLayout[] layouts;
        public Camera tableCamera;
        public CardLibrary cardLibrary;
        public WorldArrowView arrowPrefab;
        public TextMesh numberPrefab;
        public ManualTableCamera CameraRig {get;private set;}
        public bool inputEnabled=true;
        public string selectedUnit="";
        public string targetMode="enemy";
        public bool choosingTarget,placingCreature;
        bool ownAction;
        Catalog catalog;
        static TableLayout activeLayout;
        public Camera ViewCamera {get;private set;}
        public const float TableTop=1.02f;
        public static readonly Color[] SeatColors={new Color(.3f,.85f,.76f),new Color(1,.67f,.35f),new Color(.69f,.57f,.95f),new Color(.42f,.71f,.96f)};
        readonly Dictionary<string,GameObject> units=new Dictionary<string,GameObject>();
        readonly Dictionary<string,float> landingStarted=new Dictionary<string,float>();
        readonly Dictionary<string,Material> artMaterials=new Dictionary<string,Material>();
        readonly Dictionary<string,Arrow> arrows=new Dictionary<string,Arrow>();
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<GameObject> seatRoots=new List<GameObject>();
        readonly List<GameObject> slots=new List<GameObject>();
        GameObject environment,playersRoot,floatingCard,centerMarker;Material gray,seatGray,darkGray;
        Light keyLight;int count;string visibleCast="",visibleCardId="";int cameraSeat=-1;Arrow castArrow;

        public void Initialize(Catalog data)
        {
            catalog=data;
            ViewCamera=tableCamera??Camera.main??FindFirstObjectByType<Camera>();
            if(ViewCamera==null)ViewCamera=new GameObject("Table camera").AddComponent<Camera>();
            ViewCamera.name="Table camera";ViewCamera.tag="MainCamera";ViewCamera.orthographic=false;
            ViewCamera.fieldOfView=48;ViewCamera.nearClipPlane=.1f;ViewCamera.farClipPlane=80;
            ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.085f,.10f,.12f);
            var camData=ViewCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()??ViewCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();camData.renderPostProcessing=true;
            CameraRig=ViewCamera.GetComponent<ManualTableCamera>()??ViewCamera.gameObject.AddComponent<ManualTableCamera>();CameraRig.Initialize(ViewCamera);
            gray=Solid(new Color(.40f,.42f,.44f));seatGray=Solid(new Color(.49f,.51f,.53f));darkGray=Solid(new Color(.19f,.21f,.24f));
            if(authoredEnvironment!=null)
            {
                environment=authoredEnvironment.gameObject;
                centerMarker=environment.GetComponentsInChildren<BoardTarget>(true).First(t=>t.kind=="center").gameObject;
                keyLight=environment.GetComponentInChildren<Light>(true);
            }
            else
            {
                environment=new GameObject("Replaceable graybox environment");environment.transform.SetParent(transform);
                var table=GameObject.CreatePrimitive(PrimitiveType.Cylinder);table.name="Round table placeholder";table.transform.SetParent(environment.transform);table.transform.position=new Vector3(0,.7f,0);table.transform.localScale=new Vector3(12,.3f,12);table.GetComponent<Renderer>().sharedMaterial=gray;
                Cube("Floor",new Vector3(0,-.13f,0),new Vector3(28,.2f,28),darkGray,environment.transform);
                centerMarker=Cube("Random target — table center",new Vector3(0,TableTop,0),new Vector3(1.5f,.035f,1.5f),Flat(new Color(.68f,.70f,.72f)),environment.transform);
                centerMarker.AddComponent<BoardTarget>().kind="center";
                var lightObject=new GameObject("Table light");lightObject.transform.SetParent(environment.transform);
                keyLight=lightObject.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.intensity=1.25f;
                keyLight.shadows=LightShadows.Soft;lightObject.transform.rotation=Quaternion.Euler(48,-32,0);
            }
            RenderSettings.ambientLight=new Color(.50f,.53f,.59f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            playersRoot=new GameObject("Seats and creature slot anchors");playersRoot.transform.SetParent(transform);
            gameObject.SetActive(false);
        }
        Material Solid(Color color)
        {
            var s = Shader.Find("Toon Shaders Pro/URP/Toon") ?? Resources.Load<Shader>("BoardGray");
            var m = new Material(s);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_LightTint")) {
                m.SetColor("_LightTint", Color.white);
                m.SetColor("_MiddleTint", new Color(0.82f, 0.82f, 0.86f, 1f));
                m.SetColor("_ShadowTint", new Color(0.45f, 0.45f, 0.54f, 1f));
                m.SetVector("_DiffuseThresholds", new Vector4(0.1f, 0.15f, 0.55f, 0.60f));
                m.SetVector("_ShadowThresholds", new Vector4(0.25f, 0.30f, 0f, 0f));
                m.SetFloat("_UseSecondThreshold", 1f);
                m.SetColor("_RimColor", new Color(1f, 0.95f, 0.85f, 1f));
                m.SetVector("_RimThresholds", new Vector4(0.65f, 0.70f, 0f, 0f));
                m.SetFloat("_RimExtension", 0.35f);
            }
            ownedMaterials.Add(m);
            return m;
        }
        Material Flat(Color color)
        {
            var m=new Material(Resources.Load<Shader>("BoardCard"));m.color=color;ownedMaterials.Add(m);return m;
        }
        Material Art(string id)
        {
            if(!artMaterials.TryGetValue(id,out var material))
            {material=Flat(Color.white);material.mainTexture=Resources.Load<Texture2D>("Art/"+id);artMaterials[id]=material;}
            return material;
        }
        GameObject Cube(string name,Vector3 position,Vector3 size,Material material,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent);
            obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        GameObject Visual(GameObject prefab,string name,Vector3 position,Vector3 fallbackSize,Material material,Transform parent)
        {
            if(prefab==null)return Cube(name,position,fallbackSize,material,parent);
            var obj=Instantiate(prefab,parent);obj.name=name;obj.transform.localPosition=position;return obj;
        }
        public static Vector3 Away(int seat,int players)
        {return Quaternion.Euler(0,45+seat*360f/players,0)*Vector3.back;}
        public static Vector3 SlotPosition(int seat,int slot,int players)
        {
            if(activeLayout!=null&&activeLayout.playerCount==players&&activeLayout.slotAnchors.Length>seat*5+slot)return activeLayout.slotAnchors[seat*5+slot].position;
            var away=Away(seat,players);var tangent=Vector3.Cross(Vector3.up,away);
            return away*4.15f+tangent*((slot-2)*1.07f)+Vector3.up*TableTop;
        }
        public static Vector3 HeroPosition(int seat,int players){if(activeLayout!=null&&activeLayout.playerCount==players)return activeLayout.heroes[seat].position;return Away(seat,players)*6.8f+Vector3.up*1.55f;}
        public Vector3 TargetPosition(int seat,string uid,MatchState state)
        {
            if(seat<0||seat>=state.players.Count||!state.players[seat].alive)return new Vector3(0,TableTop+.12f,0);
            var u=state.players[seat].units.Find(x=>x.uid==uid);
            return u==null?HeroPosition(seat,count):SlotPosition(seat,u.slot,count)+Vector3.up*.16f;
        }
        void Seats(int n)
        {
            if(count==n)return;count=n;cameraSeat=-1;activeLayout=null;
            if(layouts!=null&&layouts.Any(l=>l!=null&&l.playerCount==n))
            {
                foreach(var l in layouts)if(l!=null)l.gameObject.SetActive(l.playerCount==n);
                activeLayout=layouts.First(l=>l!=null&&l.playerCount==n);
                seatRoots.Clear();slots.Clear();
                foreach(var h in activeLayout.heroes)seatRoots.Add(h.parent.gameObject);
                foreach(var anchor in activeLayout.slotAnchors)slots.Add(anchor.gameObject);
                foreach(var u in units.Values)Destroy(u);units.Clear();
                foreach(var a in arrows.Values)a.Destroy();arrows.Clear();return;
            }
            foreach(var obj in seatRoots)Destroy(obj);seatRoots.Clear();slots.Clear();
            foreach(var u in units.Values)Destroy(u);units.Clear();
            foreach(var a in arrows.Values)a.Destroy();arrows.Clear();
            for(int s=0;s<n;s++)
            {
                var root=new GameObject("Seat "+s);root.transform.SetParent(playersRoot.transform);seatRoots.Add(root);
                var away=Away(s,n);
                Visual(chairPrefab,"Chair placeholder",away*7+Vector3.up*.48f,new Vector3(1.35f,.95f,1.35f),gray,root.transform).transform.rotation=Quaternion.LookRotation(-away);
                Visual(avatarPrefab,"Avatar placeholder",HeroPosition(s,n),new Vector3(.9f,1.5f,.8f),seatGray,root.transform).transform.rotation=Quaternion.LookRotation(-away);
                var hit=Cube("Hero target",HeroPosition(s,n),new Vector3(1.25f,2,1.25f),seatGray,root.transform);
                hit.GetComponent<Renderer>().enabled=false;var marker=hit.AddComponent<BoardTarget>();marker.kind="hero";marker.seat=s;
                for(int slot=0;slot<5;slot++)
                {
                    var tile=Cube("Creature slot "+slot,SlotPosition(s,slot,n),new Vector3(.96f,.045f,1.25f),darkGray,root.transform);
                    tile.transform.rotation=Quaternion.LookRotation(-away);marker=tile.AddComponent<BoardTarget>();marker.kind="slot";marker.seat=s;marker.slot=slot;slots.Add(tile);
                }
            }
        }
        GameObject Card(string name,string cardId,Vector3 position,Quaternion rotation,Vector2 size)
        {
            GameObject root;
            if(cardId=="card_back"){root=Instantiate(cardLibrary.cardBackPrefab,transform);root.transform.GetChild(0).localScale=new Vector3(size.x,size.y,1);}
            else {var view=Instantiate(cardLibrary.Find(cardId),transform);view.Mode("world");view.Highlight(false,false);view.worldFace.transform.localScale=new Vector3(size.x,size.y,1);root=view.gameObject;}
            root.name=name;root.transform.position=position;root.transform.rotation=rotation;return root;
        }
        public void Sync(MatchState state,int viewer,double clock,int selectedSlot=-1)
        {
            if(state==null){gameObject.SetActive(false);return;}
            gameObject.SetActive(true);Seats(state.players.Count);
            ownAction=state.phase=="action"&&state.activeSeat==viewer;
            var liveUnits=new HashSet<string>();
            foreach(var player in state.players)
            {
                seatRoots[player.seat].SetActive(player.connected);
                foreach(var unit in player.units)
                {
                    liveUnits.Add(unit.uid);
                    if(!units.TryGetValue(unit.uid,out var obj))
                    {
                        var faceRotation=Quaternion.LookRotation(-Away(player.seat,count))*Quaternion.Euler(90,0,0);
                        obj=Card("Unit "+unit.uid,unit.cardId,SlotPosition(player.seat,unit.slot,count)+Vector3.up*.055f,faceRotation,new Vector2(.91f,1.18f));
                        var collider=obj.AddComponent<BoxCollider>();collider.size=new Vector3(.96f,1.24f,.10f);
                        var marker=obj.AddComponent<BoardTarget>();marker.kind="unit";marker.seat=player.seat;marker.slot=unit.slot;marker.uid=unit.uid;
                        units[unit.uid]=obj;
                        landingStarted[unit.uid]=Time.unscaledTime;
                    }
                    if(!arrows.TryGetValue(unit.uid,out var arrow)){arrow=new Arrow(transform,arrowPrefab,SeatColors[player.seat]);arrows[unit.uid]=arrow;}
                    float landing=landingStarted.TryGetValue(unit.uid,out float began)?1-Mathf.SmoothStep(0,1,(Time.unscaledTime-began)/.28f):0;
                    var position=SlotPosition(player.seat,unit.slot,count)+Vector3.up*(.065f+.3f*landing);
                    obj.transform.position=position;
                    obj.transform.rotation=Quaternion.LookRotation(-Away(player.seat,count))*Quaternion.Euler(90,0,0);
                    obj.transform.localScale=Vector3.one;
                    var target=TargetPosition(unit.plannedSeat,unit.plannedUnit,state);
                    arrow.Set(obj.transform.position+Vector3.up*.16f,target,unit.plannedSeat<0?.28f:.7f,unit.targetAssigned&&unit.uid!=selectedUnit?(unit.exhausted?.024f:.045f):0);
                    TintUnit(obj,unit,player.seat,viewer);
                }
            }
            foreach(string uid in units.Keys.Where(id=>!liveUnits.Contains(id)).ToList())
            {StartCoroutine(Dissolve(units[uid]));units.Remove(uid);landingStarted.Remove(uid);arrows[uid].Destroy();arrows.Remove(uid);}
            foreach(var slot in slots)
            {
                var marker=slot.GetComponent<BoardTarget>();
                var renderer=slot.GetComponent<Renderer>();var props=new MaterialPropertyBlock();
                Color color=marker.seat==viewer&&marker.slot==selectedSlot?new Color(.87f,.68f,.30f):new Color(.16f,.18f,.21f);
                if(placingCreature&&marker.seat==viewer&&!state.players[viewer].units.Any(u=>u.slot==marker.slot))color=new Color(.7f,.88f,.8f);
                props.SetColor("_Color",color);renderer.SetPropertyBlock(props);
            }
            if(state.cast!=null)
            {
                if(castArrow==null)castArrow=new Arrow(transform,arrowPrefab,new Color(1,.77f,.3f));
                Vector3 from=state.cast.slot>=0?SlotPosition(state.cast.owner,state.cast.slot,count):HeroPosition(state.cast.owner,count);
                castArrow.Set(from+Vector3.up*.2f,TargetPosition(state.cast.targetSeat,state.cast.targetUnit,state),.9f,state.cast.slot>=0?0:.07f);
            }
            else
            {
                if(castArrow!=null){castArrow.Destroy();castArrow=null;}
            }
            CameraRig.Sync(viewer,count,inputEnabled,cameraSeat!=viewer);cameraSeat=viewer;
            SyncHeroes(state,viewer);
            SyncHandBacks(state,viewer);
            if(activeLayout!=null)for(int i=0;i<activeLayout.avatars.Length;i++)Actor(i)?.SetVisible(!(i==viewer&&CameraRig.Mode==0));
            SyncEffects(state,viewer,clock);
        }
        public void MoveCamera(int viewer,bool ownTurn,bool snap=false)
        {CameraRig.SetMode(ownTurn?2:1);CameraRig.Sync(viewer,Math.Max(2,count),false,snap);}
        public BoardTarget Pick(Vector2 screenPoint)
        {
            if(!gameObject.activeSelf)return null;
            Physics.SyncTransforms();
            var hits=Physics.RaycastAll(ViewCamera.ScreenPointToRay(screenPoint),70).OrderBy(h=>h.distance);
            foreach(var hit in hits){var marker=hit.collider.GetComponent<BoardTarget>();if(marker!=null)return marker;}
            return null;
        }
        public Vector2 Project(Vector3 point,float scale,Vector2 offset)
        {var p=ViewCamera.WorldToScreenPoint(point);return new Vector2((p.x-offset.x)/scale,(Screen.height-p.y-offset.y)/scale);}
        public void SnapCamera(int seat,bool ownTurn){MoveCamera(seat,ownTurn,true);}
        void OnDestroy(){activeLayout=null;foreach(var material in ownedMaterials)if(material!=null)Destroy(material);}
        sealed class Arrow
        {
            readonly WorldArrowView view;readonly Color color;
            public Arrow(Transform parent,WorldArrowView prefab,Color tint){view=UnityEngine.Object.Instantiate(prefab,parent);color=tint;}
            public void Set(Vector3 a,Vector3 b,float lift,float width){view.Set(a,b,lift,width,color);}
            public void Destroy(){UnityEngine.Object.Destroy(view.gameObject);}
        }
    }
}
