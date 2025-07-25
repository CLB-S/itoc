﻿// Adapted from https://github.com/nol1fe/delaunator-sharp v1.0.11

using Godot;

namespace ITOC.Core.DelaunatorSharp;

public struct Edge
{
    public Vector2 P { get; set; }
    public Vector2 Q { get; set; }
    public int Index { get; set; }

    public Edge(int e, Vector2 p, Vector2 q)
    {
        Index = e;
        P = p;
        Q = q;
    }
}

public struct Triangle
{
    public int Index { get; set; }

    public IEnumerable<Vector2> Points { get; set; }

    public Triangle(int t, IEnumerable<Vector2> points)
    {
        Points = points;
        Index = t;
    }
}

public struct VoronoiCell
{
    public Vector2[] Points { get; set; }
    public int Index { get; set; }

    public VoronoiCell(int triangleIndex, Vector2[] points)
    {
        Points = points;
        Index = triangleIndex;
    }
}

public class Delaunator
{
    private readonly double[] coords;
    private readonly int[] EDGE_STACK = new int[512];
    private readonly double EPSILON = Mathf.Pow(2, -52.0);

    private readonly int hashSize;
    private readonly int[] hullHash;
    private readonly int[] hullNext;
    private readonly int[] hullPrev;
    private readonly int hullSize;
    private readonly int hullStart;
    private readonly int[] hullTri;

    private readonly double cx;
    private readonly double cy;

    private int trianglesLen;

