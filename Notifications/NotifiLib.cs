using BepInEx;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TFS.Notifications
{
    [BepInPlugin("org.thatguy.gorillatag.tfsnotifs", "TFS NotifiLib", "1.0.0")]
    public class NotifiLib : BaseUnityPlugin
    {
        private void Init()
        {
            MainCamera = GameObject.Find("Main Camera");
            HUDObj = new GameObject("NOTIFICATIONLIB_HUD_OBJ");
            HUDObj2 = new GameObject("NOTIFICATIONLIB_HUD_OBJ");
            HUDObj.AddComponent<Canvas>();
            HUDObj.AddComponent<CanvasScaler>();
            HUDObj.AddComponent<GraphicRaycaster>();
            HUDObj.GetComponent<Canvas>().enabled = true;
            HUDObj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            HUDObj.GetComponent<Canvas>().worldCamera = MainCamera.GetComponent<Camera>();
            HUDObj.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 5f);
            HUDObj.GetComponent<RectTransform>().position = new Vector3(MainCamera.transform.position.x, MainCamera.transform.position.y, MainCamera.transform.position.z);
            HUDObj2.transform.position = new Vector3(MainCamera.transform.position.x, MainCamera.transform.position.y, MainCamera.transform.position.z - 4.6f);
            HUDObj.transform.parent = HUDObj2.transform;
            HUDObj.GetComponent<RectTransform>().localPosition = new Vector3(0f, 0f, 1.6f);
            var eulerAngles = HUDObj.GetComponent<RectTransform>().rotation.eulerAngles;
            eulerAngles.y = -270f;
            HUDObj.transform.localScale = new Vector3(1f, 1f, 1f);
            HUDObj.GetComponent<RectTransform>().rotation = Quaternion.Euler(eulerAngles);
            Testtext = new GameObject
            {
                transform =
                {
                    parent = HUDObj.transform
                }
            }.AddComponent<Text>();
            Testtext.text = "";
            Testtext.fontSize = 30;
            Testtext.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Testtext.rectTransform.sizeDelta = new Vector2(450f, 210f);
            Testtext.alignment = TextAnchor.LowerLeft;
            Testtext.rectTransform.localScale = new Vector3(.00333333333f, .00333333333f, .33333333f);
            Testtext.rectTransform.localPosition = new Vector3(-1f, -1f, -.5f);
            Testtext.material = AlertText;
            NotifiText = Testtext;
        }

        private void FixedUpdate()
        {
            if (!HasInit && GameObject.Find("Main Camera") != null)
            {
                Init();
                HasInit = true;
            }
            HUDObj2.transform.position = new Vector3(MainCamera.transform.position.x, MainCamera.transform.position.y, MainCamera.transform.position.z);
            HUDObj2.transform.rotation = MainCamera.transform.rotation;
            if (Testtext.text != "")
            {
                NotificationDecayTimeCounter++;
                if (NotificationDecayTimeCounter > NotificationDecayTime)
                {
                    Notifilines = null;
                    newtext = "";
                    NotificationDecayTimeCounter = 0;
                    Notifilines = Testtext.text.Split(Environment.NewLine.ToCharArray()).Skip(1).ToArray();
                    foreach (var text in Notifilines)
                        if (text != "") newtext = newtext + text + "\n";
                    Testtext.text = newtext;
                }
            }
            else NotificationDecayTimeCounter = 0;
        }

        public static void SendNotification(string NotificationText, NotifUtils.MessageInfo messageInfo = NotifUtils.MessageInfo.None)
        {
            if (!disableNotifications)
            {
                try
                {
                    NotificationText = NotifUtils.MessageText(messageInfo) + NotificationText;
                    if (IsEnabled && PreviousNotifi != NotificationText)
                    {
                        if (!NotificationText.Contains(Environment.NewLine))
                        {
                            NotificationText += Environment.NewLine;
                        }
                        NotifiText.text = NotifiText.text + NotificationText;
                        NotifiText.supportRichText = true;
                        PreviousNotifi = NotificationText;
                    }
                }
                catch
                {
                    Debug.LogError("Notification failed, object probably null due to third person | " + NotificationText);
                }
            }
        }

        private GameObject HUDObj;

        private GameObject HUDObj2;

        private GameObject MainCamera;

        private Text Testtext;

        private Material AlertText = new Material(Shader.Find("GUI/Text Shader"));

        private int NotificationDecayTime = 144;

        private int NotificationDecayTimeCounter;

        public static int NoticationThreshold = 30;

        private string[] Notifilines;

        private string newtext;

        public static string PreviousNotifi;

        public static bool disableNotifications;

        private bool HasInit;

        private static Text NotifiText;

        public static bool IsEnabled = true;
    }
}