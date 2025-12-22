using OpenTK.Graphics.OpenGL.Compatibility;
using StbImageSharp;

public static class IconLoader
{
    public static int FolderIcon;
    public static int FileIcon;

    public static int CSharpIcon;
    public static int ImageIcon;
    public static int ObjIcon;
    public static int SceneIcon;

    public static void InitIcons()
    {

        var baseDir = AppContext.BaseDirectory;
        var iconsDir = Path.Combine(baseDir, "Editor","Editor", "Icons");

        FolderIcon = LoadTexture(Path.Combine(iconsDir, "folderIcon.png"));
        FileIcon = LoadTexture(Path.Combine(iconsDir, "fileIcon.png"));

        CSharpIcon = LoadTexture(Path.Combine(iconsDir, "csharpIcon.png"));
        ImageIcon = LoadTexture(Path.Combine(iconsDir, "imageIcon.png"));
        ObjIcon = LoadTexture(Path.Combine(iconsDir, "ObjIcon.png"));
        SceneIcon  = LoadTexture(Path.Combine(iconsDir, "sceneIcon.png"));
    }

    public static int LoadTexture(string path)
    {
        var tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, tex);

        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(
            TextureTarget.Texture2d,
            0,
            InternalFormat.Rgba,
            image.Width,
            image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            image.Data
        );

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.BindTexture(TextureTarget.Texture2d, 0);
        return tex;
    }
}
