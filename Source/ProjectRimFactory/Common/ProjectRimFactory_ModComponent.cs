﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using SimpleFixes;
using ProjectRimFactory.Storage;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModComponent : Mod
    {
        public ProjectRimFactory_ModComponent(ModContentPack content) : base(content)
        {
            try
            {
                ProjectRimFactory_ModSettings.LoadXml(content);
                this.Settings = GetSettings<ProjectRimFactory_ModSettings>();
                this.HarmonyInstance = new Harmony("com.spdskatr.projectrimfactory");
                this.HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message($"Project RimFactory Core {typeof(ProjectRimFactory_ModComponent).Assembly.GetName().Version} - Harmony patches successful");
                NoMessySpawns.Instance.Add(ShouldSuppressDisplace, (Building_MassStorageUnit b, Map map) => true);
                availableSpecialSculptures = SpecialSculpture.LoadAvailableSpecialSculptures(content);
                LoadModSupport();

            }
            catch (Exception ex)
            {
                Log.Error("Project RimFactory Core :: Caught exception: " + ex);
            }
        }

        //Mod Support
        //Cached MethodInfo as Reflection is Slow
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_GetFridgeCache = null;
        public static System.Reflection.MethodInfo ModSupport_RrimFridge_HasFridgeAt = null;
        public static bool ModSupport_RrimFrige_Dispenser = false;

        public static System.Reflection.MethodInfo ModSupport_ReserchPal_ResetLayout = null;
        public static bool ModSupport_ReserchPal = false;
        
        private void LoadModSupport()
        {
            if (ModLister.HasActiveModWithName("[KV] RimFridge"))
            {
                ModSupport_RrimFridge_GetFridgeCache = AccessTools.Method("RimFridge.FridgeCache:GetFridgeCache");
                ModSupport_RrimFridge_HasFridgeAt = AccessTools.Method("RimFridge.FridgeCache:HasFridgeAt");
                if (ModSupport_RrimFridge_GetFridgeCache != null && ModSupport_RrimFridge_HasFridgeAt != null)
                {
                    Log.Message("Project Rimfactory - added Support for shared Nutrient Dispenser with [KV] RimFridge");
                    ModSupport_RrimFrige_Dispenser = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for shared Nutrient Dispenser with [KV] RimFridge");
                }

            }
            if (ModLister.HasActiveModWithName("ResearchPal - Forked"))
            {
                ModSupport_ReserchPal_ResetLayout = AccessTools.Method("ResearchPal.Tree:ResetLayout");
                if (ModSupport_ReserchPal_ResetLayout != null)
                {
                    Log.Message("Project Rimfactory - added Support for ResearchPal when using Lite Mode");
                    ModSupport_ReserchPal = true;
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for ResearchPal when using Lite Mode");
                }


            }
            if (ModLister.HasActiveModWithName("Locks 2: Lock Them Out!"))
            {
                MethodBase methodBase = null;
                Type ConfigRuleRace = Type.GetType("Locks2.Core.LockConfig, Locks2", false).GetNestedType("ConfigRuleRace");

                if (ConfigRuleRace != null)
                {
                    methodBase = AccessTools.Method(ConfigRuleRace, "Allows");
                }

                if (methodBase != null)
                {
                    var prefix = typeof(HarmonyPatches.Patch_Locks2_ConfigRuleRace_Allows).GetMethod("Prefix");
                    var postfix = typeof(HarmonyPatches.Patch_Locks2_ConfigRuleRace_Allows).GetMethod("Postfix");
                    this.HarmonyInstance.Patch(methodBase, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

                    Log.Message("Project Rimfactory - added Support for Locks 2: Lock Them Out!");
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to added Support for Locks 2: Lock Them Out!");
                }
            }


        }


        public Harmony HarmonyInstance { get; private set; }
 
        public ProjectRimFactory_ModSettings Settings { get; private set; }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "ProjectRimFactoryModName".Translate();
        }

        public override void WriteSettings()
        {
            this.Settings.Apply();
            Settings.Write();
            if (this.Settings.RequireReboot)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PRF.Settings.RequireReboot".Translate(), () => GenCommandLine.Restart()));
            }
        }

        public static bool ShouldSuppressDisplace(IntVec3 cell, Map map, bool respawningAfterLoad)
        {
            return !respawningAfterLoad || map?.thingGrid.ThingsListAtFast(cell).OfType<Building_MassStorageUnit>().Any() != true;
        }
        // I am happy enough to make this static; it's not like there will be more than once
        //   instance of the mod loaded or anything.
        public static List<SpecialSculpture> availableSpecialSculptures; // loaded on startup in SpecialScupture; see above
    }
}
