using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using InputSystem;
using Utils;

public class MoveBoxCode : MonoBehaviour
{
    private Transform _boxTransform;
    private Vector3 _direction = Vector3.Zero;
    private float _movementSpeed = 1f;

    private void Start()
    {
        _boxTransform = GetComponent<Transform>();
        Debug.Log(_boxTransform.ToString());
    }

    public void Update()
    {
        float rotationSpeed = 1f;
        _boxTransform.LocalRotation.Y += rotationSpeed * (float)Time.DeltaTime;

        _direction = Vector3.Zero;
        // if(Input.IsKeyDown(KeyCode.I)) Debug.Log(_direction);
        if (Input.IsKeyDown(KeyCode.I)) _direction += _boxTransform.Forward;
        MoveBox();
    }


    private void MoveBox()
    {
        if (_direction.LengthSquared > 0f)
        {
            _direction = _direction.Normalized();
            _boxTransform.LocalPosition += _direction * _movementSpeed * Time.DeltaTime;
        }
    }
}
