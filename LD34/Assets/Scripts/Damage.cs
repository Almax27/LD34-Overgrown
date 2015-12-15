using UnityEngine;
using System.Collections;

public class Damage {

    public Damage(int damage, GameObject sender)
    {
        value = damage;
        owner = sender;
    }
    public int value;
    public GameObject owner;
}
