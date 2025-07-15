﻿using System.Linq;
using RimWorld;
using Verse;

namespace PokeWorld;

internal class IncidentWorker_BabyPokemonWanderIn : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        var map = (Map)parms.target;
        PawnKindDef kind;
        if (RCellFinder.TryFindRandomPawnEntryCell(out var _, map, CellFinder.EdgeRoadChance_Animal))
            return TryFindRandomPawnKind(map, out kind);
        return false;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        if (!RCellFinder.TryFindRandomPawnEntryCell(out var result, map, CellFinder.EdgeRoadChance_Animal))
            return false;
        if (!TryFindRandomPawnKind(map, out var kind)) return false;

        var loc = CellFinder.RandomClosewalkCellNear(result, map, 12);
        var pawn = PawnGenerator.GeneratePawn(kind);
        var comp = pawn.TryGetComp<CompPokemon>();
        comp.levelTracker.level = 5;
        comp.levelTracker.UpdateExpToNextLvl();
        GenSpawn.Spawn(pawn, loc, map, Rot4.Random);
        pawn.SetFaction(Faction.OfPlayer);
        Find.World.GetComponent<PokedexManager>().AddPokemonKindCaught(pawn.kindDef);
        SendStandardLetter(
            "PW_IncidentBabyPokemon".Translate(), "PW_IncidentBabyPokemonDesc".Translate(pawn.kindDef.label),
            LetterDefOf.PositiveEvent, parms, new TargetInfo(result, map)
        );
        return true;
    }

    private bool TryFindRandomPawnKind(Map map, out PawnKindDef kind)
    {
        if (PokeWorldSettings.OkforPokemon())
            return DefDatabase<PawnKindDef>.AllDefs.Where(
                x => x.RaceProps.Animal && x.race.HasComp(typeof(CompPokemon)) &&
                     x.race.GetCompProperties<CompProperties_Pokemon>().attributes.Contains(PokemonAttribute.Baby) &&
                     PokeWorldSettings.GenerationAllowed(
                         x.race.GetCompProperties<CompProperties_Pokemon>().generation
                     ) && map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(x.race)
            ).TryRandomElement(out kind);

        kind = null;
        return false;
    }
}
