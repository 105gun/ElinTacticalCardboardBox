using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;
using CardboardBoxMod;
/*
// AI_Steal.Run
[HarmonyPatch(typeof(AI_Steal), nameof(AI_Steal.Run))]
class FultonPatch
{
    static IEnumerable<AIAct.Status> Postfix(IEnumerable<AIAct.Status> values, AI_Steal __instance)
    {
        Chara target = __instance.target.Chara;
        Chara player = EClass.pc.Chara;
        if (target == null || player == null)
        {
            goto Passthrough;
        }
        if (Plugin.IsUsingCardboardBox(player))
        {
            // Using Fulton instead of steal
            __instance.owner.Say("loytel_bill_give_wait");
            yield return __instance.Cancel();
            yield break;
        }
Passthrough:
        foreach (AIAct.Status value in values)
        {
            yield return value;
        }
    }
}*/