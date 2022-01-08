using UnityEngine;

namespace Impostors.Example.Player{

	[AddComponentMenu("")]
	internal class FoVCameraControl : MonoBehaviour {

		Camera _camera;
		[SerializeField] 
		[Range(45, 90)]
		float _fieldOfView = 60;
		void Start () {
			_camera = GetComponent<Camera>();
            _fieldOfView = _camera.fieldOfView;
		}
		
		void Update () {
			_fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * 10;
			_fieldOfView = Mathf.Clamp(_fieldOfView, 45, 90);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _fieldOfView, _fieldOfView * Time.deltaTime);
		}
	}
}