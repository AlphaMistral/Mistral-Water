using UnityEditor;
using UnityEngine;

namespace LPWAsset {
    [CustomEditor(typeof(LowPolyWaterScript))]
    public class LPWInspector : Editor {

        private LowPolyWaterScript _target;

        // We need to use and to call an instnace of the default MaterialEditor
        private MaterialEditor _materialEditor;

        static GUIContent reflCfgLbl = new GUIContent("Camera Settings for Reflection & Refraction");
        static Texture2D bannerTex = null;
        static GUIStyle rateTxt = null;
        static GUIStyle title = null;
        static GUIStyle linkStyle = null;

        void OnEnable() {
            _target = (LowPolyWaterScript)target;
            //SetKeywords();

            if (_target.material != null) {
                // Create an instance of the default MaterialEditor
                _materialEditor = (MaterialEditor)CreateEditor(_target.material);
            }

            if (bannerTex == null) bannerTex = Resources.Load<Texture2D>("banner");

            if (rateTxt == null) {
                rateTxt = new GUIStyle();
                //rateTxt.alignment = TextAnchor.LowerCenter;
                rateTxt.alignment = TextAnchor.LowerRight;
                rateTxt.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                rateTxt.fontSize = 9;
                rateTxt.padding = new RectOffset(0, 0, 0, 0);
            }

            if (title == null) {
                title = new GUIStyle(rateTxt);
                //title.alignment = TextAnchor.UpperCenter;
                title.normal.textColor = new Color(1f, 1f, 1f);
                title.alignment = TextAnchor.MiddleCenter;
                title.fontSize = 19;
                title.padding = new RectOffset(0, 0, 0, 3);
            }

            linkStyle = new GUIStyle();

            LPWHiddenProps.Scale(_target);

            if (_target.material != null) {
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();

                if (_target.material.HasProperty("_EnableShadows"))
                    _target.receiveShadows = _target.material.GetFloat("_EnableShadows") > .5f;
                var oldRefl = _target.enableReflection;
                var oldRefr = _target.enableRefraction;
                _target.enableRefraction = _target.material.IsKeywordEnabled("WATER_REFRACTIVE");
                _target.enableReflection = _target.material.IsKeywordEnabled("WATER_REFLECTIVE");
                if(oldRefl != _target.enableReflection || oldRefr != _target.enableRefraction)
                    _target.Generate();
            }
            LPWHiddenProps.SetKeywords(_target);
        }

        public override void OnInspectorGUI() {
            bool isLite = _target.material != null && _target.material.shader.name.Contains("Lite");
            LPWHiddenProps.Scale(_target);
            bool guiChanged = false;

            // Banner
            if (bannerTex != null ) {
                GUILayout.Space(5);
                var rect = GUILayoutUtility.GetRect(0, int.MaxValue, 30, 30);
                EditorGUI.DrawPreviewTexture(rect, bannerTex, null, ScaleMode.ScaleAndCrop);
                //EditorGUI.LabelField(rect, "Rate \u2605\u2605\u2605\u2605\u2605", rateTxt);
                EditorGUI.LabelField(rect, "Rate | Review", rateTxt);
                EditorGUI.LabelField(rect, "Low Poly Water", title);
                
                if (GUI.Button(rect, "", linkStyle) ){
                    Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/account/downloads/search=Low%20Poly%20Water");
                }
                GUILayout.Space(3);
            } 

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("material"));
            bool matChanged = EditorGUI.EndChangeCheck();
            guiChanged = guiChanged || matChanged;
            if (matChanged || _materialEditor == null) {
                if (_materialEditor != null) {
                    // Free the memory used by the previous MaterialEditor
                    DestroyImmediate(_materialEditor);
                }

                if (_target.material != null) {
                    // Create a new instance of the default MaterialEditor
                    _materialEditor = (MaterialEditor)CreateEditor(_target.material);

                }
            }

            EditorGUI.BeginChangeCheck();

            if (isLite) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gridTypeLite"));
            } else {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gridType"));
            }
            
