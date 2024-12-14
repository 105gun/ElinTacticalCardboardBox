using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;

namespace CardboardBoxMod;


// ActRide.Perform
[HarmonyPatch(typeof(ActRide))]
public class ActRidePatch
{
    static int originShowRideValue = 2;

    static void StoreBoxToChicken(Thing box, Chara chara)
    {
        box.things.Clear();
        chara.AddThing(box, false);
    }

    static Thing GetBoxFromChicken(Chara chara)
    {
        Thing box = chara.things.Find<TraitContainer>();
        if (box != null && box.id == "cardboard_box")
        {
            return box;
        }
        return null;
    }

    static void DeliverChestTeleport()
    {
        int uidDest = 0;
        List<Zone> list = EClass.game.spatials.ListReturnLocations();
        if (list == null || list.Count == 0)
        {
            EClass.player.returnInfo = null;
            Msg.Say("returnAbort");
            return;
        }
        EClass.ui.AddLayer<LayerList>().SetList2<Zone>(list, (Zone a) => a.NameWithDangerLevel, delegate(Zone a, ItemGeneral b)
        {
            uidDest = a.uid;
            if (a is Zone_Dungeon)
            {
                uidDest = a.FindDeepestZone().uid;
            }
            EClass.player.returnInfo = new Player.ReturnInfo
            {
                turns = 1,
                uidDest = uidDest
            };
            EClass.player.EndTurn(true);
			Thing bill = ThingGen.CreateBill(25, false);
			EClass.pc.faction.TryPayBill(bill);
            EClass.pc.PlaySound("custom_MGSTeleport", 1f, true);
        }, delegate(Zone a, ItemGeneral b)
        {
            string lang = (a is Zone_Dungeon) ? a.TextDeepestLv : Lang.Get("surface");
            b.SetSubText(lang, 200, FontColor.Default, TextAnchor.MiddleRight);
            b.Build();
            b.button1.mainText.rectTransform.sizeDelta = new Vector2(350f, 20f);
        }, true).SetSize(500f, -1f).SetOnKill(delegate
        {
            if (uidDest == 0)
            {
                EClass.player.returnInfo = null;
                Msg.Say("returnAbort");
            }
        }).SetTitles("wReturn", null);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActRide.Perform))]
    static bool PerformPrefix()
    {
        // Check deliver chest first
        List<Card> deliverList = Act.TP.ListThings<TraitDeliveryChest>(true);
        List<Card> containerList = Act.TP.ListThings<TraitContainer>(false);
        bool errorAudioPlayed = false;

        if (deliverList.Count > 0 && Plugin.IsUsingCardboardBox(Act.CC))
        {
            DeliverChestTeleport();
            return false;
        }

        // Passthrough when the player is riding something
        if (Act.CC.ride != null)
        {
            return true;
        }

        // If there is a cardboard box in this location, check it first
        foreach (Card card in containerList)
        {
            if (card.id == "cardboard_box")
            {
                if (card.things.Count != 0)
                {
                    if (!errorAudioPlayed)
                    {
                        errorAudioPlayed = true;
                        EClass.pc.PlaySound("custom_MGSError", 1f, true);
                    }
			        Msg.SetColor("negative");
                    Msg.SayRaw("TCB_box_0".lang());
                    continue;
                }

                // If the player is not riding anything, and has a empty cardboard box in this location, they can "ride" it
                Chara boxChicken = CharaGen.Create("boxchicken", -1);
                EClass._zone.AddCard(boxChicken, Act.TP.GetNearestPoint(false, true, true, false));
                /*
				List<string> ridePCCList = EClass.core.pccs.sets["ride"].map["body"].map.Keys.ToList<string>();
                foreach (string ridePCC in ridePCCList)
                {
                    Plugin.ModLog($"Ride PCC: {ridePCC}", CardboardBoxLogLevel.Debug);
                }
                */
                boxChicken.c_idRidePCC = Plugin.ridePCC;
                boxChicken._CreateRenderer();

				ActRide.Ride(Act.CC, boxChicken, false);

                StoreBoxToChicken(card as Thing, boxChicken);
                return false;
            }
        }
        return true;
    }

    // This should be the only legal way to ride a cardboard box
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActRide.Ride))]
    static bool RidePrefix(Chara host, Chara t, bool parasite)
    {
        if (!parasite && Plugin.IsCardboardBox(t))
        {
            Plugin.ModLog("Riding cardboard box", CardboardBoxLogLevel.Debug);

            // Store the original value of showRide
            originShowRideValue = EClass.core.config.game.showRide;
            EClass.core.config.game.showRide = 2;
            
			if (host.ride != null)
			{
				ActRide.Unride(host, false);
			}
            Msg.SayRaw("TCB_box_1".lang());
            
			host.ride = t;
            if (!t.IsPCFaction)
            {
                t.MakeAlly(false);
            }
            
            t.host = host;
            t._CreateRenderer();
            host.PlaySound("custom_MGSSelectItem", 1f, true);
            host.SetDirtySpeed();
            t.SetDirtySpeed();
            host.SyncRide();
            t.noMove = false;
            host.Refresh(false);

            // DEBUG Adding CQC ability
            /*
            if (host.HasElement(6514) == false)
            {
                host.GainAbility(6514);
            }*/
            return false;
        }
        return true;
    }

    // This should be the only legal way to unride a cardboard box
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ActRide.Unride))]
    static bool UnridePrefix(Chara host, bool parasite)
    {
        if (!parasite && Plugin.IsCardboardBox(host.ride))
        {
            Plugin.ModLog("Unriding cardboard box", CardboardBoxLogLevel.Debug);
            Msg.SayRaw("TCB_box_2".lang());

            // Restore the original value of showRide
            EClass.core.config.game.showRide = originShowRideValue;

            Chara chara = host.ride;
			host.ride = null;
            chara.host = null;
            chara._CreateRenderer();
            host.PlaySound("custom_MGSSelectItem", 1f, true);
            host.SetDirtySpeed();
            chara.SetDirtySpeed();
            host.Refresh(false);
            EClass.pc.party.RemoveMember(chara);
            EClass.pc.homeBranch.RemoveMemeber(chara);
            EClass._zone.RemoveCard(chara);

            Thing box;
            if ((box = GetBoxFromChicken(chara)) == null)
            {
                Plugin.ModLog("Cardboard box not found in boxchicken", CardboardBoxLogLevel.Warning);
                box = ThingGen.Create("cardboard_box");
            }
            box.things.Clear();

            EClass._zone.AddCard(box, EClass.pc.pos);
            chara.Destroy();
            return false;
        }
        return true;
    }
}

