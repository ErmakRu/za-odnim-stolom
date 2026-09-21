using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SummonersTable
{
    public sealed class BoardTarget : MonoBehaviour
    {
        public string kind, uid="";
        public int seat=-1,slot=-1;
    }

    // Replace the visual prefabs without changing rules, slot anchors or hit targets.
    public sealed class TableBoard : MonoBehaviour
    {
        public GameObject tablePrefab,chairPrefab,avatarPrefab;
        public Camera ViewCamera {get;private set;}
        public const float TableTop=1.02f;
        public static readonly Color[] SeatColors={new Color(.3f,.85f,.76f),new Color(1,.67f,.35f),new Color(.69f,.57f,.95f),new Color(.42f,.71f,.96f)};
        readonly Dictionary<string,GameObject> units=new Dictionary<string,GameObject>();
        readonly Dictionary<string,Material> artMaterials=new Dictionary<string,Material>();
        readonly Dictionary<string,Arrow> arrows=new Dictionary<string,Arrow>();
        readonly Dictionary<string,GameObject> reactionCards=new Dictionary<string,GameObject>();
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<GameObject> seatRoots=new List<GameObject>();
        readonly List<GameObject> slots=new List<GameObject>();
        GameObject environment,playersRoot,floatingCard,centerMarker;Material gray,seatGray,darkGray;
        Light keyLight;int count;string visibleCast="";int cameraSeat=-1;Arrow castArrow;

        public void Initialize(Catalog data)
        {
            ViewCamera=Camera.main??FindFirstObjectByType<Camera>();
            if(ViewCamera==null)ViewCamera=new GameObject("Table camera").AddComponent<Camera>();
            ViewCamera.name="Table camera";ViewCamera.tag="MainCamera";ViewCamera.orthographic=false;
            ViewCamera.fieldOfView=48;ViewCamera.nearClipPlane=.1f;ViewCamera.farClipPlane=80;
            ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.085f,.10f,.12f);
            gray=Solid(new Color(.40f,.42f,.44f));seatGray=Solid(new Color(.49f,.51f,.53f));darkGray=Solid(new Color(.19f,.21f,.24f));
            environment=new GameObject("Replaceable graybox environment");environment.transform.SetParent(transform);
            Visual(tablePrefab,"Table placeholder",new Vector3(0,.7f,0),new Vector3(12,.6f,12),gray,environment.transform);
            Cube("Floor",new Vector3(0,-.13f,0),new Vector3(28,.2f,28),darkGray,environment.transform);
            // A separate hit area remains even when the table art is replaced.
            centerMarker=Cube("Random target — table center",new Vector3(0,TableTop,.0f),new Vector3(1.5f,.035f,1.5f),Flat(new Color(.68f,.70f,.72f)),environment.transform);
            centerMarker.AddComponent<BoardTarget>().kind="center";
            var lightObject=new GameObject("Table light");lightObject.transform.SetParent(environment.transform);
            keyLight=lightObject.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.intensity=1.25f;
            keyLight.shadows=LightShadows.Soft;lightObject.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.ambientLight=new Color(.50f,.53f,.59f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            playersRoot=new GameObject("Seats and creature slot anchors");playersRoot.transform.SetParent(transform);
            gameObject.SetActive(false);
        }
        Material Solid(Color color)
        {
            var m=new Material(Resources.Load<Shader>("BoardGray"));m.color=color;ownedMaterials.Add(m);return m;
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
            var away=Away(seat,players);var tangent=Vector3.Cross(Vector3.up,away);
            return away*4.15f+tangent*((slot-2)*1.07f)+Vector3.up*TableTop;
        }
        public static Vector3 HeroPosition(int seat,int players){return Away(seat,players)*6.8f+Vector3.up*1.55f;}
        public Vector3 TargetPosition(int seat,string uid,MatchState state)
        {
            if(seat<0||seat>=state.players.Count||!state.players[seat].alive)return new Vector3(0,TableTop+.12f,0);
            var u=state.players[seat].units.Find(x=>x.uid==uid);
            return u==null?HeroPosition(seat,count):SlotPosition(seat,u.slot,count)+Vector3.up*.16f;
        }
        void Seats(int n)
        {
            if(count==n)return;count=n;cameraSeat=-1;
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
            var root=new GameObject(name);root.transform.SetParent(transform);root.transform.position=position;root.transform.rotation=rotation;
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Replaceable card face";quad.transform.SetParent(root.transform,false);
            quad.transform.localScale=new Vector3(size.x,size.y,1);quad.GetComponent<Renderer>().sharedMaterial=Art(cardId);
            Destroy(quad.GetComponent<Collider>());return root;
        }
        public void Sync(MatchState state,int viewer,double clock,int selectedSlot=-1)
        {
            if(state==null){gameObject.SetActive(false);return;}
            gameObject.SetActive(true);Seats(state.players.Count);
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
                    }
                    if(!arrows.TryGetValue(unit.uid,out var arrow)){arrow=new Arrow(transform,Flat(SeatColors[player.seat]));arrows[unit.uid]=arrow;}
                    var target=TargetPosition(unit.plannedSeat,unit.plannedUnit,state);
                    arrow.Set(obj.transform.position+Vector3.up*.16f,target,unit.plannedSeat<0?.28f:.7f,unit.exhausted?.024f:.045f);
                }
            }
            foreach(string uid in units.Keys.Where(id=>!liveUnits.Contains(id)).ToList())
            {Destroy(units[uid]);units.Remove(uid);arrows[uid].Destroy();arrows.Remove(uid);}
            foreach(var slot in slots)
            {
                var marker=slot.GetComponent<BoardTarget>();
                var renderer=slot.GetComponent<Renderer>();var props=new MaterialPropertyBlock();
                Color color=marker.seat==viewer&&marker.slot==selectedSlot?new Color(.87f,.68f,.30f):new Color(.16f,.18f,.21f);
                props.SetColor("_Color",color);renderer.SetPropertyBlock(props);
            }
            if(state.cast!=null)
            {
                if(visibleCast!=state.cast.id)
                {
                    if(floatingCard!=null)Destroy(floatingCard);
                    floatingCard=Card("Announced card",state.cast.cardId,new Vector3(0,3.7f,0),Quaternion.identity,new Vector2(1.8f,2.6f));visibleCast=state.cast.id;
                }
                bool reveal=state.phase=="reveal";
                floatingCard.transform.position=reveal?new Vector3(0,3.7f,0):new Vector3(0,TableTop+.12f,0);
                floatingCard.transform.rotation=reveal?Quaternion.LookRotation(ViewCamera.transform.forward,ViewCamera.transform.up):Quaternion.Euler(90,0,0);
                if(castArrow==null)castArrow=new Arrow(transform,Flat(new Color(1,.77f,.3f)));
                Vector3 from=state.cast.slot>=0?SlotPosition(state.cast.owner,state.cast.slot,count):HeroPosition(state.cast.owner,count);
                castArrow.Set(from+Vector3.up*.2f,TargetPosition(state.cast.targetSeat,state.cast.targetUnit,state),.9f,.07f);
            }
            else
            {
                if(floatingCard!=null){Destroy(floatingCard);floatingCard=null;visibleCast="";}
                if(castArrow!=null){castArrow.Destroy();castArrow=null;}
            }
            var visibleReactions=new HashSet<string>();
            foreach(var reaction in state.tableReactions)
            {
                if(clock-reaction.playedAt>6&&(state.cast==null||state.cast.id!=reaction.castId))continue;
                visibleReactions.Add(reaction.uid);
                if(!reactionCards.ContainsKey(reaction.uid))
                {
                    var pos=Away(reaction.owner,count)*2.1f+Vector3.up*(TableTop+.15f);
                    reactionCards[reaction.uid]=Card("Reaction "+reaction.cardId,reaction.cardId,pos,Quaternion.LookRotation(-Away(reaction.owner,count))*Quaternion.Euler(90,0,0),new Vector2(.72f,1.02f));
                }
            }
            foreach(var id in reactionCards.Keys.Where(id=>!visibleReactions.Contains(id)).ToList()){Destroy(reactionCards[id]);reactionCards.Remove(id);}
            MoveCamera(viewer,state.activeSeat==viewer&&state.players[viewer].alive,cameraSeat!=viewer);cameraSeat=viewer;
        }
        public void MoveCamera(int viewer,bool ownTurn,bool snap=false)
        {
            var away=Away(viewer,Math.Max(2,count));
            Vector3 overheadDirection=Quaternion.Euler(0,45,0)*away;
            Vector3 pos=ownTurn?overheadDirection*3.5f+Vector3.up*17.6f:away*13+Vector3.up*9.4f;
            Vector3 focus=ownTurn?new Vector3(0,1,0):new Vector3(0,1.1f,0);
            var rotation=Quaternion.LookRotation(focus-pos,Vector3.up);float t=snap?1:1-Mathf.Exp(-Time.unscaledDeltaTime*4);
            ViewCamera.transform.position=Vector3.Lerp(ViewCamera.transform.position,pos,t);
            ViewCamera.transform.rotation=Quaternion.Slerp(ViewCamera.transform.rotation,rotation,t);
        }
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
        void OnDestroy(){foreach(var material in ownedMaterials)if(material!=null)Destroy(material);}
        sealed class Arrow
        {
            readonly GameObject root;readonly LineRenderer line,head;
            public Arrow(Transform parent,Material material)
            {
                root=new GameObject("Attack intention arrow");root.transform.SetParent(parent);line=root.AddComponent<LineRenderer>();
                var tip=new GameObject("Arrow head");tip.transform.SetParent(root.transform);head=tip.AddComponent<LineRenderer>();
                foreach(var lr in new[]{line,head}){lr.sharedMaterial=material;lr.useWorldSpace=true;lr.numCapVertices=2;lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            }
            public void Set(Vector3 a,Vector3 b,float lift,float width)
            {
                line.startWidth=line.endWidth=width;head.startWidth=head.endWidth=width*1.4f;line.positionCount=25;
                Vector3 last=a;
                for(int i=0;i<25;i++){float t=i/24f;last=Vector3.Lerp(a,b,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*lift);line.SetPosition(i,last);}
                Vector3 direction=(b-line.GetPosition(22)).normalized;Vector3 side=Vector3.Cross(direction,Vector3.up).normalized;
                head.positionCount=3;head.SetPositions(new[]{b-direction*.26f+side*.14f,b,b-direction*.26f-side*.14f});
            }
            public void Destroy(){UnityEngine.Object.Destroy(root);}
        }
    }
}
