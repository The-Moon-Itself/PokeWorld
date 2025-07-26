using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PokeWorld;

public class Projectile_Pokeball : Projectile
{
    private Sustainer ambientSustainer;
    public float bonusBall;
    private int ticksToDetonation;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksToDetonation, "PW_ticksToDetonation");
        Scribe_Values.Look(ref bonusBall, "PW_bonusBall");
    }

    protected override void Tick()
    {
        base.Tick();
        if (ticksToDetonation > 0)
        {
            ticksToDetonation--;
            if (ticksToDetonation <= 0) Explode();
        }
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (def.projectile.explosionDelay == 0)
        {
            Explode();
            return;
        }

        landed = true;
        ticksToDetonation = def.projectile.explosionDelay;
    }

    public void Launch(
        Thing launcher, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags,
        Thing equipment = null
    )
    {
        Launch(launcher, Position.ToVector3Shifted(), usedTarget, intendedTarget, hitFlags, false, equipment);
    }
    /*
    public void Launch(
        Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
        ProjectileHitFlags hitFlags, Thing equipment = null, ThingDef targetCoverDef = null
    )
    {
        this.launcher = launcher;
        this.origin = origin;
        this.usedTarget = usedTarget;
        this.intendedTarget = intendedTarget;
        this.targetCoverDef = targetCoverDef;
        HitFlags = hitFlags;
        if (equipment != null)
        {
            this.equipment = equipment;
            bonusBall = this.equipment.GetStatValue(DefDatabase<StatDef>.GetNamed("PW_BonusBall"));
            equipmentDef = equipment.def;
            weaponDamageMultiplier = equipment.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier);
        }
        else
        {
            equipmentDef = null;
            weaponDamageMultiplier = 1f;
        }

        destination = usedTarget.Cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
        ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
        if (ticksToImpact < 1) ticksToImpact = 1;
        if (!def.projectile.soundAmbient.NullOrUndefined())
        {
            var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
        }
    }
    */

    public override void Launch(
        Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
        ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null
    )
    {
        this.launcher = launcher;
        this.origin = origin;
        this.usedTarget = usedTarget;
        this.intendedTarget = intendedTarget;
        this.targetCoverDef = targetCoverDef;
        this.preventFriendlyFire = preventFriendlyFire;
        HitFlags = hitFlags;
        stoppingPower = def.projectile.stoppingPower;
        if (stoppingPower == 0f && def.projectile.damageDef != null)
        {
            stoppingPower = def.projectile.damageDef.defaultStoppingPower;
        }
        if (equipment != null)
        {
            this.equipment = equipment;
            equipmentDef = equipment.def;
            equipment.TryGetQuality(out equipmentQuality);
            if (equipment.TryGetComp(out CompUniqueWeapon comp))
            {
                foreach (WeaponTraitDef item in comp.TraitsListForReading)
                {
                    if (!Mathf.Approximately(item.additionalStoppingPower, 0f))
                    {
                        stoppingPower += item.additionalStoppingPower;
                    }
                }
            }
            bonusBall = this.equipment.GetStatValue(DefDatabase<StatDef>.GetNamed("PW_BonusBall"));
        }
        else
        {
            equipmentDef = null;
        }

        destination = usedTarget.Cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.3f);
        ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
        if (ticksToImpact < 1) ticksToImpact = 1;
        if (!def.projectile.soundAmbient.NullOrUndefined())
        {
            var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
        }
    }

    protected virtual void Explode()
    {
        var map = Map;
        Destroy();
        if (def.projectile.explosionEffect != null)
        {
            var effecter = def.projectile.explosionEffect.Spawn();
            effecter.Trigger(new TargetInfo(Position, map), new TargetInfo(Position, map));
            effecter.Cleanup();
        }

        GenPokeBallExplosion.DoExplosion(
            Position, map, def.projectile.explosionRadius, def.projectile.damageDef, launcher, bonusBall,
            base.DamageAmount, base.ArmorPenetration, def.projectile.soundExplode, equipmentDef, def,
            intendedTarget.Thing, def.projectile.postExplosionSpawnThingDef, def.projectile.postExplosionSpawnChance,
            def.projectile.postExplosionSpawnThingCount,
            preExplosionSpawnThingDef: def.projectile.preExplosionSpawnThingDef,
            preExplosionSpawnChance: def.projectile.preExplosionSpawnChance,
            preExplosionSpawnThingCount: def.projectile.preExplosionSpawnThingCount,
            applyDamageToExplosionCellsNeighbors: def.projectile.applyDamageToExplosionCellsNeighbors,
            chanceToStartFire: def.projectile.explosionChanceToStartFire,
            damageFalloff: def.projectile.explosionDamageFalloff, direction: origin.AngleToFlat(destination)
        );
    }
}
