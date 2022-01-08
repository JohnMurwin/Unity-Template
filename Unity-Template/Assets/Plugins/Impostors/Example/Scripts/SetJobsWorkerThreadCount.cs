using System;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Impostors.Example
{
    [AddComponentMenu("")]
    public class SetJobsWorkerThreadCount : MonoBehaviour
    {
        [SerializeField]
        [Range(0,12)]
        private int _count = 12;

        private void Update()
        {
            _count = Mathf.Clamp(_count, 0, JobsUtility.JobWorkerMaximumCount);
            JobsUtility.JobWorkerCount = _count;
        }
    }
}