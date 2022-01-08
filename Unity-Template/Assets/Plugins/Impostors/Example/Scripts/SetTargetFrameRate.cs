using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Impostors.Example
{
    [AddComponentMenu("")]
    internal class SetTargetFrameRate : MonoBehaviour
    {
        [SerializeField] private int _targetFrameRate = 1000;

        [Range(0f,2f)]
        [SerializeField]
        private float _timeScale = 1f;

        [Range(1, 12)]
        [SerializeField]
        private int _jobWorkerCount = 4;
        
        private void Update()
        {
            Application.targetFrameRate = _targetFrameRate;
            Time.timeScale = _timeScale;
            _jobWorkerCount = Mathf.Clamp(_jobWorkerCount, 1, JobsUtility.JobWorkerMaximumCount);
            JobsUtility.JobWorkerCount = _jobWorkerCount;
        }
    }
}