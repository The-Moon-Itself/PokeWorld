﻿using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PokeWorld;

public class CompPokemonEggHatcher : CompHatcher
{
    private float pokemonEggGestateProgress;

    public override void CompTick()
    {
        if (!TemperatureDamaged)
        {
            var num = 1f / (Props.hatcherDaystoHatch * 60000f);
            pokemonEggGestateProgress += num;
            if (pokemonEggGestateProgress > 1f) HatchPokemon();
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref pokemonEggGestateProgress, "PW_pokemonEggGestateProgress");
    }

    public override string CompInspectStringExtra()
    {
        if (!TemperatureDamaged)
        {
            if (Props.hatcherDaystoHatch - Props.hatcherDaystoHatch * pokemonEggGestateProgress < 1)
                return "PW_CompPokemonEggHatcherSoon".Translate();
            if (Props.hatcherDaystoHatch - Props.hatcherDaystoHatch * pokemonEggGestateProgress < 5)
                return "PW_CompPokemonEggHatcherClose".Translate();
            if (Props.hatcherDaystoHatch - Props.hatcherDaystoHatch * pokemonEggGestateProgress < 15)
                return "PW_CompPokemonEggHatcherNotClose".Translate();
            return "PW_CompPokemonEggHatcherLong".Translate();
        }

        return null;
    }

    public override string TransformLabel(string label)
    {
        return "PW_PokemonEgg".Translate();
    }

    public void HatchPokemon()
    {
        try
        {
            var request = new PawnGenerationRequest(
                Props.hatcherPawn, hatcheeFaction,
                developmentalStages: DevelopmentalStage.Newborn
            );
            for (var i = 0; i < parent.stackCount; i++)
            {
                var pawn = PawnGenerator.GeneratePawn(request);
                var comp = pawn.TryGetComp<CompPokemon>();
                if (comp != null)
                {
                    comp.levelTracker.level = 1;
                    comp.levelTracker.UpdateExpToNextLvl();
                    comp.statTracker.UpdateStats();
                }

                if (PawnUtility.TrySpawnHatchedOrBornPawn(pawn, parent))
                {
                    if (pawn != null)
                    {
                        if (hatcheeParent != null)
                        {
                            if (pawn.playerSettings != null && hatcheeParent.playerSettings != null &&
                                hatcheeParent.Faction == hatcheeFaction)
                                pawn.playerSettings.AreaRestrictionInPawnCurrentMap =
                                    hatcheeParent.playerSettings.AreaRestrictionInPawnCurrentMap;
                            if (pawn.RaceProps.IsFlesh)
                                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, hatcheeParent);
                            if (comp.formTracker != null)
                            {
                                if (hatcheeParent.TryGetComp<CompDittoEggLayer>() != null && otherParent != null)
                                {
                                    var comp2 = otherParent.TryGetComp<CompPokemon>();
                                    if (comp2 != null && comp2.formTracker != null)
                                        comp.formTracker.TryInheritFormFromParent(comp2.formTracker);
                                }
                                else
                                {
                                    var comp3 = hatcheeParent.TryGetComp<CompPokemon>();
                                    if (comp3 != null && comp3.formTracker != null)
                                        comp.formTracker.TryInheritFormFromParent(comp3.formTracker);
                                }
                            }
                        }

                        if (otherParent != null &&
                            (hatcheeParent == null || hatcheeParent.gender != otherParent.gender) &&
                            pawn.RaceProps.IsFlesh)
                            pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, otherParent);
                        if (pawn.Faction == Faction.OfPlayer)
                            Find.World.GetComponent<PokedexManager>().AddPokemonKindCaught(pawn.kindDef);
                    }

                    if (parent.Spawned)
                        FilthMaker.TryMakeFilth(parent.Position, parent.Map, ThingDefOf.Filth_AmnioticFluid);
                }
                else
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                }
            }
        }
        finally
        {
            parent.Destroy();
        }
    }
}

public class CompProperties_PokemonEggHatcher : CompProperties_Hatcher
{
    public CompProperties_PokemonEggHatcher()
    {
        compClass = typeof(CompPokemonEggHatcher);
    }
}
