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
        public static void Validate(){ConfigTests.Run();CoreTests.Run();PresentationTests.Run();PrefabTests.Run();ShaderStyleTests.Run();TableScaleAuthoring.Validate();RuleOptionsTests.Run();}
        [MenuItem("Summoners Table/Build Windows")]
        public static void BuildWindows()
        {
            ConfigAuthoring.BuildAssetRegistry();ConfigAuthoring.CopyDefaults();ConfigAuthoring.SyncCards();ConfigTests.Run();
            PrefabAuthoring.Ensure();
            ShaderStyleTests.Run();
            TableScaleAuthoring.Validate();RuleOptionsTests.Run();
            PrefabTests.Run();
            CoreTests.Run();
            foreach(string path in AssetDatabase.GetAllAssetPaths())
            {
                if(!path.StartsWith("Assets/Resources/Art/")||!path.EndsWith(".png"))continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=!path.EndsWith("menu.png")&&!path.EndsWith("board.png");
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
            PlayerSettings.companyName="GameJams";PlayerSettings.productName="Za odnim stolom";PlayerSettings.bundleVersion="0.7.0";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ManagedStrippingLevel.Low);
            AssetDatabase.SaveAssets();
            string output=Path.GetFullPath("../Builds/Windows-v0.7.0");Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ProjectScaffolder.ScenePaths,
                locationPathName=Path.Combine(output,"ZaOdnimStolom.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText(Path.Combine(output,"steam_appid.txt"),"480\n");
            Directory.CreateDirectory(Path.Combine(output,"Config"));foreach(string file in Directory.GetFiles(ConfigAuthoring.Folder,"*.json"))File.Copy(file,Path.Combine(output,"Config",Path.GetFileName(file)),true);
            string reference=Path.GetFullPath("../output/pdf/config-reference-v0.7.pdf");if(File.Exists(reference))File.Copy(reference,Path.Combine(output,"Config-reference-RU.pdf"),true);
            string pdf=Path.GetFullPath("../output/pdf/arena-test-decks-v0.6.pdf");
            if(File.Exists(pdf))File.Copy(pdf,Path.Combine(output,"Cards-and-rules-RU.pdf"),true);
            string readme=Path.GetFullPath("../docs/PLAYTEST-RU.txt");if(File.Exists(readme))File.Copy(readme,Path.Combine(output,"READ-ME-RU.txt"),true);
            string licenses=Path.GetFullPath("../docs/THIRD-PARTY.txt");if(File.Exists(licenses))File.Copy(licenses,Path.Combine(output,"THIRD-PARTY.txt"),true);
            Debug.Log("BUILD_SUCCEEDED "+output+" bytes="+report.summary.totalSize);
        }
    }
}
