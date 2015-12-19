using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

    #region Types
    [System.Serializable]
    public class Character
    {
        public PlayerController playerPrefab;

    }
    [System.Serializable]
    public class Enemy
    {
        public GameObject prefab;
    }
    #endregion


    [Header("Difficulty Config")]
    public float spawnRate = 1;
    public int maxEnemies = 10;
    public float flyingSpawnRate = 1;
    public int maxFlyingEnemies = 10;
    float spawnTick = 0;
    float spawnFlyingTick = 0;

    [Header("World")]
    public AudioSource musicPrefab;
    AudioSource music;

    public FollowCamera followCamera = null;
    public TextMesh playerHealthText = null;

    public Transform title = null;
    public Transform world = null;

    public Transform playerSpawnPointsRoot = null;
    public Transform enemySpawnPointsRoot = null;
    public Transform climbableRoot = null;
    public PolygonCollider2D worldCollider = null;

    List<Transform> playerSpawnPoints = new List<Transform>();
    List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("Players")]

    public List<Character> characters = new List<Character>();
    public PlayerController activePlayerInstance = null;

    [Header("Enemies")]

    public List<Enemy> enemies = new List<Enemy>();
    public List<Enemy> flyingEnemies = new List<Enemy>();
    List<GameObject> activeEnemies = new List<GameObject>();
    List<GameObject> activeFlyingEnemies = new List<GameObject>();
    public float optimiseEnemyDistance = 20;
    float optimiseEnemyTick = 0;

	// Use this for initialization
	void Start () 
    {
        if (musicPrefab)
        {
            music = Instantiate(musicPrefab.gameObject).GetComponent<AudioSource>();
            music.transform.parent = transform;
        }

        if (climbableRoot)
        {
            foreach(var collider in climbableRoot.GetComponentsInChildren<Transform>())
            {
                collider.gameObject.layer = LayerMask.NameToLayer("Climbable");
            }
        }

        if (worldCollider)
        {
            worldCollider.gameObject.layer = LayerMask.NameToLayer("World");
        }

        foreach (var col in playerSpawnPointsRoot.GetComponentsInChildren<Collider2D>())
        {
            playerSpawnPoints.Add(col.transform);
        }
        Debug.Assert(playerSpawnPoints.Count > 0, "No player spawn points found!");

        foreach (var col in enemySpawnPointsRoot.GetComponentsInChildren<Collider2D>())
        {
            enemySpawnPoints.Add(col.transform);
        }
        Debug.Assert(enemySpawnPoints.Count > 0, "No enemy spawn points found!");
	}
	
    bool hadPlayer = true;

	// Update is called once per frame
	void Update () 
    {
        bool hasPlayer = activePlayerInstance != null;
        if (hadPlayer != hasPlayer)
        {
            hadPlayer = hasPlayer;
            title.gameObject.SetActive(!hasPlayer);
            world.gameObject.SetActive(hasPlayer);
            playerHealthText.transform.parent.gameObject.SetActive(hasPlayer);
        }

        if (!activePlayerInstance)
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

        if (activePlayerInstance)
        {
            Health playerHealth = activePlayerInstance.GetComponent<Health>();
            playerHealthText.text = playerHealth ? playerHealth.currentHealth.ToString() : "No Health";
        }
        else
        {
            playerHealthText.text = "DEAD";
        }



        //spawn
        if (spawnTick > spawnRate)
        {
            if (activeEnemies.Count < maxEnemies)
            {
                var enemy = enemies[Random.Range(0, enemies.Count)];
                SpawnEnemy(enemy);
                spawnTick = 0;
            }
        }
        else
        {
            spawnTick += Time.deltaTime;
        }

        //spawn flying
        if (spawnFlyingTick > flyingSpawnRate)
        {
            if (activeFlyingEnemies.Count < maxFlyingEnemies)
            {
                var enemy = flyingEnemies[Random.Range(0, flyingEnemies.Count)];
                SpawnFlyingEnemy(enemy);
                spawnFlyingTick = 0;
            }
        }
        else
        {
            spawnFlyingTick += Time.deltaTime;
        }

        optimiseEnemyTick += Time.deltaTime;
        if (optimiseEnemyTick > 1.0f)
        {
            //cleanup null enemies
            activeEnemies.RemoveAll(v => v == null);
            activeFlyingEnemies.RemoveAll(v => v == null);
            //optimise enemies
            OptimiseEnemies(activeEnemies);
            OptimiseEnemies(activeFlyingEnemies);
            optimiseEnemyTick = 0;
        }
	}

    void SpawnCharacter(Character character)
    {
        Transform spawnPoint = playerSpawnPoints[Random.Range(0,playerSpawnPoints.Count)];

        if (activePlayerInstance)
        {
            Destroy(activePlayerInstance.gameObject);
            activePlayerInstance = null;
        }
        activePlayerInstance = Instantiate(character.playerPrefab.gameObject).GetComponent<PlayerController>();
        activePlayerInstance.transform.position = spawnPoint.transform.position;

        followCamera.target = activePlayerInstance.transform;
    }

    void SpawnEnemy(Enemy enemy)
    {
        Transform spawnPoint = enemySpawnPoints[Random.Range(0,enemySpawnPoints.Count)];

        var gobj = Instantiate(enemy.prefab.gameObject);
        gobj.transform.position = spawnPoint.transform.position;
        gobj.transform.parent = world;

        activeEnemies.Add(gobj);
    }

    void SpawnFlyingEnemy(Enemy enemy)
    {
        var gobj = Instantiate(enemy.prefab.gameObject);

        //spawn edge of level
        var pos = Vector3.zero;
        if (Random.value > 0.5f)
        {
            pos.x = Random.value > 0.5f ? -0.5f : 0.5f;
            pos.y = Random.value;
        }
        else
        {
            pos.x = Random.value;
            pos.y = Random.value > 0.5f ? -0.5f : 0.5f;
        }
        pos.Scale(new Vector3(400, 200, 0));
        gobj.transform.position = pos;
        gobj.transform.parent = world;

        activeFlyingEnemies.Add(gobj);
    }
        
    void OptimiseEnemies(List<GameObject> enemiesToOptimise)
    {
        if (activePlayerInstance)
        {
           float optimDistSq = optimiseEnemyDistance * optimiseEnemyDistance;
            foreach (var enemy in enemiesToOptimise)
            {
                float distSq = (enemy.transform.position - activePlayerInstance.transform.position).sqrMagnitude;
                enemy.SetActive(distSq < optimDistSq);
            }
        }
    }

    void OnDrawGizmos()
    {
        if(activePlayerInstance)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(activePlayerInstance.transform.position, optimiseEnemyDistance);
        }
    }
}
