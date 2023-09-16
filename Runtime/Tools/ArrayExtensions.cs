using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ArrayExtensions
{

    public static T Random<T>(this T[] self)
    {
        return self[UnityEngine.Random.Range(0, self.Length)];
    }

}