// Force player sprite stand on ground
// CardRenderer.Draw
[HarmonyPatch(typeof(CardRenderer), nameof(CardRenderer.Draw),
    new Type[] { typeof(RenderParam), typeof(Vector3), typeof(bool) },
    new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
class DrawPatch
{
    static Vector3[] ridePosBak = new Vector3[1];
    static void Prefix(CardRenderer __instance, RenderParam p, ref Vector3 v, bool drawShadow)
    {
        if (__instance.owner is Chara)
        {
            Chara chara = (Chara)__instance.owner;
            if (Plugin.IsUsingCardboardBox(chara))
            {
                if (ridePosBak.Length != RenderObject.renderSetting.ridePos.Count())
                {
                    ridePosBak = new Vector3[RenderObject.renderSetting.ridePos.Count()];
                }
                for (int i = 0; i < RenderObject.renderSetting.ridePos.Count(); i++)
                {
                    ridePosBak[i] = RenderObject.renderSetting.ridePos[i];
                    RenderObject.renderSetting.ridePos[i].y = 0.0f;
                }
            }
        }
    }

    static void Postfix(CardRenderer __instance, RenderParam p, ref Vector3 v, bool drawShadow)
    {
        if (__instance.owner is Chara)
        {
            Chara chara = (Chara)__instance.owner;
            if (Plugin.IsUsingCardboardBox(chara))
            {
                for (int i = 0; i < RenderObject.renderSetting.ridePos.Count(); i++)
                {
                    RenderObject.renderSetting.ridePos[i] = ridePosBak[i];
                }
            }

            // Fulton extra render
            if (chara.ride != null && chara.ride.c_idRidePCC == Plugin.fultonPCC)
            {
                // Plugin.ModLog("Fulton fix", CardboardBoxLogLevel.Debug);
                Vector3 vector = new Vector3(v.x, v.y, v.z);
                chara.ride.renderer.Draw(p, ref vector, false);
            }
        }
    }
}

// Point.CallGuard
[HarmonyPatch(typeof(Point), nameof(Point.CallGuard))]
class CallGuardPatch
{
    static void Prefix(Point __instance, Chara criminal, Chara caller)
    {
        // if (Plugin.IsUsingCardboardBox(criminal))
        {
            caller.PlaySound("custom_MGSAlarm", 1f, true);
        }
    }
}

// ElementContainerCard.ValueBonus
[HarmonyPatch(typeof(ElementContainerCard), nameof(ElementContainerCard.ValueBonus))]
class ValueBonusPatch
{
    static void Postfix(ElementContainerCard __instance, Element e, ref int __result)
    {
		if (EClass.game == null)
		{
			return;
		}
        if (e.id == 152 && __instance.Chara != null && __instance.Chara.IsPC && Plugin.IsUsingCardboardBox(__instance.Chara))
        {
            // aka, stealth
            __result += 40;
        }
    }
}