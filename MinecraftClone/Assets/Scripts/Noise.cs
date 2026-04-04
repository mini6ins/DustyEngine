using System;
using DustyEngine.Engine.Math.Vectors;
using MinecraftClone.Assets.Scripts;

public static class Noise
{
    private static readonly FastNoise noise2D = Create2DNoise();
    private static readonly FastNoise noise3D = Create3DNoise();
    
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        float x = (position.X + 0.1f + offset) * scale;
        float y = (position.Y + 0.1f + offset) * scale;

        float value = noise2D.GetPerlin(x, y);   
        return (value + 1f) * 0.5f;              
    }

    public static bool Get3DPerlin(Vector3 position, float offset, float scale, float threshold)
    {
        float x = (position.X + offset + 0.1f) * scale;
        float y = (position.Y + offset + 0.1f) * scale;
        float z = (position.Z + offset + 0.1f) * scale;

        float ab = Remap01(noise3D.GetPerlin(x, y, 0f));
        float bc = Remap01(noise3D.GetPerlin(y, z, 0f));
        float ac = Remap01(noise3D.GetPerlin(x, z, 0f));
        float ba = Remap01(noise3D.GetPerlin(y, x, 0f));
        float cb = Remap01(noise3D.GetPerlin(z, y, 0f));
        float ca = Remap01(noise3D.GetPerlin(z, x, 0f));

        return (ab + bc + ac + ba + cb + ca) / 6f > threshold;
    }
    
    private static FastNoise Create2DNoise()
    {
        var noise = new FastNoise(1337); 
        noise.SetNoiseType(FastNoise.NoiseType.Perlin);
        noise.SetFrequency(1f); 
        return noise;
    }

    private static FastNoise Create3DNoise()
    {
        var noise = new FastNoise(1337);
        noise.SetNoiseType(FastNoise.NoiseType.Perlin);
        noise.SetFrequency(1f);
        return noise;
    }

    private static float Remap01(float value)
    {
        return (value + 1f) * 0.5f;
    }
}