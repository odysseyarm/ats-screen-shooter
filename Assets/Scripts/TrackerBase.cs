using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Apt.Unity.Projection
{
    public class TrackerBase : MonoBehaviour
    {
        [HideInInspector]
        public bool IsTracking { get; set; }
        
        [HideInInspector]
        public ulong TrackedId { get; protected set; }
        
        [HideInInspector]
        public Vector3 Translation { get; set; }
        
        [HideInInspector]
        public float SecondsHasBeenTracked { get; protected set; }
    }
}
