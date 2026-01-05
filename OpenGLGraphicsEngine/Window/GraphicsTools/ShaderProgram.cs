using OpenTK.Graphics.OpenGL.Compatibility;
using OpenTK.Mathematics;
using System;
using System.IO;

public class ShaderProgram : IDisposable
{
    private int _programHandle;
    private bool _disposed = false;

    public ShaderProgram(string vertexFile, string fragmentFile)
    {
        int vertexShader = CreateShader(ShaderType.VertexShader, vertexFile);
        int fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentFile);

        _programHandle = GL.CreateProgram();
        GL.AttachShader(_programHandle, vertexShader);
        GL.AttachShader(_programHandle, fragmentShader);

        LinkProgram(_programHandle);

        GL.DetachShader(_programHandle, vertexShader);
        GL.DetachShader(_programHandle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }
    
    public void Use() => GL.UseProgram(_programHandle);
    public void Deactivate() => GL.UseProgram(0);
    
    public int GetAttribLocation(string name)
    {
        int location = GL.GetAttribLocation(_programHandle, name);
        if (location == -1)
            throw new Exception($"Attribute {name} not found in the shader program.");
        return location;
    }

    public int GetUniformLocation(string name)
    {
        int location = GL.GetUniformLocation(_programHandle, name);
        if (location == -1)
            throw new Exception($"Uniform {name} not found in the shader program.");
        return location;
    }

    public void SetUniform(string name, Vector4 vec)
    {
        int location = GetUniformLocation(name);
        GL.Uniform4f(location, vec.X, vec.Y, vec.Z, vec.W);
    }

    public void SetUniform(string name, Vector3 vec)
    {
        int location = GetUniformLocation(name);
        GL.Uniform3f(location, vec.X, vec.Y, vec.Z);
    }

    public void SetUniform(string name, Matrix4 matrix)
    {
        int location = GetUniformLocation(name);
        GL.UniformMatrix4f(location, 1, false, ref matrix);
    }

    public void SetUniform(string name, float value)
    {
        int location = GetUniformLocation(name);
        GL.Uniform1f(location, value);
    }

    public void SetUniform(string name, int value)
    {
        int location = GetUniformLocation(name);
        GL.Uniform1i(location, value);
    }

    private int CreateShader(ShaderType shaderType, string shaderFile)
    {
        if (!File.Exists(shaderFile))
            throw new FileNotFoundException($"Shader file not found: {shaderFile}");

        string shaderSource = File.ReadAllText(shaderFile);
        int shaderID = GL.CreateShader(shaderType);
        GL.ShaderSource(shaderID, shaderSource);
        
        GL.CompileShader(shaderID);
        
        return shaderID;
    }

    private void LinkProgram(int program)
    {
        GL.LinkProgram(program);
        GL.GetProgrami(program, ProgramProperty.LinkStatus, out int linkStatus);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_programHandle != 0)
            {
                GL.DeleteProgram(_programHandle);
                _programHandle = 0;
            }
            _disposed = true;
        }
    }

    ~ShaderProgram()
    {
        Dispose(false);
    }
}