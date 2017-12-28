using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LPWAsset {
    [ExecuteInEditMode]
    public class LowPolyWaterScript : MonoBehaviour {

        public enum GridType { Hexagonal=0, Square=1, HexagonalLOD=2, Custom=3};

        #region Public Variables
        public GridType gridType = GridType.Hexagonal;
        [Range(0, 1f)]
        public float LOD = 0.2f;
        [Range(2, 10)]
        public float LODPower = 3;
        public Mesh customMesh;
        public Material material;
        public Light sun;
        public int sizeX = 30;
        public int sizeZ = 30;
        [Range(0, 1)]
        public float noise = 0.5f;
        public bool enableReflection = false;
        public bool enableRefraction = false;
        public LPWReflectionParams reflection;
        public bool receiveShadows = false;
        #endregion

        #region Constants
        static bool enableDisplace = false;
        const int maxVerts = ushort.MaxValue;
        const float sin60 = 0.86602540378f;
        const float inv_tan60 = 0.57735026919f;
        static bool hideChildObjects_ = true;
        #endregion

        #region Deprecated
        [HideInInspector]
        public float waveScale = -1337;
        public enum GridTypeLite { Hexagonal=0, Square=1};
        [HideInInspector]
        public GridTypeLite gridTypeLite;
        #endregion

        #region Unity functions

        void OnEnable() {
            #if UNITY_EDITOR
                if(material != null) LPWHiddenProps.Calculate(material);
                LPWHiddenProps.Scale(this);
                LPWHiddenProps.SetKeywords(this);
            CustomMeshApply();
            #endif

            // handle deprecation
            if (!Mathf.Approximately(-1337, waveScale) && material != null && material.HasProperty("_Scale")) {
                material.SetFloat("_Scale", waveScale);
                waveScale = -1337;
            }

            #if UNITY_EDITOR
                displayProgress = false;
            #endif
            var chunks = GetComponentsInChildren<LPWWaterChunk>();
            if (chunks == null || chunks.Length == 0) {
                Generate();
            }
            #if UNITY_EDITOR
                else {
                    Generate(); // generate on enable in editor
                }
            #endif
        }

        void Update() {
            // Pass the sun info to the material
            if (sun == null) {
                var lights = Light.GetLights(LightType.Directional, SortingLayer.GetLayerValueFromName("Default"));
                if(lights.Length > 0) sun = lights[0];
            }
                
            if (material == null || !material.HasProperty("_Sun") || !material.HasProperty("_SunColor") || sun == null) return;
            material.SetVector("_Sun", -sun.transform.forward);
            material.SetColor("_SunColor", sun.color);

            if (enableDisplace) {
                var _Time_ = Time.realtimeSinceStartup;
                material.SetFloat("_Time_", _Time_);
            }
            
        }

        void OnDisable() {
            if (gameObject.activeInHierarchy) {
                CleanUp(true);
            }
        }

        void OnDestroy() {
            CleanUp(true);
        }
        #endregion

        void CleanUp(bool destroy= false) {
            // clear all previous objects
            var chunks = GetComponentsInChildren<LPWWaterChunk>();
            for (int i = 0; i < chunks.Length; i++) {
                if(destroy) Destroy_(chunks[i].GetComponent<MeshFilter>().sharedMesh);
                Destroy_(chunks[i].gameObject);
            }
            #if UNITY_EDITOR
                System.GC.Collect();
            #endif
        }

        void Destroy_(Object o) {
            if (Application.isPlaying) {
                Destroy(o);
            } else {
                DestroyImmediate(o);
            }
        }

        public void Generate() {
            if (material == null || !gameObject.activeInHierarchy) {
                return;
            }
            CleanUp();

            try {
                if (gridType == GridType.Hexagonal || gridType == GridType.HexagonalLOD) {
                    GenerateHexagonal();
                } else if (gridType == GridType.Square) {
                    GenerateSquare();
                } else if (gridType == GridType.Custom) {
                    BakeCustomMesh(customMesh);
                }
            } catch (System.Exception) {
                throw;
            } finally {
                #if UNITY_EDITOR
                    EditorUtility.ClearProgressBar();
                #endif
            }

        }

        #if UNITY_EDITOR
        public void CustomMeshApply() {
            // script is added to GO with mesh filter -> convert mesh to water
            var mr = GetComponent<MeshRenderer>();
            var mf = GetComponent<MeshFilter>();
            var smr = GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = null;
            Material mat_ = null;
            if (mr != null) {
                mat_ = mr.sharedMaterial;
                mr.enabled = false;
            }
            if (smr != null) {
                mesh = smr.sharedMesh;
                mat_ = smr.sharedMaterial;
                smr.enabled = false;
            } else if (mf != null) {
                mesh = mf.sharedMesh;
            }
            if (mat_ != null && mat_.shader != null && mat_.shader.name.Contains("LowPolyWater")) {
                material = mat_;
            } else if (material == null) {
                material = Resources.Load<Material>("LPWDefault");
            }
            if (mesh != null) {
                customMesh = mesh;
                gridType = GridType.Custom;
            }
        }

        [HideInInspector]
        public bool displayProgress = false;
        public void Progress(float progress) {
            EditorUtility.DisplayProgressBar("Low Poly Water GPU", "Generating Mesh..", progress);
        }

        #endif

        #region Mesh Bakers

        float Encode(Vector3 v) {
            var uv0 = Mathf.Round((v.x + 5) * 10000f);
            var uv1 = Mathf.Round((v.z + 5) * 10000f) / 100000f;
            return uv0 + uv1;
        }

        void BakeCustomMesh(Mesh originalMesh, float rotation = 0f) {
            if (originalMesh == null) return;
            var normalSolver = new LPWNormalSolver();

            var verts = originalMesh.vertices;
            var inds = originalMesh.triangles;
            var norms = originalMesh.normals;

            var uv0s = new List<Vector4>(inds.Length);
            var uv1s = new List<Vector2>(inds.Length);
            var splitIndices = new List<int>(inds.Length);
            var splitVertices = new List<Vector3>(inds.Length);
            var splitNormals = new List<Vector3>(inds.Length);

            for (int i = 0; i < inds.Length; i += 3) {
                splitIndices.Add(i % maxVerts);
                splitIndices.Add((i + 1) % maxVerts);
                splitIndices.Add((i + 2) % maxVerts);

                var v0 = verts[inds[i]];
                var v1 = verts[inds[i + 1]];
                var v2 = verts[inds[i + 2]];

                splitVertices.Add(v0);
                splitVertices.Add(v1);
                splitVertices.Add(v2);

                splitNormals.Add(norms[inds[i]]);
                splitNormals.Add(norms[inds[i + 1]]);
                splitNormals.Add(norms[inds[i + 2]]);

                var va = v0 - v1;
                var vb = v0 - v2;
                uv0s.Add(new Vector4(va.x, va.y, vb.x, vb.y));
                uv1s.Add(new Vector2(va.z, vb.z));

                va = v1 - v2;
                vb = v1 - v0;
                uv0s.Add(new Vector4(va.x, va.y, vb.x, vb.y));
                uv1s.Add(new Vector2(va.z, vb.z));

                va = v2 - v0;
                vb = v2 - v1;
                uv0s.Add(new Vector4(va.x, va.y, vb.x, vb.y));
                uv1s.Add(new Vector2(va.z, vb.z));
            }

            normalSolver.Recalculate(splitNormals, splitVertices, splitIndices);

            int numGO = Mathf.CeilToInt(splitVertices.Count / (float)maxVerts);
            var mfs = new List<MeshFilter>(numGO);
            for (int i = 0, pos = 0; i < numGO; i++, pos += maxVerts) {
                var go = new GameObject("LPWWaterChunk");
                if (gameObject != null && gameObject.layer != LayerMask.NameToLayer("Default"))
                    go.layer = gameObject.layer;
                else
                    go.layer = LayerMask.NameToLayer("Water");
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0, rotation, 0);
                go.transform.localScale = Vector3.one;
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var eb = go.AddComponent<LPWDepthEffect>();
                eb.Init(receiveShadows);
                go.AddComponent<LPWWaterChunk>();
                if (enableReflection || enableRefraction) {
                    var rc = go.AddComponent<LPWReflection>();
                    rc.Init(reflection, enableReflection, enableRefraction);
                }
                mr.sharedMaterial = material;
                mr.receiveShadows = receiveShadows;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var mesh = new Mesh();
                mesh.name = "LPWWaterChunk";

                var len = i == numGO - 1 ? splitVertices.Count - pos : maxVerts;

                mesh.SetVertices(splitVertices.GetRange(pos, len));
                mesh.SetTriangles(splitIndices.GetRange(pos, len), 0);
                mesh.SetNormals(splitNormals.GetRange(pos, len));
                mesh.SetUVs(0, uv0s.GetRange(pos, len));
                mesh.SetUVs(1, uv1s.GetRange(pos, len));
                mesh.hideFlags = hideChildObjects_ ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                mf.mesh = mesh;
                go.hideFlags = hideChildObjects_ ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                mfs.Add(mf);
            }
        }

        void BakeMesh(List<Vector3> verts, List<int> inds, float rotation = 0f) {
            var splitIndices = new List<int>(inds.Count);
            var splitVertices = new List<Vector3>(inds.Count);

            for (int i = 0; i < inds.Count; i += 3) {
                splitIndices.Add(i % maxVerts);
                splitIndices.Add((i + 1) % maxVerts);
                splitIndices.Add((i + 2) % maxVerts);

                var v0 = verts[inds[i]];
                var v1 = verts[inds[i + 1]];
                var v2 = verts[inds[i + 2]];

                splitVertices.Add(v0);
                splitVertices.Add(v1);
                splitVertices.Add(v2);
            }

            int numGO = Mathf.CeilToInt(splitVertices.Count / (float)maxVerts);
            var mfs = new List<MeshFilter>(numGO);
            for (int i = 0, pos = 0; i < numGO; i++, pos += maxVerts) {
                var go = new GameObject("LPWWaterChunk");
                if (gameObject != null && gameObject.layer != LayerMask.NameToLayer("Default"))
                    go.layer = gameObject.layer;
                else
                    go.layer = LayerMask.NameToLayer("Water");
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0, rotation, 0);
                go.transform.localScale = Vector3.one;
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var eb = go.AddComponent<LPWDepthEffect>();
                eb.Init(receiveShadows);
                go.AddComponent<LPWWaterChunk>();
                if (enableReflection || enableRefraction) {
                    var rc = go.AddComponent<LPWReflection>();
                    rc.Init(reflection, enableReflection, enableRefraction);
                }
                mr.sharedMaterial = material;
                mr.receiveShadows = receiveShadows;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var mesh = new Mesh();
                mesh.name = "LPWWaterChunk";

                var len = i == numGO - 1 ? splitVertices.Count - pos : maxVerts;

                mesh.SetVertices(splitVertices.GetRange(pos, len));
                mesh.SetTriangles(splitIndices.GetRange(pos, len), 0);
                mesh.hideFlags = hideChildObjects_ ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                mf.mesh = mesh;
                go.hideFlags = hideChildObjects_ ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                mfs.Add(mf);
            }

            if (gridType == GridType.HexagonalLOD) BakeUVsV4(splitVertices, mfs);
            else BakeUVs(splitVertices, mfs);

        }

        void BakeUVs(List<Vector3> verts, List<MeshFilter> mfs) {
            var uvs = new List<Vector2>(verts.Count);
            for (int i = 0; i < verts.Count; i += 3) {
                var v0 = verts[i];
                var v1 = verts[i + 1];
                var v2 = verts[i + 2];

                var uv = new Vector2();
                uv.x = Encode(v0 - v1);
                uv.y = Encode(v0 - v2);
                uvs.Add(uv);

                uv.x = Encode(v1 - v2);
                uv.y = Encode(v1 - v0);
                uvs.Add(uv);

                uv.x = Encode(v2 - v0);
                uv.y = Encode(v2 - v1);
                uvs.Add(uv);
            }

            for (int i = 0, pos = 0; i < mfs.Count; i++, pos += maxVerts) {
                var len = i == mfs.Count - 1 ? verts.Count - pos : maxVerts;
                mfs[i].sharedMesh.SetUVs(0, uvs.GetRange(pos, len));
            }

        }

        void BakeUVsV4(List<Vector3> verts, List<MeshFilter> mfs) {
            var uvs = new List<Vector4>(verts.Count);
            for (int i = 0; i < verts.Count; i += 3) {
                var v0 = verts[i];
                var v1 = verts[i + 1];
                var v2 = verts[i + 2];

                var va = v0 - v1;
                var vb = v0 - v2;
                uvs.Add(new Vector4(va.x, va.z, vb.x, vb.z));

                va = v1 - v2;
                vb = v1 - v0;
                uvs.Add(new Vector4(va.x, va.z, vb.x, vb.z));

                va = v2 - v0;
                vb = v2 - v1;
                uvs.Add(new Vector4(va.x, va.z, vb.x, vb.z));
            }

            for (int i = 0, pos = 0; i < mfs.Count; i++, pos += maxVerts) {
                var len = i == mfs.Count - 1 ? verts.Count - pos : maxVerts;
                mfs[i].sharedMesh.SetUVs(0, uvs.GetRange(pos, len));
            }

        }
        #endregion

        #region Mesh Generation

        Vector3 ApplyLOD(Vector3 v, float dist) {
            var xz = new Vector2(v.x, v.z);
            xz = xz * (1f + Mathf.Pow(dist * LOD / 10f, LODPower));
            return new Vector3(xz[0], v.y, xz[1]);
        }

        Vector3 AddNoise(Vector3 v) {
            var worldPos = transform.TransformPoint(v) * 4f / material.GetFloat("_Scale_");
            var noiseVec = new Vector2(
                LPWNoise.GetValue(worldPos.x, worldPos.z),
                LPWNoise.GetValue(worldPos.x, worldPos.z + 100f));
            noiseVec = noiseVec * 1.2f;
            return noiseVec * noise;
        }

        void Add(List<Vector3> verts, Vector3 toAdd, float delta) {
            if (noise > 0) {
                var n = AddNoise(toAdd) / 2f;
                toAdd.x += n.x;
                toAdd.z += n.y;
            }
            verts.Add(toAdd);
        }

        void GenerateSquare() {
            var verts = new List<Vector3>();
            var inds = new List<int>();
            //90degr rot
            var numVertsX = sizeX * 2;
            var numVertsZ = sizeZ * 2;
            var delta = sin60;
            var deltaX = Vector3.right * delta;
            var vO = new Vector3(-sizeX * sin60, 0, -sizeZ * sin60);

            for (int j = 0; j < numVertsZ + 1; j++) {
                #if UNITY_EDITOR
                    if(displayProgress && j%20==0) {
                        var p = j / (float)(numVertsZ);
                        Progress(p * .6f );
                    }
                #endif
                bool reverse = j % 2 != 0;
                var v = vO + Vector3.forward * j * delta;
                int cols = numVertsX + (reverse ? 2 : 1);
                for (int i = 0; i < cols; i++) {
                    Add(verts, v, delta);
                    if (reverse && (i == 0 || i == cols - 2)) {
                        v += deltaX / 2f;
                    } else {
                        v += deltaX;
                    }
                }
            }
            int iCur = 0;
            for (int j = 0; j < numVertsZ; j++) {
                #if UNITY_EDITOR
                    if(displayProgress && j%20==0) {
                        var p = j / (float)(numVertsZ);
                        Progress(.6f+p * .2f );
                    }
                #endif
                bool reverse = j % 2 != 0;
                int ofs = numVertsX + (reverse ? 2 : 1);
                int cols = numVertsX + (reverse ? 0 : 0);

                int iForw = iCur + ofs;

                for (int i = 0; i < cols; i++) {
                    int iRight = iCur + 1;
                    int iForwRight = iForw + 1;

                    inds.Add(iCur);
                    if (reverse) {
                        inds.Add(iForw);
                        inds.Add(iRight);
                        inds.Add(iForw);
                        inds.Add(iForwRight);
                        inds.Add(iRight);
                    } else {
                        inds.Add(iForwRight);
                        inds.Add(iRight);
                        inds.Add(iCur);
                        inds.Add(iForw);
                        inds.Add(iForwRight);
                    }
                    iCur = iRight;
                    iForw = iForwRight;
                }
                inds.Add(iCur);
                if (reverse) {
                    inds.Add(iForw);
                    inds.Add(iCur + 1);
                    iCur += 2;
                } else {
                    inds.Add(iForw);
                    inds.Add(iForw + 1);
                    iCur++;
                }
            }

            BakeMesh(verts, inds);
        }

        void GenerateHexagonal() {
            var verts = new List<Vector3>();
            var inds = new List<int>();

            int vertIndex = 0;
            int curNumPoints = 0;
            int prevNumPoints = 0;
            int numPointsCol0 = sizeX + sizeZ + 1;
            int colMin = -sizeX;
            int colMax = sizeX;

            for (int i = colMin; i <= colMax; i++) {
                #if UNITY_EDITOR
                    if(displayProgress && i%10==0) {
                        var p = (i - colMin) / (float)(colMax - colMin);
                        Progress(p * .8f );
                    }
                #endif
                float x = sin60 * i;

                int numPointsColi = numPointsCol0 - Mathf.Abs(i);

                int rowMin = -(sizeZ + sizeX) / 2;
                if (i < 0) rowMin += Mathf.Abs(i);

                int rowMax = rowMin + numPointsColi - 1;

                curNumPoints += numPointsColi;

                for (int j = rowMin; j <= rowMax; j++) {
                    float z = inv_tan60 * x + j;

                    var v = new Vector3(x, 0, z);
                    if (noise > 0) {
                        var n = AddNoise(v) / 2f;
                        v.x += n.x; 
                        v.z += n.y;
                    }

                    if (gridType == GridType.HexagonalLOD) {
                        var dist = i < 0 == j < 0 ? Mathf.Abs(i) + Mathf.Abs(j) : Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));
                        v = ApplyLOD(v, dist);
                    }

                    verts.Add(v);

                    if (vertIndex < (curNumPoints - 1)) {
                        if (i >= colMin && i < colMax) {
                            int padLeft = 0;
                            if (i < 0) padLeft = 1;
                            inds.Add(vertIndex);
                            inds.Add(vertIndex + 1);
                            inds.Add(vertIndex + numPointsColi + padLeft);
                        }

                        if (i > colMin && i <= colMax) {
                            int padRight = 0;
                            if (i > 0) padRight = 1;
                            inds.Add(vertIndex + 1);
                            inds.Add(vertIndex);
                            inds.Add(vertIndex - prevNumPoints + padRight);
                        }
                    }

                    vertIndex++;
                }

                prevNumPoints = numPointsColi;
            }

            BakeMesh(verts, inds);
        }
        #endregion

    }
}