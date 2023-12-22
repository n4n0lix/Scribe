using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DontSerialize : Attribute
    {

    }

}
