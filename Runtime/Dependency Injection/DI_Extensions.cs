using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe
{

    public static class DI_Extensions
    {
        public static T Get<T>(this MonoBehaviour self) => DI.Get<T>(self);
        public static T Get<T>(this MonoBehaviour self, string id) => DI.Get<T>(self, id);
    }

}
