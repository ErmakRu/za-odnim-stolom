using UnityEngine;
namespace SummonersTable
{
    // Startup-only visibility guard. Authored objects remain visible in edit mode.
    [DisallowMultipleComponent]
    public sealed class StartupCanvasGate:MonoBehaviour
    {
        Canvas[] canvases;bool[] enabledBefore;
        void Awake()
        {
            canvases=GetComponentsInChildren<Canvas>(true);enabledBefore=new bool[canvases.Length];
            for(int i=0;i<canvases.Length;i++){enabledBefore[i]=canvases[i].enabled;canvases[i].enabled=false;}
        }
        void Update()
        {
            var app=FindFirstObjectByType<GameApp>();if(app==null||!app.IsReady)return;
            for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null)canvases[i].enabled=enabledBefore[i];
            enabled=false;
        }
    }
}
