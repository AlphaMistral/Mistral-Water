using UnityEditor;
using UnityEngine;

namespace LPWAsset {
    public class LPWShaderGUI : ShaderGUI {

        enum Shading { Flat, VertexLit, PixelLit };

        MaterialProperty _Shadow = null;
        MaterialProperty _Color = null;
        MaterialProperty _Opacity = null;
        MaterialProperty _Gloss = null;
        MaterialProperty _Specular = null;
        MaterialProperty _SpecColor = null;
        MaterialProperty _Diffuse = null;
        MaterialProperty _PointLights = null;
        MaterialProperty _Shading = null;

        MaterialProperty _Waves = null;
        MaterialProperty _Length = null;
        MaterialProperty _Stretch = null;
        MaterialProperty _Speed = null;
        MaterialProperty _Height = null;
        MaterialProperty _Steepness = null;
        MaterialProperty _Direction = null;

        MaterialProperty _RSpeed = null;
        MaterialProperty _RHeight = null;

        MaterialProperty _FresnelTex = null;
        MaterialProperty _FresPower = null;
        MaterialProperty _FresColor = null;
        MaterialProperty _Reflection = null;
        MaterialProperty _Refraction = null;
        MaterialProperty _NormalOffset = null;
        MaterialProperty _Distortion = null;
        MaterialProperty _Distort = null;
        MaterialProperty _BumpTex = null;
        MaterialProperty _BumpScale = null;
        MaterialProperty _BumpSpeed = null;

        MaterialProperty _EdgeBlend = null;
        MaterialProperty _ShoreColor = null;
        MaterialProperty _ShoreIntensity = null;
        MaterialProperty _HQFoam = null;
        MaterialProperty _FoamScale = null;
        MaterialProperty _FoamSpeed = null;
        MaterialProperty _FoamSpread = null;
        MaterialProperty _ShoreDistance = null;
        MaterialProperty _LightAbs = null;
        MaterialProperty _Absorption = null;
        MaterialProperty _DeepColor = null;

        MaterialProperty _NoiseTex = null;
        MaterialProperty _ZWrite = null;
        MaterialProperty _GlobScale = null;
        MaterialProperty _Cull = null;

        MaterialProperty __Cull = null;
        MaterialProperty _EnableShadows = null;

        static readonly GUIContent fresnelLbl = new GUIContent("Fresnel (A)");
        static readonly GUIContent bumpLbl = new GUIContent("Distortion Map");
        static readonly GUIContent noiseLbl = new GUIContent("Noise Texture (A)");

        public static bool lightingFold {
            get { return EditorPrefs.GetBool("LPWlightingFold", false); }
            set { EditorPrefs.SetBool("LPWlightingFold", value); }
        }
        public static bool reflFold {
            get { return EditorPrefs.GetBool("LPWreflFold", false); }
            set { EditorPrefs.SetBool("LPWreflFold", value); }
        }
        public static bool wavesFold {
            get { return EditorPrefs.GetBool("LPWwavesFold", false); }
            set { EditorPrefs.SetBool("LPWwavesFold", value); }
        }
        public static bool shoreFold {
            get { return EditorPrefs.GetBool("LPWshoreFold", false); }
            set { EditorPrefs.SetBool("LPWshoreFold", value); }
        }
        public static bool otherFold {
            get { return EditorPrefs.GetBool("LPWotherFold", false); }
            set { EditorPrefs.SetBool("LPWotherFold", value); }
        }

        static GUIStyle _foldoutStyle;
        static GUIStyle foldoutStyle {
            get {
                if (_foldoutStyle == null) {
                    _foldoutStyle = new GUIStyle(EditorStyles.foldout);
                    _foldoutStyle.font = EditorStyles.boldFont;
                }
                return _foldoutStyle;
            }
        }
        static GUIStyle _boxStyle;
        static GUIStyle boxStyle {
            get {
                if (_boxStyle == null) {
                    _boxStyle = new GUIStyle(EditorStyles.helpBox);
                }
                return _boxStyle;
            }
        }

