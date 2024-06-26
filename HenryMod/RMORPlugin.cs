using BepInEx;
using RMORMod.Content.Shared.Components.Body;
using RMORMod.Content.RMORSurvivor;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using Survariants;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

//rename this namespace
namespace RMORMod
{
    [BepInDependency("pseudopulse.Survariants", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.EnforcerGang.HANDOverclocked", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.weliveinasociety.CustomEmotesAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.ThinkInvisible.ClassicItems", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Kingpinush.KingKombatArena", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    [R2APISubmoduleDependency(new string[]
    {
        "PrefabAPI",
        "SoundAPI",
        "UnlockableAPI",
        "RecalculateStatsAPI",
        "DamageAPI"
    })]

    public class RMORPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.MoriyaLuna.RMORReforged";
        public const string MODNAME = "RMOR Reforged";
        public const string MODVERSION = "1.4.0";

        public const string DEVELOPER_PREFIX = "MORIYA";

        public static RMORPlugin instance;
        public static PluginInfo pluginInfo;

        public static bool HANDLoaded = false;
        public static bool SurvariantsLoaded = false;
        public static bool ScepterStandaloneLoaded = false;
        public static bool ScepterClassicLoaded = false;
        public static bool EmoteAPILoaded = false;
        public static bool ArenaPluginLoaded = false;
        public static bool ArenaModeActive = false;
        public static bool RiskOfOptionsLoaded = false;

        private void Awake()
        {
            pluginInfo = Info;
            instance = this;

            CheckDependencies();
            Modules.Config.ReadConfig();

            Log.Init(Logger);
            Modules.Assets.Initialize(); // load assets and read config
            Modules.ItemDisplays.PopulateDisplays(); // collect item display prefabs for use in our display rules
            Modules.Projectiles.RegisterProjectiles(); // add and register custom projectiles

            new LanguageTokens();
            // survivor initialization
            //new MyCharacter().Initialize();

            new Content.Shared.SharedContent();
            Content.DamageTypes.Initialize();

            //new HANDSurvivor().Initialize();
            new RMORSurvivor().Initialize();

            // now make a content pack and add it- this part will change with the next update
            new Modules.ContentPacks().Initialize();

            if (EmoteAPILoaded) EmoteAPICompat();
            if (ArenaPluginLoaded)
            {
                Stage.onStageStartGlobal += SetArena;
            }

            RoR2.RoR2Application.onLoad += AddMechanicalBodies;
        }

        private void Start()
        {
            if (Modules.Config.addAsVariant && RMORPlugin.HANDLoaded && RMORPlugin.SurvariantsLoaded)
            {
                RoR2.RoR2Application.onLoad += AddRmorAsHandVariant;
            }
        }

        private void AddMechanicalBodies()
        {
            BodyIndex sniperClassicIndex = BodyCatalog.FindBodyIndex("SniperClassicBody");
            if (sniperClassicIndex != BodyIndex.None)
            {
                DroneStockController.mechanicalBodies.Add(sniperClassicIndex);
            }
        }

        private void CheckDependencies()
        {
            SurvariantsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("pseudopulse.Survariants");
            HANDLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.EnforcerGang.HANDOverclocked");
            ScepterStandaloneLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DestroyedClone.AncientScepter");
            ScepterClassicLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.ThinkInvisible.ClassicItems");
            EmoteAPILoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.weliveinasociety.CustomEmotesAPI");
            ArenaPluginLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Kingpinush.KingKombatArena");
            RiskOfOptionsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void SetArena(Stage obj)
        {
            RMORPlugin.ArenaModeActive = NS_KingKombatArena.KingKombatArenaMainPlugin.s_GAME_MODE_ACTIVE;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void EmoteAPICompat()
        {
            On.RoR2.SurvivorCatalog.Init += (orig) =>
            {
                orig();
                foreach (var item in SurvivorCatalog.allSurvivorDefs)
                {
                    if (item.bodyPrefab.name == "HANDOverclockedBody")
                    {
                        var skele = Modules.Assets.mainAssetBundle.LoadAsset<UnityEngine.GameObject>("animHANDEmote.prefab");
                        EmotesAPI.CustomEmotesAPI.ImportArmature(item.bodyPrefab, skele);
                        skele.GetComponentInChildren<BoneMapper>().scale = 1.5f;
                    }
                }
            };
        }

        private void AddRmorAsHandVariant()
        {
            SurvivorDef HANDSurvivorDef = SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(BodyCatalog.FindBodyIndex("HANDOverclockedBody")));
            SurvivorDef RMORSurvivorDef = SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(BodyCatalog.FindBodyIndex("RMORBody")));
            if (!HANDSurvivorDef || !RMORSurvivorDef)
            {
                return;
            }

            SurvivorVariantDef RMORVariant = ScriptableObject.CreateInstance<SurvivorVariantDef>();
            (RMORVariant as ScriptableObject).name = RMORSurvivorDef.cachedName;
            RMORVariant.name = RMORSurvivorDef.displayNameToken;
            RMORVariant.VariantSurvivor = RMORSurvivorDef;
            RMORVariant.TargetSurvivor = HANDSurvivorDef;
            RMORVariant.RequiredUnlock = UnlockableCatalog.GetUnlockableDef("MoriyaRMORSurvivorUnlock");
            RMORVariant.Description = RMORPlugin.DEVELOPER_PREFIX + "_RMOR_BODY_SUBTITLE";

            RMORSurvivorDef.hidden = true;
            SurvivorVariantCatalog.AddSurvivorVariant(RMORVariant);
        }
    }
}