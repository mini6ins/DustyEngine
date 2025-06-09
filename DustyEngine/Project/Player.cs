using DustyEngine.Components;
using System;
using DustyEngine;
using DustyEngine.Engine.Math.Vectors;
using DustyEngine.Scene;
using Utils;

public class Player : MonoBehaviour
{
    public string Message { get; set; } = "Hello from DLL!";
    private Transform cameraTransform;
    private float _movementSpeed = 0.05f;

    public void OnEnable()
    {
        cameraTransform = GetComponent<Camera>().transform;
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
        MoveCamera();
        RotateCamera();
        
        // Debug.Log("TEST");
    }

    private void MoveCamera()
    {
        Vector3 direction = new Vector3(0, 0, 0);
        if (Input.IsKeyDown(KeyCode.W)) direction += cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.S)) direction -= cameraTransform.Forward;
        if (Input.IsKeyDown(KeyCode.A)) direction -= cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.D)) direction += cameraTransform.Right;
        if (Input.IsKeyDown(KeyCode.Space)) direction += cameraTransform.Up;
        if (Input.IsKeyDown(KeyCode.LeftShift)) direction -= cameraTransform.Up;

        if (direction.LengthSquared > 0)
        {
            direction = direction.Normalized();
            var movement = direction * _movementSpeed;
            cameraTransform.LocalPosition += movement;
        }
    }

    private  void RotateCamera()
    {
        var (deltaX, deltaY) = Input.Delta;
        
        var rotation = cameraTransform.LocalRotation;
        rotation.Y += deltaX * 0.1f;
        rotation.X -= deltaY * 0.1f;
        rotation.X = Math.Math.Clamp(rotation.X, -89f, 89f);
        cameraTransform.LocalRotation = rotation;
    }
}