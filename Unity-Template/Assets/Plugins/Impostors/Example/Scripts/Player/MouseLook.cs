using UnityEngine;

namespace Impostors.Example.Player
{
    [AddComponentMenu("")]
    internal class MouseLook : MonoBehaviour
    {
        public enum RotationAxes
        {
            MouseXAndY = 0,
            MouseX = 1,
            MouseY = 2
        }

        public RotationAxes axes = RotationAxes.MouseXAndY;
        public float speed = 10;
        public float sensetivityX = 15F;
        public float sensetivityY = 15F;
        float mnog = 1;

        public float minimumX = -360F;
        public float maximumX = 360F;

        public float minimumY = -60F;
        public float maximumY = 60F;

        [SerializeField]
        float rotationX = 0F;

        [SerializeField]
        float rotationY = 0F;

        readonly Quaternion originalRotation = Quaternion.Euler(0, 0, 0);
        Quaternion targetRotation;

        void Update()
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, speed * Time.deltaTime);
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                return;
            if (Input.GetMouseButton(1))
                mnog = 0.5f;
            else
                mnog = 1;
            mnog *= Time.deltaTime;
            if (axes == RotationAxes.MouseXAndY)
            {
                // Read the mouse input axis
                rotationX += Input.GetAxis("Mouse X") * sensetivityX * mnog;
                rotationY += Input.GetAxis("Mouse Y") * sensetivityY * mnog;

                rotationX = ClampAngle(rotationX, minimumX, maximumX);
                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

                //transform.localRotation = originalRotation * xQuaternion * yQuaternion;

                targetRotation = originalRotation * xQuaternion * yQuaternion;
            }
            else if (axes == RotationAxes.MouseX)
            {
                rotationX += Input.GetAxis("Mouse X") * sensetivityX;
                rotationX = ClampAngle(rotationX, minimumX, maximumX);

                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                transform.localRotation = originalRotation * xQuaternion;
            }
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensetivityY;
                rotationY = ClampAngle(rotationY, minimumY, maximumY);

                Quaternion yQuaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
                transform.localRotation = originalRotation * yQuaternion;
            }
        }

        void Start()
        {
            rotationX = transform.eulerAngles.y;
            rotationY = -transform.eulerAngles.x;
            targetRotation = transform.rotation;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}