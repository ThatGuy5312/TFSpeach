using Fusion;
using System.Collections;
using UnityEngine;

namespace TFS.Utils
{
    public class AudioUtils : MonoBehaviour
    {
        public static AudioSource Object = null;
        public static void PlayAudio(AudioClip clip)
        {
            if (Object == null) { Object = new GameObject("TFSSpeaker").AddComponent<AudioSource>(); }
            Object.transform.position = GorillaTagger.Instance.transform.position;
            Object.loop = false;
            Object.PlayOneShot(clip);
        }
    }
}