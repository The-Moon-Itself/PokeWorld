using System.Linq;
using Verse;
using RimWorld;

namespace PokeWorld;

public class Verb_PokemonRangedMove : Verb_LaunchProjectile
{
    protected override int ShotsPerBurst => verbProps.burstShotCount;

    public override ThingDef Projectile => verbProps.defaultProjectile;

    protected override bool TryCastShot()
    {
        var result = base.TryCastShot();
        Pawn casterPawn = CasterPawn;
        if (!result || casterPawn == null || !casterPawn.Spawned || casterPawn.skills == null || (currentTarget.Pawn != null && currentTarget.Pawn.IsColonyMech))
            return result;
        casterPawn.skills.Learn(SkillDefOf.Shooting, 200f * verbProps.AdjustedFullCycleTime(this, casterPawn));
        return result;
    }

    public override bool Available()
    {
        var comp = ((Pawn)caster).TryGetComp<CompPokemon>();
        if (comp != null)
        {
            var moveDef = comp.moveTracker.unlockableMoves.Keys.Where(x => x.verb == verbProps).First();
            return PokemonAttackGizmoUtility.ShouldUseMove((Pawn)caster, moveDef);
        }

        return false;
    }

    public override void WarmupComplete()
    {
        var comp = caster.TryGetComp<CompPokemon>();
        if (comp != null)
            comp.moveTracker.lastUsedMove =
                comp.moveTracker.unlockableMoves.Keys.Where(x => x.verb == verbProps).First();
        base.WarmupComplete();
        Find.BattleLog.Add(
            new BattleLogEntry_RangedFire(
                caster, currentTarget.HasThing ? currentTarget.Thing : null,
                EquipmentSource != null ? EquipmentSource.def : null, Projectile, ShotsPerBurst > 1
            )
        );
    }
}
