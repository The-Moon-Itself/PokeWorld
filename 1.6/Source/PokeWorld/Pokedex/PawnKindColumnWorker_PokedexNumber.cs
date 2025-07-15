﻿using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PokeWorld;

public class PawnKindColumnWorker_PokedexNumber : PawnKindColumnWorker
{
    private const int LeftMargin = 3;

    private static readonly Dictionary<string, string> labelCache = new();

    private static float labelCacheForWidth = -1f;

    public override void DoCell(Rect rect, PawnKindDef pawnKind, PawnKindTable table)
    {
        var rect2 = new Rect(rect.x, rect.y, rect.width, Mathf.Min(rect.height, 30f));
        if (Mouse.IsOver(rect2)) GUI.DrawTexture(rect2, TexUI.HighlightTex);
        var str = pawnKind.race.GetCompProperties<CompProperties_Pokemon>().pokedexNumber.ToString();
        var rect4 = rect2;
        rect4.xMin += 3f;
        if (rect4.width != labelCacheForWidth)
        {
            labelCacheForWidth = rect4.width;
            labelCache.Clear();
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.WordWrap = false;
        Widgets.Label(rect4, str.Truncate(rect4.width, labelCache));
        Text.WordWrap = true;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    public override int GetMinWidth(PawnKindTable table)
    {
        return Mathf.Max(base.GetMinWidth(table), 40);
    }

    public override int GetOptimalWidth(PawnKindTable table)
    {
        return Mathf.Clamp(35, GetMinWidth(table), GetMaxWidth(table));
    }
}
