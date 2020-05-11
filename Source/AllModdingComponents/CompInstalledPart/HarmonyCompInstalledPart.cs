﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompInstalledPart
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompInstalledPart
    {
        static HarmonyCompInstalledPart()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.installedpart");
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "InterfaceDrop"),
                new HarmonyMethod(typeof(HarmonyCompInstalledPart).GetMethod("InterfaceDrop_PreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "TryDropEquipment"),
                new HarmonyMethod(typeof(HarmonyCompInstalledPart), "TryDropEquipment_PreFix"), null);
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"),
                new HarmonyMethod(typeof(HarmonyCompInstalledPart).GetMethod("DrawEquipmentAiming_PreFix")), null);
        }

        public static bool DrawEquipmentAiming_PreFix(Pawn ___pawn, Thing eq, Vector3 drawLoc,
            float aimAngle)
        {
            if (___pawn != null && eq.TryGetComp<CompInstalledPart>() is CompInstalledPart installedComp)
            {
                var flip = false;
                var num = aimAngle - 90f;
                if (aimAngle > 20f && aimAngle < 160f)
                {
                    num += eq.def.equippedAngleOffset;
                }
                else if (aimAngle > 200f && aimAngle < 340f)
                {
                    flip = true;
                    num -= 180f;
                    num -= eq.def.equippedAngleOffset;
                }
                else
                {
                    num += eq.def.equippedAngleOffset;
                }
                num %= 360f;
                var graphic_StackCount = installedComp?.Props?.installedWeaponGraphic?.Graphic as Graphic_StackCount ??
                                         eq.Graphic as Graphic_StackCount;
                Material matSingle;
                if (graphic_StackCount != null)
                    matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                else
                    matSingle = installedComp?.Props?.installedWeaponGraphic?.Graphic?.MatSingle ??
                                eq.Graphic.MatSingle;

                var s = new Vector3(
                    installedComp?.Props?.installedWeaponGraphic?.drawSize.x ?? eq.def.graphicData.drawSize.x, 1f,
                    installedComp?.Props?.installedWeaponGraphic?.drawSize.y ?? eq.def.graphicData.drawSize.y);
                var matrix = default(Matrix4x4);
                matrix.SetTRS(drawLoc, Quaternion.AngleAxis(num, Vector3.up), s);
                if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
                else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
                return false;
            }
            return true;
        }


        // Verse.Pawn_EquipmentTracker
        public static bool TryDropEquipment_PreFix(ThingWithComps eq, out ThingWithComps resultingEq, ref bool __result)
        {
            resultingEq = null;
            return __result = !!eq?.GetComp<CompInstalledPart>()?.uninstalled ?? true;
        }

        // RimWorld.Pawn_ApparelTracker
        public static bool InterfaceDrop_PreFix(ITab_Pawn_Gear __instance, Thing t)
        {
            if (t is Apparel apparel)
            {
                var pawn = itabPawnGearSelPawnForGearGetter(__instance);
                if (pawn != null)
                    if (pawn.apparel != null)
                        if (pawn.apparel.WornApparel.Contains(apparel))
                            if (pawn.apparel.WornApparel != null)
                            {
                                var installedPart = apparel.GetComp<CompInstalledPart>();
                                if (installedPart != null)
                                    if (!installedPart.uninstalled)
                                        return false;
                            }
            }
            return true;
        }

        // Note: This is an open instance delegate where the first argument is the instance.
        private static readonly Func<ITab_Pawn_Gear, Pawn> itabPawnGearSelPawnForGearGetter =
            (Func<ITab_Pawn_Gear, Pawn>) AccessTools.PropertyGetter(typeof(ITab_Pawn_Gear), "SelPawnForGear")
            .CreateDelegate(typeof(Func<ITab_Pawn_Gear, Pawn>));
    }
}