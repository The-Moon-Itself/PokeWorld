﻿using System.Text;
using RimWorld;
using Verse;

namespace PokeWorld;

internal class StatWorker_PokemonStats : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        if (!base.ShouldShowFor(req)) return false;
        if (req.HasThing && req.Thing.TryGetComp<CompPokemon>() != null) return true;
        return false;
    }

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        if (req.HasThing && req.Thing.TryGetComp<CompPokemon>() != null)
        {
            float value;
            switch (stat.defName)
            {
                case "PW_HP":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseHP;
                    break;
                case "PW_Attack":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseAttack;
                    break;
                case "PW_Defense":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseDefense;
                    break;
                case "PW_SpecialAttack":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseSpAttack;
                    break;
                case "PW_SpecialDefense":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseSpDefense;
                    break;
                case "PW_Speed":
                    value = req.Thing.TryGetComp<CompPokemon>().BaseSpeed;
                    break;
                default:
                    value = 0;
                    break;
            }

            return value;
        }

        return 0;
    }

    public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
    {
        if (req.HasThing)
        {
            var comp = req.Thing.TryGetComp<CompPokemon>();
            if (comp != null)
            {
                var IV = comp.statTracker.GetIV(stat);
                var EV = comp.statTracker.GetEV(stat);
                var level = comp.levelTracker.level;
                if (stat.defName == "PW_HP")
                {
                    val = (int)((2 * val + IV + EV / 4) * level / 100) + level + 10;
                }
                else
                {
                    var natureMultiplier = GetNatureMultiplier(comp, stat);
                    val = (int)(((2 * val + IV + EV / 4) * level / 100 + 5) * natureMultiplier);
                }
            }
        }
    }

    public float GetNatureMultiplier(CompPokemon comp, StatDef stat)
    {
        if (comp.statTracker.nature !=
            null) //Huge modelist sometimes cause unknown issue with nature save, see github issue
            return comp.statTracker.nature.GetMultiplier(stat);
        return 1;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        if (req.HasThing)
        {
            var comp = req.Thing.TryGetComp<CompPokemon>();
            if (comp != null && comp.statTracker != null)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(
                    "PW_StatBaseValue".Translate(GetValueUnfinalized(req), 2f.ToStringPercent()).ToLower()
                        .CapitalizeFirst()
                );
                if (stat.defName == "PW_HP")
                {
                    stringBuilder.AppendLine("   " + "PW_StatIndividualValue".Translate(comp.statTracker.GetIV(stat)));
                    stringBuilder.AppendLine("   " + "PW_StatEffortValue".Translate(comp.statTracker.GetEV(stat) / 4));
                    stringBuilder.AppendLine(
                        "   " + "PW_StatLevel".Translate(
                            comp.levelTracker.level, (comp.levelTracker.level / 100f).ToStringPercent()
                        ).ToLower().CapitalizeFirst()
                    );
                    stringBuilder.AppendLine(
                        "   " + "PW_StatHPAddLevel".Translate(comp.levelTracker.level, comp.levelTracker.level + 10)
                    );
                    //val = (int)((2 * val + IV + (EV / 4)) * level / 100) + level + 10;
                }
                else
                {
                    stringBuilder.AppendLine("   " + "PW_StatIndividualValue".Translate(comp.statTracker.GetIV(stat)));
                    stringBuilder.AppendLine("   " + "PW_StatEffortValue".Translate(comp.statTracker.GetEV(stat) / 4));
                    stringBuilder.AppendLine(
                        "   " + "PW_StatLevel".Translate(
                            comp.levelTracker.level, (comp.levelTracker.level / 100f).ToStringPercent()
                        ).ToLower().CapitalizeFirst()
                    );
                    stringBuilder.AppendLine("   " + "PW_StatAdd".Translate(5));
                    var natureMultiplier = GetNatureMultiplier(comp, stat);
                    if (natureMultiplier != 1f)
                        stringBuilder.AppendLine(
                            "   " + "PW_StatNatureMultiplier".Translate(natureMultiplier.ToStringPercent()).ToLower()
                                .CapitalizeFirst()
                        );
                    //val = (int)((((2 * val + IV + (EV / 4)) * level / 100) + 5) * natureMultiplier);
                }

                return stringBuilder.ToString();
            }
        }

        return "";
    }

    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        if (req.HasThing)
        {
            var comp = req.Thing.TryGetComp<CompPokemon>();
            if (comp != null && comp.statTracker != null)
            {
                var value = GetValueUnfinalized(req);
                FinalizeValue(req, ref value, true);
                return "PW_StatFinalValue".Translate(value.ToString());
            }
        }

        return "";
    }
}
