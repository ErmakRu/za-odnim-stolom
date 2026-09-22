using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        static void FinalizeInterface()
        {
            string path=Root+"UI/GameInterface.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            if(root.transform.Find("Screen background camera")==null)
            {var camera=new GameObject("Screen background camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.depth=-100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.055f);camera.cullingMask=0;PrefabUtility.SaveAsPrefabAsset(root,path);}
            PrefabUtility.UnloadPrefabContents(root);
            path=Root+"UI/PlayerStatus.prefab";root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            foreach(var img in new[]{root.GetComponent<PlayerStatusView>().healthFill,root.GetComponent<PlayerStatusView>().healthTrail})
            {if(img.sprite!=null)continue;img.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");changed=true;}
            if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static void UpgradeCardCanvas()
        {
            string path="Assets/Prefabs/CardTableCanvas.prefab";var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponent<CardTableCanvas>();
            foreach(var slot in root.GetComponentsInChildren<CardDisplaySlot>(true))
            {foreach(Transform child in slot.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);slot.library=library;}
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static void UpgradeWardrobe()
        {
            string path="Assets/Prefabs/LobbyCanvas.prefab";var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponent<FrontEndCanvas>();
            var parent=(RectTransform)ui.appearancePanel.transform;parent.sizeDelta=new Vector2(345,170);parent.anchoredPosition=new Vector2(-558,-263);
            foreach(string name in new[]{"Modal shade","Wardrobe panel"}){var child=parent.Find(name);if(child!=null)Object.DestroyImmediate(child.gameObject);}
            var title=(RectTransform)ui.appearanceTitle.transform;title.anchoredPosition=new Vector2(0,56);title.sizeDelta=new Vector2(325,28);ui.appearanceTitle.fontSize=18;ui.appearanceTitle.resizeTextMaxSize=18;
            foreach(var button in ui.buttons.Where(b=>b.transform.IsChildOf(parent)))
            {
                string action=ui.actions[System.Array.IndexOf(ui.buttons,button)];var r=(RectTransform)button.transform;var label=button.GetComponentInChildren<Text>();
                if(action.StartsWith("outfit")){r.anchoredPosition=new Vector2(action.EndsWith("prev")?-124:124,12);r.sizeDelta=new Vector2(80,35);label.text=action.EndsWith("prev")?"‹":"›";}
                else if(action.StartsWith("palette")){int i=int.Parse(action.Split(':')[1]);r.anchoredPosition=new Vector2(-102+i*68,-30);r.sizeDelta=new Vector2(61,30);}
                else {r.anchoredPosition=new Vector2(0,-71);r.sizeDelta=new Vector2(296,32);label.text="ПОДТВЕРДИТЬ ОБЛИК";}
                ((RectTransform)label.transform).sizeDelta=r.sizeDelta;label.fontSize=16;label.resizeTextMaxSize=16;
            }
            ui.CloseAppearance();PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        static void UpgradeWorld()
        {
            var arrow=new GameObject("Attack intention arrow");var a=arrow.AddComponent<WorldArrowView>();a.line=arrow.AddComponent<LineRenderer>();var tip=new GameObject("Arrow head");tip.transform.SetParent(arrow.transform,false);a.head=tip.AddComponent<LineRenderer>();
            foreach(var line in new[]{a.line,a.head}){line.sharedMaterial=Mat("Arrow",Color.white);line.useWorldSpace=true;line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            var arrowPrefab=Save(arrow,Root+"World/AttackArrow.prefab").GetComponent<WorldArrowView>();
            var number=new GameObject("Floating health number");var text=number.AddComponent<TextMesh>();text.fontSize=70;text.characterSize=.07f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.35f,.25f);text.text="−4";var numberPrefab=Save(number,Root+"World/HealthNumber.prefab").GetComponent<TextMesh>();
            string path="Assets/Prefabs/TableWorld.prefab";var root=PrefabUtility.LoadPrefabContents(path);var board=root.GetComponent<TableBoard>();board.cardLibrary=library;board.arrowPrefab=arrowPrefab;board.numberPrefab=numberPrefab;
            if(root.GetComponent<AudioSource>()==null)root.AddComponent<AudioSource>().playOnAwake=false;if(root.GetComponent<EffectsVolume>()==null)root.AddComponent<EffectsVolume>().baseVolume=.1f;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
