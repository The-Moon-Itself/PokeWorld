using HarmonyLib;
using RimWorld;
using Verse;

namespace PokeWorld;

[HarmonyPatch(typeof(Pawn))]
[HarmonyPatch("CanTakeOrder", MethodType.Getter)]
internal class Pawn_get_CanTakeOrder_Patch
{
    public static void Postfix(Pawn __instance, ref bool __result)
    {
        if (__result == false && __instance.Spawned && __instance.Faction == Faction.OfPlayer &&
            __instance.TryGetComp<CompPokemon>() != null && __instance.MentalStateDef == null && __instance.playerSettings != null &&
            __instance.playerSettings.Master != null && __instance.playerSettings.Master.Drafted) __result = true;
    }
}
