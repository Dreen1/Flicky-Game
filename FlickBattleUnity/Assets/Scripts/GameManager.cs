using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs the match: spawning, turns, flick rules, hits, win and replay.
// Put this on an empty object named "GameManager".
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public Ship blueShipPrefab;
    public Ship redShipPrefab;

    [Header("Rules")]
    public int shipsPerSide = 5;
    public float minFlickDistance = 0.4f;    // world units
    public float minFlickSpeed = 4f;         // world units per second
    public float maxFlickDuration = 0.65f;   // seconds
    public float maxCurveDegrees = 20f;
    public float hitRadiusMultiplier = 0.95f; // 1 = exact ship size, higher = more forgiving
    public float flightDuration = 0.35f;
    [Tooltip("Share of the screen at the top kept clear for the score bar")]
    public float hudFraction = 0.12f;

    [Header("Feedback")]
    public UIManager ui;
    public CameraShake cameraShake;
    public AudioSource sfx;
    public AudioClip whooshClip, explosionClip, missClip, winClip;

    public Team CurrentTeam { get; private set; } = Team.Blue;
    public bool AcceptingInput => !busy && !gameOver;

    struct Move
    {
        public Team team;
        public int index;
        public Vector2 from, ctrl, to;
    }

    readonly List<Ship> blue = new List<Ship>(), red = new List<Ship>();
    readonly List<Vector2> blueStarts = new List<Vector2>(), redStarts = new List<Vector2>();
    readonly List<Move> history = new List<Move>();
    bool busy, gameOver;
    Rect board;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60; // phones default to 30
    }

    void Start()
    {
        BuildBoard();
        SpawnShips();
        NewGame();
    }

    // The board is what the camera sees, minus the score bar at the top
    void BuildBoard()
    {
        Camera cam = Camera.main;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector2 c = cam.transform.position;
        board = new Rect(c.x - w, c.y - h, w * 2f, h * 2f * (1f - hudFraction));
    }

    // Asteroids-style: leaving one edge brings you in on the opposite edge
    public Vector2 Wrap(Vector2 p)
    {
        p.x = board.xMin + Mathf.Repeat(p.x - board.xMin, board.width);
        p.y = board.yMin + Mathf.Repeat(p.y - board.yMin, board.height);
        return p;
    }

    void SpawnShips()
    {
        float spacing = board.height / (shipsPerSide + 1);
        for (int i = 0; i < shipsPerSide; i++)
        {
            float y = board.yMax - spacing * (i + 1);
            Vector2 b = new Vector2(board.xMin + board.width * 0.1f, y);
            Vector2 r = new Vector2(board.xMin + board.width * 0.9f, y);
            blueStarts.Add(b);
            redStarts.Add(r);
            blue.Add(Instantiate(blueShipPrefab, b, Quaternion.identity));
            red.Add(Instantiate(redShipPrefab, r, Quaternion.identity));
        }
    }

    // Hook the "New Game" button to this
    public void NewGame()
    {
        StopAllCoroutines();
        ResetShips();
        history.Clear();
        busy = false;
        gameOver = false;
        CurrentTeam = Team.Blue;
        if (ui)
        {
            ui.SetTurn(CurrentTeam);
            ui.SetFeedback("", FeedbackKind.Neutral);
            ui.ShowReplayButton(false);
        }
        UpdateCounts();
    }

    void ResetShips()
    {
        for (int i = 0; i < blue.Count; i++) blue[i].ResetTo(blueStarts[i], Vector2.right);
        for (int i = 0; i < red.Count; i++) red[i].ResetTo(redStarts[i], Vector2.left);
    }

    public Ship FindShip(Vector2 point, float tapRadius)
    {
        Ship best = null;
        float bestDist = tapRadius;
        foreach (Ship s in ShipsOf(CurrentTeam))
        {
            if (!s.IsAlive) continue;
            float d = Vector2.Distance(point, s.Position);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    public void SubmitFlick(Ship ship, Vector2 fingerStart, Vector2 fingerEnd, List<Vector2> fingerPath, float duration)
    {
        if (!AcceptingInput) return;

        float dist = Vector2.Distance(fingerStart, fingerEnd);
        float speed = dist / Mathf.Max(duration, 0.0001f);
        if (dist < minFlickDistance || speed < minFlickSpeed || duration > maxFlickDuration)
        {
            if (ui) ui.SetFeedback("TOO SLOW - TURN LOST", FeedbackKind.Slow);
            Play(missClip);
            NextTurn();
            return;
        }

        // The ship copies the swipe, starting from wherever the ship is
        Vector2 from = ship.Position;
        Vector2 offset = from - fingerStart;
        Vector2 to = fingerEnd + offset;
        List<Vector2> mids = new List<Vector2>(fingerPath.Count);
        foreach (Vector2 p in fingerPath) mids.Add(p + offset);

        Move move = new Move
        {
            team = ship.team,
            index = ShipsOf(ship.team).IndexOf(ship),
            from = from,
            ctrl = FlickMath.ControlPoint(from, to, mids, maxCurveDegrees),
            to = to,
        };
        history.Add(move);
        StartCoroutine(PlayMove(move, false));
    }

    IEnumerator PlayMove(Move m, bool replay)
    {
        busy = true;
        Ship ship = ShipsOf(m.team)[m.index];
        ship.HasMoved = true;
        Play(whooshClip);

        List<(Ship ship, float t)> victims = FindVictims(ship, m);
        int next = 0;
        yield return StartCoroutine(ship.FlyAlong(m.from, m.ctrl, m.to, flightDuration, Wrap, t =>
        {
            while (next < victims.Count && victims[next].t <= t) Kill(victims[next++].ship);
        }));
        while (next < victims.Count) Kill(victims[next++].ship);

        if (victims.Count > 0)
        {
            if (ui) ui.SetFeedback(victims.Count > 1 ? $"{victims.Count}x HIT!" : "HIT!", FeedbackKind.Hit);
        }
        else
        {
            if (ui) ui.SetFeedback("Miss!", FeedbackKind.Miss);
            Play(missClip);
        }

        UpdateCounts();
        busy = false;
        if (replay) yield break;
        if (!CheckWin()) NextTurn();
    }

    // Everything the curve passes through dies, except:
    // - your own ships still on the starting grid are never hit
    // - only the first enemy still on its starting spot can die per flick,
    //   so multi-kills only happen against ships already out on the field
    List<(Ship ship, float t)> FindVictims(Ship mover, Move m)
    {
        List<(Ship ship, float t)> hits = new List<(Ship ship, float t)>();
        foreach (Ship s in AllShips())
        {
            if (s == mover || !s.IsAlive) continue;
            if (s.team == mover.team && !s.HasMoved) continue;
            float t = FlickMath.CurveHitT(m.from, m.ctrl, m.to, s.Position, s.radius * hitRadiusMultiplier, Wrap);
            if (t >= 0f) hits.Add((s, t));
        }
        hits.Sort((a, b) => a.t.CompareTo(b.t));

        List<(Ship ship, float t)> victims = new List<(Ship ship, float t)>();
        bool dockedKilled = false;
        foreach (var hit in hits)
        {
            if (!hit.ship.HasMoved)
            {
                if (dockedKilled) continue;
                dockedKilled = true;
            }
            victims.Add(hit);
        }
        return victims;
    }

    void Kill(Ship s)
    {
        s.Explode();
        Play(explosionClip);
        if (cameraShake) cameraShake.Shake();
    }

    void NextTurn()
    {
        CurrentTeam = CurrentTeam == Team.Blue ? Team.Red : Team.Blue;
        if (ui) ui.SetTurn(CurrentTeam);
    }

    bool CheckWin()
    {
        bool blueAlive = blue.Exists(s => s.IsAlive);
        bool redAlive = red.Exists(s => s.IsAlive);
        if (blueAlive && redAlive) return false;

        gameOver = true;
        Play(winClip);
        if (ui)
        {
            string msg = !blueAlive && !redAlive ? "DRAW!" : blueAlive ? "BLUE WINS!" : "RED WINS!";
            ui.SetFeedback(msg, FeedbackKind.Win);
            ui.SetGameOver();
            ui.ShowReplayButton(true);
        }
        return true;
    }

    // Hook the "Watch Replay" button to this
    public void WatchReplay()
    {
        if (history.Count == 0 || busy) return;
        StartCoroutine(Replay());
    }

    IEnumerator Replay()
    {
        if (ui) ui.ShowReplayButton(false);
        ResetShips();
        UpdateCounts();
        yield return new WaitForSeconds(0.5f);
        foreach (Move m in history)
        {
            yield return StartCoroutine(PlayMove(m, true));
            yield return new WaitForSeconds(0.6f);
        }
        busy = false;
        if (ui) ui.ShowReplayButton(true);
    }

    void UpdateCounts()
    {
        if (ui) ui.SetCounts(blue.FindAll(s => s.IsAlive).Count, red.FindAll(s => s.IsAlive).Count);
    }

    void Play(AudioClip clip)
    {
        if (sfx && clip) sfx.PlayOneShot(clip);
    }

    List<Ship> ShipsOf(Team t) => t == Team.Blue ? blue : red;

    IEnumerable<Ship> AllShips()
    {
        foreach (Ship s in blue) yield return s;
        foreach (Ship s in red) yield return s;
    }
}
