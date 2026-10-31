using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Reads touch (phone) or mouse (Editor) and turns swipes into flicks.
// Put this on any object in the scene, e.g. the GameManager object.
public class FlickInput : MonoBehaviour
{
    [Tooltip("How close (world units) a touch must be to grab a ship")]
    public float tapRadius = 1.2f;

    Camera cam;
    Ship selected;
    Vector2 start;
    float startTime;
    readonly List<Vector2> path = new List<Vector2>();

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.AcceptingInput)
        {
            Cancel();
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 w = ToWorld(touch.position);
            switch (touch.phase)
            {
                case TouchPhase.Began: if (!OverUI(touch.fingerId)) Down(gm, w); break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: Drag(w); break;
                case TouchPhase.Ended: Up(gm, w); break;
                case TouchPhase.Canceled: Cancel(); break;
            }
        }
        else
        {
            Vector2 w = ToWorld(Input.mousePosition);
            if (Input.GetMouseButtonDown(0)) { if (!OverUI(-1)) Down(gm, w); }
            else if (Input.GetMouseButton(0)) Drag(w);
            else if (Input.GetMouseButtonUp(0)) Up(gm, w);
        }
    }

    // Taps on buttons shouldn't grab a ship
    static bool OverUI(int pointerId)
    {
        EventSystem es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject(pointerId);
    }

    void Down(GameManager gm, Vector2 w)
    {
        selected = gm.FindShip(w, tapRadius);
        if (selected == null) return;
        selected.SetSelected(true);
        start = w;
        startTime = Time.unscaledTime;
        path.Clear();
    }

    void Drag(Vector2 w)
    {
        if (selected != null) path.Add(w);
    }

    void Up(GameManager gm, Vector2 w)
    {
        if (selected == null) return;
        Ship ship = selected;
        ship.SetSelected(false);
        selected = null;
        gm.SubmitFlick(ship, start, w, path, Time.unscaledTime - startTime);
    }

    void Cancel()
    {
        if (selected != null) selected.SetSelected(false);
        selected = null;
    }

    Vector2 ToWorld(Vector2 screen)
    {
        return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
    }
}
