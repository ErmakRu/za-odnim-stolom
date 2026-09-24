using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SummonersTable.Editor
{
    public static partial class PrefabAuthoring
    {
        static void CreateSpellEffects()
        {
            string folder=Root+"Spells/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            const string lana="Assets/ThirdParty/VFX/Lana Studio/Hyper Casual FX/Prefabs/";
            var fire=TavernSetup.PrepareEffect("Attack trail","Assets/ThirdParty/VFX/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Fire.prefab");
            var purple=TavernSetup.PrepareEffect("Spell purple trail","Assets/ThirdParty/VFX/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Void.prefab");
            var heal=TavernSetup.PrepareEffect("Spell healing ring",lana+"Area/Area_heal_green.prefab");
            var stars=TavernSetup.PrepareEffect("Spell dizzy stars",lana+"Area/Area_star_ellow.prefab");
            var sparkle=TavernSetup.PrepareEffect("Spell gold flash",lana+"Shine/Shine_ellow.prefab");
            var pop=TavernSetup.PrepareEffect("Hit flash",lana+"Flash/Flash_star_ellow_purple.prefab");
            var magic=TavernSetup.PrepareEffect("Spell violet flash",lana+"Flash/Flash_blue_purple.prefab");
            var smoke=TavernSetup.PrepareEffect("Death smoke","Assets/ThirdParty/VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Smoke.prefab");
            var fxFire=TavernSetup.PrepareEffect("Spell flame burst","Assets/ThirdParty/VFX/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Fire.prefab");
            var modes=new[]{SpellEffect.Motion.Projectile,SpellEffect.Motion.Area,SpellEffect.Motion.Projectile,SpellEffect.Motion.Area,SpellEffect.Motion.Exchange,SpellEffect.Motion.Draw,SpellEffect.Motion.Boost,SpellEffect.Motion.Return};
            var travel=new[]{fire,heal,sparkle,stars,purple,sparkle,sparkle,smoke};var impact=new[]{fxFire,heal,pop,stars,magic,sparkle,sparkle,smoke};
            var launches=new[]{"Abilities_Air_Puff_Short_01","Abilities_Water_Enhance_01","Abilities_Air_01","UI_Notification_Silly_Squawk_01","Abilities_Air_02","Abilities_Air_Glitter_01","Abilities_Wind_Upgrade_01","Abilities_Poof_Descend_01"};
            var hits=new[]{"Abilities_Pop_Big_01","Abilities_Wind_Shimmer_01","Abilities_Pop_Big_02","UI_Notification_Ding_01","Abilities_Air_03","UI_Notification_Ding_02","Abilities_Wind_Shimmer_02","Abilities_Poof_01"};
            for(int i=0;i<8;i++)
            {
                string id="S"+(i+1).ToString("00"),path=folder+id+".prefab";
                SpellEffect asset=null;
                if(!File.Exists(path))
                {
                    var obj=new GameObject(id+" — "+library.Find(id).definition.name);var fx=obj.AddComponent<SpellEffect>();fx.motion=modes[i];fx.travelPrefab=travel[i];fx.impactPrefab=impact[i];fx.cardBackPrefab=library.cardBackPrefab;
                    fx.travelScale=i==2?.11f:.2f;fx.impactScale=i==0?.24f:i==1?.55f:.35f;fx.persistentPrefab=i==3?stars:null;fx.persistentScale=.13f;
                    fx.launchSound=FindSound(launches[i]);fx.impactSound=FindSound(hits[i]);fx.sound=obj.AddComponent<AudioSource>();fx.sound.playOnAwake=false;obj.AddComponent<EffectsVolume>().baseVolume=.18f;
                    asset=Save(obj,path).GetComponent<SpellEffect>();
                }
                else asset=AssetDatabase.LoadAssetAtPath<SpellEffect>(path);
                string cardPath=AssetDatabase.GetAssetPath(library.Find(id));var card=PrefabUtility.LoadPrefabContents(cardPath);card.GetComponent<CardView>().spellEffect=asset;PrefabUtility.SaveAsPrefabAsset(card,cardPath);PrefabUtility.UnloadPrefabContents(card);
            }
        }
        static AudioClip FindSound(string suffix)
        {string path=AssetDatabase.GetAllAssetPaths().First(p=>p.EndsWith("Card_Game_"+suffix+".wav"));return AssetDatabase.LoadAssetAtPath<AudioClip>(path);}
        [MenuItem("Summoners Table/Config/Refresh card prefab previews from JSON")]
        public static void ExportCards()
        {
            var catalog=ConfigBundle.Read(ConfigAuthoring.Folder).Catalog();catalog.Validate();
            foreach(var entry in Resources.Load<CardLibrary>("CardLibrary").cards)
            {
                string path=AssetDatabase.GetAssetPath(entry);var root=PrefabUtility.LoadPrefabContents(path);root.GetComponent<CardView>().Import(catalog.Card(entry.definition.id),catalog);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.Refresh();
        }
    }
}
