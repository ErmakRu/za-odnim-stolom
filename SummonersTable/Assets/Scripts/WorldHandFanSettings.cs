using UnityEngine;
namespace SummonersTable
{
    public sealed class WorldHandFanSettings:MonoBehaviour
    {
        public Transform anchor;
        public Vector2 cardSize=new Vector2(1.05f,1.48f);
        public float radius=1.7f,sweep=116,maximumStep=23,sideOffset=3.1f,inwardOffset=.8f,height=1.35f,tilt=12,roll=.28f;
        public void Pose(int index,int count,Vector3 hero,Vector3 away,out Vector3 position,out Quaternion rotation)
        {
            if(ConfigRuntime.Available){var style=ConfigRuntime.Current.ui.fan;radius=style.radius;sweep=style.spread;maximumStep=style.maximumStep;cardSize=style.worldCardSize;}
            var right=Vector3.Cross(Vector3.up,-away);float degrees=FanGeometry.Angle(index,count,sweep,maximumStep);float a=degrees*Mathf.Deg2Rad;
            var origin=this.anchor!=null?this.anchor.position:hero+right*sideOffset-away*inwardOffset;if(this.anchor==null)origin.y=TableBoard.TableTop+height;
            position=origin+right*(Mathf.Sin(a)*radius)+away*((1-Mathf.Cos(a))*radius)+Vector3.up*((Mathf.Cos(a)-1)*.3f);
            rotation=Quaternion.LookRotation(-away)*Quaternion.Euler(tilt,degrees,-degrees*roll);
        }
    }
}