            if (_target.gridType == LowPolyWaterScript.GridType.Custom) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customMesh"));
            } else if (_target.gridType == LowPolyWaterScript.GridType.HexagonalLOD) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LOD"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LODPower"));
                EditorGUI.indentLevel--;
            }

            if (_target.gridType != LowPolyWaterScript.GridType.Custom) {
                var sizeX = serializedObject.FindProperty("sizeX");
                var sizeZ = serializedObject.FindProperty("sizeZ");

                // size X Z on the same line
                EditorGUILayout.BeginHorizontal();
                if (_target.sizeX > 150 || _target.sizeZ > 150)
                    EditorGUILayout.LabelField("Size (Regenerate)", GUILayout.MinWidth(30));
                else 
                    EditorGUILayout.LabelField("Size", GUILayout.MinWidth(30));
                GUILayout.FlexibleSpace();
                var lblW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 15;
                sizeX.intValue = EditorGUILayout.IntField("X", sizeX.intValue);
                sizeZ.intValue = EditorGUILayout.IntField("Z", sizeZ.intValue);
                EditorGUIUtility.labelWidth = lblW;
                EditorGUILayout.EndHorizontal();

                
            }
            guiChanged = guiChanged || EditorGUI.EndChangeCheck();

            //Scale
            var lScale = _target.transform.localScale;
            EditorGUILayout.BeginHorizontal();
            var lblWi = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            EditorGUILayout.LabelField("Scale");
            GUILayout.FlexibleSpace();
            EditorGUIUtility.labelWidth = 15;
            lScale.x = EditorGUILayout.FloatField("X", lScale.x);
            lScale.z = EditorGUILayout.FloatField("Z", lScale.z);
            EditorGUIUtility.labelWidth = lblWi;
            EditorGUILayout.EndHorizontal();
            _target.transform.localScale = lScale;

            // Noise
            EditorGUI.BeginChangeCheck();
            if (_target.gridType != LowPolyWaterScript.GridType.Custom) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("noise"));
            }
            guiChanged = guiChanged || EditorGUI.EndChangeCheck();

            if (!isLite) {
                EditorGUI.BeginChangeCheck();
                var shadows = serializedObject.FindProperty("receiveShadows");
                EditorGUILayout.PropertyField(shadows);
                var recShadChanged = EditorGUI.EndChangeCheck();
                guiChanged = guiChanged || recShadChanged;
                if (recShadChanged && _target.material != null && _target.material.HasProperty("_EnableShadows")) {
                    _target.material.SetFloat("_EnableShadows", shadows.boolValue ? 1f : 0);
                }

                bool reflChanged = false;
                EditorGUI.BeginChangeCheck();
                var refl = serializedObject.FindProperty("enableReflection");
                var refr = serializedObject.FindProperty("enableRefraction");
                EditorGUILayout.PropertyField(refl);
                EditorGUILayout.PropertyField(refr);
                reflChanged = EditorGUI.EndChangeCheck();
                guiChanged = guiChanged || reflChanged;

                if (refl.boolValue || refr.boolValue) {
                    var refOptions = serializedObject.FindProperty("reflection");
                    if (reflChanged) {
                        refOptions.isExpanded = true;
                        if (_target.material != null && _target.material.HasProperty("_ZWrite"))
                            _target.material.SetFloat("_ZWrite", 1);
                    }
                    EditorGUILayout.PropertyField(refOptions, reflCfgLbl,  true);
                }
            } else {
                _target.enableReflection = false; 
                _target.enableRefraction = false;
                _target.receiveShadows = false;
            }

            EditorGUI.BeginChangeCheck();
            if (_target.material != null && _target.material.HasProperty("_Sun")) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sun"));
            }
            guiChanged = guiChanged || EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            _target.sizeX = Mathf.Clamp(_target.sizeX, 1, 400);
            _target.sizeZ = Mathf.Clamp(_target.sizeZ, 1, 400);
            bool doGenerate = false;
            if (_target.sizeX > 150 || _target.sizeZ  > 150) {
                _target.displayProgress = true;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                doGenerate = GUILayout.Button("Regenerate", GUILayout.MaxWidth(200));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                if (doGenerate) guiChanged = true;
            } else {
                _target.displayProgress = false;
                doGenerate = true;
            }

            if (guiChanged) {
                _target.reflection.textureSize = Mathf.Clamp(_target.reflection.textureSize, 1, 4096);

                if (isLite) {
                    _target.gridType = (LowPolyWaterScript.GridType)(_target.gridTypeLite);
                }

                LPWHiddenProps.SetKeywords(_target);

                if (doGenerate) _target.Generate();

            }

            if (_materialEditor != null) {
                // Draw the material's foldout and the material shader field
                // Required to call _materialEditor.OnInspectorGUI ();
                _materialEditor.DrawHeader();

                //  We need to prevent the user to edit Unity default materials
                bool isDefaultMaterial = !AssetDatabase.GetAssetPath(_target.material).StartsWith("Assets");

                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial)) {

                    // Draw the material properties
                    // Works only if the foldout of _materialEditor.DrawHeader () is open
                    _materialEditor.OnInspectorGUI();
                }
            }

        }

        void OnDisable() {
            if (_materialEditor != null) {
                // Free the memory used by default MaterialEditor
                DestroyImmediate(_materialEditor);
            }
        }

    }
}