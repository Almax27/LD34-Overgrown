using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

    public AudioSource musicPrefab;
    AudioSource music;

    public Transform spawnPoint = null;
    public FollowCamera followCamera = null;

    [System.Serializable]
    public class Character
    {
        public PlayerController playerPrefab;

    }

    public List<Character> characters = new List<Character>();
    public PlayerController activePlayerInstance = null;


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
        if (Input.GetKeyDown(KeyCode.Alpha1) && characters.Count > 0)
        {
            SpawnCharacter(characters[0]);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && characters.Count > 1)
        {
            SpawnCharacter(characters[1]);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && characters.Count > 2)
        {
            SpawnCharacter(characters[2]);
        }
	}

    void SpawnCharacter(Character character)
    {
        if (activePlayerInstance)
        {
            Destroy(activePlayerInstance.gameObject);
            activePlayerInstance = null;
        }
        activePlayerInstance = Instantiate(character.playerPrefab.gameObject).GetComponent<PlayerController>();
        activePlayerInstance.transform.position = spawnPoint.transform.position;

        followCamera.target = activePlayerInstance.transform;
    }
}
