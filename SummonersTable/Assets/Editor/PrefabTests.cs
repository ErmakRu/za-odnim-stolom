using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static class PrefabTests
    {
        static void Check(bool value,string reason){if(!value)throw new Exception("Prefab validation: "+reason);}
        public static void Run()
        {
            var library=Resources.Load<CardLibrary>("CardLibrary");var catalog=CardLibrary.LoadCatalog();catalog.Validate();
            Check(catalog.rules.rounds==1,"one round");Check(library.cards.Length==30,"30 variants");
            foreach(var card in library.cards)
            {
                var parent=PrefabUtility.GetCorrespondingObjectFromSource(card.gameObject);
                Check(parent!=null&&AssetDatabase.GetAssetPath(parent).Contains("/Types/"),card.name+" must inherit type");
                var grand=PrefabUtility.GetCorrespondingObjectFromSource(parent);
                Check(grand!=null&&AssetDatabase.GetAssetPath(grand).EndsWith("/CardBase.prefab"),card.name+" must inherit CardBase");
                Check(card.fullArtwork.texture==Resources.Load<Texture2D>("Art/"+card.definition.id),"full artwork "+card.name);
                Check(card.worldArtwork.sharedMaterial.mainTexture==card.fullArtwork.texture,"table artwork "+card.name);
                Check(card.fullName.text==card.definition.name,"authoritative definition and label "+card.name);
                if(card.definition.kind=="spell")
                {
                    var fx=card.spellEffect;Check(fx!=null&&PrefabUtility.IsPartOfPrefabAsset(fx),"spell effect prefab "+card.name);
                    Check(fx.launchSound!=null&&fx.impactSound!=null&&fx.impactPrefab!=null&&fx.travelPrefab!=null,"existing VFX/audio "+card.name);
                    Check(fx.impactPrefab.GetComponentsInChildren<ParticleSystem>(true).Length>0,"particle asset "+card.name);
                    Check(fx.travelPrefab.GetComponentsInChildren<AudioSource>(true).Length==0&&fx.impactPrefab.GetComponentsInChildren<AudioSource>(true).Length==0,"imported VFX audio must not bypass effects volume "+card.name);
                }
            }
            var ui=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Editable/UI/GameInterface.prefab").GetComponent<PrefabInterface>();
            Check(ui.playerStatus.Length==4&&ui.hand.cardSlotPrefab!=null&&ui.history.rowPrefab!=null,"nested runtime prefabs");
            Check(ui.settings.Get<UnityEngine.UI.Slider>("master")!=null&&ui.settings.Get<UnityEngine.UI.Slider>("effects")!=null,"volume sliders");
            foreach(var file in Directory.GetFiles("Assets/Prefabs/Editable","*.prefab",SearchOption.AllDirectories))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\','/'));
                foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"missing script "+file);
            }
            Directory.CreateDirectory("../output/tests");File.WriteAllText("../output/tests/prefab-tests.txt","PASS 30 actual Prefab Variants: CardBase -> type -> card; full/table art and definitions agree; 8 spell effect prefabs reference existing particles and two audio clips each; nested hand/history/player labels; two audio sliders; all editable prefabs without missing scripts.\n");
            Debug.Log("ALL_PREFAB_TESTS_PASSED");
        }
    }
}
