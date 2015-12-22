using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ProgressOnDeath : MonoBehaviour {

    public int scoreWorth = 10;
    public float progressWorth = 0.1f;
    GameManager gameManager = null;

	// Use this for initialization
	void Start () 
    {
        gameManager = FindObjectOfType<GameManager>();
	}
	
    void OnDeath()
    {
        if (gameManager)
        {
            gameManager.score += scoreWorth;
            gameManager.progressManager.value -= progressWorth;
        }
    }
	
}
