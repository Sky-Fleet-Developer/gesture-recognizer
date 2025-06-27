using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // For Mathf and other utilities

namespace Gestures.Analysis
{
    public class ContinuousGestureRecognizer
    {
        public class Template
        {
            public string Id { get; set; }
            public List<Pt> Pts { get; set; }

            public Template(string id, List<Pt> points)
            {
                Id = id;
                Pts = points;
            }
        }

        public class Pt
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Pt(int x, int y) : this(x, y, false)
            {
            }

            public Pt(int x, int y, bool waypoint)
            {
                X = x;
                Y = y;
                // waypoint parameter is unused in original code
            }

            public Pt(Vector2 source)
            {
                X = Mathf.RoundToInt(source.x);
                Y = Mathf.RoundToInt(source.y);
            }
        }

        public class Result : IComparable<Result>
        {
            public Template Template { get; }
            public double Prob { get; set; }
            public List<Pt> Pts { get; }

            internal Result(Template template, double prob, List<Pt> pts)
            {
                Template = template;
                Prob = prob;
                Pts = pts;
            }

            public int CompareTo(Result other)
            {
                if (Prob == other.Prob) return 0;
                return Prob < other.Prob ? 1 : -1;
            }
        }

        private const double DefaultESigma = 200.0;
        private const double DefaultBeta = 400.0;
        private const double DefaultLambda = 0.4;
        private const double DefaultKappa = 1.0;
        private const int MaxResamplingPts = 1000;
        private static readonly Rect NormalizedSpace = new Rect(0, 0, 1000, 1000);

        private int _samplePointDistance;
        private readonly List<Pattern> _patterns = new List<Pattern>();

        public ContinuousGestureRecognizer(List<Template> templates) : this(templates, 5)
        {
        }

        public ContinuousGestureRecognizer(List<Template> templates, int samplePointDistance)
        {
            _samplePointDistance = samplePointDistance;
            SetTemplateSet(templates);
        }

        public void SetTemplateSet(List<Template> templates)
        {
            _patterns.Clear();
            foreach (var t in templates)
            {
                Normalize(t.Pts);
                _patterns.Add(new Pattern(t, GenerateEquiDistantProgressiveSubSequences(t.Pts, 200)));
            }

            foreach (var pattern in _patterns)
            {
                var segments = new List<List<Pt>>();
                foreach (var pts in pattern.Segments)
                {
                    var newPts = DeepCopyPts(pts);
                    Normalize(newPts);
                    segments.Add(Resample(newPts, GetResamplingPointCount(newPts, _samplePointDistance)));
                }

                pattern.Segments = segments;
            }
        }

        public List<Result> Recognize(List<Pt> input) =>
            Recognize(input, DefaultBeta, DefaultLambda, DefaultKappa, DefaultESigma);

        public List<Result> Recognize(List<Pt> input, double beta, double lambda, double kappa, double eSigma)
        {
            if (input.Count < 2)
                throw new ArgumentException("input must consist of at least two points");

            var incResults = GetIncrementalResults(input, beta, lambda, kappa, eSigma);
            var results = GetResults(incResults);
            results.Sort();
            return results;
        }

        public static List<Pt> Normalize(List<Pt> pts, int x, int y, int width, int height)
        {
            var outPts = DeepCopyPts(pts);
            ScaleTo(outPts, new Rect(0, 0, width - x, height - y));
            var c = GetCentroid(outPts);
            Translate(outPts, -c.X, -c.Y);
            Translate(outPts, width - x, height - y);
            return outPts;
        }

        private List<Result> GetResults(List<IncrementalResult> incrementalResults)
        {
            var results = new List<Result>(incrementalResults.Count);
            foreach (var ir in incrementalResults)
            {
                var r = new Result(ir.Pattern.Template, ir.Prob, ir.Pattern.Segments[ir.IndexOfMostLikelySegment]);
                results.Add(r);
            }

            return results;
        }

        private List<IncrementalResult> GetIncrementalResults(List<Pt> input, double beta, double lambda, double kappa,
            double eSigma)
        {
            var results = new List<IncrementalResult>();
            var unkPts = DeepCopyPts(input);
            Normalize(unkPts);
            foreach (var pattern in _patterns)
            {
                var result = GetIncrementalResult(unkPts, pattern, beta, lambda, eSigma);
                var lastSegmentPts = pattern.Segments.Last();
                var completeProb = GetLikelihoodOfMatch(Resample(unkPts, lastSegmentPts.Count), lastSegmentPts, eSigma,
                    eSigma / beta, lambda);
                var x = 1 - completeProb;
                result.Prob *= (1 + kappa * Math.Exp(-x * x));
                results.Add(result);
            }

            MarginalizeIncrementalResults(results);
            return results;
        }

