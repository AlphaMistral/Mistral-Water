using UnityEngine;
using System.Collections.Generic;

namespace LPWAsset {

    [System.Flags]
    public enum WaterMode {
        Simple = 0,
        Reflective = 1,
        Refractive = 2,
    };

    [System.Serializable]
    public class LPWReflectionParams {
        public bool disablePixelLights = true;
        public int textureSize = 256;
        public float clipPlaneOffset = 0.07f;
        public LayerMask reflectLayers = -1;
        public LayerMask refractLayers = -1;

        internal WaterMode waterMode = WaterMode.Refractive;

        internal Dictionary<Camera, Camera> reflCams = new Dictionary<Camera, Camera>();
        internal Dictionary<Camera, Camera> refrCams = new Dictionary<Camera, Camera>();
        internal RenderTexture reflTex = null;
        internal RenderTexture refrTex = null;

        internal WaterMode hwSupport = WaterMode.Refractive;
        internal int oldReflTexSize;
        internal int oldRefrTexSize;

        internal Dictionary<Camera, float> camState = new Dictionary<Camera, float>();
    }

    [ExecuteInEditMode]
    public class LPWReflection : MonoBehaviour {

        static LPWReflectionParams p = null;

        static bool recursiveGuard;
        static bool hideObjects = true;

        public void Init(LPWReflectionParams params_, bool enableReflection, bool enableRefraction) {
            p = params_;
            p.waterMode = WaterMode.Simple;
            if (enableReflection) p.waterMode |= WaterMode.Reflective;
            if (enableRefraction) p.waterMode |= WaterMode.Refractive;
        }

        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections / refractions and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        public void OnWillRenderObject() {
            if (!enabled || !GetComponent<Renderer>() || !GetComponent<Renderer>().sharedMaterial ||
                !GetComponent<Renderer>().enabled) {
                return;
            }

            Camera cam = Camera.current;
            if (!cam) return;


            // Render only once per camera
            float lastRender;
            if (p.camState.TryGetValue(cam, out lastRender)) {
                if (Mathf.Approximately(Time.time, lastRender) && Application.isPlaying) return;
                p.camState[cam] = Time.time;
            } else {
                p.camState.Add(cam, Time.time);
            }

            // Safeguard from recursive water reflections.
            if (recursiveGuard) return;
            recursiveGuard = true;

            // Actual water rendering mode depends on both the current setting AND
            // the hardware support. There's no point in rendering refraction textures
            // if they won't be visible in the end.
            p.hwSupport = FindHardwareWaterSupport();
            WaterMode mode = GetWaterMode();

            Camera reflectionCamera, refractionCamera;
            CreateWaterObjects(cam, out reflectionCamera, out refractionCamera);

            // find out the reflection plane: position and normal in world space
            Vector3 pos = transform.position;
            Vector3 normal = transform.up;

            // Optionally disable pixel lights for reflection/refraction
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (p.disablePixelLights) {
                QualitySettings.pixelLightCount = 0;
            }

            UpdateCameraModes(cam, reflectionCamera);
            UpdateCameraModes(cam, refractionCamera);

            // Render reflection if needed
            if ( (mode & WaterMode.Reflective) == WaterMode.Reflective) {
                // Reflect camera around reflection plane
                float d = -Vector3.Dot(normal, pos) - p.clipPlaneOffset;
                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

                Matrix4x4 reflection = Matrix4x4.zero;
                CalculateReflectionMatrix(ref reflection, reflectionPlane);
                Vector3 oldpos = cam.transform.position;
                Vector3 newpos = reflection.MultiplyPoint(oldpos);
                reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

                // Setup oblique projection matrix so that near plane is our reflection
                // plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
                reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);

                reflectionCamera.cullingMask = ~(1 << 4) & p.reflectLayers.value; // never render water layer
                reflectionCamera.targetTexture = p.reflTex;
                GL.invertCulling = true;
                reflectionCamera.transform.position = newpos;
                Vector3 euler = cam.transform.eulerAngles;
                reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
                reflectionCamera.Render();
                reflectionCamera.transform.position = oldpos;
                GL.invertCulling = false;
                GetComponent<Renderer>().sharedMaterial.SetTexture("_ReflectionTex", p.reflTex);
            }

            // Render refraction
            if ((mode & WaterMode.Refractive) == WaterMode.Refractive) {
                refractionCamera.worldToCameraMatrix = cam.worldToCameraMatrix;

                // Setup oblique projection matrix so that near plane is our reflection
                // plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(refractionCamera, pos, normal, -1.0f);
                refractionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);

                refractionCamera.cullingMask = ~(1 << 4) & p.refractLayers.value; // never render water layer
                refractionCamera.targetTexture = p.refrTex;
                refractionCamera.transform.position = cam.transform.position;
                refractionCamera.transform.rotation = cam.transform.rotation;
                refractionCamera.Render();
                GetComponent<Renderer>().sharedMaterial.SetTexture("_RefractionTex", p.refrTex);
            }

