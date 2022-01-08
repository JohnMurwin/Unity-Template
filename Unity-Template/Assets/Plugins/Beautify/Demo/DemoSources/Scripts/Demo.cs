using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using BeautifyHDRP;
using System.Collections;

namespace BeautifyHDRP_Demo {
    public class Demo : MonoBehaviour {

        public VolumeProfile profile;
        Beautify settings;

        private void Start() {
            profile.TryGet(out settings);
            UpdateText();
        }

        private void OnDestroy() {
            if (settings != null) {
                // restore frost intensity to zero (in case it has been animated by pressing F in this demo script - see below)
                settings.frostIntensity.value = 0f;
                settings.frostIntensity.overrideState = false;
            }
        }

        void Update() {

            // Toggles on/off Beautify effects
            if (Input.GetKeyDown(KeyCode.T) || Input.GetMouseButtonDown(0)) {
                settings.active = !settings.active;
                UpdateText();
            }

            // Auto blink demo
            if (Input.GetKeyDown(KeyCode.B)) {
                StartCoroutine(Beautify.Blink(0.1f));
            }

            // Animate frost
            if (Input.GetKeyDown(KeyCode.F)) {
                StartCoroutine(AnimateFrost(from: 0, to: 1, duration: 2f));
            }
        }

        void UpdateText() {
            if (settings.active) {
                GameObject.Find("Beautify").GetComponent<Text>().text = "Beautify ON";
            } else {
                GameObject.Find("Beautify").GetComponent<Text>().text = "Beautify OFF";
            }
        }

        IEnumerator AnimateFrost(float from, float to, float duration) {

            float start = Time.time;
            float t;
            do {
                t = Mathf.Clamp01((Time.time - start) / duration);
                float frostIntensity = Mathf.Lerp(from, to, t);
                settings.frostIntensity.Override(frostIntensity);
                yield return null;
            } while (t < 1f);

        }
    }
}
