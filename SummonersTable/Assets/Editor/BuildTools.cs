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
        public static void Validate(){CoreTests.Run();PresentationTests.Run();}
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
            ProjectScaffolder.Generate();
            PresentationTests.Run();
            // Legacy project settings had an empty list: uGUI's runtime default material
            // then referenced a stripped shader in Player even though Editor rendered it.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var included=graphics.FindProperty("m_AlwaysIncludedShaders");
            for(int i=included.arraySize-1;i>=0;i--)
                if(included.GetArrayElementAtIndex(i).objectReferenceValue is Shader oldShader&&(oldShader.hideFlags&HideFlags.DontSave)!=0)
                {included.GetArrayElementAtIndex(i).objectReferenceValue=null;included.DeleteArrayElementAtIndex(i);}
            foreach(string name in new[]{"UI/Default"})
            {
                var shader=Shader.Find(name);if(shader==null)throw new Exception("Required UI shader missing: "+name);
                bool exists=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
                if(!exists){int index=included.arraySize;included.InsertArrayElementAtIndex(index);included.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();graphics.Dispose();
            PlayerSettings.companyName="GameJams";PlayerSettings.productName="Za odnim stolom";PlayerSettings.bundleVersion="0.3.1";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            string output=Path.GetFullPath("../Builds/Windows-v0.3.1");Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ProjectScaffolder.ScenePaths,
                locationPathName=Path.Combine(output,"ZaOdnimStolom.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText(Path.Combine(output,"steam_appid.txt"),"480\n");
            string pdf=Path.GetFullPath("../output/pdf/arena-test-decks-v0.3.pdf");
            if(File.Exists(pdf))File.Copy(pdf,Path.Combine(output,"Cards-and-rules-RU.pdf"),true);
            string readme=Path.GetFullPath("../docs/PLAYTEST-RU.txt");if(File.Exists(readme))File.Copy(readme,Path.Combine(output,"READ-ME-RU.txt"),true);
            string licenses=Path.GetFullPath("../docs/THIRD-PARTY.txt");if(File.Exists(licenses))File.Copy(licenses,Path.Combine(output,"THIRD-PARTY.txt"),true);
            Debug.Log("BUILD_SUCCEEDED "+output+" bytes="+report.summary.totalSize);
        }
    }
}
