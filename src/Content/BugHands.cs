using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BloodbugMode
{
    internal static class BugHands
    {
        private const string Id = "senkodev-bloodbug-hands";
        private const int CellSize = 128;
        private const int Columns = 10;
        private const string PointerPrefix = "interact-";

        private static Cosmetic_HandItem cosmetic;
        private static bool failed;

        public static Cosmetic_HandItem Cosmetic
        {
            get
            {
                if (cosmetic == null && !failed)
                {
                    try
                    {
                        cosmetic = Build();
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"failed to build the bug hands: {e}");
                    }
                    failed = cosmetic == null;
                }
                return cosmetic;
            }
        }

        private static Cosmetic_HandItem Build()
        {
            string[] names = Encoding.UTF8.GetString(ModFiles.Read("bug_hands.txt"))
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            atlas.LoadImage(ModFiles.Read("bug_hands.png"));

            var data = new Cosmetic_HandItem.Cosmetic_HandItem_Data
            {
                id = Id,
                cosmeticName = "Bloodbug",
                author = "senkodev",
                description = "",
                unlock = "",
                globalMaterialBase = "",
                flags = new List<string>(),
                emotes = new List<Cosmetic_HandItem.Cosmetic_HandItem_Data.HandEmote>(),
                palettes = new List<Cosmetic_HandItem.Cosmetic_HandItem_Data.ColorPalette>(),
                allowedSpecialtyPoses = new List<string>(),
                globalSecondary = new List<Cosmetic_HandItem.SwapSprite.SecondaryTextures>(),
                swapSprites = new List<Cosmetic_HandItem.SwapSprite>(),
                interactSwaps = new List<Cosmetic_HandItem.InteractSwap>()
            };
            var swapsByName = new Dictionary<string, List<Cosmetic_HandItem.SwapSprite>>();

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                int x = i % Columns * CellSize;
                int y = atlas.height - (i / Columns + 1) * CellSize;

                Sprite sprite = CutSprite(atlas, x, y, name);
                if (name.StartsWith(PointerPrefix))
                {
                    data.interactSwaps.Add(new Cosmetic_HandItem.InteractSwap
                    {
                        spriteName = name,
                        replacementSpriteName = name,
                        replacementSprite = sprite
                    });
                    continue;
                }

                var swap = new Cosmetic_HandItem.SwapSprite
                {
                    spriteName = name,
                    replacementSpriteNames = new List<string> { name },
                    replacementSprites = new List<Sprite> { sprite },
                    secondaryTextures = new List<Cosmetic_HandItem.SwapSprite.SecondaryTextures>(),
                    materialBase = ""
                };
                data.swapSprites.Add(swap);
                swapsByName[name] = new List<Cosmetic_HandItem.SwapSprite> { swap };
            }
            UnityEngine.Object.Destroy(atlas);

            Cosmetic_HandItem result = ModFiles.Keep(ScriptableObject.CreateInstance<Cosmetic_HandItem>());
            result.name = "Cosmetic_Hand_Senkodev_Bloodbug";
            result.cosmeticData = data;
            result.cosmeticInfo = new Cosmetic_Info
            {
                id = Id,
                cosmeticName = data.cosmeticName,
                tag = "hand",
                author = data.author,
                description = "",
                unlock = "",
                flags = data.flags
            };

            SetPrivate(result, "materialDict", new Dictionary<string, Material>());
            SetPrivate(result, "hands", new Dictionary<int, Cosmetic_HandItem.HandMaterials>());
            SetPrivate(result, "swapDict", swapsByName);
            SetPrivate(result, "spriteNames", new Dictionary<Sprite, string>());
            EnsureReturnList();

            Plugin.Log.LogInfo($"bug hands: {data.swapSprites.Count} poses, {data.interactSwaps.Count} pointers");
            return result;
        }

        private static Sprite CutSprite(Texture2D atlas, int x, int y, string name)
        {
            var texture = ModFiles.Keep(new Texture2D(CellSize, CellSize, TextureFormat.RGBA32, false));
            texture.name = "Bloodbug_" + name;
            // https://docs.unity3d.com/ScriptReference/Texture2D.GetPixels.html
            texture.SetPixels(atlas.GetPixels(x, y, CellSize, CellSize));
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, true);

            var rect = new Rect(0f, 0f, CellSize, CellSize);
            Sprite sprite = ModFiles.Keep(Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect));
            sprite.name = name;
            return sprite;
        }

        private static void EnsureReturnList()
        {
            FieldInfo field = RequireField("returnSprites");
            if (field.GetValue(null) == null)
            {
                field.SetValue(null, (IList)Activator.CreateInstance(field.FieldType));
            }
        }

        private static void SetPrivate(Cosmetic_HandItem target, string name, object value)
        {
            RequireField(name).SetValue(target, value);
        }

        private static FieldInfo RequireField(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(Cosmetic_HandItem), name);
            if (field == null)
            {
                throw new MissingFieldException(nameof(Cosmetic_HandItem), name);
            }
            return field;
        }
    }
}
