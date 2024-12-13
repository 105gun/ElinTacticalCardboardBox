using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Assertions;

namespace CardboardBoxMod;

public enum CardboardBoxLogLevel
{
    None,
    Error,
    Warning,
    Info,
    Debug
};

[BepInPlugin("105gun.cardboardbox.mod", "Tactical Cardboard Box", "1.1.2.0")]
public class Plugin : BaseUnityPlugin
{
    static CardboardBoxLogLevel pluginLogLevel = CardboardBoxLogLevel.Info;
    public static string ridePCC = "cardboardbox";
    public static string fultonPCC = "fulton";

    private void Start()
    {
        ModLog("Initializing");
        var harmony = new Harmony("105gun.cardboardbox.mod");
        harmony.PatchAll();
        LoadData();
        ModLog("Initialization completed");
    }

    private void LoadData()
    {
		var dir = Path.GetDirectoryName(Info.Location);
		var excel = dir + "/Data/SourceCard.xlsx";
		var sources = Core.Instance.sources;
		ModUtil.ImportExcel(excel, "Chara", sources.charas);
		ModUtil.ImportExcel(excel, "CharaText", sources.charaText);
		ModUtil.ImportExcel(excel, "Element", sources.elements);

        ClassCache.caches.Create<AI_CQC>("CardboardBoxMod.AI_CQC", "ElinTacticalCardboardBox");

        // Icon of AI_CQC
        Texture2D tex = IO.LoadPNG(dir + "/Texture/AI_CQC.png");
        tex.name = "CardboardBoxMod.AI_CQC";// Path.GetFileNameWithoutExtension("AI_CQC.png");
        Sprite newSprite = Sprite.Create(tex,new Rect(0,0,tex.width, tex.height), Vector2.one * 0.5f);
        newSprite.name = tex.name;
        SpriteSheet.Add(newSprite);

        AudioLoader.Init();
    }

    public static void ModLog(string message, CardboardBoxLogLevel logLevel = CardboardBoxLogLevel.Info)
    {
        if (logLevel > pluginLogLevel)
        {
            return;
        }
        switch (logLevel)
        {
            case CardboardBoxLogLevel.Error:
                message = $"[CardboardBox][Error] {message}";
                break;
            case CardboardBoxLogLevel.Warning:
                message = $"[CardboardBox][Warning] {message}";
                break;
            case CardboardBoxLogLevel.Info:
                message = $"[CardboardBox][Info] {message}";
                break;
            case CardboardBoxLogLevel.Debug:
                message = $"[CardboardBox][Debug] {message}";
                break;
            default:
                break;
        }
        System.Console.WriteLine(message);
    }

    public static bool IsUsingCardboardBox(Chara chara)
    {
        if (chara.ride != null && IsCardboardBox(chara.ride))
        {
            return true;
        }
        return false;
    }

    public static bool IsCardboardBox(Card card)
    {
        if (card.c_idRidePCC != null && card.c_idRidePCC == ridePCC && card.id == "boxchicken") // Yes, the cardboard box is a chicken
        {
            return true;
        }
        return false;
    }
}