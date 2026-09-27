using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace SummonersTable.Editor
{
    // Applies to File > Build as well as the authored build commands.
    public sealed class SteamPlayerSetup : IPostprocessBuildWithReport
    {
        public int callbackOrder=>1000;
        public void OnPostprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.StandaloneWindows64)return;
            string file=Path.Combine(Path.GetDirectoryName(report.summary.outputPath),"steam_appid.txt");
            if(!File.Exists(file))File.WriteAllText(file,"480\n");
        }
    }
}
