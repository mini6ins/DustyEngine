    using System.Drawing;
    using System.Drawing.Imaging;
    using OpenTK.Graphics.OpenGL.Compatibility;
    using PixelFormat = System.Drawing.Imaging.PixelFormat;

    public class Texture
    {
        public int Handle { get; private set; }

        public Texture(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, Handle);

            using (var bitmap = new Bitmap(path))
            {
                // Переворачиваем изображение по оси Y
                  bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                  bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL.Compatibility.PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);
            }

            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.BindTexture(TextureTarget.Texture2d, 0);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2d, Handle);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }
    }