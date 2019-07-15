using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class BehaviourHelpers
    {
        // Instead of using Invoke and needing to use the name of your method, here's a helper
        public static void DelayInvoke(MonoBehaviour behavior, Action action, int seconds)
        {
            behavior.StartCoroutine(Wait(action, seconds));
        }

        private static IEnumerator<YieldInstruction> Wait(Action action, int seconds)
        {
            yield return new WaitForSeconds(seconds);

            action();
        }
    }
}
