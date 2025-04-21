using System.Collections;
using UnityEngine;

namespace TFS.Utils
{
    public class CoroutineUtils : MonoBehaviour
    {
        public static CoroutineUtils instance = null;
        void Awake() => instance = this;
        public static void RunCoroutine(IEnumerator enumerator) => instance.StartCoroutine(enumerator);
        public static void EndCoroutine(IEnumerator enumerator) => instance.StopCoroutine(enumerator);
        public static Coroutine _RunCoroutine(IEnumerator enumerator) => instance.StartCoroutine(enumerator);
        public static void _EndCoroutine(Coroutine coroutine) => instance.StopCoroutine(coroutine);
    }
}
