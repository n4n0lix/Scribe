using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe
{
    public class InjectAttribute : Attribute
    {
        public InjectAttribute(string id = null)
        {
            this.id = id;
        }

        public string id;
    }
}
