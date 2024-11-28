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

public class AI_CQC : AI_TargetCard
{
    public Func<Chara, bool> funcWitness;

    public AI_CQC()
    {
        // Porting form AI_Steal
        funcWitness = delegate(Chara c)
        {
            int num = c.CanSee(owner) ? 0 : 30;
            int num2 = c.PER * 250 / 100;
            if (c.conSleep != null)
            {
                return false;
            }
            if (c.IsUnique)
            {
                num2 *= 2;
            }
            return EClass.rnd(num2) > (this.owner.HasElement(152) ? this.owner.Evalue(152) : 0) + num;
        };
    }

    public override TargetType TargetType
	{
		get
		{
			return TargetType.SelfAndNeighbor;
		}
	}

    public override bool IsValidTC(Card c)
	{
		return !EClass._zone.IsUserZone && !c.isThing && c.trait.CanBeStolen && !c.IsPCFactionOrMinion;
	}

	public override int MaxRadius
	{
		get
		{
			return 2;
		}
	}
	public override bool IsHostileAct
	{
		get
		{
			return true;
		}
	}

	public override bool CanPerform()
	{
		return Act.TC != null;
	}

    public override bool Perform()
    {
		this.target = Act.TC;
		return base.Perform();
    }
    
    public System.Collections.IEnumerator FultionActionCoroutine(Chara chara)
    {
        // Create a fulton balloon, than do a fake ride
        Chara boxChicken = CharaGen.Create("boxchicken", -1);
        EClass._zone.AddCard(boxChicken, Act.TP.GetNearestPoint(false, true, true, false));
        boxChicken.c_idRidePCC = Plugin.fultonPCC;
		chara.ride = boxChicken;
        boxChicken.host = chara;
        boxChicken._CreateRenderer();
        boxChicken.noMove = true;
        chara.SyncRide();
        chara.Refresh(false);
        chara.noMove = true;

        if (EClass.rnd(4) == 0)
        {
            chara.SayRaw("Hey, what?");
        }

        DOTween.To(() => chara.renderer.position.y, delegate(float x)
        {
            chara.renderer.position = new Vector3(chara.renderer.position.x, x, chara.renderer.position.z);
        }, chara.renderer.position.y + 0.2f, 0.4f).SetEase(Ease.InOutSine);
        yield return new WaitForSecondsRealtime(1.3f);
        chara.PlaySound("custom_MGSBallon", 1f, true);
        yield return new WaitForSecondsRealtime(0.2f);
        
        chara.SayRaw("AAAAAAAaaaaaaaaaaaaaaaa");
        DOTween.To(() => chara.renderer.position.y, delegate(float x)
        {
            chara.renderer.position = new Vector3(chara.renderer.position.x + UnityEngine.Random.Range(-0.1f, 0.1f), x, chara.renderer.position.z);
        }, chara.renderer.position.y + 12.0f, 1.0f).SetEase(Ease.OutExpo);
        yield return new WaitForSecondsRealtime(1.01f);

        // Clean up
        chara.ride = null;
        chara.noMove = false;
        EClass._zone.RemoveCard(boxChicken);
        boxChicken.Destroy();
        chara.SetDirtySpeed();

        if (chara.Cell.HasRoof || EClass._map.IsIndoor)
        {
            // Indoor
            if (EClass.rnd(2) == 0)
            {
                // Poor guy...
			    Msg.SetColor("negative");
                Msg.SayRaw($"{chara.Name} hit the rood and smashed into pieces... R.I.P ");
                chara.AddBlood(20);
                chara.Die(); // R.I.P
            }
            else
            {
                // Move to home branch
                EClass._zone.RemoveCard(chara);
			    Msg.SetColor("positive");
                Msg.SayRaw($"{chara.Name} hit through the roof! ");
                chara._MakeAlly();
                EClass.pc.Say("hire", chara, null, null);
                EClass.Sound.Play("custom_MGSTeleport");
                EClass.player.ModKarma(-1);
                chara.Die();
            }
        }
        else
        {
            // Move to home branch
            EClass._zone.RemoveCard(chara);
			Msg.SetColor("positive");
            Msg.SayRaw($"{chara.Name} fly away! ");
            chara._MakeAlly();
            EClass.pc.Say("hire", chara, null, null);
            EClass.Sound.Play("custom_MGSTeleport");
            EClass.player.ModKarma(-1);
            chara.Die();
        }
        yield break;
    }