        public void FindProperties(MaterialProperty[] props) {
            _Shadow = FindProperty("_Shadow", props, false);
            _Color = FindProperty("_Color", props);
            _Opacity = FindProperty("_Opacity", props);
            _Gloss = FindProperty("_Gloss", props, false);
            _Specular = FindProperty("_Specular", props);
            _SpecColor = FindProperty("_SpecColor", props);
            _Diffuse = FindProperty("_Diffuse", props, false);
            _PointLights = FindProperty("_PointLights", props, false);
            _Shading = FindProperty("_Shading", props);

            _FresnelTex = FindProperty("_FresnelTex", props);
            _FresPower = FindProperty("_FresPower", props, false);
            _FresColor = FindProperty("_FresColor", props, false);
            _Reflection = FindProperty("_Reflection", props, false);
            _Refraction = FindProperty("_Refraction", props, false);
            _NormalOffset = FindProperty("_NormalOffset", props, false);
            _Distortion = FindProperty("_Distortion", props, false);
            _Distort = FindProperty("_Distort", props, false);
            _BumpTex = FindProperty("_BumpTex", props, false);
            _BumpScale = FindProperty("_BumpScale", props, false);
            _BumpSpeed = FindProperty("_BumpSpeed", props, false);

            _Waves = FindProperty("_Waves", props);
            _Length = FindProperty("_Length", props);
            _Stretch = FindProperty("_Stretch", props);
            _Speed = FindProperty("_Speed", props);
            _Height = FindProperty("_Height", props);
            _Steepness = FindProperty("_Steepness", props);
            _Direction = FindProperty("_Direction", props);

            _RSpeed = FindProperty("_RSpeed", props);
            _RHeight = FindProperty("_RHeight", props);

            _EdgeBlend = FindProperty("_EdgeBlend", props);
            _ShoreColor = FindProperty("_ShoreColor", props);
            _ShoreIntensity = FindProperty("_ShoreIntensity", props, false);
            _ShoreDistance = FindProperty("_ShoreDistance", props);
            _HQFoam = FindProperty("_HQFoam", props, false);
            _FoamScale = FindProperty("_FoamScale", props, false);
            _FoamSpeed = FindProperty("_FoamSpeed", props, false);
            _FoamSpread = FindProperty("_FoamSpread", props, false);
            _LightAbs = FindProperty("_LightAbs", props, false);
            _Absorption = FindProperty("_Absorption", props, false);
            _DeepColor = FindProperty("_DeepColor", props, false);

            _NoiseTex = FindProperty("_NoiseTex", props);
            _ZWrite = FindProperty("_ZWrite", props, false);
            _GlobScale = FindProperty("_Scale", props, false);
            _Cull = FindProperty("_Cull", props, false);

            __Cull = FindProperty("_Cull_", props, false);
            _EnableShadows = FindProperty("_EnableShadows", props, false);
        }

