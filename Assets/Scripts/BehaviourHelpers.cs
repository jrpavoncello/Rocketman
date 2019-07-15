using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class BehaviourHelpers
    {
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