    public Delaunator(Vector2[] points)
    {
        if (points.Length < 3)
            throw new ArgumentOutOfRangeException("Need at least 3 points");

        Points = points;
        coords = new double[Points.Length * 2];

        for (var i = 0; i < Points.Length; i++)
        {
            var p = Points[i];
            coords[2 * i] = p.X;
            coords[2 * i + 1] = p.Y;
        }

        var n = points.Length;
        var maxTriangles = 2 * n - 5;

        Triangles = new int[maxTriangles * 3];

        Halfedges = new int[maxTriangles * 3];
        hashSize = Mathf.CeilToInt(Mathf.Sqrt(n));

        hullPrev = new int[n];
        hullNext = new int[n];
        hullTri = new int[n];
        hullHash = new int[hashSize];

        var ids = new int[n];

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        for (var i = 0; i < n; i++)
        {
            var x = coords[2 * i];
            var y = coords[2 * i + 1];
            if (x < minX)
                minX = x;
            if (y < minY)
                minY = y;
            if (x > maxX)
                maxX = x;
            if (y > maxY)
                maxY = y;
            ids[i] = i;
        }

        var cx = (minX + maxX) / 2;
        var cy = (minY + maxY) / 2;

        var minDist = double.PositiveInfinity;
        var i0 = 0;
        var i1 = 0;
        var i2 = 0;

        // pick a seed point close to the center
        for (var i = 0; i < n; i++)
        {
            var d = Dist(cx, cy, coords[2 * i], coords[2 * i + 1]);
            if (d < minDist)
            {
                i0 = i;
                minDist = d;
            }
        }

        var i0x = coords[2 * i0];
        var i0y = coords[2 * i0 + 1];

        minDist = double.PositiveInfinity;

        // find the point closest to the seed
        for (var i = 0; i < n; i++)
        {
            if (i == i0)
                continue;
            var d = Dist(i0x, i0y, coords[2 * i], coords[2 * i + 1]);
            if (d < minDist && d > 0)
            {
                i1 = i;
                minDist = d;
            }
        }

        var i1x = coords[2 * i1];
        var i1y = coords[2 * i1 + 1];

        var minRadius = double.PositiveInfinity;

        // find the third point which forms the smallest circumcircle with the first two
        for (var i = 0; i < n; i++)
        {
            if (i == i0 || i == i1)
                continue;
            var r = Circumradius(i0x, i0y, i1x, i1y, coords[2 * i], coords[2 * i + 1]);
            if (r < minRadius)
            {
                i2 = i;
                minRadius = r;
            }
        }

        var i2x = coords[2 * i2];
        var i2y = coords[2 * i2 + 1];

        if (minRadius == double.PositiveInfinity)
            throw new Exception("No Delaunay triangulation exists for this input.");

        if (Orient(i0x, i0y, i1x, i1y, i2x, i2y))
        {
            var i = i1;
            var x = i1x;
            var y = i1y;
            i1 = i2;
            i1x = i2x;
            i1y = i2y;
            i2 = i;
            i2x = x;
            i2y = y;
        }

        var center = Circumcenter(i0x, i0y, i1x, i1y, i2x, i2y);
        this.cx = center.X;
        this.cy = center.Y;

        var dists = new double[n];
        for (var i = 0; i < n; i++)
            dists[i] = Dist(coords[2 * i], coords[2 * i + 1], center.X, center.Y);

        // sort the points by distance from the seed triangle circumcenter
        Quicksort(ids, dists, 0, n - 1);

        // set up the seed triangle as the starting hull
        hullStart = i0;
        hullSize = 3;

        hullNext[i0] = hullPrev[i2] = i1;
        hullNext[i1] = hullPrev[i0] = i2;
        hullNext[i2] = hullPrev[i1] = i0;

        hullTri[i0] = 0;
        hullTri[i1] = 1;
        hullTri[i2] = 2;

        hullHash[HashKey(i0x, i0y)] = i0;
        hullHash[HashKey(i1x, i1y)] = i1;
        hullHash[HashKey(i2x, i2y)] = i2;

        trianglesLen = 0;
        AddTriangle(i0, i1, i2, -1, -1, -1);

        double xp = 0;
        double yp = 0;

        for (var k = 0; k < ids.Length; k++)
        {
            var i = ids[k];
            var x = coords[2 * i];
            var y = coords[2 * i + 1];

            // skip near-duplicate points
            if (k > 0 && Mathf.Abs(x - xp) <= EPSILON && Mathf.Abs(y - yp) <= EPSILON)
                continue;
            xp = x;
            yp = y;

            // skip seed triangle points
            if (i == i0 || i == i1 || i == i2)
                continue;

            // find a visible edge on the convex hull using edge hash
            var start = 0;
            for (var j = 0; j < hashSize; j++)
            {
                var key = HashKey(x, y);
                start = hullHash[(key + j) % hashSize];
                if (start != -1 && start != hullNext[start])
                    break;
            }

            start = hullPrev[start];
            var e = start;
            var q = hullNext[e];

            while (
                !Orient(x, y, coords[2 * e], coords[2 * e + 1], coords[2 * q], coords[2 * q + 1])
            )
            {
                e = q;
                if (e == start)
                {
                    e = int.MaxValue;
                    break;
                }

                q = hullNext[e];
            }

            if (e == int.MaxValue)
                continue; // likely a near-duplicate point; skip it

            // add the first triangle from the point
            var t = AddTriangle(e, i, hullNext[e], -1, -1, hullTri[e]);

            // recursively flip triangles from the point until they satisfy the Delaunay condition
            hullTri[i] = Legalize(t + 2);
            hullTri[e] = t; // keep track of boundary triangles on the hull
            hullSize++;

            // walk forward through the hull, adding more triangles and flipping recursively
            var next = hullNext[e];
            q = hullNext[next];

            while (
                Orient(
                    x,
                    y,
                    coords[2 * next],
                    coords[2 * next + 1],
                    coords[2 * q],
                    coords[2 * q + 1]
                )
            )
            {
                t = AddTriangle(next, i, q, hullTri[i], -1, hullTri[next]);
                hullTri[i] = Legalize(t + 2);
                hullNext[next] = next; // mark as removed
                hullSize--;
                next = q;

                q = hullNext[next];
            }

            // walk backward from the other side, adding more triangles and flipping
            if (e == start)
            {
                q = hullPrev[e];

                while (
                    Orient(x, y, coords[2 * q], coords[2 * q + 1], coords[2 * e], coords[2 * e + 1])
                )
                {
                    t = AddTriangle(q, i, e, -1, hullTri[e], hullTri[q]);
                    Legalize(t + 2);
                    hullTri[q] = t;
                    hullNext[e] = e; // mark as removed
                    hullSize--;
                    e = q;

                    q = hullPrev[e];
                }
            }

            // update the hull indices
            hullStart = hullPrev[i] = e;
            hullNext[e] = hullPrev[next] = i;
            hullNext[i] = next;

            // save the two new edges in the hash table
            hullHash[HashKey(x, y)] = i;
            hullHash[HashKey(coords[2 * e], coords[2 * e + 1])] = e;
        }

        Hull = new int[hullSize];
        var s = hullStart;
        for (var i = 0; i < hullSize; i++)
        {
            Hull[i] = s;
            s = hullNext[s];
        }

        hullPrev = hullNext = hullTri = null; // get rid of temporary arrays

        //// trim typed triangle mesh arrays
        Triangles = Triangles.Take(trianglesLen).ToArray();
        Halfedges = Halfedges.Take(trianglesLen).ToArray();
    }

