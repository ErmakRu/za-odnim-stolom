using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace SummonersTable.Editor
{
    // Build saved content without running legacy prefab/config generators.
    public static class AuthoredPlayerBuild
    {
        internal static string ReleaseArtifacts;
        public static string Begin()=>Begin(false);
        public static string BeginRelease()=>Begin(true);
        static string Begin(bool release)
        {
            if(EditorApplication.isPlaying||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor must be idle");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save modified scenes first");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled&&(!release||!s.path.EndsWith("PresentationLab.unity"))).Select(s=>s.path).ToArray();
            if(scenes.Length==0||!scenes[0].EndsWith("MainMenu.unity"))throw new InvalidOperationException("MainMenu must be first");
            string run=Path.GetFullPath("../Builds/"+(release?"Release-":"Windows-")+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            string output=release?Path.Combine(run,"ZaOdnimStolom"):run;
            string status=Path.Combine(run,"build-status.txt");
            Directory.CreateDirectory(output);File.WriteAllText(status,"BUILDING");
            EditorApplication.delayCall+=()=>
            {
                try
                {
                    ReleaseArtifacts=release?Path.Combine(run,"DeveloperArtifacts"):null;
                    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(output,"ZaOdnimStolom.exe"),target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.CompressWithLz4HC:BuildOptions.None});
                    if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build "+report.summary.result+" errors="+report.summary.totalErrors);
                    File.WriteAllText(Path.Combine(output,"steam_appid.txt"),"480\n");
                    Directory.CreateDirectory(Path.Combine(output,"Config"));
                    foreach(var file in Directory.GetFiles(ConfigAuthoring.Folder,"*.json"))File.Copy(file,Path.Combine(output,"Config",Path.GetFileName(file)),true);
                    foreach(var name in release?new[]{"THIRD-PARTY.txt"}:new[]{"PLAYTEST-RU.txt","THIRD-PARTY.txt"}){string source="../docs/"+name;if(File.Exists(source))File.Copy(source,Path.Combine(output,name),true);}
                    if(release)
                    {
                        // Symbols are generated even by non-development Mono/Burst builds.
                        // Archive them outside the distributable instead of shipping them.
                        string excluded=Path.Combine(run,"DeveloperArtifacts");
                        foreach(var folder in Directory.GetDirectories(output,"*",SearchOption.TopDirectoryOnly).Where(x=>(Path.GetFileName(x).Contains("DoNotShip")||Path.GetFileName(x).Contains("DontShip"))))
                        {Directory.CreateDirectory(excluded);Directory.Move(folder,Path.Combine(excluded,Path.GetFileName(folder)));}
                        foreach(var file in Directory.GetFiles(output,"*",SearchOption.AllDirectories).Where(x=>new[]{".pdb",".mdb",".log"}.Contains(Path.GetExtension(x).ToLowerInvariant())))
                        {string target=Path.Combine(excluded,file.Substring(output.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Move(file,target);}
                    }
                    File.WriteAllText(status,"SUCCEEDED\nUnity "+Application.unityVersion+"\nBytes "+report.summary.totalSize+"\nSeconds "+report.summary.totalTime.TotalSeconds+"\nWarnings "+report.summary.totalWarnings+"\nCompression "+(release?"LZ4HC":"Default")+"\nScenes\n"+string.Join("\n",scenes));
                    Debug.Log("AUTHORED_BUILD_SUCCEEDED "+output);
                }
                catch(Exception error){File.WriteAllText(status,"FAILED\n"+error);Debug.LogException(error);}
                finally{ReleaseArtifacts=null;}
            };
            return output;
        }
    }
}
