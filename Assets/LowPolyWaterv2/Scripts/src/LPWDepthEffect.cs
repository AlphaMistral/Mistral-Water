using System.Collections.Generic;
using UnityEngine;

namespace LPWAsset {
    [ExecuteInEditMode]
    public class LPWDepthEffect : MonoBehaviour {

        static Dictionary<Camera, Camera> depthCams = new Dictionary<Camera, Camera>();
        static Dictionary<Camera, float> camState = new Dictionary<Camera, float>();
        static RenderTexture depthTex = null;

        static Shader depthShader = null;
        bool receiveShadows;
        static bool recursiveGuard;

        static bool hideObjects = true;

        public void Init(bool receiveShadows) {
            this.receiveShadows = receiveShadows;
        }

        public void OnWillRenderObject() {
            var act = gameObject.activeInHierarchy && enabled;
            if (!act || !GetComponent<Renderer>()) return;

            var material = GetComponent<Renderer>().sharedMaterial;
            if (!material || !material.HasProperty("_EdgeBlend")) return;

            Camera cam = Camera.current;
            if (!cam) return;

            bool hasDepth = material.GetFloat("_EdgeBlend") > 0.5f ||
                (material.HasProperty("_LightAbs") && material.GetFloat("_LightAbs") > 0.5f);

            if (hasDepth) cam.depthTextureMode |= DepthTextureMode.Depth;

            if (!receiveShadows || !hasDepth) return; // only when both depth + shadows

            // Render only once per camera
            float lastRender;
            if (camState.TryGetValue(cam, out lastRender)) {
                if (Mathf.Approximately(Time.time, lastRender) && Application.isPlaying) return;
                camState[cam] = Time.time;
            } else {
                camState.Add(cam, Time.time);
            }

            // Safeguard from recursive 
            if (recursiveGuard) return;
            recursiveGuard = true;

            // Rendertexture
            if (!depthTex) {
                depthTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 24, RenderTextureFormat.Depth);
                depthTex.name = "__DepthTex" + GetInstanceID();
                depthTex.hideFlags = HideFlags.DontSave;
            }

            // Camera
            Camera depthCam = null;
            depthCams.TryGetValue(cam, out depthCam);
            if (!depthCam) { // catch both not-in-dictionary and in-dictionary-but-deleted-GO
                GameObject go = new GameObject("Water Depth Camera id" + GetInstanceID() + " for " + cam.GetInstanceID(), typeof(Camera));
                depthCam = go.GetComponent<Camera>();
                depthCam.enabled = false;
                depthCam.transform.position = transform.position;
                depthCam.transform.rotation = transform.rotation;
                go.hideFlags = hideObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                depthCams[cam] = depthCam;
                depthCam.clearFlags = CameraClearFlags.Depth;
            }

            int oldPixelLightCount = QualitySettings.pixelLightCount;
            QualitySettings.pixelLightCount = 0;
            float oldShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = 0;

            if (depthCam != null) {
                depthCam.farClipPlane = cam.farClipPlane;
                depthCam.nearClipPlane = cam.nearClipPlane;
                depthCam.orthographic = cam.orthographic;
                depthCam.fieldOfView = cam.fieldOfView;
                depthCam.aspect = cam.aspect;
                depthCam.orthographicSize = cam.orthographicSize;
                depthCam.depth = cam.depth - 0.1f;

                //Render
                depthCam.worldToCameraMatrix = cam.worldToCameraMatrix;
                depthCam.projectionMatrix = cam.projectionMatrix;
                depthCam.cullingMask = ~(1 << 4) & cam.cullingMask; // without water
                depthCam.targetTexture = depthTex;
                depthCam.transform.position = cam.transform.position;
                depthCam.transform.rotation = cam.transform.rotation;
                if(depthShader == null) {
                    depthShader = Shader.Find("Hidden/LPWRenderDepth");
                }
                depthCam.renderingPath = RenderingPath.VertexLit;
                depthCam.RenderWithShader(depthShader, "RenderType");
                GetComponent<Renderer>().sharedMaterial.SetTexture("_DepthTexture", depthTex);
            }

            QualitySettings.pixelLightCount = oldPixelLightCount;
            QualitySettings.shadowDistance = oldShadowDistance;
            recursiveGuard = false;

        }

        // Cleanup all the objects we possibly have created
        void OnDisable() {
            if (depthTex) {
                Destroy_(depthTex);
                depthTex = null;
            }
            foreach (var kvp in depthCams) {
                Destroy_((kvp.Value).gameObject);
            }
            depthCams.Clear();
            camState.Clear();
        }

        public void Destroy_(Object o) {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

    }
}
