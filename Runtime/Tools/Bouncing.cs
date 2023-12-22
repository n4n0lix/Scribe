using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe.Tools
{
    public class Bouncing : MonoBehaviour
    {
        public float bounceDistance = 2.0f;  // The distance the object will bounce.
        public float bounceSpeed = 2.0f;     // The speed at which the object will bounce.

        private Vector3 initialPosition;
        private float startTime;

        void Start()
        {
            initialPosition = transform.position;
            startTime = Time.time;
        }

        void Update()
        {
            float timeElapsed = Time.time - startTime;
            float newYPosition = initialPosition.y + Mathf.Sin(timeElapsed * bounceSpeed) * bounceDistance;

            // Update the object's position.
            transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
        }
    }
}
