using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace CardboardBoxMod;

class AudioLoader
{
    public static List<SoundData> soundDataList = new List<SoundData>();
    public static void Init()
    {
        Plugin.ModLog("Initializing AudioLoader");

        // Load all audio files from the Sound directory
        string directoryPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Sound");
        List<string> audioPaths = System.IO.Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav"))
            .Select(s => "file://" + s.Replace("\\", "/"))
            .ToList();

        // Add all audio files to soundDataList
        foreach (string path in audioPaths)
        {
            AudioClip clip = LoadFromDiskToAudioClip(path, AudioType.UNKNOWN);
            if (clip != null)
            {
                Plugin.ModLog($"Loaded audio file: {path}");
                soundDataList.Add(CreateSoundData(System.IO.Path.GetFileNameWithoutExtension(path), clip));
            }
        }
        Plugin.ModLog("AudioLoader initialized");
    }

    public static SoundData CreateSoundData(string fullName, AudioClip audioClip)
    {
        SoundData soundData = ScriptableObject.CreateInstance<SoundData>();

        // fullName has an optional postfix like "_1.0" to indicate the volume
        // soundData.name will have a "custom_" prefix to indicate that this is a custom sound
        if (fullName.Contains("_"))
        {
            string[] split = fullName.Split('_');
            soundData.name = $"custom_{split[0]}";
            if (split.Length > 1 && float.TryParse(split[1], out _))
                soundData.volume = float.Parse(split[1]);
            else
                soundData.volume = 0.5f;
        }
        else
        {
            soundData.name = $"custom_{fullName}";
            soundData.volume = 0.5f;
        }

        // Rest of the fields are copied from "ride" sound data
        soundData.clip = audioClip;
        soundData.loop = 0;
        soundData.allowMultiple = false;
        soundData.skipIfPlaying = false;
        soundData.important = false;
        soundData.alwaysPlay = false;
        soundData.fadeAtStart = false;
        soundData.delay = 0;
        soundData.startAt = 0;
        soundData.fadeLength = 0;
        soundData.type = SoundData.Type.Default;
        soundData.spatial = 0.5f;
        soundData.pitch = 1;
        soundData.randomPitch = 0;
        soundData.chance = 1;
        soundData.reverbMix = 1;
        soundData.minInterval = 0;
        soundData.variationPlayMethod = SoundData.VariationPlayMethod.Sequence;
        soundData.noSameSound = false;
        soundData.volumeAsMTP = false;
        soundData.variations = null;
        soundData.extraData = null;
        soundData.variationIndex = 0;
        soundData.lastVariation = null;
        soundData.lastPlayed = 0;
        soundData.altLastPlayed = 0;

        return soundData;
    }

    // Porting from https://github.com/susy-bakaa/LCSoundTool/blob/main/Utilities/AudioUtility.cs
    public static AudioClip LoadFromDiskToAudioClip(string path, AudioType type)
    {
        AudioClip clip = null;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, type))
        {
            uwr.SendWebRequest();

            try
            {
                while (!uwr.isDone)
                {

                }

                if (uwr.result != UnityWebRequest.Result.Success)
                    Plugin.ModLog($"Failed to load WAV AudioClip from path: {path} Full error: {uwr.error}", CardboardBoxLogLevel.Error);
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(uwr);
                }
            }
            catch (Exception err)
            {
                Plugin.ModLog($"{err.Message}, {err.StackTrace}", CardboardBoxLogLevel.Error);
            }
        }
        return clip;
    }

    public static void DumpSoundData(SoundData __result)
    {
        // Print every field of SoundData
        Plugin.ModLog($"\tclip :{__result.clip}");

        Plugin.ModLog($"\tname :{__result.name}");
        Plugin.ModLog($"\tloop :{__result.loop}");
        Plugin.ModLog($"\tallowMultiple :{__result.allowMultiple}");
        Plugin.ModLog($"\tskipIfPlaying :{__result.skipIfPlaying}");
        Plugin.ModLog($"\timportant :{__result.important}");
        Plugin.ModLog($"\talwaysPlay :{__result.alwaysPlay}");
        Plugin.ModLog($"\tfadeAtStart :{__result.fadeAtStart}");
        Plugin.ModLog($"\tdelay :{__result.delay}");
        Plugin.ModLog($"\tstartAt :{__result.startAt}");
        Plugin.ModLog($"\tfadeLength :{__result.fadeLength}");
        Plugin.ModLog($"\ttype :{__result.type}");

        Plugin.ModLog($"\tvolume :{__result.volume}");
        Plugin.ModLog($"\tspatial :{__result.spatial}");
        Plugin.ModLog($"\tpitch :{__result.pitch}");
        Plugin.ModLog($"\trandomPitch :{__result.randomPitch}");
        Plugin.ModLog($"\tchance :{__result.chance}");
        Plugin.ModLog($"\treverbMix :{__result.reverbMix}");
        Plugin.ModLog($"\tminInterval :{__result.minInterval}");
        Plugin.ModLog($"\tvariationPlayMethod :{__result.variationPlayMethod}");
        Plugin.ModLog($"\tnoSameSound :{__result.noSameSound}");
        Plugin.ModLog($"\tvolumeAsMTP :{__result.volumeAsMTP}");
        Plugin.ModLog($"\tvariations :{__result.variations}");
        Plugin.ModLog($"\textraData :{__result.extraData}");
        Plugin.ModLog($"\tvariationIndex :{__result.variationIndex}");
        Plugin.ModLog($"\tlastVariation :{__result.lastVariation}");
        Plugin.ModLog($"\tlastPlayed :{__result.lastPlayed}");
        Plugin.ModLog($"\taltLastPlayed :{__result.altLastPlayed}");
    }
}

// SoundManager.GetData
[HarmonyPatch(typeof(SoundManager))]
class SoundManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SoundManager.GetData))]
    static bool GetDataPrefix(SoundManager __instance, string id, ref SoundData __result)
    {
        // If the sound is a custom sound, return the custom sound data
        if (id != null && id.Contains("custom_"))
        {
            Plugin.ModLog($"Getting custom sound data: {id}", CardboardBoxLogLevel.Debug);
            foreach (SoundData soundData in AudioLoader.soundDataList)
            {
                if (soundData.name == id)
                {
                    __result = soundData;
                    return false;
                }
            }
        }
        return true;
    }
}

/*

An example

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
*/