#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LPWAsset {
    public static class LPWHiddenProps{
        public static void Calculate(Material m) {
            float scale = 1;
            if (m.HasProperty("_Scale") && m.HasProperty("_TransformScale_")) {
                scale = m.GetFloat("_Scale") * m.GetFloat("_TransformScale_");
                m.SetFloat("_Scale_", scale);
            } else {
                scale = m.GetFloat("_Scale_");
            }

            if (m.HasProperty("_BumpScale_")) m.SetFloat("_BumpScale_", m.GetFloat("_BumpScale") * scale);
            if (m.HasProperty("_RHeight_")) m.SetFloat("_RHeight_", m.GetFloat("_RHeight") * scale);
            if (m.HasProperty("_RSpeed_")) m.SetFloat("_RSpeed_", m.GetFloat("_RSpeed") * scale); 

            var steepness = m.GetFloat("_Steepness") * m.GetFloat("_Length") * scale;
            var angle = Mathf.Deg2Rad * m.GetFloat("_Direction");
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);
            m.SetVector("_Direction_", new Vector4(cos, sin, cos * steepness, sin * steepness));
            m.SetFloat("_Height_", m.GetFloat("_Height") * scale);
            m.SetFloat("_Speed_", m.GetFloat("_Speed") * scale);

            var noiseTex = m.GetTexture("_NoiseTex");
            if (noiseTex != null) m.SetFloat("_TexSize_", noiseTex.height * scale);
        }

        public static void Scale(LowPolyWaterScript _target) {
            // don't scale prefabs 
            if (PrefabUtility.GetPrefabParent(_target) == null && PrefabUtility.GetPrefabObject(_target) != null) return;
            if (_target.material == null || !_target.material.HasProperty("_TransformScale_")) return;

            // scale everything when scaling transform's localscale
            var locScale = _target.transform.localScale;
            var scale = _target.material.GetFloat("_TransformScale_");
            float newScale = 1;
            if (_target.gridType != LowPolyWaterScript.GridType.Custom) {
                newScale = Mathf.Min(locScale.x, locScale.z);
                if (!Mathf.Approximately(scale, newScale)) {
                    _target.material.SetFloat("_TransformScale_", newScale);
                    Calculate(_target.material);
                }
            }
        }

        public static void SetKeywords(LowPolyWaterScript _target) {
            var mat = _target.material;
            if (mat == null || !mat.shader.name.Contains("LowPolyWater")) return;

            bool isCustom = _target.gridType == LowPolyWaterScript.GridType.Custom;
            if (isCustom) {
                mat.EnableKeyword("_CUSTOM_SHAPE");
                mat.DisableKeyword("_USE_LOD");
            } else if (_target.gridType == LowPolyWaterScript.GridType.HexagonalLOD) {
                mat.DisableKeyword("_CUSTOM_SHAPE");
                mat.EnableKeyword("_USE_LOD");
            } else {
                mat.DisableKeyword("_CUSTOM_SHAPE");
                mat.DisableKeyword("_USE_LOD");
            }

            if (_target.enableReflection) {
                mat.EnableKeyword("WATER_REFLECTIVE");
            } else {
                mat.DisableKeyword("WATER_REFLECTIVE");
            }

            if (_target.enableRefraction) {
                mat.EnableKeyword("WATER_REFRACTIVE");
            } else {
                mat.DisableKeyword("WATER_REFRACTIVE");
            }

            if (_target.receiveShadows && SystemInfo.graphicsShaderLevel >= 30) {
                mat.EnableKeyword("LPW_SHADOWS");
                mat.renderQueue = (int)RenderQueue.AlphaTest + 50;
                if (mat.HasProperty("_ZWrite")) {
                    mat.SetFloat("_ZWrite", 1);
                }
            } else {
                mat.DisableKeyword("LPW_SHADOWS");
                mat.renderQueue = (int)RenderQueue.AlphaTest + 51;
            }
        }
    }
}
#endif