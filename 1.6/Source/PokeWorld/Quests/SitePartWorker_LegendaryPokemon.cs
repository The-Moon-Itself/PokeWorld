﻿using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace PokeWorld;

public class SitePartWorker_LegendaryPokemon : SitePartWorker
{
    public override void Notify_GeneratedByQuestGen(
        SitePart part, Slate slate, List<Rule> outExtraDescriptionRules,
        Dictionary<string, string> outExtraDescriptionConstants
    )
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        var pawnKind = slate.Get<PawnKindDef>("legendaryKind");
        var pawn = LegendaryPokemonQuestUtility.GenerateLegendaryPokemon(part.site.Tile, pawnKind);
        part.things = new ThingOwner<Pawn>(part, true);
        part.things.TryAdd(pawn);
        if (pawn.relations != null) pawn.relations.everSeenByPlayer = true;
        var mostImportantColonyRelative = PawnRelationUtility.GetMostImportantColonyRelative(pawn);
        if (mostImportantColonyRelative != null)
        {
            var mostImportantRelation = mostImportantColonyRelative.GetMostImportantRelation(pawn);
            TaggedString text = "";
            if (mostImportantRelation != null && mostImportantRelation.opinionOffset > 0)
            {
                pawn.relations.relativeInvolvedInRescueQuest = mostImportantColonyRelative;
                text = "\n\n" + "RelatedPawnInvolvedInQuest".Translate(
                    mostImportantColonyRelative.LabelShort, mostImportantRelation.GetGenderSpecificLabel(pawn),
                    mostImportantColonyRelative.Named("RELATIVE"), pawn.Named("PAWN")
                ).AdjustedFor(pawn);
            }
            else
            {
                PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, pawn);
            }

            outExtraDescriptionRules.Add(new Rule_String("pawnInvolvedInQuestInfo", text));
        }

        slate.Set("legendaryPokemon", pawn);
    }

    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        var text = base.GetPostProcessedThreatLabel(site, sitePart);
        if (sitePart.things != null && sitePart.things.Any) text = text + ": " + sitePart.things[0].LabelShortCap;
        if (site.HasWorldObjectTimeout)
            text += " (" + "DurationLeft".Translate(site.WorldObjectTimeoutTicksLeft.ToStringTicksToPeriod()) + ")";
        return text;
    }

    public override void PostDestroy(SitePart sitePart)
    {
        base.PostDestroy(sitePart);
        if (sitePart.things == null || !sitePart.things.Any) return;
        var pawn = (Pawn)sitePart.things[0];
        if (!pawn.Dead)
        {
            if (pawn.relations != null) pawn.relations.Notify_FailedRescueQuest();
            HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
        }
    }
}
