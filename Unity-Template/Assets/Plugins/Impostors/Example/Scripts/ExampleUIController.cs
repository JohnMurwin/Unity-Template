using Impostors.Example.Player;
using Impostors.Managers;
using UnityEngine;

namespace Impostors.Example
{
    /// <summary>
    /// Wrapper to share UI prefab between scenes
    /// </summary>
    [AddComponentMenu("")]
    internal class ExampleUIController : MonoBehaviour
    {
        public bool ImposterSystemEnabled
        {
            get { return ImpostorLODGroupsManager.Instance.enabled; }
            set
            {
                var ilods = FindObjectsOfType<ImpostorLODGroup>();
                foreach (var lodGroup in ilods)
                {
                    lodGroup.enabled = value;
                }
            }
        }

        public void Spawn(int value)
        {
            FindObjectOfType<ExampleSpawner>().OnSpawn(value);
        }

        public void SetPlayerMovementEnabled(bool value)
        {
            FindObjectOfType<MouseLook>().enabled = value;
            FindObjectOfType<PlayerController>().enabled = value;
        }
    }
}