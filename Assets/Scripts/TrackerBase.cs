using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Apt.Unity.Projection
{
    public class TrackerBase : MonoBehaviour
    {
        [HideInInspector]
        public bool IsTracking { get; set; }  // Made public setter for QualificationDistanceManager
        [HideInInspector]
        public ulong TrackedId { get; protected set; }
        [HideInInspector]
        public Vector3 Translation { 
            get => translation; 
            set => translation = value;  // Made settable for QualificationDistanceManager
        }
        [HideInInspector]
        public float SecondsHasBeenTracked { get; protected set; }

        protected Vector3 translation;
    }
}