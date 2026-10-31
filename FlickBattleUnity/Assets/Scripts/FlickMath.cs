using System.Collections.Generic;
using UnityEngine;

// Pure math for flicks: curve shaping, bezier sampling and hit tests.
public static class FlickMath
{
    // Builds a quadratic bezier control point from the finger's path.
    // A straight swipe gives a straight shot; a swept swipe bends the shot,
    // capped so the launch angle never leaves the straight line by more than maxDegrees.
    public static Vector2 ControlPoint(Vector2 start, Vector2 end, IList<Vector2> fingerPath, float maxDegrees)
    {
        Vector2 chord = end - start;
        float len = chord.magnitude;
        Vector2 mid = (start + end) * 0.5f;
        if (len < 0.0001f) return mid;

        Vector2 perp = new Vector2(-chord.y, chord.x) / len;

        // Largest sideways bulge of the swipe
        float bulge = 0f;
        foreach (Vector2 p in fingerPath)
        {
            float t = Vector2.Dot(p - start, chord) / (len * len);
            if (t <= 0f || t >= 1f) continue;
            float dev = Vector2.Dot(p - (start + chord * t), perp);
            if (Mathf.Abs(dev) > Mathf.Abs(bulge)) bulge = dev;
        }

        // For a quadratic bezier, launch angle = atan(4 * bulge / len)
        float maxBulge = len * Mathf.Tan(maxDegrees * Mathf.Deg2Rad) / 4f;
        bulge = Mathf.Clamp(bulge, -maxBulge, maxBulge);

        return mid + perp * bulge * 2f;
    }

    public static Vector2 Bezier(Vector2 s, Vector2 c, Vector2 e, float t)
    {
        float u = 1f - t;
        return u * u * s + 2f * u * t * c + t * t * e;
    }

    public static Vector2 BezierTangent(Vector2 s, Vector2 c, Vector2 e, float t)
    {
        return 2f * (1f - t) * (c - s) + 2f * t * (e - c);
    }

    // Returns how far along the curve (0..1) it first touches the circle, or -1 for no hit.
    public static float CurveHitT(Vector2 s, Vector2 c, Vector2 e, Vector2 center, float radius, int samples = 40)
    {
        Vector2 prev = s;
        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector2 p = Bezier(s, c, e, t);
            if (SegmentHitsCircle(prev, p, center, radius)) return t;
            prev = p;
        }
        return -1f;
    }

    public static bool SegmentHitsCircle(Vector2 a, Vector2 b, Vector2 center, float radius)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        float t = len2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(center - a, ab) / len2);
        return (a + ab * t - center).sqrMagnitude <= radius * radius;
    }
}
