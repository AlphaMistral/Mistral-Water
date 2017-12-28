/**
 * The following code is loosely inspired by: http://schemingdeveloper.com
 *
 * Visit their game studio website: http://stopthegnomes.com
 *
 * License: You may use this code however you see fit, as long as you give credit when
 * 			explicitly asked and as long as you include this notice without any modifications.
 *
 * 			You may not publish a paid asset on Unity store if its main function is based on
 *			the following code, but you may publish a paid asset that uses this as part of a
 *			larger suite. You may still publish a free asset whose main function is using this
 *			script if you give us credit in the asset description.
 *
 *			If you intend to use this in a Unity store asset, it would be appreciated, but
 *			not required, if you let us know with a link to the asset.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
namespace LPWAsset {
    public class LPWNormalSolver {

        List<Vector3> triNormals; //Holds the normal of each triangle
        List<LPWPointDataEntry> pointData;
        List<LPWPoint> points;
        Dictionary<LPWPosition, int> pointsDict;
        Dictionary<int, int> toMerge;

        Dictionary<int, int> idxDict;
        List<Vector3> newVerts, newNormals;
        List<Color32> newColors;
        List<int> newTris;

        public LPWNormalSolver() {
            triNormals = new List<Vector3>();
            pointData = new List<LPWPointDataEntry>();
            points = new List<LPWPoint>();
            pointsDict = new Dictionary<LPWPosition, int>();
        }

        public void Recalculate(List<Vector3> normals, List<Vector3> vertices, List<int> triangles) {
            triNormals.Clear();
            pointData.Clear();
            points.Clear();
            pointsDict.Clear();

            // triangle normals + num tris per point
            for (var i = 0; i < triangles.Count; i += 3) {
                var v0 = vertices[triangles[i]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                //Calculate the normal of the triangle
                triNormals.Add(Vector3.Cross(v1 - v0, v2 - v0).normalized);

                AddPoint(v0);
                AddPoint(v1);
                AddPoint(v2);
            }

            // make point list
            int curIdx = 0;
            for (int i = 0; i < points.Count; i++) {
                var point = points[i];
                point.idx = curIdx;
                curIdx += point.count;
                for (int j = 0; j < point.count; j++) {
                    pointData.Add(new LPWPointDataEntry());
                }
                point.count = 0;
                points[i] = point;
            }

            // add data
            for (var i = 0; i < triangles.Count; i += 3) {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];
                int triIdx = i / 3;
                AddData(vertices[i0], triIdx, i0);
                AddData(vertices[i1], triIdx, i1);
                AddData(vertices[i2], triIdx, i2);
            }

            //Foreach point in space (not necessarily the same vertex index!)
            for (int i = 0; i < points.Count; ++i) {
                var pnt = points[i];
                for (int j = 0; j < pnt.count; ++j) { //  Foreach triangle T1 that point belongs to
                    var sum = new Vector3();
                    var pj = pointData[pnt.idx + j];
                    for (int k = 0; k < pnt.count; ++k) {  //    Foreach other triangle T2 (including self) that point belongs to and that
                        var pk = pointData[pnt.idx + k];
                        sum += triNormals[pk.triIdx];
                    }
                    //    > Normalize temporary Vector3 to find the average
                    //    > Assign the normal to corresponding vertex of T1
                    normals[pj.vertIdx] = sum.normalized;
                }
            }
        }

        bool EqualApprox(Vector3 a, Vector3 b) {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }

        void AddData(Vector3 v, int triIdx, int vertIdx) {
            var pntIdx = pointsDict[new LPWPosition(v)];
            var point = points[pntIdx];
            pointData[point.idx + point.count] = new LPWPointDataEntry(triIdx, vertIdx);
            point.count += 1;
            points[pntIdx] = point;
        }

        void AddPoint(Vector3 v) {
            var key = new LPWPosition(v);
            LPWPoint point;
            int idx;

            if (!pointsDict.TryGetValue(key, out idx)) {
                point = new LPWPoint(0, 1);
                pointsDict.Add(key, points.Count);
                points.Add(point);
            } else {
                point = points[idx];
                point.count += 1;
                points[idx] = point;
            }
        }

        struct LPWPointDataEntry {
            public int triIdx, vertIdx;
            public LPWPointDataEntry(int triIdx, int vertIdx) {
                this.triIdx = triIdx;
                this.vertIdx = vertIdx;
            }
        }

        struct LPWPoint {
            public int idx, count;

            public LPWPoint(int idx, int count) {
                this.idx = idx;
                this.count = count;
            }
        }

        struct LPWPair : IEquatable<LPWPair> {
            public int x, y;

            public LPWPair(int i, int j) { this.x = i; this.y = j; }

            public override int GetHashCode() {
                return x.GetHashCode() + y.GetHashCode();
            }

            public bool Equals(LPWPair p) {
                return (x == p.x && y == p.y) || (x == p.y && y == p.x);
            }
        }

        struct LPWPosition : IEquatable<LPWPosition> {
            readonly long _x;
            readonly long _y;
            readonly long _z;

            //Change this if you require a different precision.
            const int Tolerance = 100000;

            public LPWPosition(Vector3 position) {
                _x = (long)(Mathf.Round(position.x * Tolerance));
                _y = (long)(Mathf.Round(position.y * Tolerance));
                _z = (long)(Mathf.Round(position.z * Tolerance));
            }

            public override int GetHashCode() {
                return (_x * 7 ^ _y * 13 ^ _z * 27).GetHashCode();
            }

            public bool Equals(LPWPosition key) {
                return _x == key._x && _y == key._y && _z == key._z;
            }
        }
    }
}