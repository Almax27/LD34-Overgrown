using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndGame : MonoBehaviour 
{
    public Text scoreText = null;
    public Text highScoreText = null;
    public Text messageText = null;
    public float waitDuration = 1.0f;
    float endTime = 0;

    bool isWaiting = false;

    public void OnGameEnded(int score, int highScore, string message)
    {
        if (!isWaiting)
        {
            scoreText.text = score.ToString();
            messageText.text = message;
            highScoreText.text = highScore > 0 ? string.Format("Previous Highscore\n{0}", highScore) : "";
            endTime = Time.time;
            isWaiting = true;
        }
    }

    void Update()
    {
        float time = Time.time;
        if (time - endTime > waitDuration && Input.anyKeyDown)
        {
            FindObjectOfType<GameManager>().ResetToTitle();
            isWaiting = false;
        }
    }
}
