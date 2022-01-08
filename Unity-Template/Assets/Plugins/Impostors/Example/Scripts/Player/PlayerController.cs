using UnityEngine;

namespace Impostors.Example.Player
{
    [AddComponentMenu("")]
    internal class PlayerController : MonoBehaviour
    {
        [SerializeField] float speed = 10;
        [SerializeField] float runSpeed = 20;
        [SerializeField] bool allowUpDown = false;

        void Update()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 plusPos = transform.forward * v + transform.right * h;
            if (allowUpDown && Input.GetKey(KeyCode.E))
                plusPos += transform.up;
            if (allowUpDown && Input.GetKey(KeyCode.Q))
                plusPos -= transform.up;
            if (Input.GetKey(KeyCode.LeftShift))
                plusPos *= runSpeed;
            else
                plusPos *= speed;
            transform.position += plusPos * Time.deltaTime;
        }
    }
}