﻿using RimWorld;
using UnityEngine;
using Verse;

namespace PokeWorld;

public class PawnColumnWorker_InPokeball : PawnColumnWorker_Icon
{
    protected override Texture2D GetIconFor(Pawn pawn)
    {
        if (pawn.TryGetComp<CompPokemon>() != null && pawn.TryGetComp<CompPokemon>().inBall)
            return ContentFinder<Texture2D>.Get("Things/Item/Utility/Balls/PokeBall");
        return null;
    }

    protected override string GetIconTip(Pawn pawn)
    {
        if (pawn.TryGetComp<CompPokemon>() != null && pawn.TryGetComp<CompPokemon>().inBall)
            return "PW_TipInPokeball".Translate();
        return null;
    }

    public override int Compare(Pawn a, Pawn b)
    {
        return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
    }

    private int GetValueToCompare(Pawn pawn)
    {
        var comp = pawn.TryGetComp<CompPokemon>();
        if (comp != null)
        {
            if (comp.inBall)
                return 1;
            return 0;
        }

        return 0;
    }
}
