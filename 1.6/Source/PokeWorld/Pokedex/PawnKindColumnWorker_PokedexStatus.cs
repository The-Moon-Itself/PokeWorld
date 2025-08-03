using UnityEngine;
using Verse;

namespace PokeWorld;

[StaticConstructorOnStartup]
public class PawnKindColumnWorker_PokedexStatus : PawnKindColumnWorker_Icon
{
    private static readonly Texture2D caughtIcon = ContentFinder<Texture2D>.Get("UI/Icon/PokeBall");
    private static readonly Texture2D seenIcon = ContentFinder<Texture2D>.Get("UI/Icon/Seen");

    protected override Texture2D GetIconFor(PawnKindDef pawnKind)
    {
        if (!pawnKind.race.HasComp(typeof(CompPokemon))) return null;
        if (Find.World.GetComponent<PokedexManager>().IsPokemonCaught(pawnKind.race.GetCompProperties<CompProperties_Pokemon>().pokedexNumber))
            return caughtIcon;
        if (Find.World.GetComponent<PokedexManager>().IsPokemonSeen(pawnKind.race.GetCompProperties<CompProperties_Pokemon>().pokedexNumber)) return seenIcon;
        return null;
    }
}
