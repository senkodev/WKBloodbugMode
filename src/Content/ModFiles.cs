using System.IO;
using UnityEngine;

namespace BloodbugMode
{
    internal static class ModFiles
    {
        private static readonly string Folder = Path.GetDirectoryName(typeof(ModFiles).Assembly.Location);

        public static byte[] Read(string fileName)
        {
            return File.ReadAllBytes(Path.Combine(Folder, fileName));
        }

        public static Sprite LoadSprite(string fileName, string spriteName)
        {
            var texture = Keep(new Texture2D(2, 2, TextureFormat.RGBA32, false));
            texture.name = spriteName;
            texture.LoadImage(Read(fileName));
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            Sprite sprite = Keep(Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f));
            sprite.name = spriteName;
            return sprite;
        }

        public static Sprite WhitePixel()
        {
            var texture = Keep(new Texture2D(1, 1, TextureFormat.RGBA32, false));
            texture.name = "Bloodbug_Pixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Keep(Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f));
        }

        // https://docs.unity3d.com/ScriptReference/HideFlags.DontUnloadUnusedAsset.html
        public static T Keep<T>(T asset) where T : Object
        {
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return asset;
        }
    }
}
