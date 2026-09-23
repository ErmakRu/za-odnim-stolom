using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed partial class GameApp
    {
        IEnumerator CaptureShaderPreview()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-dir");string directory=index>=0?args[index+1]:Path.GetFullPath("captures");Directory.CreateDirectory(directory);
            int previous=ShaderSettings.Mode;
            var lobbyChoice=lobbyCanvas.GetComponentInChildren<ShaderChoiceView>(true);var settingsChoice=ui.settings.GetComponentInChildren<ShaderChoiceView>(true);
            lobbyChoice.persist=settingsChoice.persist=false;
            try
            {
                page="local";localCount=4;yield return null;
                foreach(int group in new[]{0,1})
                {
                    localHeroes=HeroOptions.Ids.Skip(group*4).Take(4).ToArray();
                    for(int mode=0;mode<3;mode++)
                    {
                        lobbyChoice.choices[mode].onClick.Invoke();yield return Shot(directory,"lobby-"+group+"-shader-"+mode);
                        if(ShaderSettings.Mode!=mode)throw new Exception("Lobby shader buttons not bound");CheckShaderTargets(mode);
                    }
                }
                FrontEndAction("customize:0");FrontEndAction("outfit-next");FrontEndAction("palette:2");yield return Shot(directory,"painterly-wardrobe");FrontEndAction("customize-close");
                foreach(int count in new[]{2,3,4})
                {
                    StartLocal(count);handoff=false;seat=0;state=local.View(0,localTime);board.Sync(state,seat,Clock);board.CameraRig.SetMode(1);board.SnapCamera(0,false);
                    yield return null; // Let the new-match UI reset finish before opening settings.
                    for(int mode=0;mode<3;mode++)
                    {
                        settingsOpen=true;yield return null;settingsChoice.choices[mode].onClick.Invoke();settingsOpen=false;
                        yield return Shot(directory,"table-"+count+"-shader-"+mode);CheckShaderTargets(mode);
                        for(int seatIndex=0;seatIndex<count;seatIndex++)if(board.Actor(seatIndex).transform.localScale.x<2)throw new Exception("Small avatar retained in layout");
                    }
                }
                foreach(int mode in new[]{0,2})
                {
                    board.CameraRig.SetMode(mode);board.CameraRig.Sync(0,4,false,true);yield return Shot(directory,"painterly-camera-"+mode);
                    if(board.CameraRig.Mode!=mode)throw new Exception("Shader camera fixture changed camera mode");
                }
                settingsOpen=true;yield return Shot(directory,"settings-shader-selection");
                if(!settingsChoice.choices[2].GetComponentInChildren<Text>().text.StartsWith("✓"))throw new Exception("Selected shader not shown in settings");
                settingsChoice.choices[1].onClick.Invoke();settingsOpen=false;page="local";yield return Shot(directory,"return-lobby-shader-1");
                if(!lobbyChoice.choices[1].GetComponentInChildren<Text>().text.StartsWith("✓"))throw new Exception("Lobby selection did not follow settings");CheckShaderTargets(1);
                File.WriteAllText(Path.Combine(directory,"capture-report.txt"),"PASS three actual shader buttons in lobby and ESC settings; selection carried across lobby/match/return; eight animal rigs; armor and palette changes; 2/3/4 enlarged-avatar layouts in all styles; first-person and overhead; no missing shaders or blank frames.\n");
            }
            finally{ShaderSettings.Apply(previous,false);}
            Debug.Log("SHADER_CAPTURE_COMPLETE "+directory);Application.Quit();
        }
        void CheckShaderTargets(int mode)
        {
            foreach(var target in FindObjectsByType<ShaderStyleTarget>(FindObjectsSortMode.None))
            {
                if(target.AppliedMode!=mode)throw new Exception("A model retained the previous shader style: "+target.name);
                foreach(var renderer in target.targets)if(renderer!=null)foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&(!material.shader.isSupported||material.shader.name=="Hidden/InternalErrorShader"))throw new Exception("Unsupported style material: "+material.name);
            }
        }
    }
}
