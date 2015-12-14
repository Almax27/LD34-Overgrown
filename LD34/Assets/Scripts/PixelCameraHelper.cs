using UnityEngine;
using System.Collections;

public class PixelCameraHelper : MonoBehaviour
{
    public float pixelsPerUnit = 16;
    public int pixelScale = 1;
    new Camera camera = null;

    // Use this for initialization
    void Start()
    {
        camera = GetComponent<Camera>();
    }
	
    // Update is called once per frame
    void Update()
    {
        float orthographicSize = 0.5f * (Screen.height / pixelsPerUnit);
        camera.orthographicSize = orthographicSize / pixelScale;
    }
}

