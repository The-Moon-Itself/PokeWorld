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

internal class FloatMenuOptionProvider_PokemonDraftedMove : FloatMenuOptionProvider
{
    private static List<Pawn> tmpPawns = new List<Pawn>();
    protected override bool Drafted => false;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;
    protected override bool IgnoreFogged => false;

    protected override FloatMenuOption GetSingleOption(FloatMenuContext context)
    {
        IntVec3 curLoc = CellFinder.StandableCellNear(context.ClickedCell, context.map, 2.9f);
        if (!curLoc.IsValid)
        {
            return null;
        }
        FloatMenuOption floatMenuOption;
        if (!context.IsMultiselect)
        {
            var pawn = context.FirstSelectedPawn;
            if (curLoc == context.FirstSelectedPawn.Position)
            {
                return null;
            }
            var comp = pawn.TryGetComp<CompPokemon>();
            if (comp == null)
            {
                return null;
            }
            if (pawn.MentalStateDef != null || pawn.playerSettings == null || pawn.playerSettings.Master == null || !pawn.playerSettings.Master.Drafted)
            {
                return null;
            }
            AcceptanceReport acceptanceReport = PawnCanGoto(context.FirstSelectedPawn, curLoc);
            if (!acceptanceReport.Accepted)
            {
                return new FloatMenuOption(acceptanceReport.Reason, null);
            }
            floatMenuOption = new FloatMenuOption("GoHere".Translate(), delegate
            {
                PawnGotoAction(context.ClickedCell, context.FirstSelectedPawn, RCellFinder.BestOrderedGotoDestNear(curLoc, context.FirstSelectedPawn));
            }, MenuOptionPriority.GoHere);
        }
        else
        {
            tmpPawns.Clear();
            foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
            {
                if (PawnCanGoto(validSelectedPawn, curLoc).Accepted)
                {
                    tmpPawns.Add(validSelectedPawn);
                }
            }
            if (tmpPawns.Count == 0)
            {
                return null;
            }
            floatMenuOption = new FloatMenuOption("GoHere".Translate(), delegate
            {
                Find.Selector.gotoController.StartInteraction(curLoc);
                foreach (Pawn tmpPawn in tmpPawns)
                {
                    Find.Selector.gotoController.AddPawn(tmpPawn);
                }
                Find.Selector.gotoController.FinalizeInteraction();
            }, MenuOptionPriority.GoHere);
        }
        floatMenuOption.isGoto = true;
        floatMenuOption.autoTakeable = true;
        floatMenuOption.autoTakeablePriority = 10f;
        return floatMenuOption;
    }

    public static AcceptanceReport PawnCanGoto(Pawn pawn, IntVec3 gotoLoc)
    {
        if (!PokemonMasterUtility.IsPokemonMasterDrafted(pawn))
            return "PW_CannotGoNoMaster".Translate();
        if (gotoLoc.DistanceTo(pawn.playerSettings.Master.Position) >
            PokemonMasterUtility.GetMasterObedienceRadius(pawn))
            return "PW_CannotGoTooFarFromMaster".Translate();
        if (ModsConfig.BiotechActive && pawn.IsColonyMech && !MechanitorUtility.InMechanitorCommandRange(pawn, gotoLoc))
        {
            return "CannotGoOutOfRange".Translate() + ": " + "OutOfCommandRange".Translate();
        }
        if (!pawn.CanReach(gotoLoc, PathEndMode.OnCell, Danger.Deadly))
        {
            return "CannotGoNoPath".Translate();
        }
        return true;
    }

    public static void PawnGotoAction(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
    {
        bool flag;
        if (pawn.Position == gotoLoc)
        {
            flag = true;
            if (pawn.CurJobDef == JobDefOf.Goto)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            }
        }
        else if (pawn.CurJobDef == JobDefOf.Goto && pawn.CurJob.targetA.Cell == gotoLoc)
        {
            flag = true;
        }
        else
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, gotoLoc);
            if (pawn.Map.exitMapGrid.IsExitCell(clickCell))
            {
                job.exitMapOnArrival = !pawn.IsColonyMech;
            }
            else if (!pawn.Map.IsPlayerHome && !pawn.Map.exitMapGrid.MapUsesExitGrid && CellRect.WholeMap(pawn.Map).IsOnEdge(clickCell, 3) && pawn.Map.Parent.GetComponent<FormCaravanComp>() != null && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" + pawn.Map.uniqueID, 60f))
            {
                Messages.Message(pawn.Map.Parent.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow ? "MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate() : "MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, historical: false);
            }
            flag = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        if (flag)
        {
            FleckMaker.Static(gotoLoc, pawn.Map, FleckDefOf.FeedbackGoto);
        }
    }
}