        private static void MarginalizeIncrementalResults(List<IncrementalResult> results)
        {
            var totalMass = results.Sum(r => r.Prob);
            foreach (var r in results)
            {
                r.Prob /= totalMass;
            }
        }

        private static IncrementalResult GetIncrementalResult(List<Pt> unkPts, Pattern pattern, double beta,
            double lambda, double eSigma)
        {
            var segments = pattern.Segments;
            double maxProb = 0.0d;
            int maxIndex = -1;
            for (int i = 0; i < segments.Count; i++)
            {
                var pts = segments[i];
                int samplingPtCount = pts.Count;
                var unkResampledPts = Resample(unkPts, samplingPtCount);
                var prob = GetLikelihoodOfMatch(unkResampledPts, pts, eSigma, eSigma / beta, lambda);
                if (prob > maxProb)
                {
                    maxProb = prob;
                    maxIndex = i;
                }
            }

            return new IncrementalResult(pattern, maxProb, maxIndex);
        }

        private static List<Pt> DeepCopyPts(List<Pt> pts) =>
            pts.Select(pt => new Pt(pt.X, pt.Y)).ToList();

        private static void Normalize(List<Pt> pts)
        {
            ScaleTo(pts, NormalizedSpace);
            var c = GetCentroid(pts);
            Translate(pts, -c.X, -c.Y);
        }

        private static void ScaleTo(List<Pt> pts, Rect targetBounds)
        {
            var bounds = GetBoundingBox(pts);
            double a1 = targetBounds.width;
            double a2 = targetBounds.height;
            double b1 = bounds.width;
            double b2 = bounds.height;
            double scale = Math.Sqrt(a1 * a1 + a2 * a2) / Math.Sqrt(b1 * b1 + b2 * b2);
            Scale(pts, scale, scale, bounds.x, bounds.y);
        }

        private static void Scale(List<Pt> pts, double sx, double sy, double originX, double originY)
        {
            Translate(pts, -originX, -originY);
            Scale(pts, sx, sy);
            Translate(pts, originX, originY);
        }

        private static void Scale(List<Pt> pts, double sx, double sy)
        {
            foreach (var pt in pts)
            {
                pt.X = (int)(pt.X * sx);
                pt.Y = (int)(pt.Y * sy);
            }
        }

        private static void Translate(List<Pt> pts, double dx, double dy)
        {
            int dxFloor = Mathf.FloorToInt((float)dx);
            int dyFloor = Mathf.FloorToInt((float)dy);
            foreach (var pt in pts)
            {
                pt.X += dxFloor;
                pt.Y += dyFloor;
            }
        }

