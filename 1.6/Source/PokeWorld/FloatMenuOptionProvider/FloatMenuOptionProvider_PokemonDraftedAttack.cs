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
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!CanTarget(clickedThing))
        {
            return Enumerable.Empty<FloatMenuOption>();
        }
        tmpPawns.Clear();
        bool flag = clickedThing.HostileTo(Faction.OfPlayer);
        IEnumerable<FloatMenuOption> floatMenuOptions = (context.IsMultiselect ? GetMultiselectAttackOption(clickedThing, context) : GetSingleSelectAttackOption(clickedThing, context));
        if (floatMenuOptions == null)
        {
            return Enumerable.Empty<FloatMenuOption>();
        }
        foreach (var floatMenuOption in floatMenuOptions)
        {
            if (!floatMenuOption.Disabled)
            {
                floatMenuOption.Priority = (flag ? MenuOptionPriority.AttackEnemy : MenuOptionPriority.VeryLow);
                floatMenuOption.autoTakeable = flag || (clickedThing.def.building?.quickTargetable ?? false);
                floatMenuOption.autoTakeablePriority = 40f;
            }
        }
        return floatMenuOptions;
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
        if (pawn.Drafted) return base.SelectedPawnValid(pawn, context);
        if (pawn.MentalStateDef != null || pawn.playerSettings == null || pawn.playerSettings.Master == null || !pawn.playerSettings.Master.Drafted)
        {
            return false;
        }
        return base.SelectedPawnValid(pawn, context);
    }

    private IEnumerable<FloatMenuOption> GetMultiselectAttackOption(Thing clickedThing, FloatMenuContext context)
    {
        string label = null;
        foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
        {
            if (GetAttackAction(validSelectedPawn, clickedThing).FirstOrDefault<(Action action, FleckDef, string, string)>(p => p.action != null).action != null)
            {
                tmpPawns.Add(validSelectedPawn);
            }
        }
        if (tmpPawns.Count == 0)
        {
            yield break;
        }
        FleckDef fleck = (PokemonAttackGizmoUtility.CanUseAnyRangedVerb(tmpPawns[0]) ? FleckDefOf.FeedbackShoot : FleckDefOf.FeedbackMelee);
        yield return new FloatMenuOption(label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing)), delegate
        {
            foreach (Pawn tmpPawn in tmpPawns)
            {
                var attackData = GetAttackAction(tmpPawn, clickedThing).FirstOrDefault<(Action action, FleckDef fleck, string, string)>(p => p.action != null);
                FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, attackData.fleck);
                attackData.action?.Invoke();
            }
        }, MenuOptionPriority.AttackEnemy);
    }

    private static IEnumerable<FloatMenuOption> GetSingleSelectAttackOption(Thing clickedThing, FloatMenuContext context)
    {
        foreach (var (action, fleck, label, failStr) in GetAttackAction(context.FirstSelectedPawn, clickedThing))
        {
            if (action == null)
            {
                if (!failStr.NullOrEmpty())
                    yield return new FloatMenuOption((label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing))) + ": " + failStr, null);
                continue;
            }
            yield return new FloatMenuOption(label ?? ((string)"Attack".Translate(clickedThing.Label, clickedThing)), delegate
            {
                FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, fleck);
                action();
            }, MenuOptionPriority.AttackEnemy);

        }
    }

    private static IEnumerable<(Action action, FleckDef fleck, string label, string failStr)> GetAttackAction(Pawn pawn, Thing target)
    {
        string failStr;
        string text;
        var comp = pawn.TryGetComp<CompPokemon>();
        if (comp != null && comp.moveTracker != null && PokemonAttackGizmoUtility.CanUseAnyRangedVerb(pawn))
        {
            var rangedAct = PokemonFloatMenuUtility.GetRangedAttackAction(pawn, target, out failStr);
            text = "FireAt".Translate(target.Label, target);
            var floatMenuOption = new FloatMenuOption("", null, MenuOptionPriority.High, null, target);
            if (rangedAct == null)
            {
                yield return (null, FleckDefOf.FeedbackShoot, text + ": " + failStr, failStr);
            }
            else if (!PokemonMasterUtility.IsPokemonDrafted(pawn))
            {
                yield return (null, FleckDefOf.FeedbackShoot, "PW_CannotGoNoMaster".Translate(), failStr);
            }
            else if (PokemonMasterUtility.IsPokemonMasterDrafted(pawn) && target.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                     PokemonMasterUtility.GetMasterObedienceRadius(pawn))
                yield return (null, FleckDefOf.FeedbackShoot, "PW_CannotGoTooFarFromMaster".Translate(), failStr);
            else
            {
                yield return (rangedAct, FleckDefOf.FeedbackShoot, text, failStr);
            }
        }

        var meleeAct = PokemonFloatMenuUtility.GetMeleeAttackAction(pawn, target, out failStr);
        text = !(target is Pawn pawn2) || !pawn2.Downed
            ? "MeleeAttack".Translate(target.Label, target)
            : (string)"MeleeAttackToDeath".Translate(target.Label, target);
        var priority = target == null || !pawn.HostileTo(target)
            ? MenuOptionPriority.VeryLow
            : MenuOptionPriority.AttackEnemy;
        var floatMenuOption2 = new FloatMenuOption("", null, priority, null, target);
        if (meleeAct == null)
        {
            yield return (null, FleckDefOf.FeedbackMelee, text + ": " + failStr.CapitalizeFirst(), failStr);
        }
        else if (!PokemonMasterUtility.IsPokemonDrafted(pawn))
        {
            yield return (null, FleckDefOf.FeedbackMelee, "PW_CannotAttackNoMaster".Translate(), failStr);
        }
        else if (PokemonMasterUtility.IsPokemonMasterDrafted(pawn) && target.Position.DistanceTo(pawn.playerSettings.Master.Position) >
                 PokemonMasterUtility.GetMasterObedienceRadius(pawn))
        {
            yield return (null, FleckDefOf.FeedbackMelee, "PW_CannotAttackTooFarFromMaster".Translate(), failStr);
        }
        yield return (meleeAct, FleckDefOf.FeedbackMelee, text, failStr);
    }

}

