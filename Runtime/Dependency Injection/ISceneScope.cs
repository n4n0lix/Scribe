using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scribe
{
    public interface ISceneScope : IScope
    {
        public Scene scene { get; }
    }
}
