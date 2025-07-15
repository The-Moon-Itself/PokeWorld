﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PokeWorld;

public static class PokemonAttackGizmoUtility
{
    public static IEnumerable<Gizmo> GetAttackGizmos(Pawn pawn)
    {
        if (CanUseAnyMeleeVerb(pawn)) yield return GetMeleeAttackGizmo(pawn);
        if (CanUseAnyRangedVerb(pawn))
        {
            if (ShouldUseSquadAttackGizmo())
                yield return GetSquadAttackGizmo(pawn);
            else
                yield return GetRangedAttackGizmo(pawn);
        }
    }

    public static bool CanUseAnyMeleeVerb(Pawn pawn)
    {
        var comp = pawn.TryGetComp<CompPokemon>();
        foreach (var kvp in comp.moveTracker.unlockableMoves)
            if (kvp.Key.tool != null && ShouldUseMove(pawn, kvp.Key))
                return true;
        return false;
    }

    public static bool CanUseAnyRangedVerb(Pawn pawn)
    {
        var comp = pawn.TryGetComp<CompPokemon>();
        foreach (var kvp in comp.moveTracker.unlockableMoves)
            if (kvp.Key.verb != null && ShouldUseMove(pawn, kvp.Key))
                return true;
        return false;
    }

    private static Gizmo GetSquadAttackGizmo(Pawn pawn)
    {
        var command_Target = new Command_Target();
        command_Target.defaultLabel = "CommandSquadAttack".Translate();
        command_Target.defaultDesc = "CommandSquadAttackDesc".Translate();
        command_Target.targetingParams = TargetingParameters.ForAttackAny();
        command_Target.hotKey = KeyBindingDefOf.Misc1;
        command_Target.icon = TexCommand.SquadAttack;
        if (PokemonFloatMenuUtility.GetMeleeAttackAction(pawn, LocalTargetInfo.Invalid, out var failStr) == null &&
            CanUseAnyRangedVerb(pawn) &&
            PokemonFloatMenuUtility.GetRangedAttackAction(pawn, LocalTargetInfo.Invalid, out var failStr2) == null)
        {
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.playerSettings.Master == null || pawn.playerSettings.Master.Map != pawn.Map)
        {
            failStr = "PW_WarningNoMaster".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.playerSettings.Master.Drafted == false)
        {
            failStr = "PW_WarningMasterNotDrafted".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                 PokemonMasterUtility.GetMasterObedienceRadius(pawn))
        {
            failStr = "PW_WarningMasterTooFar".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }

        command_Target.action = delegate(LocalTargetInfo target)
        {
            foreach (var item in Find.Selector.SelectedObjects.Where(
                         delegate(object x)
                         {
                             var pawn2 = x as Pawn;
                             return pawn2 != null && pawn2.TryGetComp<CompPokemon>() != null &&
                                    PokemonMasterUtility.IsPokemonMasterDrafted(pawn2);
                         }
                     ).Cast<Pawn>())
            {
                string failStr3;
                var attackAction = PokemonFloatMenuUtility.GetRangedAttackAction(item, target, out failStr3);
                if (attackAction == null)
                {
                    attackAction = PokemonFloatMenuUtility.GetMeleeAttackAction(item, target, out failStr3);
                    if (attackAction != null)
                        attackAction();
                    else if (!failStr3.NullOrEmpty())
                        Messages.Message(failStr3, target.Thing, MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    attackAction();
                }
            }
        };
        return command_Target;
    }

    private static Gizmo GetMeleeAttackGizmo(Pawn pawn)
    {
        var command_Target = new Command_Target();
        command_Target.defaultLabel = "CommandMeleeAttack".Translate();
        command_Target.defaultDesc = "CommandMeleeAttackDesc".Translate();
        command_Target.targetingParams = TargetingParameters.ForAttackAny();
        command_Target.hotKey = KeyBindingDefOf.Misc2;
        command_Target.icon = TexCommand.AttackMelee;
        if (PokemonFloatMenuUtility.GetMeleeAttackAction(pawn, LocalTargetInfo.Invalid, out var failStr) == null)
        {
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.playerSettings.Master == null || pawn.playerSettings.Master.Map != pawn.Map)
        {
            failStr = "PW_WarningNoMaster".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.playerSettings.Master.Drafted == false)
        {
            failStr = "PW_WarningMasterNotDrafted".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                 PokemonMasterUtility.GetMasterObedienceRadius(pawn))
        {
            failStr = "PW_WarningMasterTooFar".Translate();
            command_Target.Disable(failStr.CapitalizeFirst() + ".");
        }

        command_Target.action = delegate(LocalTargetInfo target)
        {
            foreach (var item in Find.Selector.SelectedObjects.Where(
                         delegate(object x)
                         {
                             var pawn2 = x as Pawn;
                             return pawn2 != null && pawn2.TryGetComp<CompPokemon>() != null &&
                                    PokemonMasterUtility.IsPokemonMasterDrafted(pawn2);
                         }
                     ).Cast<Pawn>())
            {
                string failStr2;
                var meleeAttackAction = PokemonFloatMenuUtility.GetMeleeAttackAction(item, target, out failStr2);
                if (meleeAttackAction != null)
                    meleeAttackAction();
                else if (!failStr2.NullOrEmpty())
                    Messages.Message(failStr2, target.Thing, MessageTypeDefOf.RejectInput, false);
            }
        };
        return command_Target;
    }

    private static Gizmo GetRangedAttackGizmo(Pawn pawn)
    {
        var command_PokemonVerbTarget = new Command_PokemonVerbTarget();
        command_PokemonVerbTarget.defaultLabel = "CommandSquadAttack".Translate();
        command_PokemonVerbTarget.defaultDesc = "CommandSquadAttackDesc".Translate();
        command_PokemonVerbTarget.targetingParams = TargetingParameters.ForAttackAny();
        command_PokemonVerbTarget.icon = TexCommand.Attack;
        command_PokemonVerbTarget.tutorTag = "VerbTarget";
        command_PokemonVerbTarget.verb = GetLongestRangeVerb(pawn);
        string failStr;
        if (pawn.playerSettings.Master == null || pawn.playerSettings.Master.Map != pawn.Map)
        {
            failStr = "PW_WarningNoMaster".Translate();
            command_PokemonVerbTarget.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.playerSettings.Master.Drafted == false)
        {
            failStr = "PW_WarningMasterNotDrafted".Translate();
            command_PokemonVerbTarget.Disable(failStr.CapitalizeFirst() + ".");
        }
        else if (pawn.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                 PokemonMasterUtility.GetMasterObedienceRadius(pawn))
        {
            failStr = "PW_WarningMasterTooFar".Translate();
            command_PokemonVerbTarget.Disable(failStr.CapitalizeFirst() + ".");
        }

        command_PokemonVerbTarget.action = delegate(LocalTargetInfo target)
        {
            foreach (var item in Find.Selector.SelectedObjects.Where(
                         delegate(object x)
                         {
                             var pawn2 = x as Pawn;
                             return pawn2 != null && pawn2.TryGetComp<CompPokemon>() != null &&
                                    PokemonMasterUtility.IsPokemonMasterDrafted(pawn2);
                         }
                     ).Cast<Pawn>())
            {
                string failStr3;
                var attackAction = PokemonFloatMenuUtility.GetRangedAttackAction(item, target, out failStr3);
                if (attackAction != null)
                    attackAction();
                else if (!failStr3.NullOrEmpty())
                    Messages.Message(failStr3, target.Thing, MessageTypeDefOf.RejectInput, false);
            }
        };
        return command_PokemonVerbTarget;
    }

    private static bool AtLeastOneSelectedPokemonHasRangedMove()
    {
        var selectedObjectsListForReading = Find.Selector.SelectedObjectsListForReading;
        for (var i = 0; i < selectedObjectsListForReading.Count; i++)
        {
            var pawn = selectedObjectsListForReading[i] as Pawn;
            if (pawn != null && pawn.TryGetComp<CompPokemon>() != null &&
                PokemonMasterUtility.IsPokemonMasterDrafted(pawn) && CanUseAnyRangedVerb(pawn)) return true;
        }

        return false;
    }

    private static bool AtLeastTwoSelectedPokemonsHaveRangedMoves()
    {
        if (Find.Selector.NumSelected <= 1) return false;
        var flag = false;
        var selectedObjectsListForReading = Find.Selector.SelectedObjectsListForReading;
        for (var i = 0; i < selectedObjectsListForReading.Count; i++)
        {
            var pawn = selectedObjectsListForReading[i] as Pawn;
            if (pawn != null && pawn.TryGetComp<CompPokemon>() != null &&
                PokemonMasterUtility.IsPokemonMasterDrafted(pawn))
            {
                if (!flag && CanUseAnyRangedVerb(pawn))
                    flag = true;
                else if (flag && CanUseAnyRangedVerb(pawn)) return true;
            }
        }

        return false;
    }

    public static Verb GetLongestRangeVerb(Pawn pokemon)
    {
        return GetVerbFromMove(
            pokemon,
            pokemon.TryGetComp<CompPokemon>().moveTracker.unlockableMoves.Keys
                .Where(x => x.verb != null && ShouldUseMove(pokemon, x)).MaxBy(x => x.verb.range)
        );
    }

    public static Verb GetAnyRangedVerb(Pawn pokemon)
    {
        return GetVerbFromMove(
            pokemon,
            pokemon.TryGetComp<CompPokemon>().moveTracker.unlockableMoves.Keys
                .Where(x => x.verb != null && ShouldUseMove(pokemon, x)).RandomElement()
        );
    }

    public static bool ShouldUseMove(Pawn pokemon, MoveDef md)
    {
        if (md == DefDatabase<MoveDef>.GetNamed("Struggle"))
            foreach (var kvp in pokemon.TryGetComp<CompPokemon>().moveTracker.unlockableMoves)
            {
                if (kvp.Key == DefDatabase<MoveDef>.GetNamed("Struggle")) continue;
                if (kvp.Key.tool != null && pokemon.TryGetComp<CompPokemon>().moveTracker.HasUnlocked(kvp.Key) &&
                    pokemon.TryGetComp<CompPokemon>().moveTracker.GetWanted(kvp.Key)) return false;
            }

        if (pokemon.TryGetComp<CompPokemon>().moveTracker.HasUnlocked(md) &&
            pokemon.TryGetComp<CompPokemon>().moveTracker.GetWanted(md)) return true;
        return false;
    }

    private static bool ShouldUseSquadAttackGizmo()
    {
        return AtLeastTwoSelectedPokemonsHaveRangedMoves();
    }

    private static Verb GetVerbFromMove(Pawn pokemon, MoveDef moveDef)
    {
        var verbs = pokemon.verbTracker.AllVerbs.Where(
            x => (x.verbProps != null && x.verbProps == moveDef.verb) || (x.tool != null && x.tool == moveDef.tool)
        );
        if (verbs.Any()) return verbs.First();
        return null;
    }
}
