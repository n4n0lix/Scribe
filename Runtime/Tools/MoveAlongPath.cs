using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe.Tools
{
    public class MoveAlongPath : MonoBehaviour
    {
        public Vector3[] pathPoints;        // Array of Vector3 points defining the path.
        public float movementSpeed = 5.0f;  // Speed of movement.
        public float accelerationTime = 1.0f; // Time to accelerate.
        public float decelerationTime = 1.0f; // Time to decelerate.

        private float journeyLength;
        private float startTime;
        private int currentPointIndex = 0;
        private bool isMoving = false;

        void Start()
        {
            if (pathPoints.Length < 2)
            {
                Debug.LogError("Path must contain at least two points.");
                return;
            }

            journeyLength = Vector3.Distance(pathPoints[0], pathPoints[pathPoints.Length - 1]);
            StartMoving();
        }

        void Update()
        {
            if (isMoving)
            {
                float journeyTime = Time.time - startTime;
                float journeyFraction = journeyTime / (journeyLength / movementSpeed);

                if (journeyFraction < 1.0f)
                {
                    float t = journeyFraction;
                    // Apply acceleration and deceleration.
                    if (journeyTime < accelerationTime)
                    {
                        t = Mathf.Pow(t, 2); // Acceleration phase
                    }
                    else if (journeyTime > (journeyLength / movementSpeed) - decelerationTime)
                    {
                        t = 1 - Mathf.Pow(1 - t, 2); // Deceleration phase
                    }

                    // Interpolate between points using Lerp.
                    transform.position = Vector3.Lerp(pathPoints[currentPointIndex], pathPoints[currentPointIndex + 1], t);
                }
                else
                {
                    // Move to the next segment of the path or loop back to the beginning.
                    currentPointIndex++;
                    if (currentPointIndex >= pathPoints.Length - 1)
                    {
                        currentPointIndex = 0; // Loop back to the beginning
                    }

                    startTime = Time.time;
                }
            }
        }

        public void StartMoving()
        {
            if (pathPoints.Length < 2)
            {
                Debug.LogError("Path must contain at least two points.");
                return;
            }

            currentPointIndex = 0;
            startTime = Time.time;
            isMoving = true;
        }
    }
}
