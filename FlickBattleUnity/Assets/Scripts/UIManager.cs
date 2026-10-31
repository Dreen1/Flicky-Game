using TMPro;
using UnityEngine;

public enum FeedbackKind { Neutral, Hit, Miss, Slow, Win }

// Updates the on-screen text. Put this on the Canvas.
public class UIManager : MonoBehaviour
{
    public TMP_Text turnText;
    public TMP_Text feedbackText;
    public TMP_Text blueCountText;
    public TMP_Text redCountText;
    public GameObject replayButton;

    public Color blueColor = new Color(0.38f, 0.65f, 0.98f);
    public Color redColor = new Color(0.97f, 0.44f, 0.44f);
    public Color hitColor = new Color(0.29f, 0.87f, 0.5f);
    public Color missColor = new Color(0.98f, 0.8f, 0.08f);
    public Color winColor = new Color(1f, 0.84f, 0f);

    public void SetTurn(Team team)
    {
        turnText.text = team == Team.Blue ? "BLUE'S TURN" : "RED'S TURN";
        turnText.color = team == Team.Blue ? blueColor : redColor;
    }

    public void SetGameOver()
    {
        turnText.text = "GAME OVER";
        turnText.color = Color.white;
    }

    public void SetFeedback(string message, FeedbackKind kind)
    {
        feedbackText.text = message;
        switch (kind)
        {
            case FeedbackKind.Hit: feedbackText.color = hitColor; break;
            case FeedbackKind.Miss: feedbackText.color = missColor; break;
            case FeedbackKind.Slow: feedbackText.color = redColor; break;
            case FeedbackKind.Win: feedbackText.color = winColor; break;
            default: feedbackText.color = Color.white; break;
        }
    }

    public void SetCounts(int blueAlive, int redAlive)
    {
        blueCountText.text = $"BLUE: {blueAlive}";
        redCountText.text = $"RED: {redAlive}";
    }

    public void ShowReplayButton(bool show)
    {
        if (replayButton) replayButton.SetActive(show);
    }
}
