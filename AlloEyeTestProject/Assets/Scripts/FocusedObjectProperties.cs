using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class FocusedObjectProperties : MonoBehaviour
{
    private Renderer Renderer;
    string objectID; //UNIQUE OBJECT ID FOR THIS OBJECT

    void Awake()
    {
        Renderer = GetComponent<Renderer>(); //renderer of this object    
        objectID = gameObject.name;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
