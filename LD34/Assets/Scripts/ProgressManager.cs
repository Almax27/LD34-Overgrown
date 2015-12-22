using UnityEngine;
using System.Collections.Generic;

public class ProgressManager : MonoBehaviour {

    [System.Serializable]
    public class Stage
    {
        [Tooltip("Seconds to fill progress bar from empty")]
        public float fillDuration = 60;
        public float stageDuration = 60;
        public float enemyMultiplier = 1;
    }

    public float value = 0.5f;
    public List<Stage> stages = new List<Stage>(1);
    public int currentStageIndex = 0;

    public SpriteRenderer progressBar = null;
    public List<Color> progressColors = new List<Color>();
    List<Sprite> progressBarSprites = new List<Sprite>();
    public TextMesh stageText = null;
    public TextMesh stageDurationText = null;

    float tick = 0;

	// Use this for initialization
	void Start () 
    {
        if(stages.Count == 0)
        {   
            Debug.LogError("No Stages defined!");
        }   
        if (progressBar)
        {
            var sprite = progressBar.sprite;
            Rect rect = sprite.rect;
            for (int i = 0; i < sprite.rect.width; i++)
            {
                rect.width = i;
                var newSprite = Sprite.Create(sprite.texture, rect, sprite.pivot, sprite.pixelsPerUnit, 0, SpriteMeshType.Tight, sprite.border);
                progressBarSprites.Add(newSprite);
            }
        }
        Reset();
	}

    public void Reset()
    {
        value = 0;
        tick = 0;
        currentStageIndex = 0;
        stageText.text = string.Format("Stage: {0}", currentStageIndex + 1);
    }

	// Update is called once per frame
	void Update () 
    {
        var currentStage = GetCurrentStage();
        if (currentStage != null)
        {
            tick += Time.deltaTime;
            if (tick > currentStage.stageDuration)
            {
                tick = 0;
                currentStageIndex = currentStageIndex + 1;
                if (currentStageIndex < stages.Count)
                {
                    stageText.text = string.Format("Stage: {0}", currentStageIndex + 1);
                }
                else
                {
                    FindObjectOfType<GameManager>().OnWin("Mission Successful\nExtraction Inbound");
                    return;
                }
            }

            value = Mathf.Clamp01(value + (Time.deltaTime / currentStage.fillDuration));

            stageDurationText.text = string.Format("{0:F1}s", currentStage.stageDuration - tick);

            UpdateProgressBarSprite();

            if (value >= 1)
            {
                FindObjectOfType<GameManager>().OnLoss("Mission Failed!\nThe population grew out of control");
            }
        }
        else
        {
            FindObjectOfType<GameManager>().OnLoss("Mission Failed!\nThe population grew out of control");
        }
	}

    public Stage GetCurrentStage()
    {
        if (currentStageIndex >= 0 && currentStageIndex < stages.Count)
        {
            return stages[currentStageIndex];
        }
        return null;
    }

    void UpdateProgressBarSprite()
    {
        int index = (int)((progressBarSprites.Count - 1) * value);
        progressBar.sprite = progressBarSprites[index];

        if (progressColors.Count >= 2)
        {
            float colorSpace = value * (progressColors.Count - 1);
            if (value == 1)
            {
                colorSpace = progressColors.Count - 2;
            }
            int fromIndex = Mathf.FloorToInt(colorSpace);
            progressBar.color = Color.Lerp(progressColors[fromIndex], progressColors[fromIndex + 1], colorSpace - fromIndex);
        }
    }
}
