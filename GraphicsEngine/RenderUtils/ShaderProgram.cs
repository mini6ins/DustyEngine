using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace GraphicsEngineOpenGL.RenderUtils;

public class ShaderProgram
{
    public readonly int Program = 0;

    public ShaderProgram(string vertexFile, string fragmentFile)
    {
        var vertexShader = CreateShader(ShaderType.VertexShader, vertexFile);
        var fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentFile);

        Program = GL.CreateProgram();
        GL.AttachShader(Program, vertexShader);
        GL.AttachShader(Program, fragmentShader);

        GL.LinkProgram(Program);


        DeleteShader(vertexShader);
        DeleteShader(fragmentShader);
    }

    public void ActiveProgram() => GL.UseProgram(Program);

    public void DeactiveProgram() => GL.UseProgram(0);

    public void DeleteProgram() => GL.DeleteProgram(Program);

    public int GetAttribProgram(string name) => GL.GetAttribLocation(Program, name);

    public int GetAttribLocation(string name)
    {
        int location = GL.GetAttribLocation(Program, name);
        return location;
    }

    public int GetUniformLocation(string name)
    {
        int location = GL.GetUniformLocation(Program, name);
        if (location == -1)
            throw new Exception($"Uniform {name} not found in the shader program.");
        return location;
    }

    public void SetUniform4(string name, Vector4 vec)
    {
        int location = GL.GetUniformLocation(Program, name);
        GL.Uniform4f(location, vec.X, vec.Y, vec.Z, vec.W);
    }

    public void SetUniform(string name, Matrix4 matrix)
    {
        int location = GetUniformLocation(name);
        GL.UniformMatrix4f(location, 1, false, ref matrix);
    }

    public void SetUniform(string name, Vector3 vector)
    {
        int location = GetUniformLocation(name);
        GL.Uniform3f(location, vector.X, vector.Y, vector.Z);
    }

    private int CreateShader(ShaderType shaderType, string shaderFile)
    {
        string shaderStr = File.ReadAllText(shaderFile);
        int shaderID = GL.CreateShader(shaderType);
        GL.ShaderSource(shaderID, shaderStr);
        GL.CompileShader(shaderID);

        return shaderID;
    }

    private void DeleteShader(int shader)
    {
        GL.DetachShader(Program, shader);
        GL.DeleteShader(shader);
    }
}