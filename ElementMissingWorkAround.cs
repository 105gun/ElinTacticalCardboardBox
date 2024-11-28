using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;
using System.Diagnostics;
using DG.Tweening;

namespace CardboardBoxMod;

/*
 * If the game was saved when player's currentHotItem was a custom ability,
 * it will try to initialize the HotItem with its custom element id, 
 * directly call ACT.Create(EClass.sources.elements.map[id]); and crash.
 * So we just set currentHotItem to null before saving and restore it after saving.
 */

// Game.Save
[HarmonyPatch(typeof(Game), nameof(Game.Save))]
class GameSavePatch
{
    static HotItem tempHotItem = null;
    static void Prefix(Game __instance)
    {
        if (EClass.player.currentHotItem is HotItemAct hotItemAct && hotItemAct.id == 6514)
        {
            tempHotItem = EClass.player.currentHotItem;
            EClass.player.currentHotItem = null;
            ELayer.player.RefreshCurrentHotItem();
        }
    }

    static void Postfix(Game __instance)
    {
        if (EClass.player == null || EClass.player.chara == null)
        {
            // Save when quitting to main menu
            return;
        }
        if (tempHotItem != null)
        {
            EClass.player.currentHotItem = tempHotItem;
            ELayer.player.RefreshCurrentHotItem();
            tempHotItem = null;
        }
    }
}