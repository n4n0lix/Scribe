using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe
{
    public class InjectAttribute : Attribute
    {
        public InjectAttribute() : this(null, false) { }
        public InjectAttribute(string id) : this(id, false) { }
        public InjectAttribute(bool optional) : this(null, optional) { }

        public InjectAttribute(string id, bool optional)
        {
            this.id = id;
            this.optional = optional;
        }

        public string id;
        public bool optional;
    }
}
