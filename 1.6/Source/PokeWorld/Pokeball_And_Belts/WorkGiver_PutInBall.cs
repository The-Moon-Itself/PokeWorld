using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PokeWorld;

internal class WorkGiver_PutInBall : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn)
    {
        return Danger.Deadly;
    }

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var desList = pawn.Map.designationManager.AllDesignations;
        for (var i = 0; i < desList.Count; i++)
            if (desList[i].def == DefDatabase<DesignationDef>.GetNamed("PW_PutInBall"))
                yield return desList[i].target.Thing;
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(
            DefDatabase<DesignationDef>.GetNamed("PW_PutInBall")
        );
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn.Map.designationManager.DesignationOn(t, DefDatabase<DesignationDef>.GetNamed("PW_PutInBall")) ==
            null) return false;
        if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PW_PutInBall"), t);
    }
}