    /// <summary>
    ///     One value per half-edge, containing the point index of where a given half edge starts.
    /// </summary>
    public int[] Triangles { get; }

    /// <summary>
    ///     One value per half-edge, containing the opposite half-edge in the adjacent triangle, or -1 if there is no adjacent
    ///     triangle
    /// </summary>
    public int[] Halfedges { get; }

    /// <summary>
    ///     The initial points Delaunator was constructed with.
    /// </summary>
    public Vector2[] Points { get; }

    /// <summary>
    ///     A list of point indices that traverses the hull of the points.
    /// </summary>
    public int[] Hull { get; }

    #region CreationLogic

    private int Legalize(int a)
    {
        var i = 0;
        int ar;

        // recursion eliminated with a fixed-size stack
        while (true)
        {
            var b = Halfedges[a];

            /* if the pair of triangles doesn't satisfy the Delaunay condition
             * (p1 is inside the circumcircle of [p0, pl, pr]), flip them,
             * then do the same check/flip recursively for the new pair of triangles
             *
             *           pl                    pl
             *          /||\                  /  \
             *       al/ || \bl            al/    \a
             *        /  ||  \              /      \
             *       /  a||b  \    flip    /___ar___\
             *     p0\   ||   /p1   =>   p0\---bl---/p1
             *        \  ||  /              \      /
             *       ar\ || /br             b\    /br
             *          \||/                  \  /
             *           pr                    pr
             */
            var a0 = a - a % 3;
            ar = a0 + (a + 2) % 3;

            if (b == -1)
            {
                // convex hull edge
                if (i == 0)
                    break;
                a = EDGE_STACK[--i];
                continue;
            }

            var b0 = b - b % 3;
            var al = a0 + (a + 1) % 3;
            var bl = b0 + (b + 2) % 3;

            var p0 = Triangles[ar];
            var pr = Triangles[a];
            var pl = Triangles[al];
            var p1 = Triangles[bl];

            var illegal = InCircle(
                coords[2 * p0],
                coords[2 * p0 + 1],
                coords[2 * pr],
                coords[2 * pr + 1],
                coords[2 * pl],
                coords[2 * pl + 1],
                coords[2 * p1],
                coords[2 * p1 + 1]
            );

            if (illegal)
            {
                Triangles[a] = p1;
                Triangles[b] = p0;

                var hbl = Halfedges[bl];

                // edge swapped on the other side of the hull (rare); fix the halfedge reference
                if (hbl == -1)
                {
                    var e = hullStart;
                    do
                    {
                        if (hullTri[e] == bl)
                        {
                            hullTri[e] = a;
                            break;
                        }

                        e = hullPrev[e];
                    } while (e != hullStart);
                }

                Link(a, hbl);
                Link(b, Halfedges[ar]);
                Link(ar, bl);

                var br = b0 + (b + 1) % 3;

                // don't worry about hitting the cap: it can only happen on extremely degenerate input
                if (i < EDGE_STACK.Length)
                    EDGE_STACK[i++] = br;
            }
            else
            {
                if (i == 0)
                    break;
                a = EDGE_STACK[--i];
            }
        }

        return ar;
    }

