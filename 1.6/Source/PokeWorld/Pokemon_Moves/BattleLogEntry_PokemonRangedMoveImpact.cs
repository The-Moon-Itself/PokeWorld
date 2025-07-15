﻿using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace PokeWorld;

public class BattleLogEntry_PokemonRangedMoveImpact : LogEntry_DamageResult
{
    [TweakValue("LogFilter", 0f, 1f)]
    private static float DisplayChanceOnMiss = 0.25f;

    private ThingDef coverDef;
    private Pawn initiatorPawn;

    private ThingDef initiatorThing;

    private MoveDef moveDef;

    private bool originalTargetMobile;

    private Pawn originalTargetPawn;

    private ThingDef originalTargetThing;

    private ThingDef projectileDef;

    private Pawn recipientPawn;

    private ThingDef recipientThing;

    public BattleLogEntry_PokemonRangedMoveImpact()
    {
    }

    public BattleLogEntry_PokemonRangedMoveImpact(
        Thing initiator, Thing recipient, Thing originalTarget, MoveDef moveDef, ThingDef projectileDef,
        ThingDef coverDef
    )
    {
        if (initiator is Pawn)
            initiatorPawn = initiator as Pawn;
        else if (initiator != null) initiatorThing = initiator.def;
        if (recipient is Pawn)
            recipientPawn = recipient as Pawn;
        else if (recipient != null) recipientThing = recipient.def;
        if (originalTarget is Pawn)
        {
            originalTargetPawn = originalTarget as Pawn;
            originalTargetMobile = !originalTargetPawn.Downed && !originalTargetPawn.Dead && originalTargetPawn.Awake();
        }
        else if (originalTarget != null)
        {
            originalTargetThing = originalTarget.def;
        }

        this.moveDef = moveDef;
        this.projectileDef = projectileDef;
        this.coverDef = coverDef;
    }

    private string InitiatorName
    {
        get
        {
            if (initiatorPawn != null) return initiatorPawn.LabelShort;
            if (initiatorThing != null) return initiatorThing.defName;
            return "null";
        }
    }

    private string RecipientName
    {
        get
        {
            if (recipientPawn != null) return recipientPawn.LabelShort;
            if (recipientThing != null) return recipientThing.defName;
            return "null";
        }
    }

    public override bool Concerns(Thing t)
    {
        if (t != initiatorPawn && t != recipientPawn) return t == originalTargetPawn;
        return true;
    }

    public override IEnumerable<Thing> GetConcerns()
    {
        if (initiatorPawn != null) yield return initiatorPawn;
        if (recipientPawn != null) yield return recipientPawn;
        if (originalTargetPawn != null) yield return originalTargetPawn;
    }

    public override bool CanBeClickedFromPOV(Thing pov)
    {
        if (recipientPawn != null)
        {
            if (pov != initiatorPawn || !CameraJumper.CanJump(recipientPawn))
            {
                if (pov == recipientPawn) return CameraJumper.CanJump(initiatorPawn);
                return false;
            }

            return true;
        }

        return false;
    }

    public override void ClickedFromPOV(Thing pov)
    {
        if (recipientPawn == null) return;
        if (pov == initiatorPawn)
        {
            CameraJumper.TryJumpAndSelect(recipientPawn);
            return;
        }

        if (pov == recipientPawn)
        {
            CameraJumper.TryJumpAndSelect(initiatorPawn);
            return;
        }

        throw new NotImplementedException();
    }

    public override Texture2D IconFromPOV(Thing pov)
    {
        if (damagedParts.NullOrEmpty()) return null;
        if (deflected) return null;
        if (pov == null || pov == recipientPawn) return Blood;
        if (pov == initiatorPawn) return BloodTarget;
        return null;
    }

    protected override BodyDef DamagedBody()
    {
        if (recipientPawn == null) return null;
        return recipientPawn.RaceProps.body;
    }

    protected override GrammarRequest GenerateGrammarRequest()
    {
        var result = base.GenerateGrammarRequest();
        if (recipientPawn != null || recipientThing != null)
            result.Includes.Add(deflected ? RulePackDefOf.Combat_RangedDeflect : RulePackDefOf.Combat_RangedDamage);
        else
            result.Includes.Add(RulePackDefOf.Combat_RangedMiss);
        if (initiatorPawn != null)
            result.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiatorPawn, result.Constants));
        else if (initiatorThing != null)
            result.Rules.AddRange(GrammarUtility.RulesForDef("INITIATOR", initiatorThing));
        else
            result.Constants["INITIATOR_missing"] = "True";
        if (recipientPawn != null)
            result.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipientPawn, result.Constants));
        else if (recipientThing != null)
            result.Rules.AddRange(GrammarUtility.RulesForDef("RECIPIENT", recipientThing));
        else
            result.Constants["RECIPIENT_missing"] = "True";
        if (originalTargetPawn != recipientPawn || originalTargetThing != recipientThing)
        {
            if (originalTargetPawn != null)
            {
                result.Rules.AddRange(
                    GrammarUtility.RulesForPawn("ORIGINALTARGET", originalTargetPawn, result.Constants)
                );
                result.Constants["ORIGINALTARGET_mobile"] = originalTargetMobile.ToString();
            }
            else if (originalTargetThing != null)
            {
                result.Rules.AddRange(GrammarUtility.RulesForDef("ORIGINALTARGET", originalTargetThing));
            }
            else
            {
                result.Constants["ORIGINALTARGET_missing"] = "True";
            }
        }

        result.Rules.AddRange(CustomPlayLogEntryUtility.RulesForOptionalMove("WEAPON", moveDef, projectileDef));
        if (initiatorPawn != null && initiatorPawn.skills != null)
            result.Constants["INITIATOR_skill"] =
                initiatorPawn.skills.GetSkill(SkillDefOf.Shooting).Level.ToStringCached();
        if (recipientPawn != null && recipientPawn.skills != null)
            result.Constants["RECIPIENT_skill"] =
                recipientPawn.skills.GetSkill(SkillDefOf.Shooting).Level.ToStringCached();
        result.Constants["COVER_missing"] = coverDef != null ? "False" : "True";
        if (coverDef != null) result.Rules.AddRange(GrammarUtility.RulesForDef("COVER", coverDef));
        return result;
    }

    public override bool ShowInCompactView()
    {
        if (!deflected)
        {
            if (recipientPawn != null) return true;
            if (originalTargetThing != null && originalTargetThing == recipientThing) return true;
        }

        var num = 1;
        if (moveDef != null && moveDef.verb != null) num = moveDef.verb.burstShotCount;
        return Rand.ChanceSeeded(DisplayChanceOnMiss / num, logID);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref initiatorPawn, "initiatorPawn", true);
        Scribe_Defs.Look(ref initiatorThing, "initiatorThing");
        Scribe_References.Look(ref recipientPawn, "recipientPawn", true);
        Scribe_Defs.Look(ref recipientThing, "recipientThing");
        Scribe_References.Look(ref originalTargetPawn, "originalTargetPawn", true);
        Scribe_Defs.Look(ref originalTargetThing, "originalTargetThing");
        Scribe_Values.Look(ref originalTargetMobile, "originalTargetMobile");
        Scribe_Defs.Look(ref moveDef, "moveDef");
        Scribe_Defs.Look(ref projectileDef, "projectileDef");
        Scribe_Defs.Look(ref coverDef, "coverDef");
    }

    public override string ToString()
    {
        return "BattleLogEntry_PokemonRangedMoveImpact: " + InitiatorName + "->" + RecipientName;
    }
}
