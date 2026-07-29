using UnityEngine;
using UnityEngine.Assertions;
namespace Scribe.Tools
{
    public class S
    {
        public static bool Guard(bool condition, string errorMessage)
        {
            if (!condition) return false;

            Debug.LogError(errorMessage);
            return true;
        }

        public static bool Assert(bool condition, string errorMessage)
        {
            #if !UNITY_EDITOR
                return true;
            #endif
            if (condition) return true;

            Debug.LogAssertion(errorMessage);
            throw new AssertionException(errorMessage, errorMessage);
        }
    }
}