    private static bool InCircle(
        double ax,
        double ay,
        double bx,
        double by,
        double cx,
        double cy,
        double px,
        double py
    )
    {
        var dx = ax - px;
        var dy = ay - py;
        var ex = bx - px;
        var ey = by - py;
        var fx = cx - px;
        var fy = cy - py;

        var ap = dx * dx + dy * dy;
        var bp = ex * ex + ey * ey;
        var cp = fx * fx + fy * fy;

        return dx * (ey * cp - bp * fy) - dy * (ex * cp - bp * fx) + ap * (ex * fy - ey * fx) < 0;
    }

    private int AddTriangle(int i0, int i1, int i2, int a, int b, int c)
    {
        var t = trianglesLen;

        Triangles[t] = i0;
        Triangles[t + 1] = i1;
        Triangles[t + 2] = i2;

        Link(t, a);
        Link(t + 1, b);
        Link(t + 2, c);

        trianglesLen += 3;
        return t;
    }

    private void Link(int a, int b)
    {
        Halfedges[a] = b;
        if (b != -1)
            Halfedges[b] = a;
    }

    private int HashKey(double x, double y) =>
        Mathf.FloorToInt(PseudoAngle(x - cx, y - cy) * hashSize) % hashSize;

    private static double PseudoAngle(double dx, double dy)
    {
        var p = dx / (Mathf.Abs(dx) + Mathf.Abs(dy));
        return (dy > 0 ? 3 - p : 1 + p) / 4; // [0..1]
    }

    private static void Quicksort(int[] ids, double[] dists, int left, int right)
    {
        if (right - left <= 20)
        {
            for (var i = left + 1; i <= right; i++)
            {
                var temp = ids[i];
                var tempDist = dists[temp];
                var j = i - 1;
                while (j >= left && dists[ids[j]] > tempDist)
                    ids[j + 1] = ids[j--];
                ids[j + 1] = temp;
            }
        }
        else
        {
            var median = (left + right) >> 1;
            var i = left + 1;
            var j = right;
            Swap(ids, median, i);
            if (dists[ids[left]] > dists[ids[right]])
                Swap(ids, left, right);
            if (dists[ids[i]] > dists[ids[right]])
                Swap(ids, i, right);
            if (dists[ids[left]] > dists[ids[i]])
                Swap(ids, left, i);

            var temp = ids[i];
            var tempDist = dists[temp];
            while (true)
            {
                do
                {
                    i++;
                } while (dists[ids[i]] < tempDist);

                do
                {
                    j--;
                } while (dists[ids[j]] > tempDist);

                if (j < i)
                    break;
                Swap(ids, i, j);
            }

            ids[left + 1] = ids[j];
            ids[j] = temp;

            if (right - i + 1 >= j - left)
            {
                Quicksort(ids, dists, i, right);
                Quicksort(ids, dists, left, j - 1);
            }
            else
            {
                Quicksort(ids, dists, left, j - 1);
                Quicksort(ids, dists, i, right);
            }
        }
    }

    private static void Swap(int[] arr, int i, int j)
    {
        var tmp = arr[i];
        arr[i] = arr[j];
        arr[j] = tmp;
    }

    private static bool Orient(double px, double py, double qx, double qy, double rx, double ry) =>
        (qy - py) * (rx - qx) - (qx - px) * (ry - qy) < 0;

