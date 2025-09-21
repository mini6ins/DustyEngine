using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using Utils;

namespace GameTestEngine.Assets;

public class MoveBoxCode: MonoBehaviour
{
    private Transform _boxTransform;
    private Vector3 direction = Vector3.Zero;
    private float movementSpeed = 1f;
    
    private void Start()
    {
        _boxTransform = GetComponent<Transform>();
        Debug.Log(_boxTransform.ToString());
    }
    
    public void Update()
    {
        float rotationSpeed = 1f; // градусов в секунду
        _boxTransform.LocalRotation.Y += rotationSpeed * (float)Time.DeltaTime;
        
        direction = Vector3.Zero;
        if (Input.IsKeyDown(KeyCode.I)) direction += _boxTransform.Forward;
        MoveBox();
    }
    
    
    private void MoveBox()
    {
        if (direction.LengthSquared > 0f)
        {
            direction = direction.Normalized();
            _boxTransform.LocalPosition += direction * movementSpeed * Time.DeltaTime;
        }
    }
}