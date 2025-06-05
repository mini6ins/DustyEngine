using DustyEngine.Components;
using System;
using DustyEngine;
using DustyEngine.Scene;

public class Player : MonoBehaviour
{
    public string Message { get; set; } = "Hello from DLL!";

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
        
    }

    public void Test()
    {
         Debug.Log("TEST");
    }
    public void Start()
    {
        // Debug.Log(Parent.GetComponent<Transform>());
    }

    public void Update()
    {
        
        // Debug.Log("TEST");
    }
}