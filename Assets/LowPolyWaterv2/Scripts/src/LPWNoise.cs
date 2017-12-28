using UnityEngine;

namespace LPWAsset {
    public static class LPWNoise {

        public static float GetValue(float x, float y) {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);

            float xs = x - ix;
            float ys = y - iy;

            float n0 = Hash(ix, iy);
            float n1 = Hash(ix + 1, iy);
            float ix0 = Mathf.Lerp(n0, n1, xs);

            n0 = Hash(ix, iy + 1);
            n1 = Hash(ix + 1, iy + 1);
            float ix1 = Mathf.Lerp(n0, n1, xs);

            return Mathf.Lerp(ix0, ix1, ys);
        }

        const int factorX = 1619;
        const int factorY = 31337;
        const int factorZ = 6971;
        const int factorSeed = 1013;
        //static int seed = 1337;

        public static float Hash(int x, int z) {
            int n = (
                factorX * x
              + factorZ * z
              + factorSeed) //* seed)
              & 0x7fffffff;
            n = (n >> 13) ^ n;
            n = (n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff;
            return 1 - (n / 1073741824f); // normalize for [-1,1]
        }
    }
}