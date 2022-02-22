#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace CF_AnimalMeleeEnhance
{
    public class Patcher : Mod
    {
        public static Settings Settings = new();
        string? meleeDodgeChanceBuffer;
        string? meleeMeleeHitChanceBuffer;

        public Patcher(ModContentPack pack) : base(pack)
        {
            Settings = GetSettings<Settings>();
            DoPatching();
        }
        public override string SettingsCategory()
        {
            return "Animal Melee Enhance";
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            {
                var rect = list.Label(StatDefOf.MeleeDodgeChance.label, tooltip: "Additional dodge chance for animals trained with Obedience. 15 is similar to Nimble trait");
                Widgets.TextFieldNumeric(rect.RightPartPixels(50), ref Settings.MeleeDodgeChance, ref meleeDodgeChanceBuffer, -100, 100);
            }
            {
                var rect = list.Label(StatDefOf.MeleeHitChance.label, tooltip: "Additional hit chance for animals trained with Release. 4 is similar to Brawler trait");
                Widgets.TextFieldNumeric(rect.RightPartPixels(50), ref Settings.MeleeHitChance, ref meleeMeleeHitChanceBuffer, -100, 100);
            }

            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public void DoPatching()
        {
            var harmony = new Harmony("com.colinfang.AnimalMeleeEnhance");
            harmony.PatchAll();
        }
    }

    public class Settings : ModSettings
    {
        public float MeleeDodgeChance;
        public float MeleeHitChance;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref MeleeDodgeChance, "MeleeDodgeChance", 10);
            Scribe_Values.Look(ref MeleeHitChance, "MeleeHitChance", 3);
            base.ExposeData();
        }
    }


    public class StatPart_TrainedAnimal: StatPart
    {
        public List<StatModifier> statOffsets = new();

        public bool IsValid(Pawn pawn) => pawn.Faction is not null && pawn.Faction.IsPlayer && pawn.RaceProps.Animal && pawn.training is not null && IsTrained(parentStat, pawn.training);
        public bool IsTrained(StatDef stat, Pawn_TrainingTracker training)
        {
            if (stat == StatDefOf.MeleeDodgeChance)
            {
                return training.HasLearned(TrainableDefOf.Obedience);

            } else if (stat == StatDefOf.MeleeHitChance)
            {
                return training.HasLearned(TrainableDefOf.Release);
            }
            return false;
        }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is not Pawn pawn || !IsValid(pawn))
            {
                return;
            }

            val += statOffsets.GetStatValueFromList(parentStat, 0);

        }
        public override string? ExplanationPart(StatRequest req)
        {
            if (req.Thing is not Pawn pawn || !IsValid(pawn))
            {
                return null;
            }
            var offset = statOffsets.GetStatValueFromList(parentStat, 0);
            if (offset == 0)
            {
                return null;
            }

            string text = $"Training: +{offset:F1}";
            return text;
        }
    }

    [HarmonyPatch(typeof(DefGenerator))]
    [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static class Patch_DefGenerator_GenerateImpliedDefs_PreResolve
    {
        public static void AddStatpart(StatDef stat, float value)
        {
            var statpart = new StatPart_TrainedAnimal();
            statpart.statOffsets.Add(new() { stat = stat, value = value });
            stat.parts ??= new();
            stat.parts.Add(statpart);
        }

        public static void Postfix()
        {
            AddStatpart(StatDefOf.MeleeDodgeChance, Patcher.Settings.MeleeDodgeChance);
            AddStatpart(StatDefOf.MeleeHitChance, Patcher.Settings.MeleeHitChance);
        }
    }


}