using RimWorld;
using Verse;
using Verse.AI;

namespace PokeWorld;

internal class Verb_LaunchPokeball : Verb_CastBase
{
    public virtual ThingDef Projectile
    {
        get
        {
            if (EquipmentSource != null)
            {
                var comp = EquipmentSource.GetComp<CompChangeableProjectile>();
                if (comp != null && comp.Loaded) return comp.Projectile;
            }

            return verbProps.defaultProjectile;
        }
    }

    public override void WarmupComplete()
    {
        base.WarmupComplete();
        Find.BattleLog.Add(
            new BattleLogEntry_RangedFire(
                caster, currentTarget.HasThing ? currentTarget.Thing : null, EquipmentSource?.def, Projectile,
                ShotsPerBurst > 1
            )
        );
    }

    public override bool TryStartCastOn(
        LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false,
        bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false
    )
    {
        if (caster == null)
        {
            Log.Error("Verb " + GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).");
            return false;
        }

        if (!caster.Spawned) return false;
        if (state == VerbState.Bursting || !CanHitTarget(castTarg)) return false;
        this.surpriseAttack = surpriseAttack;
        canHitNonTargetPawnsNow = canHitNonTargetPawns;
        currentTarget = castTarg;
        currentDestination = destTarg;
        if (CasterIsPawn && verbProps.warmupTime > 0f)
        {
            if (!TryFindShootLineFromTo(caster.Position, castTarg, out var resultingLine)) return false;
            CasterPawn.Drawer.Notify_WarmingCastAlongLine(resultingLine, caster.Position);
            var statValue = CasterPawn.GetStatValue(DefDatabase<StatDef>.GetNamed("PW_PokeBallAimingDelayFactor"));
            var ticks = (verbProps.warmupTime * statValue).SecondsToTicks();
            CasterPawn.stances.SetStance(new Stance_Warmup(ticks, castTarg, this));
        }
        else
        {
            WarmupComplete();
        }

        return true;
    }

    protected override bool TryCastShot()
    {
        if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map) return false;
        var projectile = Projectile;
        if (projectile == null) return false;
        var flag = TryFindShootLineFromTo(caster.Position, currentTarget, out var resultingLine);
        if (verbProps.stopBurstWithoutLos && !flag) return false;
        if (EquipmentSource != null)
        {
            EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
        }

        var launcher = caster;
        Thing equipment = EquipmentSource;
        var compMannable = caster.TryGetComp<CompMannable>();
        if (compMannable != null && compMannable.ManningPawn != null)
        {
            launcher = compMannable.ManningPawn;
            equipment = caster;
        }

        var drawPos = caster.DrawPos;
        var projectile2 = (Projectile_Pokeball)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
        if (verbProps.ForcedMissRadius > 0.5f)
        {
            var num = VerbUtility.CalculateAdjustedForcedMiss(
                verbProps.ForcedMissRadius, currentTarget.Cell - caster.Position
            );
            if (num > 0.5f)
            {
                var max = GenRadial.NumCellsInRadius(num);
                var num2 = Rand.Range(0, max);
                if (num2 > 0)
                {
                    var c = currentTarget.Cell + GenRadial.RadialPattern[num2];
                    ThrowDebugText("ToRadius");
                    ThrowDebugText("Rad\nDest", c);
                    var projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                    if (Rand.Chance(0.5f)) projectileHitFlags = ProjectileHitFlags.All;
                    if (!canHitNonTargetPawnsNow) projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                    projectile2.Launch(launcher, drawPos, c, currentTarget, projectileHitFlags, equipment);
                    return true;
                }
            }
        }

        var shotReport = PokeBallShotReport.HitReportFor(caster, this, currentTarget);
        var randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
        var targetCoverDef = randomCoverToMissInto?.def;
        if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
        {
            resultingLine.ChangeDestToMissWild_NewTemp(shotReport.AimOnTargetChance_StandardTarget, false, caster.Map);
            ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
            ThrowDebugText("Wild\nDest", resultingLine.Dest);
            var projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
            if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow) projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
            projectile2.Launch(
                launcher, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags2, equipment, targetCoverDef
            );
            return true;
        }

        if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn &&
            !Rand.Chance(shotReport.PassCoverChance))
        {
            ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
            ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
            var projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
            if (canHitNonTargetPawnsNow) projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
            projectile2.Launch(
                launcher, drawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, equipment, targetCoverDef
            );
            return true;
        }

        var projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
        if (canHitNonTargetPawnsNow) projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
        if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
            projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
        ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
        if (currentTarget.Thing != null)
        {
            projectile2.Launch(
                launcher, drawPos, currentTarget, currentTarget, projectileHitFlags4, equipment, targetCoverDef
            );
            ThrowDebugText("Hit\nDest", currentTarget.Cell);
        }
        else
        {
            projectile2.Launch(
                launcher, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags4, equipment, targetCoverDef
            );
            ThrowDebugText("Hit\nDest", resultingLine.Dest);
        }

        return true;
    }

    private void ThrowDebugText(string text)
    {
        if (DebugViewSettings.drawShooting) MoteMaker.ThrowText(caster.DrawPos, caster.Map, text);
    }

    private void ThrowDebugText(string text, IntVec3 c)
    {
        if (DebugViewSettings.drawShooting) MoteMaker.ThrowText(c.ToVector3Shifted(), caster.Map, text);
    }

    public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
    {
        needLOSToCenter = true;
        return Projectile?.projectile.explosionRadius ?? 0f;
    }

    public override bool Available()
    {
        if (!base.Available()) return false;
        if (CasterIsPawn)
        {
            var casterPawn = CasterPawn;
            if (casterPawn.Faction != Faction.OfPlayer && casterPawn.mindState.MeleeThreatStillThreat &&
                casterPawn.mindState.meleeThreat.Position.AdjacentTo8WayOrInside(casterPawn.Position)) return false;
        }

        return Projectile != null;
    }
}
