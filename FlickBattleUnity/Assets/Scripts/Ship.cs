using System;
using System.Collections;
using UnityEngine;

public enum Team { Blue, Red }

// One game piece. Put this on the root of a ship prefab.
// The ship model should be a child object with its nose pointing along +X.
public class Ship : MonoBehaviour
{
    public Team team;
    [Tooltip("Body size in world units, used for hits")]
    public float radius = 0.45f;
    public GameObject selectionRing;
    public GameObject explosionPrefab;
    public TrailRenderer trail;

    public bool IsAlive { get; private set; } = true;
    public bool HasMoved { get; set; }
    public Vector2 Position => transform.position;

    public void ResetTo(Vector2 position, Vector2 facing)
    {
        gameObject.SetActive(true);
        IsAlive = true;
        HasMoved = false;
        transform.position = position;
        Face(facing);
        SetSelected(false);
        if (trail) trail.Clear();
    }

    public void SetSelected(bool on)
    {
        if (selectionRing) selectionRing.SetActive(on);
    }

    public void Face(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    // Flies along the curve. onProgress gets 0..1 every frame so the
    // GameManager can blow up targets the moment the ship reaches them.
    public IEnumerator FlyAlong(Vector2 s, Vector2 c, Vector2 e, float duration, Action<float> onProgress)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float eased = 1f - (1f - t) * (1f - t); // fast launch, glide in
            transform.position = FlickMath.Bezier(s, c, e, eased);
            Face(FlickMath.BezierTangent(s, c, e, eased));
            onProgress?.Invoke(eased);
            yield return null;
        }
        transform.position = e;
        onProgress?.Invoke(1f);
    }

    public void Explode()
    {
        IsAlive = false;
        SetSelected(false);
        if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }
}
