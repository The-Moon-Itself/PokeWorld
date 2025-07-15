using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PokeWorld;

internal class DamageWorker_TryCatch : DamageWorker
{
    public float bonusBall;

    public override DamageResult Apply(DamageInfo dinfo, Thing thing)
    {
        if (thing.GetType() == typeof(Pawn))
        {
            var pawn = thing as Pawn;
            var instigator = dinfo.Instigator as Pawn;
            Pawn targetPawn = null;
            if (pawn == null) return base.Apply(dinfo, thing);
            if (dinfo.IntendedTarget != null && dinfo.IntendedTarget.GetType() == typeof(Pawn))
                targetPawn = dinfo.IntendedTarget as Pawn;
            if (pawn == targetPawn)
            {
                var compPokemon = pawn.TryGetComp<CompPokemon>();
                if (pawn.AnimalOrWildMan() &&
                    (pawn.Faction == null || pawn.Faction == Find.FactionManager.AllFactions
                        .Where(f => f.def.defName == "PW_HostilePokemon").First()) && compPokemon != null &&
                    compPokemon.tryCatchCooldown <= 0)
                {
                    var compPokeballBelt = dinfo.Weapon.GetCompProperties<CompProperties_Pokeball>();
                    var catchRate = pawn.GetStatValue(DefDatabase<StatDef>.GetNamed("PW_CatchRate"));
                    if (catchRate > 0)
                    {
                        float bonusDowned = 1;
                        if (pawn.Downed) bonusDowned = 1.5f;
                        var currentHealthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
                        var aValue = (1 - 2 / 3f * currentHealthPercent) * catchRate * bonusBall * bonusDowned;
                        var bValue = aValue / 255;
                        var rand = Rand.Range(0f, 1f);
                        if (bValue > rand)
                        {
                            var compXpEvGiver = pawn.TryGetComp<CompXpEvGiver>();
                            if (compXpEvGiver != null) compXpEvGiver.DistributeAfterCatch();
                            InteractionWorker_RecruitAttempt.DoRecruit(instigator, pawn);
                            pawn.training.Train(DefDatabase<TrainableDef>.GetNamed("Obedience"), instigator, true);
                            pawn.training.SetWantedRecursive(DefDatabase<TrainableDef>.GetNamed("Obedience"), true);
                            pawn.ClearMind();
                            compPokemon.ballDef = compPokeballBelt.ballDef;
                            Find.World.GetComponent<PokedexManager>().AddPokemonKindCaught(pawn.kindDef);
                            PutInBallUtility.PutPokemonInBall(pawn);
                            if (instigator.Faction != null && instigator.Faction == Faction.OfPlayer &&
                                instigator.skills != null &&
                                !instigator.skills.GetSkill(SkillDefOf.Animals).TotallyDisabled)
                                instigator.skills.Learn(SkillDefOf.Animals, compPokemon.levelTracker.level * 50);
                        }
                        else
                        {
                            if (instigator.Faction != null && instigator.Faction == Faction.OfPlayer &&
                                instigator.skills != null &&
                                !instigator.skills.GetSkill(SkillDefOf.Animals).TotallyDisabled)
                                instigator.skills.Learn(SkillDefOf.Animals, compPokemon.levelTracker.level * 10);
                            compPokemon.tryCatchCooldown = 120;
                            string text = "PW_TextMoteCatchFailed".Translate(bValue.ToStringPercent());
                            if (pawn.Downed)
                            {
                                if (Rand.Chance(compPokemon.tryCatchKillChanceIfDown))
                                {
                                    pawn.Kill(dinfo);
                                    Messages.Message(
                                        "PW_MessagePokemonCatchDied".Translate(pawn.KindLabel), pawn.Corpse,
                                        MessageTypeDefOf.NegativeEvent
                                    );
                                }
                                else
                                {
                                    compPokemon.tryCatchKillChanceIfDown += 0.05f;
                                }
                            }
                            else if (Rand.Chance(pawn.RaceProps.manhunterOnTameFailChance))
                            {
                                if (pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter))
                                {
                                    string text2 = "AnimalManhunterFromTaming".Translate(pawn.Label, pawn.Named("PAWN"))
                                        .AdjustedFor(pawn);
                                    GlobalTargetInfo target = pawn;
                                    var num = 1;
                                    if (Find.Storyteller.difficulty.allowBigThreats && Rand.Value < 0.5f)
                                    {
                                        var pawnRoom = pawn.GetRoom();
                                        var raceMates = (List<Pawn>)pawn.Map.mapPawns.AllPawnsSpawned;
                                        for (var i = 0; i < raceMates.Count; i++)
                                            if (pawn != raceMates[i] && raceMates[i].def == pawn.def &&
                                                raceMates[i].Faction == pawn.Faction &&
                                                raceMates[i].Position.InHorDistOf(pawn.Position, 24f) &&
                                                raceMates[i].GetRoom() == pawnRoom)
                                                if (raceMates[i].mindState.mentalStateHandler
                                                    .TryStartMentalState(MentalStateDefOf.Manhunter))
                                                    num++;
                                        if (num > 1)
                                        {
                                            target = new TargetInfo(pawn.Position, pawn.Map);
                                            text2 += "\n\n";
                                            text2 += "AnimalManhunterOthers".Translate(
                                                pawn.kindDef.GetLabelPlural(), pawn
                                            );
                                        }
                                    }

                                    var value = pawn.RaceProps.Animal ? pawn.Label : pawn.def.label;
                                    string str = "LetterLabelAnimalManhunterRevenge".Translate(value).CapitalizeFirst();
                                    Find.LetterStack.ReceiveLetter(
                                        str, text2, num == 1 ? LetterDefOf.ThreatSmall : LetterDefOf.ThreatBig, target
                                    );
                                }
                            }
                            else if (!pawn.mindState.mentalStateHandler.InMentalState &&
                                     Find.TickManager.TicksGame - pawn.LastAttackTargetTick > 600)
                            {
                                pawn.mindState.StartFleeingBecauseOfPawnAction(dinfo.Instigator);
                            }

                            if (!pawn.Dead)
                            {
                                if (pawn.Downed)
                                    text += "\n" + "PW_TextMoteDownedChanceToDie".Translate(
                                        compPokemon.tryCatchKillChanceIfDown.ToStringPercent()
                                    );
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                            }
                        }
                    }
                }
                else if (dinfo.Weapon == DefDatabase<ThingDef>.GetNamed("PW_MasterBallBelt") &&
                         pawn.def == ThingDefOf.Human)
                {
                    var rand = Rand.Range(0f, 1f);
                    if (rand < 0.1f)
                    {
                        var compPokeballBelt = dinfo.Weapon.GetCompProperties<CompProperties_Pokeball>();
                        HealthUtility.DamageUntilDead(pawn);
                        PutInBallUtility.PutCorpseInBall(pawn.Corpse, compPokeballBelt.ballDef);
                    }
                    else
                    {
                        string text = "PW_TextMoteCatchFailed".Translate(0.1f.ToStringPercent());
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                    }
                }
                else if (compPokemon == null)
                {
                    string text = "PW_TextMoteCatchFailedNotPokemon".Translate();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                }
                else if (pawn.Faction != null && pawn.Faction != Find.FactionManager.AllFactions
                             .Where(f => f.def.defName == "PW_HostilePokemon").First())
                {
                    string text = "PW_TextMoteCatchFailedAlreadyOwned".Translate();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                }
                /*
                else if (pawn.Downed)
                {
                    string text = "PW_TextMoteCatchFailedFainted".Translate();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                }
                */
                else if (compPokemon.tryCatchCooldown > 0)
                {
                    string text = "PW_TextMoteCatchFailedDodged".Translate();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, text, 8f);
                }
            }
        }

        return base.Apply(dinfo, thing);
    }

    public override void ExplosionAffectCell(
        Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes
    )
    {
        bonusBall = ((PokeBallExplosion)explosion).bonusBall;
        base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
    }
}