	public override IEnumerable<AIAct.Status> Run()
	{
		Chara chara = this.target.Chara;
		Card card = chara;
		Card root = card.GetRootCard();
        if (chara != null)
        {
            if (chara.HasCondition<ConSleep>())
            {
                // Fulton actions
                Msg.SayRaw($"Applying Fulton recovery system on {chara.Name}. ");
                if (chara.Cell.HasRoof || EClass._map.IsIndoor)
                {
			        Msg.SetColor("ono");
                    Msg.SayRaw($"It's dangerous to use Fulton indoor... ");
                }
                this.owner.PlaySound("custom_MGSSelectItem", 1f, true);
                int skill = this.owner.HasElement(281) ? this.owner.Evalue(281) : 1;
                int duration = Mathf.Clamp((int)(20 * Mathf.Sqrt(chara.LV / skill)), 10, 100);
                
                Progress_Custom progress_Custom = new Progress_Custom();
                progress_Custom.canProgress = (() => (chara == null || chara.ExistsOnMap));
                progress_Custom.onProgressBegin = delegate()
                {
                };progress_Custom.onProgress = delegate(Progress_Custom p)
                {
                    this.owner.LookAt(root);
                    this.owner.PlaySound("steal", 1f, true);
                    root.renderer.PlayAnime(AnimeID.Shiver, default(Vector3), false);
                    if (chara.conSleep == null)
                    {
                        chara.DoHostileAction(this.owner, false);
                        p.Cancel();
                        return;
                    }
                    if (chara != null && this.owner.Dist(chara) > 1)
                    {
                        EClass.pc.TryMoveTowards(chara.pos);
                        if (this.owner == null)
                        {
                            p.Cancel();
                            return;
                        }
                        if (chara != null && this.owner.Dist(chara) > 1)
                        {
                            EClass.pc.Say("targetTooFar", null, null);
                            p.Cancel();
                            return;
                        }
                    }
                    int count = this.owner.pos.ListWitnesses(this.owner, 6, WitnessType.crime, chara).Count;
                    Point pos = this.owner.pos;
                    if (pos.TryWitnessCrime(this.owner, chara, 5, funcWitness))
                    {
                        p.Cancel();
                        return;
                    }
                    this.owner.elements.ModExp(281, Mathf.Min(count * 5 + 5, 25), false);
                };
                progress_Custom.onProgressComplete = delegate()
                {
                    // The actual fulton action, use some random MonoBehaviour to run the coroutine for us
                    EMono.screen.StartCoroutine(FultionActionCoroutine(chara));
                    this.owner.elements.ModExp(281, 100, false);
                    EClass.pc.stamina.Mod(-EClass.rnd(5));
                };
                Progress_Custom seq = progress_Custom.SetDuration(duration, 4);
                yield return base.Do(seq, null);
                yield break;
            }
            else
            {
                // CQC actions
                this.owner.LookAt(root);
                this.owner.PlaySound("kick", 1f, true);
                root.renderer.PlayAnime(AnimeID.Attack, default(Vector3), false);
                Act.TP.Animate(AnimeID.HitObj, true);
                int CQCSkill = EClass.rnd(Act.CC.STR) + (Act.CC.HasElement(100) ? Act.CC.Evalue(100) : 0);
                if (CQCSkill > EClass.rndHalf(chara.PER))
                {
                    // Success
			        Msg.SetColor("positive");
                    Msg.SayRaw($"You knocked {chara.Name} unconscious... ");
                    chara.AddCondition<ConSleep>(300 + 10 * EClass.rnd(this.owner.STR), force: true);
                    this.owner.ModExp(100, 75);
                    this.owner.pos.TryWitnessCrime(owner, chara, 3, funcWitness);

                    // Rest hostiles
                    chara.hostility = chara.OriginalHostility;
                    chara.enemy = null;
                }
                else
                {
                    // Fail
			        Msg.SetColor("negative");
                    Msg.SayRaw($"{chara.Name} dodged your attack! ");
                    chara.DoHostileAction(this.owner, false);
                    EClass.player.ModKarma(-1);
                    this.owner.ModExp(100, 25);
                    this.owner.pos.TryWitnessCrime(owner, chara, 5, funcWitness);
                }
            }
        }
        yield break;
    }

    public override void OnCancel()
    {
        EClass.pc.PlaySound("custom_MGSError", 1f, true);
        base.OnCancel();
    }
}
/*
// CharaRenderer.Draw
[HarmonyPatch(typeof(CharaRenderer), nameof(CharaRenderer.UpdatePosition))]
class CharaRendererPatch
{
    public static bool pnt = false;
    static void Postfix(CharaRenderer __instance)
    {
        if (pnt)
        {
            pnt = false;
            Plugin.ModLog($"test {new StackTrace().ToString()}", CardboardBoxLogLevel.Error);
        }
    }
}
*/
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