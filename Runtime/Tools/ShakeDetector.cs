using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
namespace Scribe.Tools
{
    public class ShakeDetector : MonoBehaviour
    {
        [SerializeField] float shakeThreshold   = 1.5f;
        [SerializeField] float minShakeInterval = 0.5f;

        public UnityEvent OnShake = new UnityEvent();

        Vector3                  lastAcceleration;
        float                    lastShakeTime;
        LinearAccelerationSensor linearAccel;

        void OnEnable()
        {
            linearAccel = LinearAccelerationSensor.current;

            if (linearAccel != null)
            {
                InputSystem.EnableDevice(linearAccel);
                lastAcceleration = linearAccel.acceleration.ReadValue();
            }
            else if (Accelerometer.current != null)
            {
                InputSystem.EnableDevice(Accelerometer.current);
                lastAcceleration = Accelerometer.current.acceleration.ReadValue();
            }
        }

        void Update()
        {
            if (DetectShake())
                OnShake.Invoke();
        }

        bool DetectShake()
        {
            Vector3 currentAcceleration;

            if (linearAccel != null)
            {
                currentAcceleration = linearAccel.acceleration.ReadValue();
            }
            else if (Accelerometer.current != null)
            {
                currentAcceleration = Accelerometer.current.acceleration.ReadValue();
            }
            else
            {
                return false;
            }

            var delta = (currentAcceleration - lastAcceleration).magnitude;
            lastAcceleration = currentAcceleration;

            if (delta > shakeThreshold && Time.time - lastShakeTime > minShakeInterval)
            {
                lastShakeTime = Time.time;
                return true;
            }

            return false;
        }
    }
}