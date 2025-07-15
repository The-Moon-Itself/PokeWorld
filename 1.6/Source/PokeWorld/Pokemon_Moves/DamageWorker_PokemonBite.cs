﻿using Verse;

namespace PokeWorld;

internal class DamageWorker_PokemonBite : DamageWorker_PokemonMeleeMove
{
    protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
    {
        return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside);
    }
}
