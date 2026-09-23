#if UNITY_EDITOR
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        public static System.Action<string> EditorCapture;
        public bool EditorConfigSmokeDone {get;private set;}
        public bool EditorRegressionSmokeDone {get;private set;}
        public void BeginEditorConfigSmoke()
        {UserSettings.TestPath=System.IO.Path.GetFullPath("../tmp/config-work/play-user-settings.json");UserSettings.Load();editorSmokeNoCapture=true;editorSmokeFrames=0;captureMode=true;StartCoroutine(CaptureConfigPreview());}
        public void BeginEditorRegressionSmoke()
        {
            editorSmokeFrames=0;localOptions=ConfigRuntime.Current.rules.defaults.Copy();local=null;state=null;page="menu";ClearSelection();
            StartCoroutine(CapturePreview());
        }
    }
}
#endif
