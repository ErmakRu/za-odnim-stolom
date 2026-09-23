using System;
using System.Linq;
using UnityEngine;
namespace SummonersTable
{
    [ExecuteAlways] public sealed class SeatingLayout:MonoBehaviour
    {
        public PlayerSeatView seatPrefab;
        [Range(1,8)]public int previewCount=4;
        [Min(.1f)]public float radius=8.48f;
        public float startAngle=45;
        public bool clockwise=true;
        [Range(.25f,3)]public float bodyScale=1;
        public TableLayout layout;
        int builtCount;float builtRadius,builtAngle,builtScale;bool builtClockwise;
        public PlayerSeatView[] Seats=>GetComponentsInChildren<PlayerSeatView>(true);
        public TableLayout Build(int count,float scale,bool force=false)
        {
            count=Mathf.Clamp(count,1,8);
            if(!force&&builtCount==count&&builtRadius==radius&&builtAngle==startAngle&&builtScale==scale&&builtClockwise==clockwise&&Seats.Length==count)return layout;
            Clear();if(seatPrefab==null)return layout;
            if(layout==null)layout=GetComponent<TableLayout>()??gameObject.AddComponent<TableLayout>();
            var seats=new PlayerSeatView[count];
            for(int i=0;i<count;i++)
            {
                GameObject obj;
#if UNITY_EDITOR
                if(!Application.isPlaying)obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(seatPrefab.gameObject,transform);
                else
#endif
                obj=Instantiate(seatPrefab.gameObject,transform);
                obj.name="Место "+(i+1);
                if(!Application.isPlaying)obj.hideFlags=HideFlags.DontSaveInEditor;
                var away=Quaternion.Euler(0,startAngle+(clockwise?1:-1)*i*360f/count,0)*Vector3.back;
                obj.transform.localPosition=away*radius;obj.transform.localRotation=Quaternion.LookRotation(-away);obj.transform.localScale=Vector3.one;
                seats[i]=obj.GetComponent<PlayerSeatView>();seats[i].Assign(i);if(seats[i].body!=null)seats[i].body.localScale=Vector3.one*scale;
#if UNITY_EDITOR
                if(!Application.isPlaying){seats[i].avatar.Configure("badger",0,i%4,true);seats[i].avatar.Animator?.Rebind();seats[i].avatar.Animator?.Update(0);seats[i].avatar.EditorPose();if(seats[i].status!=null){seats[i].status.nickname.text="Игрок "+(i+1);seats[i].status.healthNumber.text="30 / 30";seats[i].status.PreviewPose(GetComponentInParent<TableBoard>()?.tableCamera);}}
#endif
            }
            layout.playerCount=count;layout.heroes=seats.Select(s=>s.heroTarget).ToArray();layout.avatars=seats.Select(s=>s.avatar.gameObject).ToArray();layout.slotAnchors=seats.SelectMany(s=>s.slots).ToArray();
            builtCount=count;builtRadius=radius;builtAngle=startAngle;builtClockwise=clockwise;builtScale=scale;return layout;
        }
        public void Clear()
        {
            foreach(var seat in Seats){if(Application.isPlaying){seat.gameObject.SetActive(false);Destroy(seat.gameObject);}else DestroyImmediate(seat.gameObject);}
            builtCount=0;
        }
#if UNITY_EDITOR
        void OnEnable(){if(!Application.isPlaying)UnityEditor.EditorApplication.delayCall+=Preview;}
        void OnValidate(){if(!Application.isPlaying)UnityEditor.EditorApplication.delayCall+=Preview;}
        void Preview(){if(this!=null&&!Application.isPlaying&&!UnityEditor.EditorUtility.IsPersistent(this)&&gameObject.scene.IsValid())Build(previewCount,bodyScale);}
#endif
    }
}
