using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PokeWorld;

internal class FloatMenuOptionProvider_PokemonDraftedAttack : FloatMenuOptionProvider
{
    private static readonly List<Pawn> tmpPawns = new List<Pawn>();
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!CanTarget(clickedThing))
        {
            return null;
        }
        tmpPawns.Clear();
        bool flag = clickedThing.HostileTo(Faction.OfPlayer);
        FloatMenuOption floatMenuOption = (context.IsMultiselect ? GetMultiselectAttackOption(clickedThing, context) : GetSingleSelectAttackOption(clickedThing, context));
        if (floatMenuOption == null)
        {
            return null;
        }
        if (!floatMenuOption.Disabled)
        {
            floatMenuOption.Priority = (flag ? MenuOptionPriority.AttackEnemy : MenuOptionPriority.VeryLow);
            floatMenuOption.autoTakeable = flag || (clickedThing.def.building?.quickTargetable ?? false);
            floatMenuOption.autoTakeablePriority = 40f;
        }
        return floatMenuOption;
    }
    private static bool CanTarget(Thing clickedThing)
    {
        if (clickedThing.def.noRightClickDraftAttack && clickedThing.HostileTo(Faction.OfPlayer))
        {
            return false;
        }
        if (clickedThing.def.IsNonDeconstructibleAttackableBuilding)
        {
            return true;
        }
        BuildingProperties building = clickedThing.def.building;
        if (building != null && building.quickTargetable)
        {
            return true;
        }
        if (!clickedThing.def.destroyable)
        {
            return false;
        }
        if (clickedThing.HostileTo(Faction.OfPlayer))
        {
            return true;
        }
        if (clickedThing is Pawn p && p.NonHumanlikeOrWildMan())
        {
            return true;
        }
        return false;
    }

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
    {
        var comp = pawn.TryGetComp<CompPokemon>();
        if (comp == null)
        {
            return false;
        }
        if (pawn.MentalStateDef != null || pawn.playerSettings == null || pawn.playerSettings.Master == null || !pawn.playerSettings.Master.Drafted)
        {
            return false;
        }
        return base.SelectedPawnValid(pawn, context);
    }

    private FloatMenuOption GetMultiselectAttackOption(Thing clickedThing, FloatMenuContext context)
    {
        string label = null;
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (GetAttackAction(validSelectedPawn, clickedThing, out label, out var _) != null)
            {
                tmpPawns.Add(validSelectedPawn);
            }
        }
        if (tmpPawns.Count == 0)
        {
            return null;
        }
        FleckDef fleck = (PokemonAttackGizmoUtility.CanUseAnyRangedVerb(tmpPawns[0]) ? FleckDefOf.FeedbackShoot : FleckDefOf.FeedbackMelee);
        return new FloatMenuOption(label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing)), delegate
        {
            foreach (Pawn tmpPawn in tmpPawns)
            {
                FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, fleck);
                GetAttackAction(tmpPawn, clickedThing, out var _, out var _)?.Invoke();
            }
        }, MenuOptionPriority.AttackEnemy);
    }

    private static FloatMenuOption GetSingleSelectAttackOption(Thing clickedThing, FloatMenuContext context)
    {
        string label;
        string failStr;
        Action action = GetAttackAction(context.FirstSelectedPawn, clickedThing, out label, out failStr);
        FleckDef fleck = (PokemonAttackGizmoUtility.CanUseAnyRangedVerb(context.FirstSelectedPawn) ? FleckDefOf.FeedbackShoot : FleckDefOf.FeedbackMelee);
        if (action == null)
        {
            if (!failStr.NullOrEmpty())
            {
                return new FloatMenuOption((label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing))) + ": " + failStr, null);
            }
            return null;
        }
        return new FloatMenuOption(label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing)), delegate
        {
            FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, fleck);
            action();
        }, MenuOptionPriority.AttackEnemy);
    }

    private static Action GetAttackAction(Pawn pawn, Thing target, out string label, out string failStr)
    {
        var comp = pawn.TryGetComp<CompPokemon>();
        if (comp != null && comp.moveTracker != null && PokemonAttackGizmoUtility.CanUseAnyRangedVerb(pawn))
        {
            var rangedAct = PokemonFloatMenuUtility.GetRangedAttackAction(pawn, target, out failStr);
            string text = "FireAt".Translate(target.Label, target);
            var floatMenuOption = new FloatMenuOption("", null, MenuOptionPriority.High, null, target);
            if (rangedAct == null)
            {
                label = text + ": " + failStr;
                return null;
            }
            else if (!PokemonMasterUtility.IsPokemonMasterDrafted(pawn))
            {
                label = "PW_CannotGoNoMaster".Translate();
                return null;
            }
            else if (target.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                     PokemonMasterUtility.GetMasterObedienceRadius(pawn))
            {
                label = "PW_CannotGoTooFarFromMaster".Translate();
                return null;
            }

            label = text;
            return rangedAct;
        }

        var meleeAct = PokemonFloatMenuUtility.GetMeleeAttackAction(pawn, target, out failStr);
        string text2 = !(target is Pawn pawn2) || !pawn2.Downed
            ? "MeleeAttack".Translate(target.Label, target)
            : (string)"MeleeAttackToDeath".Translate(target.Label, target);
        var priority = target == null || !pawn.HostileTo(target)
            ? MenuOptionPriority.VeryLow
            : MenuOptionPriority.AttackEnemy;
        var floatMenuOption2 = new FloatMenuOption("", null, priority, null, target);
        if (meleeAct == null)
        {
            label = text2 + ": " + failStr.CapitalizeFirst();
            return null;
        }
        else if (!PokemonMasterUtility.IsPokemonMasterDrafted(pawn))
        {
            label = "PW_CannotAttackNoMaster".Translate();
            return null;
        }
        else if (target.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                 PokemonMasterUtility.GetMasterObedienceRadius(pawn))
        {
            label = "PW_CannotAttackTooFarFromMaster".Translate();
            return null;
        }

        label = text2;
        return meleeAct;
    }

}

