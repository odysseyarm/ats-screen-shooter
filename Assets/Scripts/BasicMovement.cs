using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Apt.Unity.Projection
{
    [RequireComponent(typeof(ProjectionPlaneCamera))]
    public class BasicMovement : MonoBehaviour
    {
        public TrackerBase Tracker;

        private ProjectionPlaneCamera projectionCamera;
        private Vector3 initialLocalPosition;

        void Start()
        {
            projectionCamera = GetComponent<ProjectionPlaneCamera>();
            initialLocalPosition = projectionCamera.transform.localPosition;
        }

        private bool wasTracking = false;

        void Update()
        {
            if (Tracker == null)
                return;

            if(Tracker.IsTracking)
            {
                projectionCamera.transform.localPosition = initialLocalPosition + Tracker.Translation;
                wasTracking = true;
            }
            else if (wasTracking)
            {
                // When tracking is disabled, reset camera to initial position
                projectionCamera.transform.localPosition = initialLocalPosition;
                wasTracking = false;
            }
        }

    }
}
