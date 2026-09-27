using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace SummonersTable.Editor
{
    // Performance Testing creates these Resources even when tests are not included.
    // Run after its callback, before resource packing, and keep metadata outside Assets.
    public sealed class ReleaseTestDataFilter : IPreprocessBuildWithReport
    {
        public int callbackOrder=>10000;
        public void OnPreprocessBuild(BuildReport report)
        {
            string excluded=AuthoredPlayerBuild.ReleaseArtifacts;if(string.IsNullOrEmpty(excluded))return;
            Directory.CreateDirectory(excluded);
            foreach(string name in new[]{"PerformanceTestRunInfo.json","PerformanceTestRunSettings.json"})
                foreach(string suffix in new[]{"",".meta"})
                {
                    string source="Assets/Resources/"+name+suffix;
                    if(File.Exists(source))File.Move(source,Path.Combine(excluded,name+suffix));
                }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
