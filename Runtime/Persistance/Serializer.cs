using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Scribe.Persistance
{
    public class Serializer
    {
        public static string ToJson(MonoBehaviour behaviour)
        {
            return JsonConvert.SerializeObject(behaviour);
        }
    }
}
