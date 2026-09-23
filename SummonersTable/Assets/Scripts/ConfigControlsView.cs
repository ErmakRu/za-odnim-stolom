using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class ConfigControlsView:MonoBehaviour
    {
        public Button reload;public Text status;string extra="";
        void Awake(){reload.onClick.AddListener(()=>{var app=FindFirstObjectByType<GameApp>();if(app!=null)app.ReloadConfiguration();else ConfigRuntime.Reload();extra=ConfigRuntime.Message;});}
        void Update(){status.text=string.IsNullOrEmpty(ConfigRuntime.Error)?extra:ConfigRuntime.Message;}
    }
}
