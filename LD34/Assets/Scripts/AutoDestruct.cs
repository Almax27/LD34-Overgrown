using UnityEngine;
using System.Collections;

public class AutoDestruct : MonoBehaviour {

    public float delay = 1.0f;

    void Start () 
    {
        Destroy(gameObject, delay);
    }
}