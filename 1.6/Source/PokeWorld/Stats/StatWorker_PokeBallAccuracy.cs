﻿using System.Text;
using RimWorld;
using Verse;

namespace PokeWorld;

public class StatWorker_PokeBallAccuracy : StatWorker
{
    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        var stringBuilder = new StringBuilder();
        for (var i = 5; i <= 45; i += 5)
        {
            var f = PokeBallShotReport.HitFactorFromShooter(finalVal, i);
            stringBuilder.AppendLine(
                "distance".Translate().CapitalizeFirst() + " " + i.ToString() + ": " + f.ToStringPercent("F1")
            );
        }

        stringBuilder.AppendLine(base.GetExplanationFinalizePart(req, numberSense, finalVal));
        return stringBuilder.ToString();
    }

    public override bool ShouldShowFor(StatRequest req)
    {
        if (!base.ShouldShowFor(req)) return false;
        if (req.Def is ThingDef thingDef && thingDef.category == ThingCategory.Pawn) return true;
        return false;
    }
}
