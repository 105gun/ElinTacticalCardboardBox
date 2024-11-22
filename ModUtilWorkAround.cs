using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;

namespace CardboardBoxMod;

/*
 * Well, I found that the current implementation of ModUtil is incorrect.
 * When you load your own chara data from excel file, it simply call SourceChara.CreateRow in the end.
 * Which still need some extra initialization to make it possible to be rendered in the game:
 * RenderRow.OnImportData(_tiles) & SourceCard.AddRow(elementMap, renderData...), or maybe more.
 * In future it might be fixed. But for now, here is just a simple workaround.
 */


// SourceChara.CreateRow
[HarmonyPatch(typeof(SourceChara), nameof(SourceChara.CreateRow))]
class CreateRowPatch
{
    static void Postfix(SourceChara __instance, ref SourceChara.Row __result)
    {
        if (__result.id == "boxchicken") // id of my custom Chara
        {
            if (__result.elementMap == null)
            {
                __result._tiles = new int[0];
                Core.Instance.sources.cards.AddRow(__result, true);
            }
        }
    }
}