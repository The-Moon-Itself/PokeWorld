using BigAndSmall;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PokeWorld
{

    [HarmonyPatch(typeof(RaceMorpher))]
    [HarmonyPatch("SwapAnimalToSapientVersion")]
    public class SapientAnimals_RaceMorpher_SwapAnimalToSapientVersion
    {
        public static void Postfix(Pawn __0, Pawn __result)
        {
            var oldComp = __0.TryGetComp<CompPokemon>();
            var newComp = __result.TryGetComp<CompPokemon>();
            if (oldComp == null || newComp == null)
                return;
            // Level tracker sync
            newComp.levelTracker.level = oldComp.levelTracker.level;
            newComp.levelTracker.experience = oldComp.levelTracker.experience;
            newComp.levelTracker.flagEverstoneOn = oldComp.levelTracker.flagEverstoneOn;
            newComp.levelTracker.UpdateExpToNextLvl();
            // Friendship tracker sync
            newComp.friendshipTracker.friendship = oldComp.friendshipTracker.friendship;
            newComp.friendshipTracker.flagMaxFriendshipMessage = oldComp.friendshipTracker.flagMaxFriendshipMessage;
            // Stat tracker sync
            newComp.statTracker.CopyPreEvoStat(oldComp);
            newComp.statTracker.UpdateStats();
            // Move tracker sync
            newComp.moveTracker.GetUnlockedMovesFromPreEvolution(oldComp);
            // Shiny tracker sync
            if (oldComp.shinyTracker.isShiny)
                newComp.shinyTracker.MakeShiny();
            else
                newComp.shinyTracker.isShiny = false;
            __result.Drawer.renderer.SetAllGraphicsDirty();
            //Misc sync
            newComp.ballDef = oldComp.ballDef;
            newComp.inBall = oldComp.inBall;
            newComp.tryCatchKillChanceIfDown = oldComp.tryCatchKillChanceIfDown;
            newComp.wantPutInBall = oldComp.wantPutInBall;
        }
    }
}
