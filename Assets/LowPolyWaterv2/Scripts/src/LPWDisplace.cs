using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LPWAsset {
    public static class Displacement {

        static float _Scale_, _TexSize_, _Stretch, _Length, _Height_, _RHeight_;
        static Vector4 _Direction_;
        static Texture2D _NoiseTex;
        static bool wavesHQ;

        public static float Get(Vector3 position, Material waterMaterial) {
            var mat = waterMaterial;
            if (mat == null || !mat.HasProperty("_Scale_")) return 0;

            _Scale_ = mat.GetFloat("_Scale_");
            _TexSize_ = mat.GetFloat("_TexSize_");
            _Stretch = mat.GetFloat("_Stretch");
            _Length = mat.GetFloat("_Length");
            _Height_ = mat.GetFloat("_Height_");
            _RHeight_ = mat.GetFloat("_RHeight_");
            //var _RSpeed_ = mat.GetFloat("_RSpeed_");
            var _Speed_ = mat.GetFloat("_Speed_");
            var _Time_ = Time.realtimeSinceStartup;

            //mat.SetFloat("_Time_", _Time_);
            _Direction_ = mat.GetVector("_Direction_");

            _NoiseTex = mat.GetTexture("_NoiseTex") as Texture2D;

            #if UNITY_EDITOR
            SetTextureReadable(_NoiseTex);
            #endif

            bool wavesOff = mat.IsKeywordEnabled("_WAVES_OFF");
            wavesHQ = mat.IsKeywordEnabled("_WAVES_HIGHQUALITY");

            //position.y += ripple(new Vector2(position.x, position.z), _Time_ * _RSpeed_);
            if (!wavesOff) gerstner(ref position, _Time_ * _Speed_);

            return position.y;
        }

        static float noise(Vector2 x) {
            x /= _Scale_;
            var p = floor(x);
            var f = frac(x);
            f = Vector2.Scale(Vector2.Scale(f, f), (Vector2.one * 3f - 2f * f));
            float n = p.x * 57f + p.y;
            return Mathf.Lerp(Mathf.Lerp(hash(n), hash(n + 1f), f.y), Mathf.Lerp(hash(n + 57f), hash(n + 58f), f.y), f.x) - 0.5f;
        }

        static float noiseLQ(Vector2 uv) {
            uv /= _TexSize_;
            //uv = Vector2.one - uv;
            var n = _NoiseTex.GetPixelBilinear(uv.x, uv.y).a;
            return Mathf.SmoothStep(0, 1, n) - 0.5f;
        }

        static void gerstner(ref Vector3 p, float phase) {
            var x = p.x * _Direction_.x - p.z * _Direction_.y;
            var z = p.z * _Direction_.x + p.x * _Direction_.y;

            float n = 0;
            if (wavesHQ)
                n = noise(new Vector2(x / _Stretch, z / _Length + phase));
            else
                n = noiseLQ(new Vector2(x / _Stretch, z / _Length + phase));
            p.y += _Height_ * n;
            //p.x -= n * _Direction_.w;
            //p.z -= n * _Direction_.z;
        }

        static float ripple(Vector2 p, float phase) {
            var uv = new Vector2(p.x, phase + p.y) / _TexSize_;
            return (_NoiseTex.GetPixelBilinear(uv.x, uv.y).a - 0.5f) * _RHeight_;
        }

        static float hash(float n) {
            return frac(Mathf.Sin(n)*10f);// * 43758.5453f);
        }

        static float frac(float x) {
            return x - floor(x);
        }

        static Vector2 frac(Vector2 v) {
            return new Vector2(frac(v.x), frac(v.y));
        }

        static float floor(float x) {
            var xi = (int)x;
            return x < xi ? xi - 1 : xi;
        }

        static Vector2 floor(Vector2 v) {
            return new Vector2(floor(v.x), floor(v.y));
        }

        #if UNITY_EDITOR
        public static void SetTextureReadable(Texture2D texture) {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null) {
                #if UNITY_5_4
                    tImporter.textureType = TextureImporterType.Advanced;
                #else
                    tImporter.textureType = TextureImporterType.Default;
                #endif

                if (tImporter.isReadable) return;
                tImporter.isReadable = true;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }
#endif
            }
}