using DustyEngine;
using DustyEngine.Components;
using DustyEngine.Engine.Math.Vectors;
using InputSystem;

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
        const float rotationSpeed = 1f;

        var rotation = _boxTransform.LocalRotation;
        rotation.Y += rotationSpeed * Time.DeltaTime;
        _boxTransform.LocalRotation = rotation;

        _direction = Vector3.Zero;
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
