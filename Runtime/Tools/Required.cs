using System;
using UnityEngine;
namespace Scribe
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotNullAttribute : PropertyAttribute
    {
    }
}