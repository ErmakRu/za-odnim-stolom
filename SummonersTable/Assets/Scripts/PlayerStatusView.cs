using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace SummonersTable
{
    public sealed class PlayerStatusView : MonoBehaviour
    {
        public RectTransform nameAnchor,healthAnchor;
        public bool seatOwned;public Transform seatNamePoint,seatHealthPoint;
        public Vector2 healthSize=new Vector2(170,26);public Color healthColor=new Color(.3f,.85f,.76f);
        public Text nickname,healthNumber;
        public Image healthFill,healthTrail;
        public Vector3 nameOffset=new Vector3(0,1.25f,0);
        [Range(0,1)]public float healthBetweenHeroAndSlots=.5f;
        public float healthHeight=1.25f;
        public float minimumNameGap=48;
        public float nameScreenPadding=32;
        public float nameCollisionPadding=6;
        Vector3 nameScale,healthScale;bool scalesSaved;
        readonly Vector3[] corners=new Vector3[4];
        readonly List<Rect> labelObstacles=new List<Rect>();
        public void Present(PlayerState player,MatchState state,TableBoard board,int maxHp)
        {
            nickname.text=player.name;nickname.color=TableBoard.SeatColors[player.seat];healthNumber.text=player.hp+" / "+maxHp;
            healthFill.fillAmount=player.hp/(float)maxHp;var style=ConfigRuntime.Current?.ui;healthFill.color=style!=null?(style.healthUsesPlayerColor?TableBoard.SeatColors[player.seat]:style.healthColor):healthColor;
            healthAnchor.sizeDelta=style?.healthSize??healthSize;if(style!=null)healthTrail.color=style.healthTrailColor;healthTrail.fillAmount=board.DisplayHp("hero-"+player.seat,player.hp)/maxHp;
            if(seatOwned&&seatNamePoint!=null&&seatHealthPoint!=null){nameAnchor.position=seatNamePoint.position;healthAnchor.position=seatHealthPoint.position;nameAnchor.rotation=healthAnchor.rotation=board.ViewCamera.transform.rotation;ReadableScale(board.ViewCamera);return;}
            var hero=TableBoard.HeroPosition(player.seat,state.players.Count);var namePosition=hero+nameOffset;
            var actor=board.Actor(player.seat);if(actor!=null&&actor.NamePosition.y>namePosition.y)namePosition=actor.NamePosition;
            Place(nameAnchor,board.ViewCamera,namePosition);
            var p=Vector3.Lerp(hero,TableBoard.SlotPosition(player.seat,2,state.players.Count),healthBetweenHeroAndSlots);p.y=healthHeight;Place(healthAnchor,board.ViewCamera,p);
            // A top-down projection flattens vertical offsets: keep the name above the head in screen space too.
            var head=board.ViewCamera.WorldToScreenPoint(namePosition);
            var body=board.ViewCamera.WorldToScreenPoint(hero);
            float uiScale=nameAnchor.lossyScale.y;
            if(Mathf.Abs(head.y-body.y)<minimumNameGap*uiScale)nameAnchor.anchoredPosition+=Vector2.up*(minimumNameGap-Mathf.Abs(head.y-body.y)/uiScale);
            nameAnchor.anchoredPosition+=Vector2.up*nameScreenPadding;
            var viewport=board.ViewCamera.pixelRect;var screen=RectTransformUtility.WorldToScreenPoint(null,nameAnchor.position);
            var half=nameAnchor.rect.size*uiScale*.5f;
            var clamped=new Vector2(Mathf.Clamp(screen.x,viewport.xMin+half.x,viewport.xMax-half.x),Mathf.Clamp(screen.y,viewport.yMin+half.y+4,viewport.yMax-half.y-4));
            nameAnchor.anchoredPosition+=(clamped-screen)/uiScale;
        }
        void ReadableScale(Camera camera)
        {
            if(!scalesSaved){nameScale=nameAnchor.localScale;healthScale=healthAnchor.localScale;scalesSaved=true;}
            Scale(nameAnchor,nameScale);Scale(healthAnchor,healthScale);
            var nameBounds=Projected(nameAnchor);var healthBounds=Projected(healthAnchor);
            if(nameBounds.Overlaps(healthBounds)){var p=camera.WorldToScreenPoint(nameAnchor.position);p.y+=healthBounds.yMax-nameBounds.yMin+6;nameAnchor.position=camera.ScreenToWorldPoint(p);}
            Rect Projected(RectTransform item){item.GetWorldCorners(corners);var a=(Vector2)camera.WorldToScreenPoint(corners[0]);var b=a;for(int i=1;i<4;i++){var p=(Vector2)camera.WorldToScreenPoint(corners[i]);a=Vector2.Min(a,p);b=Vector2.Max(b,p);}return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
            void Scale(RectTransform item,Vector3 authored)
            {
                float depth=Mathf.Max(.1f,camera.WorldToScreenPoint(item.position).z);
                float unitsPerPixel=camera.orthographic?2*camera.orthographicSize/Mathf.Max(1,camera.pixelHeight):2*depth*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)/Mathf.Max(1,camera.pixelHeight);
                float logicalScale=Application.isPlaying?Mathf.Min(Screen.width/1600f,Screen.height/1000f):Mathf.Min(camera.pixelWidth/1600f,camera.pixelHeight/1000f);
                item.localScale=authored*(unitsPerPixel*logicalScale/Mathf.Max(.0001f,item.parent.lossyScale.x));
            }
        }
        public void PreviewPose(Camera camera)
        {
            if(seatNamePoint!=null)nameAnchor.position=seatNamePoint.position;if(seatHealthPoint!=null)healthAnchor.position=seatHealthPoint.position;
            if(camera!=null){nameAnchor.rotation=healthAnchor.rotation=camera.transform.rotation;ReadableScale(camera);}
        }
        public void AvoidNameOverlap(IEnumerable<RectTransform> obstacles,Camera camera)
        {
            if(seatOwned||!nameAnchor.gameObject.activeInHierarchy)return;
            var name=ScreenRect(nameAnchor);float padding=nameCollisionPadding*Mathf.Abs(nameAnchor.lossyScale.y);labelObstacles.Clear();
            foreach(var obstacle in obstacles)
            {
                if(obstacle==null||!obstacle.gameObject.activeInHierarchy)continue;
                var bounds=ScreenRect(obstacle);bounds.xMin-=padding;bounds.xMax+=padding;bounds.yMin-=padding;bounds.yMax+=padding;
                if(bounds.yMax>name.yMin&&bounds.yMin<name.yMax)labelObstacles.Add(bounds);
            }
            bool blocked=false;foreach(var obstacle in labelObstacles)blocked|=name.Overlaps(obstacle);if(!blocked)return;
            float best=float.PositiveInfinity;
            foreach(var obstacle in labelObstacles){Consider(obstacle.xMax-name.xMin);Consider(obstacle.xMin-name.xMax);}
            if(float.IsFinite(best))nameAnchor.anchoredPosition+=Vector2.right*best/nameAnchor.lossyScale.x;
            void Consider(float shift)
            {
                if(Mathf.Abs(shift)>=Mathf.Abs(best))return;var moved=name;moved.x+=shift;
                if(moved.xMin<camera.pixelRect.xMin||moved.xMax>camera.pixelRect.xMax)return;
                foreach(var obstacle in labelObstacles)if(moved.Overlaps(obstacle))return;best=shift;
            }
        }
        Rect ScreenRect(RectTransform transform)
        {
            transform.GetWorldCorners(corners);var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var max=min;
            for(int i=1;i<4;i++){var p=RectTransformUtility.WorldToScreenPoint(null,corners[i]);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        static void Place(RectTransform r,Camera camera,Vector3 p){var s=camera.WorldToScreenPoint(p);r.gameObject.SetActive(s.z>0);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)r.parent,s,null,out var local);r.anchoredPosition=local;}
    }
}
