namespace PokeWorld;
/*
[HarmonyPatch(typeof(SymbolResolver_Interior_AncientTemple))]
[HarmonyPatch("Resolve")]
public class SymbolResolver_Interior_AncientTemple_Resolve_Patch
{
    public static void Postfix(ResolveParams __0)
    {
        if (PokeWorldSettings.allowGen2){
            int randInt = Rand.RangeInclusive(4, 7);
            for (int i = 0; i < randInt; i++)
            {
                ResolveParams resolveParams = __0;
                resolveParams.singlePawnKindDef = DefDatabase<PawnKindDef>.GetNamed("PW_Unown");
                BaseGen.symbolStack.Push("pawn", resolveParams);
            }
        }
        if (PokeWorldSettings.allowGen4){
            int randInt2 = Rand.RangeInclusive(1, 3);
            for (int i = 0; i < randInt2; i++)
            {
                ResolveParams resolveParams2 = __0;
                resolveParams2.singlePawnKindDef = DefDatabase<PawnKindDef>.GetNamed("PW_Bronzor");
                BaseGen.symbolStack.Push("pawn", resolveParams2);
            }
            if (Rand.Value < 0.3)
            {
                ResolveParams resolveParams3 = __0;
                resolveParams3.singlePawnKindDef = DefDatabase<PawnKindDef>.GetNamed("PW_Bronzong");
                BaseGen.symbolStack.Push("pawn", resolveParams3);
            }
        }
    }
}
*/
