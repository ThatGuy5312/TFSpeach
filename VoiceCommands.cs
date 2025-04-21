using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static TFS.Notifications.NotifUtils;
using UnityEngine.Windows.Speech;
using TFS.Notifications;
using TFS.Utils;
using System.Linq;

public class VoiceCommands : MonoBehaviour
{
    // originaly made by kingofnetflix https://github.com/kingofnetflix/BAnANA/blob/master/BAnANA/BAnANA/Main/VoiceManager.cs
    private static KeywordRecognizer enablePhrase;
    private static KeywordRecognizer modPhrase;

    private static Dictionary<string, Action> commands;

    private static string[] phrase = { "TFS", "TFSS" };

    private static bool listening = false;

    public static bool canUseTFS = true;

    static string saidEnablePhrase;

    void Start()
    {
        commands = new Dictionary<string, Action>
        {
            { "Change Voice", ()=> TFS.Plugin.ChangeNarrationVoice() },
            { "Use Voice", ()=> TFS.Plugin.UseVoice() },
            { "Use TFS", ()=> TFS.Plugin.UseTFS() }
        };
        EnableVoiceCommands();
    }

    void Update()
    {
        if (enablePhrase == null && dictationRecognizer == null && PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
            EnableVoiceCommands();
    }

    public static void EnableVoiceCommands()
    {
        if (enablePhrase == null || !enablePhrase.IsRunning)
        {
            enablePhrase = new KeywordRecognizer(phrase);
            enablePhrase.OnPhraseRecognized += Recognition;
            enablePhrase.Start();
        }
    }

    private static void Recognition(PhraseRecognizedEventArgs args)
    {
        if (Array.Exists(phrase, element => element == args.text))
        {
            listening = true;
            saidEnablePhrase = args.text;
            StartCommandRecognition();
            listeningCoroutine = CoroutineUtils._RunCoroutine(Timeout());
            NotifiLib.SendNotification("listening..", MessageInfo.Voice);
        }
    }
    private static DictationRecognizer dictationRecognizer;
    private static void StartCommandRecognition()
    {
        if (saidEnablePhrase == "TFSS")
        {
            modPhrase = new KeywordRecognizer(commands.Keys.ToArray());
            modPhrase.OnPhraseRecognized += CommandRecognition;
            modPhrase.Start();
        } else CoroutineUtils.RunCoroutine(SetupDictation());
    }

    private static IEnumerator SetupDictation()
    {
        if (enablePhrase != null)
        {
            if (enablePhrase.IsRunning)
                enablePhrase.Stop();
            enablePhrase.Dispose();
            enablePhrase = null;
        }

        if (modPhrase != null)
        {
            if (modPhrase.IsRunning)
                modPhrase.Stop();
            modPhrase.Dispose();
            modPhrase = null;
        }

        PhraseRecognitionSystem.Shutdown();
        while (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
            yield return null;

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += (text, confidence) =>
        {
            if (listening)
            {
                CoroutineUtils.RunCoroutine(TFS.Plugin.SpeakText(text));
                if (listeningCoroutine != null)
                    CoroutineUtils._EndCoroutine(listeningCoroutine);
                listening = false;
                dictationRecognizer.Stop();
            }
        };

        dictationRecognizer.DictationComplete += (completionCause) =>
        {
            Debug.Log("Dictation complete: " + completionCause);
            RestartCommands();
        };

        dictationRecognizer.DictationError += (error, hresult) =>
        {
            Debug.LogError("Dictation error: " + error);
            RestartCommands();
        };

        dictationRecognizer.Start();
    }

    private static void CommandRecognition(PhraseRecognizedEventArgs args)
    {
        if (listening)
        {
            if (saidEnablePhrase == "TFSS")
            {
                if (commands.ContainsKey(args.text))
                    commands[args.text]?.Invoke();
            }
            else
            {
                if (canUseTFS)
                {
                    CoroutineUtils.RunCoroutine(TFS.Plugin.SpeakText(args.text));
                    RestartCommands();
                }
            }
            if (listeningCoroutine != null)
                CoroutineUtils._EndCoroutine(listeningCoroutine);
            listening = false;
        }
    }
    private static Coroutine listeningCoroutine;
    public static IEnumerator Timeout()
    {
        yield return new WaitForSeconds(6);
        if (listening)
        {
            listening = false;
            if (enablePhrase != null && modPhrase != null)
                CancelVoiceCommand();
            NotifiLib.SendNotification("No input stopped listening", MessageInfo.Voice);
        }
    }

    private static void CancelVoiceCommand()
    {
        NotifiLib.SendNotification("Canceling..", MessageInfo.Voice);
        listening = false;
        if (modPhrase != null)
        {
            modPhrase.Stop();
            modPhrase.Dispose();
            modPhrase = null;
        }
    }

    private static void RestartCommands()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.Stop();
            dictationRecognizer.Dispose();
            dictationRecognizer = null;
        }

        EnableVoiceCommands();
    }
}