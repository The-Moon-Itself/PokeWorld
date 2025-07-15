using HarmonyLib;
using RimWorld;
using Verse;

namespace PokeWorld;

[HarmonyPatch(typeof(TameUtility))]
[HarmonyPatch("CanTame")]
internal class TameUtility_CanTame_Patch
{
    public static void Postfix(Pawn __0, ref bool __result)
    {
        if (__0.TryGetComp<CompPokemon>() != null) __result = false;
    }
}

[HarmonyPatch(typeof(PawnColumnWorker_Tame))]
[HarmonyPatch("HasCheckbox")]
internal class PawnColumnWorker_Tame_HasCheckbox_Patch
{
    public static void Postfix(Pawn __0, ref bool __result)
    {
        if (__0.TryGetComp<CompPokemon>() != null) __result = false;
    }
}
