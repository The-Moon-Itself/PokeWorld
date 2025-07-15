﻿using UnityEngine;
using Verse;

namespace PokeWorld;

public abstract class PawnKindColumnWorker_Icon : PawnKindColumnWorker
{
    protected virtual int Width => 26;

    protected virtual int Padding => 2;

    public override void DoCell(Rect rect, PawnKindDef pawnKind, PawnKindTable table)
    {
        var iconFor = GetIconFor(pawnKind);
        if (!(iconFor != null)) return;
        var iconSize = GetIconSize(pawnKind);
        var num = (int)((rect.width - iconSize.x) / 2f);
        var num2 = Mathf.Max((int)((30f - iconSize.y) / 2f), 0);
        var rect2 = new Rect(rect.x + num, rect.y + num2, iconSize.x, iconSize.y);
        GUI.color = GetIconColor(pawnKind);
        GUI.DrawTexture(rect2.ContractedBy(Padding), iconFor);
        GUI.color = Color.white;
        if (Mouse.IsOver(rect2))
        {
            var iconTip = GetIconTip(pawnKind);
            if (!iconTip.NullOrEmpty()) TooltipHandler.TipRegion(rect2, iconTip);
        }

        if (Widgets.ButtonInvisible(rect2, false)) ClickedIcon(pawnKind);
    }

    public override int GetMinWidth(PawnKindTable table)
    {
        return Mathf.Max(base.GetMinWidth(table), Width);
    }

    public override int GetMaxWidth(PawnKindTable table)
    {
        return Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table));
    }

    public override int GetMinCellHeight(PawnKindDef pawnKind)
    {
        return Mathf.Max(base.GetMinCellHeight(pawnKind), Mathf.CeilToInt(GetIconSize(pawnKind).y));
    }

    public override int Compare(PawnKindDef a, PawnKindDef b)
    {
        return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
    }

    private int GetValueToCompare(PawnKindDef pawnKind)
    {
        var iconFor = GetIconFor(pawnKind);
        if (!(iconFor != null)) return int.MinValue;
        return iconFor.GetInstanceID();
    }

    protected abstract Texture2D GetIconFor(PawnKindDef pawnKind);

    protected virtual string GetIconTip(PawnKindDef pawnKind)
    {
        return null;
    }

    protected virtual Color GetIconColor(PawnKindDef pawnKind)
    {
        return Color.white;
    }

    protected virtual void ClickedIcon(PawnKindDef pawnKind)
    {
    }

    protected virtual void PaintedIcon(PawnKindDef pawnKind)
    {
    }

    protected virtual Vector2 GetIconSize(PawnKindDef pawnKind)
    {
        if (GetIconFor(pawnKind) == null) return Vector2.zero;
        return new Vector2(Width, Width);
    }
}
