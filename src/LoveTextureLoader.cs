

using System;
using System.IO;
using Love;

namespace Spine
{
    public class LoveTextureLoader : TextureLoader
    {

        public void Load(AtlasPage page, String path)
        {
            var texture = Util.LoadTexture(path);
            page.rendererObject = texture;
            page.width = texture.GetWidth();
            page.height = texture.GetHeight();
        }

        public void Unload(Object texture)
        {
            // ((Image)texture).Dispose();  // todo or not ?
        }
    }

    static class Util
    {

        static public Image LoadTexture(String path)
        {
            try
            {
                return Graphics.NewImage(path);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading texture file: " + path, ex);
            }
        }

        public static byte[] ReadFully(System.IO.Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        static public Image LoadTexture(System.IO.Stream input)
        {
            var fd = FileSystem.NewFileData(ReadFully(input), Guid.NewGuid().ToString());
            var idata = Image.NewImageData(fd);
            return Graphics.NewImage(idata);
        }
    }
}
