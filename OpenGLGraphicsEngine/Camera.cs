﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera
{
    public Vector3 Position { get; private set; }
    public Vector3 Front { get; private set; } = -Vector3.UnitZ;
    public Vector3 Up { get; private set; } = Vector3.UnitY;

    private float speed = 5f;
    private float sensitivity = 0.2f;

    private Vector2 lastMousePos;
    private bool firstMouseMove = true;
    private float pitch = 0.0f;
    private float yaw = -90.0f;

    private bool is2D;

    public Camera(Vector3 position, bool is2D)
    {
        Position = position;
        this.is2D = is2D;

        if (is2D)
        {
            Front = Vector3.UnitY;  // В 2D камера смотрит вдоль оси Y
            Up = Vector3.UnitZ;     // В 2D ось Z используется как "вверх"
        }
    }

    // Получение матрицы вида
    public Matrix4 GetViewMatrix()
    {
        if (is2D)
        {
            // В 2D-режиме камера смотрит вдоль оси Z на плоскость XY
            return Matrix4.LookAt(
                new Vector3(Position.X, Position.Y, 10.0f),  // Камера на небольшом расстоянии
                new Vector3(Position.X, Position.Y, 0.0f),  // Смотрим на плоскость Z=0
                Vector3.UnitY                                // "Вверх" в 2D-режиме — ось Y
            );
        }
        else
        {
            // Стандартная матрица для 3D
            return Matrix4.LookAt(Position, Position + Front, Up);
        }
    }

    // Обработка ввода с клавиатуры для движения
    public void ProcessKeyboard(KeyboardState input, float deltaTime)
    {
        float velocity = speed * deltaTime;

        if (is2D)
        {
            // Ограничиваем движение в плоскости X-Y
            Vector3 right = Vector3.Normalize(Vector3.Cross(Front, Up));

            if (input.IsKeyDown(Keys.W))  // Движение вперед по оси Y
                Position += Front * velocity;
            if (input.IsKeyDown(Keys.S))  // Движение назад по оси Y
                Position -= Front * velocity;
            if (input.IsKeyDown(Keys.A))  // Движение влево по оси X
                Position -= right * velocity;
            if (input.IsKeyDown(Keys.D))  // Движение вправо по оси X
                Position += right * velocity;
        }
        else
        {
            // В 3D-режиме можно двигаться по всем осям
            if (input.IsKeyDown(Keys.W))
                Position += Front * velocity;  // Вперед
            if (input.IsKeyDown(Keys.S))
                Position -= Front * velocity;  // Назад
            if (input.IsKeyDown(Keys.A))
                Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * velocity;  // Влево
            if (input.IsKeyDown(Keys.D))
                Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * velocity;  // Вправо
            if (input.IsKeyDown(Keys.Space))
                Position += Up * velocity;  // Вверх
            if (input.IsKeyDown(Keys.LeftShift))
                Position -= Up * velocity;  // Вниз
        }
    }
    
    public void ProcessMouseMove(MouseState mouse, float deltaTime)
    {
        if (firstMouseMove)
        {
            lastMousePos = mouse.Position;
            firstMouseMove = false;
        }

        var offset = mouse.Position - lastMousePos;
        lastMousePos = mouse.Position;

        yaw += offset.X * sensitivity;
        pitch -= offset.Y * sensitivity;

        pitch = Clamp(pitch, -89.0f, 89.0f);

        Vector3 direction;
        direction.X = MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        direction.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
        direction.Z = MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch));
        Front = Vector3.Normalize(direction);
    }
    
    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}