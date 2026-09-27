using UnityEngine;
namespace SummonersTable
{
    public sealed class DreamTableBackdrop:MonoBehaviour
    {
        public Material dreamSky;
        public GameObject[] surroundings;
        public Camera view;
        Material previousSky;CameraClearFlags previousClear;bool[] previousActive;
        public bool Showing{get;private set;}
        public void Show(bool value)
        {
            if(Showing==value)return;Showing=value;
            if(value){previousSky=RenderSettings.skybox;previousClear=view.clearFlags;previousActive=new bool[surroundings.Length];
                for(int i=0;i<surroundings.Length;i++)if(surroundings[i]!=null){previousActive[i]=surroundings[i].activeSelf;surroundings[i].SetActive(false);}
                RenderSettings.skybox=dreamSky;view.clearFlags=CameraClearFlags.Skybox;
            }
            else {RenderSettings.skybox=previousSky;view.clearFlags=previousClear;for(int i=0;i<surroundings.Length;i++)if(surroundings[i]!=null)surroundings[i].SetActive(previousActive[i]);}
        }
        void OnDestroy(){if(Showing)RenderSettings.skybox=previousSky;}
    }
}
