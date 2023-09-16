using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe
{
    public class Run : MonoBehaviour
    {
        public static void Now(IEnumerator coroutine)
            => Runner.StartCoroutine(coroutine);

        #region Lazy Singleton
        private static Run _runner;
        private static Run Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("Scribe.Run");
                    _runner = go.AddComponent<Run>();
                    DontDestroyOnLoad(go);
                }
            
                return _runner;
            }
        }
        #endregion
    }

}
