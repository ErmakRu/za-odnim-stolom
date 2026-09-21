using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SummonersTable.Editor
{
    public static class BuildTools
    {
        [MenuItem("Summoners Table/Validate rules")]
        public static void Validate(){CoreTests.Run();}
        [MenuItem("Summoners Table/Build Windows")]
        public static void BuildWindows()
        {
            CoreTests.Run();
            foreach(string path in AssetDatabase.GetAllAssetPaths())
            {
                if(!path.StartsWith("Assets/Resources/Art/")||!path.EndsWith(".png"))continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
                importer.alphaIsTransparency=false;importer.isReadable=false;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.maxTextureSize=path.EndsWith("menu.png")||path.EndsWith("board.png")?2048:1024;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            Directory.CreateDirectory("Assets/Scenes");
            var scene=File.Exists("Assets/Scenes/Main.unity")?EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"):
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(camera==null)camera=new GameObject("Table Camera").AddComponent<Camera>();
            camera.name="Table Camera";camera.orthographic=false;camera.tag="MainCamera";
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.06f,.08f);
            if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();
            if(UnityEngine.Object.FindFirstObjectByType<TableBoard>(FindObjectsInactive.Include)==null)
                new GameObject("3D Table — assign replacement prefabs here").AddComponent<TableBoard>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
            PlayerSettings.companyName="GameJams";PlayerSettings.productName="Za odnim stolom";PlayerSettings.bundleVersion="0.2.0";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            string output=Path.GetFullPath("../Builds/Windows-v0.2.0");Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},
                locationPathName=Path.Combine(output,"ZaOdnimStolom.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText(Path.Combine(output,"steam_appid.txt"),"480\n");
            string pdf=Path.GetFullPath("../output/pdf/arena-test-decks-v0.2.pdf");
            if(File.Exists(pdf))File.Copy(pdf,Path.Combine(output,"Cards-and-rules-RU.pdf"),true);
            string readme=Path.GetFullPath("../docs/PLAYTEST-RU.txt");if(File.Exists(readme))File.Copy(readme,Path.Combine(output,"READ-ME-RU.txt"),true);
            string licenses=Path.GetFullPath("../docs/THIRD-PARTY.txt");if(File.Exists(licenses))File.Copy(licenses,Path.Combine(output,"THIRD-PARTY.txt"),true);
            Debug.Log("BUILD_SUCCEEDED "+output+" bytes="+report.summary.totalSize);
        }
    }
}
