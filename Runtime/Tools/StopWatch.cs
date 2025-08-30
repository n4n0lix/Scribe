using System;
using System.Collections.Generic;
using UnityEngine;
namespace Scribe.Tools
{


    [Serializable]
    public class StopWatch : ISerializationCallbackReceiver
    {
        private float _lastTick = 0f;

        [SerializeField]
        private float _serializedElapsed = 0f;

        public StopWatch() { }

        public StopWatch Start()
        {
            _lastTick = Time.time;
            _serializedElapsed = 0; // Flushed buffered serialized 
            return this;
        }

        public void Restart()
        {
            Start();
        }

        public float Tick()
        {
            float elapsed = Elapsed();
            _lastTick = Time.time;
            return elapsed;
        }

        public float Elapsed()
        {
            // Check and handle deserialization
            if (_lastTick == 0f && _serializedElapsed > 0f)
            {
                _lastTick = Time.time - _serializedElapsed;
                _serializedElapsed = 0;
            }

            // Calculate elapsed time
            return Time.time - _lastTick;
        }

        public bool Wait(double timeSpanS)
        {
            return Wait((float)timeSpanS);
        }

        public bool Wait(float timeSpanS)
        {
            if (Elapsed() >= timeSpanS)
                return true;

            return false;
        }

        public bool Every(float timeSpanS)
        {
            bool trigger = Wait(timeSpanS);
            if (trigger)
                Restart();

            return trigger;
        }

        public bool Every(double timeSpanS)
        {
            return Every((float)timeSpanS);
        }

        public void OnBeforeSerialize()
        {
            _serializedElapsed = Elapsed();
        }

        public void OnAfterDeserialize()
        {

        }
    }

}
