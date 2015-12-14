using UnityEngine;
using System.Collections;

public class BackgroundHelper : MonoBehaviour {

    public Camera relativeCamera = null;
    public Vector2 parallaxScale = Vector2.one;
    public Vector2 relativePosition = new Vector2(0.5f,0.5f);

    public SpriteRenderer spritePrefab = null;
    public int tileUp = 0;
    public int tileDown = 0;
    public int tileLeft = 0;
    public int tileRight = 0;
    public bool randomFlipX = false;
    public bool randomFlipY = false;
    public Vector2 randomOffset = Vector2.zero;

    void Start()
    {
        var centerSprite = CreateSprite();
        centerSprite.transform.position = Vector3.zero;

        for (int i = -tileLeft; i < tileRight || i == 0; i++)
        {
            for (int j = -tileDown; j < tileUp || j == 0; j++)
            {
                if (i != 0 || j != 0)
                {
                    var sprite = CreateSprite();
                    Vector3 pos = new Vector3(i * sprite.bounds.size.x, j * sprite.bounds.size.y, 0);
                    pos.x += randomOffset.x * Random.value;
                    pos.y += randomOffset.y * Random.value;
                    sprite.transform.position = pos;
                    if(randomFlipX)
                    {
                        sprite.flipX = Random.value > 0.5f;
                    }
                    if(randomFlipY)
                    {
                        sprite.flipY = Random.value > 0.5f;
                    }
                }
            }
        }
    }

    SpriteRenderer CreateSprite()
    {
        var spriteRenderer = Instantiate(spritePrefab.gameObject).GetComponent<SpriteRenderer>();
        spriteRenderer.transform.parent = this.transform;
        return spriteRenderer;
    }

	void LateUpdate () 
    {
        Camera camera = relativeCamera;
        if (camera == null)
        {
            camera = Camera.main;
        }
        Vector2 pos = Vector2.zero;
        pos.x = camera.transform.position.x * parallaxScale.x;
        pos.y = camera.transform.position.y * parallaxScale.y;

        pos.x += (relativePosition.x - 0.5f) * camera.orthographicSize * 2.0f;
        pos.y += (relativePosition.y - 0.5f) * camera.orthographicSize * 2.0f;

        transform.position = pos;
	}
}