    private static double Circumradius(
        double ax,
        double ay,
        double bx,
        double by,
        double cx,
        double cy
    )
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;
        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5 / (dx * ey - dy * ex);
        var x = (ey * bl - dy * cl) * d;
        var y = (dx * cl - ex * bl) * d;
        return x * x + y * y;
    }

    private static Vector2 Circumcenter(
        double ax,
        double ay,
        double bx,
        double by,
        double cx,
        double cy
    )
    {
        var dx = bx - ax;
        var dy = by - ay;
        var ex = cx - ax;
        var ey = cy - ay;
        var bl = dx * dx + dy * dy;
        var cl = ex * ex + ey * ey;
        var d = 0.5f / (dx * ey - dy * ex);
        var x = ax + (ey * bl - dy * cl) * d;
        var y = ay + (dx * cl - ex * bl) * d;

        return new Vector2(x, y);
    }

    private static double Dist(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy;
    }

    #endregion CreationLogic

    #region GetMethods

    public IEnumerable<Triangle> GetTriangles()
    {
        for (var t = 0; t < Triangles.Length / 3; t++)
            yield return new Triangle(t, GetTrianglePoints(t));
    }

    public IEnumerable<Edge> GetEdges()
    {
        for (var e = 0; e < Triangles.Length; e++)
            if (e > Halfedges[e])
            {
                var p = Points[Triangles[e]];
                var q = Points[Triangles[NextHalfedge(e)]];
                yield return new Edge(e, p, q);
            }
    }

    public IEnumerable<Edge> GetVoronoiEdges(Func<int, Vector2> triangleVerticeSelector = null)
    {
        if (triangleVerticeSelector == null)
            triangleVerticeSelector = x => GetCentroid(x);
        for (var e = 0; e < Triangles.Length; e++)
            if (e < Halfedges[e])
            {
                var p = triangleVerticeSelector(TriangleOfEdge(e));
                var q = triangleVerticeSelector(TriangleOfEdge(Halfedges[e]));
                yield return new Edge(e, p, q);
            }
    }

    public IEnumerable<Edge> GetVoronoiEdgesBasedOnCircumCenter() =>
        GetVoronoiEdges(GetTriangleCircumcenter);

    public IEnumerable<Edge> GetVoronoiEdgesBasedOnCentroids() => GetVoronoiEdges(GetCentroid);

    public IEnumerable<VoronoiCell> GetVoronoiCells(
        Func<int, Vector2> triangleVerticeSelector = null
    )
    {
        if (triangleVerticeSelector == null)
            triangleVerticeSelector = x => GetCentroid(x);

        var seen = new HashSet<int>();
        var vertices = new List<Vector2>(10); // Keep it outside the loop, reuse capacity, less resizes.

        for (var e = 0; e < Triangles.Length; e++)
        {
            var pointIndex = Triangles[NextHalfedge(e)];
            // True if element was added, If resize the set? O(n) : O(1)
            if (seen.Add(pointIndex))
            {
                foreach (var edge in EdgesAroundPoint(e))
                    // triangleVerticeSelector cant be null, no need to check before invoke (?.).
                    vertices.Add(triangleVerticeSelector.Invoke(TriangleOfEdge(edge)));
                yield return new VoronoiCell(pointIndex, vertices.ToArray());
                vertices.Clear(); // Clear elements, keep capacity
            }
        }
    }

    public IEnumerable<VoronoiCell> GetVoronoiCellsBasedOnCircumcenters() =>
        GetVoronoiCells(GetTriangleCircumcenter);

    public IEnumerable<VoronoiCell> GetVoronoiCellsBasedOnCentroids() =>
        GetVoronoiCells(GetCentroid);

    public IEnumerable<Edge> GetHullEdges() => CreateHull(GetHullPoints());

    public Vector2[] GetHullPoints() => Array.ConvertAll(Hull, x => Points[x]);

    public Vector2[] GetTrianglePoints(int t)
    {
        var points = new List<Vector2>();
        foreach (var p in PointsOfTriangle(t))
            points.Add(Points[p]);
        return points.ToArray();
    }

    public Vector2[] GetRellaxedPoints()
    {
        var points = new List<Vector2>();
        foreach (var cell in GetVoronoiCellsBasedOnCircumcenters())
            points.Add(GetCentroid(cell.Points));
        return points.ToArray();
    }

    public IEnumerable<Edge> GetEdgesOfTriangle(int t) =>
        CreateHull(EdgesOfTriangle(t).Select(e => Points[Triangles[e]]));

    public static IEnumerable<Edge> CreateHull(IEnumerable<Vector2> points) =>
        points
            .Zip(points.Skip(1).Append(points.FirstOrDefault()), (a, b) => new Edge(0, a, b))
            .OfType<Edge>();

    public Vector2 GetTriangleCircumcenter(int t)
    {
        var vertices = GetTrianglePoints(t);
        return GetCircumcenter(vertices[0], vertices[1], vertices[2]);
    }

    public Vector2 GetCentroid(int t)
    {
        var vertices = GetTrianglePoints(t);
        return GetCentroid(vertices);
    }

    public static Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c) =>
        Circumcenter(a.X, a.Y, b.X, b.Y, c.X, c.Y);

    public static Vector2 GetCentroid(Vector2[] points)
    {
        var accumulatedArea = 0.0;
        var centerX = 0.0;
        var centerY = 0.0;

        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            var temp = points[i].X * points[j].Y - points[j].X * points[i].Y;
            accumulatedArea += temp;
            centerX += (points[i].X + points[j].X) * temp;
            centerY += (points[i].Y + points[j].Y) * temp;
        }

        if (Mathf.Abs(accumulatedArea) < 1E-7f)
            return new Vector2();

        accumulatedArea *= 3;
        return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
    }

    #endregion GetMethods

    #region ForEachMethods

    public void ForEachTriangle(Action<Triangle> callback)
    {
        foreach (var triangle in GetTriangles())
            callback?.Invoke(triangle);
    }

    public void ForEachTriangleEdge(Action<Edge> callback)
    {
        foreach (var edge in GetEdges())
            callback?.Invoke(edge);
    }

    public void ForEachVoronoiEdge(Action<Edge> callback)
    {
        foreach (var edge in GetVoronoiEdges())
            callback?.Invoke(edge);
    }

    public void ForEachVoronoiCellBasedOnCentroids(Action<VoronoiCell> callback)
    {
        foreach (var cell in GetVoronoiCellsBasedOnCentroids())
            callback?.Invoke(cell);
    }

    public void ForEachVoronoiCellBasedOnCircumcenters(Action<VoronoiCell> callback)
    {
        foreach (var cell in GetVoronoiCellsBasedOnCircumcenters())
            callback?.Invoke(cell);
    }

    public void ForEachVoronoiCell(
        Action<VoronoiCell> callback,
        Func<int, Vector2> triangleVertexSelector = null
    )
    {
        foreach (var cell in GetVoronoiCells(triangleVertexSelector))
            callback?.Invoke(cell);
    }

    #endregion ForEachMethods

    #region Methods based on index

    /// <summary>
    ///     Returns the half-edges that share a start point with the given half edge, in order.
    /// </summary>
    public IEnumerable<int> EdgesAroundPoint(int start)
    {
        var incoming = start;
        do
        {
            yield return incoming;
            var outgoing = NextHalfedge(incoming);
            incoming = Halfedges[outgoing];
        } while (incoming != -1 && incoming != start);
    }

    /// <summary>
    ///     Returns the three point indices of a given triangle id.
    /// </summary>
    public IEnumerable<int> PointsOfTriangle(int t)
    {
        foreach (var edge in EdgesOfTriangle(t))
            yield return Triangles[edge];
    }

    /// <summary>
    ///     Returns the triangle ids adjacent to the given triangle id.
    ///     Will return up to three values.
    /// </summary>
    public IEnumerable<int> TrianglesAdjacentToTriangle(int t)
    {
        var adjacentTriangles = new List<int>();
        var triangleEdges = EdgesOfTriangle(t);
        foreach (var e in triangleEdges)
        {
            var opposite = Halfedges[e];
            if (opposite >= 0)
                adjacentTriangles.Add(TriangleOfEdge(opposite));
        }

        return adjacentTriangles;
    }

    public static int NextHalfedge(int e) => e % 3 == 2 ? e - 2 : e + 1;

    public static int PreviousHalfedge(int e) => e % 3 == 0 ? e + 2 : e - 1;

    /// <summary>
    ///     Returns the three half-edges of a given triangle id.
    /// </summary>
    public static int[] EdgesOfTriangle(int t) => new[] { 3 * t, 3 * t + 1, 3 * t + 2 };

    /// <summary>
    ///     Returns the triangle id of a given half-edge.
    /// </summary>
    public static int TriangleOfEdge(int e) => e / 3;

    #endregion Methods based on index
}
