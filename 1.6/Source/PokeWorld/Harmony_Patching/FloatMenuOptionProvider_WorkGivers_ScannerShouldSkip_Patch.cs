using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PokeWorld;

[HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers))]
[HarmonyPatch("ScannerShouldSkip")]
internal class FloatMenuOptionProvider_WorkGivers_ScannerShouldSkip_Patch
{
    public static bool Prefix(Pawn __0, ref bool __result)
    {
        if (__0.TryGetComp<CompPokemon>() != null && __0.RaceProps?.Humanlike == false)
        {
            __result = true;
            return false;
        }
        return true;
    }
    
}
