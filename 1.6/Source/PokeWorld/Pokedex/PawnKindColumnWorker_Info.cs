using UnityEngine;
using Verse;

namespace PokeWorld;

public class PawnKindColumnWorker_Info : PawnKindColumnWorker
{
    public override void DoCell(Rect rect, PawnKindDef kindDef, PawnKindTable table)
    {
        if (kindDef.race.HasComp(typeof(CompPokemon)) && Find.World.GetComponent<PokedexManager>().IsPokemonCaught(kindDef.race.GetCompProperties<CompProperties_Pokemon>().pokedexNumber))
            Widgets.InfoCardButton(rect.center.x - 12f, rect.center.y - 12f, kindDef.race);
    }

    public override int GetMinWidth(PawnKindTable table)
    {
        return 24;
    }

    public override int GetMaxWidth(PawnKindTable table)
    {
        return 24;
    }
}