            // Restore pixel light count
            if (p.disablePixelLights) {
                QualitySettings.pixelLightCount = oldPixelLightCount;
            }

            recursiveGuard = false;
        }


        // Cleanup all the objects we possibly have created
        void OnDisable() {
            if (p == null) return;
            if (p.reflTex != null) {
                Destroy_(p.reflTex);
                p.reflTex = null;
            }
            if (p.refrTex != null) {
                Destroy_(p.refrTex);
                p.refrTex = null;
            }
            foreach (var kvp in p.reflCams) {
                Destroy_((kvp.Value).gameObject);
            }
            p.reflCams.Clear();
            foreach (var kvp in p.refrCams) {
                Destroy_((kvp.Value).gameObject);
            }
            p.refrCams.Clear();
            p.camState.Clear();
        }

        public void Destroy_(Object o) {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        void UpdateCameraModes(Camera src, Camera dest) {
            if (dest == null) {
                return;
            }
            // set water camera to clear the same way as current camera
            dest.clearFlags = src.clearFlags;
            var bgCol = src.backgroundColor;
            if (src.clearFlags == CameraClearFlags.Skybox) {
                Skybox sky = src.GetComponent<Skybox>();
                Skybox mysky = dest.GetComponent<Skybox>();
                if (!sky || !sky.material) {
                    mysky.enabled = false;
                } else {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
                
                if (RenderSettings.skybox && RenderSettings.skybox.HasProperty("_GroundColor")) {
                    bgCol = RenderSettings.skybox.GetColor("_GroundColor");
                    src.backgroundColor = bgCol;
                }
            }
            dest.backgroundColor = bgCol;
            // update other values to match current camera.
            // even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane)
            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }


        // On-demand create any objects we need for water
        void CreateWaterObjects(Camera currentCamera, out Camera reflectionCamera, out Camera refractionCamera) {
            WaterMode mode = GetWaterMode();

            reflectionCamera = null;
            refractionCamera = null;

            if ((mode & WaterMode.Reflective) == WaterMode.Reflective) {
                // Reflection render texture
                if (!p.reflTex || p.oldReflTexSize != p.textureSize) {
                    if (p.reflTex) {
                        DestroyImmediate(p.reflTex);
                    }
                    p.reflTex = new RenderTexture(p.textureSize, p.textureSize, 16);
                    p.reflTex.name = "__WaterReflection" + GetInstanceID();
                    p.reflTex.isPowerOfTwo = true;
                    p.reflTex.hideFlags = HideFlags.DontSave;
                    p.oldReflTexSize = p.textureSize;
                }

                // Camera for reflection
                p.reflCams.TryGetValue(currentCamera, out reflectionCamera);
                if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
                {
                    GameObject go = new GameObject("Water Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
                    reflectionCamera = go.GetComponent<Camera>();
                    reflectionCamera.enabled = false;
                    reflectionCamera.transform.position = transform.position;
                    reflectionCamera.transform.rotation = transform.rotation;
                    reflectionCamera.gameObject.AddComponent<FlareLayer>();
                    go.hideFlags = hideObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                    p.reflCams[currentCamera] = reflectionCamera;
                }
            }

            if ((mode & WaterMode.Refractive) == WaterMode.Refractive) {
                // Refraction render texture
                if (!p.refrTex || p.oldRefrTexSize != p.textureSize) {
                    if (p.refrTex) {
                        DestroyImmediate(p.refrTex);
                    }
                    p.refrTex = new RenderTexture(p.textureSize, p.textureSize, 16);
                    p.refrTex.name = "__WaterRefraction" + GetInstanceID();
                    p.refrTex.isPowerOfTwo = true;
                    p.refrTex.hideFlags = HideFlags.DontSave;
                    p.oldRefrTexSize = p.textureSize;
                }

                // Camera for refraction
                p.refrCams.TryGetValue(currentCamera, out refractionCamera);
                if (!refractionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
                {
                    GameObject go =
                        new GameObject("Water Refr Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(),
                            typeof(Camera), typeof(Skybox));
                    refractionCamera = go.GetComponent<Camera>();
                    refractionCamera.enabled = false;
                    refractionCamera.transform.position = transform.position;
                    refractionCamera.transform.rotation = transform.rotation;
                    refractionCamera.gameObject.AddComponent<FlareLayer>();
                    go.hideFlags = hideObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                    p.refrCams[currentCamera] = refractionCamera;
                }
            }
        }

        WaterMode GetWaterMode() {
            if (p.hwSupport < p.waterMode) {
                return p.hwSupport;
            }
            return p.waterMode;
        }

        WaterMode FindHardwareWaterSupport() {
            if (!GetComponent<Renderer>()) {
                return WaterMode.Simple;
            }

            Material mat = GetComponent<Renderer>().sharedMaterial;
            if (!mat) {
                return WaterMode.Simple;
            }

            return WaterMode.Reflective | WaterMode.Refractive;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
            Vector3 offsetPos = pos + normal * p.clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane) {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }

    }
}
