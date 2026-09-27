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
        public static string Begin()
        {
            if(EditorApplication.isPlaying||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor must be idle");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save modified scenes first");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if(scenes.Length==0||!scenes[0].EndsWith("MainMenu.unity"))throw new InvalidOperationException("MainMenu must be first");
            string output=Path.GetFullPath("../Builds/Windows-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"build-status.txt"),"BUILDING");
            EditorApplication.delayCall+=()=>
            {
                try
                {
                    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(output,"ZaOdnimStolom.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                    if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build "+report.summary.result+" errors="+report.summary.totalErrors);
                    File.WriteAllText(Path.Combine(output,"steam_appid.txt"),"480\n");
                    Directory.CreateDirectory(Path.Combine(output,"Config"));
                    foreach(var file in Directory.GetFiles(ConfigAuthoring.Folder,"*.json"))File.Copy(file,Path.Combine(output,"Config",Path.GetFileName(file)),true);
                    foreach(var name in new[]{"PLAYTEST-RU.txt","THIRD-PARTY.txt"}){string source="../docs/"+name;if(File.Exists(source))File.Copy(source,Path.Combine(output,name),true);}
                    File.WriteAllText(Path.Combine(output,"build-status.txt"),"SUCCEEDED\nUnity "+Application.unityVersion+"\nBytes "+report.summary.totalSize+"\nSeconds "+report.summary.totalTime.TotalSeconds+"\nWarnings "+report.summary.totalWarnings+"\nScenes\n"+string.Join("\n",scenes));
                    Debug.Log("AUTHORED_BUILD_SUCCEEDED "+output);
                }
                catch(Exception error){File.WriteAllText(Path.Combine(output,"build-status.txt"),"FAILED\n"+error);Debug.LogException(error);}
            };
            return output;
        }
    }
}
