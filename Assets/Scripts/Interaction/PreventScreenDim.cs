using UnityEngine;

namespace Interaction
{
    public class PreventScreenDim : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
