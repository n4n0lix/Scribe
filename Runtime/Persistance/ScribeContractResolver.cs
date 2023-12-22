using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scribe.Persistance
{
    public class ScribeContractResolver : DefaultContractResolver
    {

        public new static readonly ScribeContractResolver Instance = new ScribeContractResolver();

        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract = base.CreateContract(objectType);
            return contract;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            // Only filter class that is derived from MonoBehaviour
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                // Keep name property OR properties derived from MonoBehaviour
                properties = properties.Where(x => x.PropertyName.Equals("name") || x.DeclaringType.IsSubclassOf(typeof(MonoBehaviour))).ToList();
            }

            return properties;
        }
    }
}
