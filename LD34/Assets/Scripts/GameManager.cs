using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    public AudioSource musicPrefab;
    AudioSource music;

	// Use this for initialization
	void Start () 
    {
        if (musicPrefab)
        {
            music = Instantiate(musicPrefab.gameObject).GetComponent<AudioSource>();
            music.transform.parent = transform;
        }
	}
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}