        const int space = 10;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props) {
            FindProperties(props);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(-7);
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            DrawGUI(materialEditor);
            if(EditorGUI.EndChangeCheck()){
                var material = (Material)materialEditor.target;
                EditorUtility.SetDirty(material);
                LPWHiddenProps.Calculate(material);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(1);
            EditorGUILayout.EndHorizontal();
            //base.OnGUI(materialEditor, props);
        }

        public void DrawGUI(MaterialEditor materialEditor) {
            materialEditor.SetDefaultGUIWidths();
            materialEditor.UseDefaultMargins();

            lightingFold = BeginFold("Lighting", lightingFold);
            if (lightingFold) {
                materialEditor.ShaderProperty(_Color, _Color.displayName);
                if (_DeepColor != null && _LightAbs != null && _LightAbs.floatValue > .5f)
                    materialEditor.ShaderProperty(_DeepColor, _DeepColor.displayName);
                materialEditor.ShaderProperty(_Opacity, _Opacity.displayName);
                if (_Gloss != null) materialEditor.ShaderProperty(_Gloss, _Gloss.displayName);
                materialEditor.ShaderProperty(_Specular, _Specular.displayName);
                materialEditor.ShaderProperty(_SpecColor, _SpecColor.displayName);
                if(_Shadow != null && _EnableShadows != null && _EnableShadows.floatValue > .5f)
                    materialEditor.ShaderProperty(_Shadow, _Shadow.displayName); 
                if (_Diffuse != null)
                    materialEditor.ShaderProperty(_Diffuse, _Diffuse.displayName); 
                if (_PointLights != null)
                    materialEditor.ShaderProperty(_PointLights, _PointLights.displayName);
                EditorGUIUtility.labelWidth -= 30f;
                materialEditor.ShaderProperty(_Shading, _Shading.displayName);
                EditorGUIUtility.labelWidth += 30f;
            }
            EndFold();

            var material = (Material) materialEditor.target;
            var hasRefr = material.IsKeywordEnabled("WATER_REFRACTIVE");
            var hasRefl = material.IsKeywordEnabled("WATER_REFLECTIVE");
            reflFold = BeginFold("Reflection", reflFold);
            if (reflFold) {
                var lblWTemp = EditorGUIUtility.labelWidth;

                if (hasRefl && _Reflection!=null) materialEditor.ShaderProperty(_Reflection, _Reflection.displayName);

                EditorGUIUtility.labelWidth = 0f;
                materialEditor.TexturePropertySingleLine(fresnelLbl, _FresnelTex, _FresPower);
                EditorGUIUtility.labelWidth = lblWTemp;
                if(_FresColor!=null) materialEditor.ShaderProperty(_FresColor, _FresColor.displayName);
                
                if (hasRefr || hasRefl) {
                    if (_NormalOffset != null) materialEditor.ShaderProperty(_NormalOffset, _NormalOffset.displayName);
                    GUILayout.Space(space);

                    if (_Distort != null) {
                        materialEditor.ShaderProperty(_Distort, _Distort.displayName);
                        if (_Distort.floatValue > .5f) {
                            if (_Distortion != null && hasRefl) materialEditor.ShaderProperty(_Distortion, _Distortion.displayName);
                            if (_Refraction != null && hasRefr) materialEditor.ShaderProperty(_Refraction, _Refraction.displayName);
                            EditorGUIUtility.labelWidth = 0f;
                            if (_BumpTex != null) {
                                materialEditor.TexturePropertySingleLine(bumpLbl, _BumpTex, _BumpScale);
                            }
                            EditorGUIUtility.labelWidth = lblWTemp;
                            if (_BumpSpeed != null) materialEditor.ShaderProperty(_BumpSpeed, _BumpSpeed.displayName);
                        }
                    }
                }
            }
            EndFold();

            wavesFold = BeginFold("Waves", wavesFold);
            if (wavesFold) {
                EditorGUIUtility.labelWidth -= 30f;
                materialEditor.ShaderProperty(_Waves, _Waves.displayName);
                EditorGUIUtility.labelWidth += 30f;
                if (_Waves.floatValue > .5f) {
                    materialEditor.ShaderProperty(_Length, _Length.displayName);
                    materialEditor.ShaderProperty(_Stretch, _Stretch.displayName);
                    materialEditor.ShaderProperty(_Speed, _Speed.displayName);
                    materialEditor.ShaderProperty(_Height, _Height.displayName);
                    materialEditor.ShaderProperty(_Steepness, _Steepness.displayName);
                    materialEditor.ShaderProperty(_Direction, _Direction.displayName);
                }
                GUILayout.Space(space);
                materialEditor.ShaderProperty(_RSpeed, _RSpeed.displayName);
                materialEditor.ShaderProperty(_RHeight, _RHeight.displayName);
            }
            EndFold();

            shoreFold = BeginFold("Depth Effects", shoreFold);
            if (shoreFold) {
                materialEditor.ShaderProperty(_EdgeBlend, _EdgeBlend.displayName);
                bool hasDepth = _EdgeBlend.floatValue > .5f;
                if (hasDepth) {
                    materialEditor.ShaderProperty(_ShoreColor, _ShoreColor.displayName);
                    if (_ShoreIntensity != null)
                        materialEditor.ShaderProperty(_ShoreIntensity, _ShoreIntensity.displayName);
                    materialEditor.ShaderProperty(_ShoreDistance, _ShoreDistance.displayName);
                    if (_ShoreDistance.floatValue < 0) _ShoreDistance.floatValue = 0f;

                    if (_HQFoam != null) {
                        materialEditor.ShaderProperty(_HQFoam, _HQFoam.displayName);
                        if (_HQFoam.floatValue > .5f) {
                            if (_FoamScale != null)
                                materialEditor.ShaderProperty(_FoamScale, _FoamScale.displayName);
                            if (_FoamSpeed != null)
                                materialEditor.ShaderProperty(_FoamSpeed, _FoamSpeed.displayName);
                            if (_FoamSpread != null)
                                materialEditor.ShaderProperty(_FoamSpread, _FoamSpread.displayName);
                            material.DisableKeyword("LPW_FOAM");
                            material.EnableKeyword("LPW_HQFOAM");
                        } else {
                            material.EnableKeyword("LPW_FOAM");
                            material.DisableKeyword("LPW_HQFOAM");
                        }
                    }
                    
                } else {
                    material.DisableKeyword("LPW_FOAM");
                    material.DisableKeyword("LPW_HQFOAM");
                    if(_HQFoam != null) _HQFoam.floatValue = 0;
                }

                if (_LightAbs != null) {
                    GUILayout.Space(space);
                    materialEditor.ShaderProperty(_LightAbs, _LightAbs.displayName);
                    if (_LightAbs.floatValue > .5f) {

                        if (_Absorption != null) {
                            materialEditor.ShaderProperty(_Absorption, _Absorption.displayName);
                            if (_Absorption.floatValue < 0) _Absorption.floatValue = 0f;
                        }
                        if (_DeepColor != null)
                            materialEditor.ShaderProperty(_DeepColor, _DeepColor.displayName);
                    }
                }
            }
            EndFold();

            otherFold = BeginFold("Other", otherFold);
            if (otherFold) {
                if(_GlobScale != null) {
                    materialEditor.ShaderProperty(_GlobScale, _GlobScale.displayName);
                }
                materialEditor.TexturePropertySingleLine(noiseLbl, _NoiseTex);
                if (_ZWrite != null ) {
                    if(_EnableShadows == null || _EnableShadows.floatValue < 0.5f) {
                        materialEditor.ShaderProperty(_ZWrite, _ZWrite.displayName);
                    }
                }
                if (__Cull != null) {
                    materialEditor.ShaderProperty(_Cull, _Cull.displayName);
                    __Cull.floatValue = 2 - _Cull.floatValue * 2;
                }
            }
            EndFold();

        }

        public static bool BeginFold(string foldName, bool foldState) {
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Space(3);

            EditorGUI.indentLevel++;
            foldState = EditorGUI.Foldout(EditorGUILayout.GetControlRect(),
                foldState, " - " + foldName + " - ", true, foldoutStyle);
            EditorGUI.indentLevel--;

            if (foldState) GUILayout.Space(3);
            return foldState;
        }

        public static void EndFold() {
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
            GUILayout.Space(0);
        }

        bool init = false;
        public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background) {
            base.OnMaterialPreviewGUI(materialEditor, r, background);
            if (init) return;
            LPWHiddenProps.Calculate((Material)materialEditor.target);
            init = true;
        }

    }
}
