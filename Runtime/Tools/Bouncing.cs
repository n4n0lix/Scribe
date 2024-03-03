using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe.Tools
{
    public class Bouncing : MonoBehaviour
    {
        public float bounceDistance = 2.0f;  // The distance the object will bounce.
        public float bounceTime = 2.0f;     // The speed at which the object will bounce.
        public AnimationCurve bounceCurve;

        private Vector3 initialPosition;
        private float startTime;
        private float t;

        void Start()
        {
            initialPosition = transform.position;
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t > bounceTime)
                t = 0;

            var bounce = bounceCurve.Evaluate(t / bounceTime);

            float newYPosition = initialPosition.y + bounce * bounceDistance;

            // Update the object's position.
            transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
        }
    }
}
