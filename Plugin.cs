using BepInEx;
using Photon.Pun;
using Photon.Voice.Unity;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TFS.Notifications;
using TFS.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace TFS
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private float windowScale = 1f;
        private float animationSpeed = 5f;
        private float minScale = .1f;

        private bool isMinimized = true;

        private Texture2D backgroundTexture;

        private Color DarkGray = new Color32(30, 30, 30, 255);

        private Rect windowRect;

        Vector2 scroll = Vector2.zero;

        private string input;

        public static IInputSystem UserInput => UnityInput.Current;

        void Start()
        {
            var width = 300f;
            var height = 150f;
            var x = (Screen.width - width) / 2;
            var y = (Screen.height - height) / 2;
            windowRect = new Rect(x, y, width, height);

            backgroundTexture = CreateRounded(DarkGray, (int)windowRect.width, (int)windowRect.height, 10);

            timeMenuStarted = Time.time;

            var obj = new GameObject("loader");
            obj.AddComponent<CoroutineUtils>();
            obj.AddComponent<VoiceCommands>();
            DontDestroyOnLoad(obj);
        }

        void Update()
        {
            if (UserInput.GetKeyDown(KeyCode.Slash))
                isMinimized = !isMinimized;
            var targetScale = isMinimized ? minScale : 1f;
            windowScale = Mathf.Lerp(windowScale, targetScale, Time.deltaTime * animationSpeed);
        }

        void OnGUI()
        {
            if (windowScale <= minScale + .01f) return;

            var backup = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * (1 - windowScale) / 2, Screen.height * (1 - windowScale) / 2, 0), Quaternion.identity, new Vector3(windowScale, windowScale, 1));

            GUI.DrawTexture(windowRect, backgroundTexture);

            GUILayout.BeginArea(windowRect);

            GUILayout.BeginVertical();

            input = GUILayout.TextArea(input, GUILayout.Height(100));

            if (GUILayout.Button("Speak", GUILayout.Height(20)))
                CoroutineUtils.RunCoroutine(SpeakText(input));

            if (GUILayout.Button($"Voice: {narratorName}", GUILayout.Height(20)))
                ChangeNarrationVoice();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUI.matrix = backup;
        }

        public static void UseVoice()
        {
            VoiceCommands.canUseTFS = false;
            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.Microphone;
                GorillaTagger.Instance.myRecorder.RestartRecording(true);
            }
        }

        public static void UseTFS()
        {
            VoiceCommands.canUseTFS = true;
            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
                GorillaTagger.Instance.myRecorder.RestartRecording(true);
            }
        }

        // all the stuff in the region is from https://github.com/iiDk-the-actual/iis.Stupid.Menu
        #region from ii stupid menu

        public static string narratorName = "Default";
        public static int narratorIndex;
        public static float timeMenuStarted = -1f;
        public static System.Collections.IEnumerator SpeakText(string text)
        {
            if (Time.time < (timeMenuStarted + 5f))
                yield break;

            string fileName = GetSHA256(text) + (narratorIndex == 0 ? ".wav" : ".mp3");
            string directoryPath = "TFSpeach/TTS" + (narratorName == "Default" ? "" : narratorName);

            if (!Directory.Exists("TFSpeach"))
                Directory.CreateDirectory("TFSpeach");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists("TFSpeach/TTS" + (narratorName == "Default" ? "" : narratorName) + "/" + fileName))
            {
                string filePath = directoryPath + "/" + fileName;

                if (!File.Exists(filePath))
                {
                    string postData = "{\"text\": \"" + text.Replace("\n", "").Replace("\r", "").Replace("\"", "") + "\"}";

                    if (narratorIndex == 0)
                    {
                        using (UnityWebRequest request = new UnityWebRequest("https://iidk.online/tts", "POST"))
                        {
                            byte[] raw = Encoding.UTF8.GetBytes(postData);

                            request.uploadHandler = new UploadHandlerRaw(raw);
                            request.SetRequestHeader("Content-Type", "application/json");

                            request.downloadHandler = new DownloadHandlerBuffer();
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                            {
                                Debug.LogError("Error downloading TTS: " + request.error);
                                yield break;
                            }

                            byte[] response = request.downloadHandler.data;
                            File.WriteAllBytes(filePath, response);
                        }
                    }
                    else
                    {
                        using (UnityWebRequest request = UnityWebRequest.Get("https://api.streamelements.com/kappa/v2/speech?voice=" + narratorName + "&text=" + UnityWebRequest.EscapeURL(text)))
                        {
                            yield return request.SendWebRequest();

                            if (request.result != UnityWebRequest.Result.Success)
                                Debug.LogError("Error downloading TTS: " + request.error);
                            else
                                File.WriteAllBytes(filePath, request.downloadHandler.data);
                        }
                    }
                }
            }

            PlayAudio("TTS" + (narratorName == "Default" ? "" : narratorName) + "/" + fileName);
        }

        public static string GetSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder stringy = new StringBuilder();
                foreach (byte by in bytes)
                {
                    stringy.Append(by.ToString("x2"));
                }

                return stringy.ToString();
            }
        }

        public static void ChangeNarrationVoice()
        {
            string[] narratorNames = new string[]
            {
                "Default",
                "Kimberly",
                "Brian",
                "Matthew",
                "Joey",
                "Justin",
                "Cristiano",
                "Giorgio",
                "Ewa"
            };

            narratorIndex++;
            if (narratorIndex > narratorNames.Length - 1)
                narratorIndex = 0;

            var notif = "Changed Narration Voice To <color=grey>[</color><color=green>" + narratorNames[narratorIndex] + "</color><color=grey>]</color>";
            NotifiLib.SendNotification(notif);
            narratorName = narratorNames[narratorIndex];
        }

        public static bool AudioIsPlaying = false;
        public static float RecoverTime = -1f;
        public static bool LoopAudio = false;
        public static void PlayAudio(string file)
        {
            AudioClip sound = LoadSoundFromFile(file);
            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myRecorder.SourceType = Recorder.InputSourceType.AudioClip;
                GorillaTagger.Instance.myRecorder.AudioClip = sound;
                GorillaTagger.Instance.myRecorder.RestartRecording(true);
                GorillaTagger.Instance.myRecorder.DebugEchoMode = true;
            }
            else
            {
                AudioUtils.PlayAudio(sound);
            }
            if (!LoopAudio)
            {
                AudioIsPlaying = true;
                RecoverTime = Time.time + sound.length + 0.4f;
            }
        }

        public static Dictionary<string, AudioClip> audioFilePool = new Dictionary<string, AudioClip> { };
        public static AudioClip LoadSoundFromFile(string fileName) // Thanks to ShibaGT for help with loading the audio from file - iiDk
        {
            AudioClip sound = null;
            if (!audioFilePool.ContainsKey(fileName))
            {
                if (!Directory.Exists("TFSpeach"))
                {
                    Directory.CreateDirectory("TFSpeach");
                }
                string filePath = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "TFSpeach/" + fileName);
                filePath = filePath.Split("BepInEx\\")[0] + "TFSpeach/" + fileName;
                filePath = filePath.Replace("\\", "/");

                UnityWebRequest actualrequest = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, GetAudioType(GetFileExtension(fileName)));
                UnityWebRequestAsyncOperation newvar = actualrequest.SendWebRequest();
                while (!newvar.isDone) { }

                AudioClip actualclip = DownloadHandlerAudioClip.GetContent(actualrequest);
                sound = Task.FromResult(actualclip).Result;

                audioFilePool.Add(fileName, sound);
            }
            else
            {
                sound = audioFilePool[fileName];
            }

            return sound;
        }

        public static AudioType GetAudioType(string extension)
        {
            switch (extension.ToLower())
            {
                case "mp3":
                    return AudioType.MPEG;
                case "wav":
                    return AudioType.WAV;
                case "ogg":
                    return AudioType.OGGVORBIS;
                case "aiff":
                    return AudioType.AIFF;
            }
            return AudioType.WAV;
        }

        public static string GetFileExtension(string fileName)
        {
            return fileName.ToLower().Split(".")[fileName.Split(".").Length - 1];
        }
        #endregion

        private Texture2D CreateRounded(Color color, int width, int height, int radius)
        {
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    var corner = (x < radius && y < radius && (x - radius) * (x - radius) + (y - radius) * (y - radius) > radius * radius) ||
                        (x > width - radius - 1 && y < radius && (x - (width - radius - 1)) * (x - (width - radius - 1)) + (y - radius) * (y - radius) > radius * radius) ||
                        (x < radius && y > height - radius - 1 && (x - radius) * (x - radius) + (y - (height - radius - 1)) * (y - (height - radius - 1)) > radius * radius) ||
                        (x > width - radius - 1 && y > height - radius - 1 && (x - (width - radius - 1)) * (x - (width - radius - 1)) + (y - (height - radius - 1)) * (y - (height - radius - 1)) > radius * radius);
                    pixels[x + y * width] = corner ? Color.clear : color;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
