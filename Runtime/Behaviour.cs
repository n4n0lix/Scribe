using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Scribe
{

    public class Behaviour : MonoBehaviour
    {
        protected T Get<T>() => DI.Get<T>(this);
    }

}