        private static Rect GetBoundingBox(List<Pt> pts)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            foreach (var pt in pts)
            {
                int x = pt.X;
                int y = pt.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static Centroid GetCentroid(List<Pt> pts)
        {
            double totalMass = pts.Count;
            double xIntegral = 0.0;
            double yIntegral = 0.0;
            foreach (var pt in pts)
            {
                xIntegral += pt.X;
                yIntegral += pt.Y;
            }

            return new Centroid(xIntegral / totalMass, yIntegral / totalMass);
        }

        private static List<List<Pt>> GenerateEquiDistantProgressiveSubSequences(List<Pt> pts, int ptSpacing)
        {
            var sequences = new List<List<Pt>>();
            int nSamplePoints = GetResamplingPointCount(pts, ptSpacing);
            var resampledPts = Resample(pts, nSamplePoints);
            for (int i = 1; i < resampledPts.Count; i++)
            {
                var seq = DeepCopyPts(resampledPts.GetRange(0, i + 1));
                sequences.Add(seq);
            }

            return sequences;
        }

        private static int GetResamplingPointCount(List<Pt> pts, int samplePointDistance)
        {
            double len = GetSpatialLength(pts);
            return (int)(len / samplePointDistance) + 1;
        }

        private static double GetSpatialLength(List<Pt> pts)
        {
            double len = 0.0d;
            for (int i = 1; i < pts.Count; i++)
            {
                len += Distance(pts[i - 1], pts[i]);
            }

            return len;
        }

        private static double Distance(Pt p1, Pt p2) =>
            Distance(p1.X, p1.Y, p2.X, p2.Y);

        private static int Distance(int x1, int y1, int x2, int y2)
        {
            x2 -= x1;
            if (x2 < 0) x2 = -x2;
            y2 -= y1;
            if (y2 < 0) y2 = -y2;
            return x2 + y2 - (((x2 > y2) ? y2 : x2) >> 1);
        }

        private static double GetLikelihoodOfMatch(List<Pt> pts1, List<Pt> pts2, double eSigma, double aSigma,
            double lambda)
        {
            if (eSigma <= 0)
                throw new ArgumentException("eSigma must be positive");
            if (aSigma <= 0)
                throw new ArgumentException("aSigma must be positive");
            if (lambda < 0 || lambda > 1)
                throw new ArgumentException("lambda must be in the range between zero and one");

            double x_e = GetEuclidianDistance(pts1, pts2);
            double x_a = GetTurningAngleDistance(pts1, pts2);
            return Math.Exp(-(x_e * x_e / (eSigma * eSigma) * lambda + x_a * x_a / (aSigma * aSigma) * (1 - lambda)));
        }

        private static double GetEuclidianDistance(List<Pt> pts1, List<Pt> pts2)
        {
            if (pts1.Count != pts2.Count)
                throw new ArgumentException($"lists must be of equal lengths, cf. {pts1.Count} with {pts2.Count}");

            int n = pts1.Count;
            double td = 0;
            for (int i = 0; i < n; i++)
            {
                td += GetEuclideanDistance(pts1[i], pts2[i]);
            }

            return td / n;
        }

        private static double GetTurningAngleDistance(List<Pt> pts1, List<Pt> pts2)
        {
            if (pts1.Count != pts2.Count)
                throw new ArgumentException($"lists must be of equal lengths, cf. {pts1.Count} with {pts2.Count}");

            int n = pts1.Count;
            double td = 0;
            for (int i = 0; i < n - 1; i++)
            {
                td += Math.Abs(GetTurningAngleDistance(pts1[i], pts1[i + 1], pts2[i], pts2[i + 1]));
            }

            if (double.IsNaN(td))
                return 0.0;
            return td / (n - 1);
        }

        private static double GetEuclideanDistance(Pt pt1, Pt pt2) =>
            Math.Sqrt(GetSquaredEuclidenDistance(pt1, pt2));

        private static double GetSquaredEuclidenDistance(Pt pt1, Pt pt2) =>
            (pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y);

        private static double GetTurningAngleDistance(Pt ptA1, Pt ptA2, Pt ptB1, Pt ptB2)
        {
            double len_a = GetEuclideanDistance(ptA1, ptA2);
            double len_b = GetEuclideanDistance(ptB1, ptB2);
            if (len_a == 0 || len_b == 0)
                return 0.0;

            float cos = (float)(((ptA1.X - ptA2.X) * (ptB1.X - ptB2.X) + (ptA1.Y - ptA2.Y) * (ptB1.Y - ptB2.Y)) /
                                (len_a * len_b));
            if (Math.Abs(cos) > 1.0)
                return 0.0;
            else
                return Math.Acos(cos);
        }

        private static List<Pt> Resample(List<Pt> points, int numTargetPoints)
        {
            var r = new List<Pt>();
            int[] inArray = ToArray(points);
            int[] outArray = new int[numTargetPoints * 2];

            Resample(inArray, outArray, points.Count, numTargetPoints);
            for (int i = 0; i < outArray.Length; i += 2)
            {
                r.Add(new Pt(outArray[i], outArray[i + 1], false));
            }

            return r;
        }

        private static int[] ToArray(List<Pt> points)
        {
            int[] outArr = new int[points.Count * 2];
            for (int i = 0; i < points.Count; i++)
            {
                outArr[i * 2] = points[i].X;
                outArr[i * 2 + 1] = points[i].Y;
            }

            return outArr;
        }

        private static void Resample(int[] template, int[] buffer, int n, int numTargetPoints)
        {
            int[] segmentBuf = new int[MaxResamplingPts];

            double l, segmentLen, horizRest, verticRest, dx, dy;
            int x1, y1, x2, y2;
            int i, m, a, segmentPoints, j, maxOutputs, end;

            m = n * 2;
            l = GetSpatialLength(template, n);
            segmentLen = l / (numTargetPoints - 1);
            GetSegmentPoints(template, n, segmentLen, segmentBuf);
            horizRest = 0.0f;
            verticRest = 0.0f;
            x1 = template[0];
            y1 = template[1];
            a = 0;
            maxOutputs = numTargetPoints * 2;
            for (i = 2; i < m; i += 2)
            {
                x2 = template[i];
                y2 = template[i + 1];
                segmentPoints = segmentBuf[(i / 2) - 1];
                dx = -1.0f;
                dy = -1.0f;
                if (segmentPoints - 1 <= 0)
                {
                    dx = 0.0f;
                    dy = 0.0f;
                }
                else
                {
                    dx = (x2 - x1) / (double)(segmentPoints);
                    dy = (y2 - y1) / (double)(segmentPoints);
                }

                if (segmentPoints > 0)
                {
                    for (j = 0; j < segmentPoints; j++)
                    {
                        if (j == 0)
                        {
                            if (a < maxOutputs)
                            {
                                buffer[a] = (int)(x1 + horizRest);
                                buffer[a + 1] = (int)(y1 + verticRest);
                                horizRest = 0.0;
                                verticRest = 0.0;
                                a += 2;
                            }
                        }
                        else
                        {
                            if (a < maxOutputs)
                            {
                                buffer[a] = (int)(x1 + j * dx);
                                buffer[a + 1] = (int)(y1 + j * dy);
                                a += 2;
                            }
                        }
                    }
                }

                x1 = x2;
                y1 = y2;
            }

            end = (numTargetPoints * 2) - 2;
            if (a < end)
            {
                for (i = a; i < end; i += 2)
                {
                    buffer[i] = (buffer[i - 2] + template[m - 2]) / 2;
                    buffer[i + 1] = (buffer[i - 1] + template[m - 1]) / 2;
                }
            }

            buffer[maxOutputs - 2] = template[m - 2];
            buffer[maxOutputs - 1] = template[m - 1];
        }

        private static double GetSegmentPoints(int[] pts, int n, double length, int[] buffer)
        {
            int i, m;
            int x1, y1, x2, y2, ps;
            double rest, currentLen;

            m = n * 2;
            rest = 0.0f;
            x1 = pts[0];
            y1 = pts[1];
            for (i = 2; i < m; i += 2)
            {
                x2 = pts[i];
                y2 = pts[i + 1];
                currentLen = Distance(x1, y1, x2, y2);
                currentLen += rest;
                rest = 0.0f;
                ps = (int)((currentLen / length));
                if (ps == 0)
                {
                    rest += currentLen;
                }
                else
                {
                    rest += currentLen - (ps * length);
                }

                if (i == 2 && ps == 0)
                {
                    ps = 1;
                }

                buffer[(i / 2) - 1] = ps;
                x1 = x2;
                y1 = y2;
            }

            return rest;
        }

        private static int GetSpatialLength(int[] pat, int n)
        {
            int l;
            int i, m;
            int x1, y1, x2, y2;

            l = 0;
            m = 2 * n;
            if (m > 2)
            {
                x1 = pat[0];
                y1 = pat[1];
                for (i = 2; i < m; i += 2)
                {
                    x2 = pat[i];
                    y2 = pat[i + 1];
                    l += Distance(x1, y1, x2, y2);
                    x1 = x2;
                    y1 = y2;
                }

                return l;
            }
            else
            {
                return 0;
            }
        }

        private class Pattern
        {
            public Template Template { get; }
            public List<List<Pt>> Segments { get; set; }

            public Pattern(Template template, List<List<Pt>> segments)
            {
                Template = template;
                Segments = segments;
            }
        }

        private class Centroid
        {
            public double X { get; }
            public double Y { get; }

            public Centroid(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        private class IncrementalResult
        {
            public Pattern Pattern { get; }
            public double Prob { get; set; }
            public int IndexOfMostLikelySegment { get; }

            public IncrementalResult(Pattern pattern, double prob, int indexOfMostLikelySegment)
            {
                Pattern = pattern;
                Prob = prob;
                IndexOfMostLikelySegment = indexOfMostLikelySegment;
            }
        }
    }
}