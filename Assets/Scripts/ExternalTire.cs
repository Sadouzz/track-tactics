using System.Collections;
using System.Collections.Generic;
//using FishNet.
using UnityEngine;

public class ExternalTire : MonoBehaviour
{
    public Transform tire;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = tire.position;
    }
}
