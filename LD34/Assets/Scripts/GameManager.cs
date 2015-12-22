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
        public float spawnWeighting = 1;
    }
    #endregion


    [Header("Difficulty Config")]
    public int maxEnemies = 10;
    public int maxFlyingEnemies = 10;

    [Header("Scoring")]
    public int score = 0;
    public int highScore = -1;


    [Header("World")]
    public AudioSource musicPrefab;
    AudioSource music;

    public FollowCamera followCamera = null;
    public TextMesh playerHealthText = null;
    public TextMesh scoreText = null;
    public TextMesh highScoreText = null;
    public ProgressManager progressManager = null;
    public EndGame endGame = null;

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

    Animator animator = null;
    bool gameEnded = false;

	// Use this for initialization
	void Start () 
    {
        animator = GetComponent<Animator>();

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

        ResetToTitle();
	}
	
    public void ResetToTitle()
    {
        if (score > 0 && score > highScore)
        {
            highScore = score;
        }
        highScoreText.text = highScore >= 0 ? string.Format("Highscore: {0}", highScore) : "";
        score = 0;

        progressManager.enabled = false;
        progressManager.Reset();

        foreach (var enemy in activeEnemies)
        {
            Destroy(enemy);
        }
        foreach (var enemy in activeFlyingEnemies)
        {
            Destroy(enemy);
        }
        foreach (var tempGobj in GameObject.FindGameObjectsWithTag("Temp"))
        {
            Destroy(tempGobj);
        }
        animator.SetTrigger("onTitle");
        world.gameObject.SetActive(false);
        if (activePlayerInstance)
        {
            Destroy(activePlayerInstance.gameObject);
            activePlayerInstance = null;
        }
    }

	// Update is called once per frame
	void Update () 
    {
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

        //update UI
        if (activePlayerInstance)
        {
            Health playerHealth = activePlayerInstance.GetComponent<Health>();
            if (playerHealth)
            {
                playerHealthText.text = Mathf.Max(0, playerHealth.currentHealth).ToString();
            }
            else
            {
                playerHealthText.text = "No Health";
            }
        }
        else
        {
            playerHealthText.text = "???";
        }

        if (scoreText)
        {
            scoreText.text = string.Format("Score: {0}", score);
        }

        float enemyMultiplier = 1;
        if (progressManager)
        {
            var progressStage = progressManager.GetCurrentStage();
            if (progressStage != null)
            {
                enemyMultiplier = progressStage.enemyMultiplier;
            }
        }
            
        //spawn
        if (activeEnemies.Count < maxEnemies*enemyMultiplier)
        {
            var enemy = PickRandomEnemy(enemies);
            SpawnEnemy(enemy);
        }

        //spawn flying
        if (activeFlyingEnemies.Count < maxFlyingEnemies*enemyMultiplier)
        {
            var enemy = PickRandomEnemy(flyingEnemies);
            SpawnFlyingEnemy(enemy);
        }

        optimiseEnemyTick += Time.deltaTime;
        if (optimiseEnemyTick > 1.0f)
        {
            //cleanup null enemies
            activeEnemies.RemoveAll(v => v == null);
            activeFlyingEnemies.RemoveAll(v => v == null);
            //optimise enemies
            OptimiseEnemies(activeEnemies);
            //OptimiseEnemies(activeFlyingEnemies); //don't optimise flying enemies or they can't fly to the player
            optimiseEnemyTick = 0;
        }

        //mute button
        if (Input.GetButtonDown("Mute"))
        {
            AudioListener audioListener = followCamera.GetComponent<AudioListener>();
            audioListener.enabled = !audioListener.enabled;
        }
        if (Input.GetButtonDown("PixelScaleToggle"))
        {
            var pixelScaleHelper = followCamera.GetComponent<PixelCameraHelper>();
            if (pixelScaleHelper)
            {
                pixelScaleHelper.pixelScale = pixelScaleHelper.pixelScale == 1 ? 2 : 1;
            }
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

        animator.SetTrigger("onGameStart");
        world.gameObject.SetActive(true);

        progressManager.enabled = true;
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

        Rect worldRect = new Rect(-200, -100, 400, 200);
        //spawn distance away from player
        if (activePlayerInstance)
        {
            var offset = Random.onUnitSphere * 100;
            offset.z = 0;
            var pos = activePlayerInstance.transform.position + offset;
            pos.x = Mathf.Clamp(pos.x, worldRect.xMin, worldRect.xMax); 
            pos.y = Mathf.Clamp(pos.y, worldRect.yMin, worldRect.yMax);
            gobj.transform.position = pos;
        }
        else
        {
            var pos = new Vector3(Random.Range(worldRect.xMin, worldRect.xMax), Random.Range(worldRect.yMin, worldRect.yMax), 0);
            gobj.transform.position = pos;
        }

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

    Enemy PickRandomEnemy(List<Enemy> enemiesToPickFrom)
    {
        float totalWeight = 0;
        for (int i = 0; i < enemiesToPickFrom.Count; i++)
        {
            totalWeight += enemiesToPickFrom[i].spawnWeighting;
        }

        float chance = Random.value;

        for(int i = 0; i < enemiesToPickFrom.Count; i++)
        {
            float spawnChance = enemiesToPickFrom[i].spawnWeighting / totalWeight;
            if (chance <= spawnChance)
            {
                return enemiesToPickFrom[i];
            }
            else
            {
                chance -= spawnChance;
            }
        }
        return null;
    }

    public void OnWin(string message)
    {
        endGame.OnGameEnded(score, highScore, message);
        animator.SetTrigger("onGameEnd");
        progressManager.enabled = false;
        //destroy health so player is no longer targeted
        Destroy(activePlayerInstance.GetComponent<Health>());
    }

    public void OnLoss(string message)
    {
        endGame.OnGameEnded(score, highScore, message);
        animator.SetTrigger("onGameEnd");
        progressManager.enabled = false;
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
