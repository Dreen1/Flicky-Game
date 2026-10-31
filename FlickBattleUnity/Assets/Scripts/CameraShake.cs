using UnityEngine;

// Short screen shake on hits. Put this on the Main Camera.
public class CameraShake : MonoBehaviour
{
    public float duration = 0.25f;
    public float magnitude = 0.15f;

    Vector3 origin;
    float timeLeft;

    void Awake()
    {
        origin = transform.localPosition;
    }

    public void Shake()
    {
        timeLeft = duration;
    }

    void LateUpdate()
    {
        if (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            float strength = magnitude * Mathf.Max(timeLeft, 0f) / duration;
            transform.localPosition = origin + (Vector3)(Random.insideUnitCircle * strength);
        }
        else
        {
            transform.localPosition = origin;
        }
    }
}
