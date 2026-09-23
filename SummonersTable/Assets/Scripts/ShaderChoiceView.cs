using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class ShaderChoiceView:MonoBehaviour
    {
        public Button[] choices;
        public Text description;
        public Color selected=new Color(.7f,.43f,.12f),normal=new Color(.16f,.21f,.23f);
        public bool persist=true;
        void Awake(){for(int i=0;i<choices.Length;i++){int mode=i;choices[i].onClick.AddListener(()=>ShaderSettings.Apply(mode,persist));}}
        void OnEnable(){ShaderSettings.Changed+=Refresh;Refresh();}
        void OnDisable(){ShaderSettings.Changed-=Refresh;}
        public void Refresh()
        {
            int mode=ShaderSettings.Mode;
            for(int i=0;i<choices.Length;i++){choices[i].targetGraphic.color=i==mode?selected:normal;choices[i].GetComponentInChildren<Text>().text=(i==mode?"✓ ":"")+ShaderSettings.Names[i];}
            if(description!=null)description.text=mode==0?"Исходные материалы":mode==1?"Toon — мультяшное освещение":"Painterly — живописные мазки";
        }
    }
}
