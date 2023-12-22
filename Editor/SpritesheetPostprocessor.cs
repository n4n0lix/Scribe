using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Finity
{
    /// <summary>
    /// A custom asset postprocessor for Sprite sheets.
    /// </summary>
    public class SpritesheetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Represents a sprite definition from a Figma export.
        /// </summary>
        [Serializable]
        private class FigmaSprite
        {
            [SerializeField] public string name;
            [SerializeField] public float x;
            [SerializeField] public float y;
            [SerializeField] public float width;
            [SerializeField] public float height;

            /// <summary>
            /// Converts FigmaSprite data to a Unity Rect based on the given texture.
            /// </summary>
            public Rect ToSpriteRect(Texture2D texture)
            {
                return new Rect(
                    Mathf.Clamp(x, 0, texture.width),
                    Mathf.Clamp(texture.height - (y + height), 0, texture.height),
                    Mathf.Clamp(width, 0, texture.width),
                    Mathf.Clamp(height, 0, texture.height)
                );
            }
        }

        [Serializable]
        private class SpriteSheetFile
        {
            [SerializeField]
            public FigmaSprite[] spriteRects;
        }

        /// <summary>
        /// Handles the processing of a texture after importing.
        /// </summary>
        private void OnPostprocessTexture(Texture2D texture)
        {
            var jsonFilePath = GetJsonFilePath();
            if (!File.Exists(jsonFilePath))
            {
                //Debug.Log($"No spritesheet JSON file found: {jsonFilePath}");
                return;
            }

            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning("Spritesheet JSON file found, but texture is not in SpriteImportMode.Multiple.");
                return;
            }

            var json = File.ReadAllText(jsonFilePath);
            var figma = JsonUtility.FromJson<SpriteSheetFile>(json);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(assetImporter);
            dataProvider.InitSpriteEditorDataProvider();

            var sprites = dataProvider.GetSpriteRects().ToList();
            foreach (var figmaSprite in figma.spriteRects)
            {
                var spriteRect = sprites.FirstOrDefault(s => s.name == figmaSprite.name);
                if (spriteRect != null)
                {
                    spriteRect.rect = figmaSprite.ToSpriteRect(texture);
                }
                else
                {
                    var newSprite = new SpriteRect()
                    {
                        spriteID = GUID.Generate(),
                        name = figmaSprite.name,
                        rect = figmaSprite.ToSpriteRect(texture),
                        pivot = Vector2.one * 0.5f
                    };
                    sprites.Add(newSprite);

                    // Register the new Sprite Rect's name and GUID with the ISpriteNameFileIdDataProvider
                    var spriteNameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                    var nameFileIdPairs = spriteNameFileIdDataProvider.GetNameFileIdPairs().ToList();
                    nameFileIdPairs.Add(new SpriteNameFileIdPair(newSprite.name, newSprite.spriteID));
                    spriteNameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
                }

                Debug.Log($"imported sprite `{figmaSprite.name}`");
            }

            dataProvider.SetSpriteRects(sprites.ToArray());
            dataProvider.Apply();
        }

        /// <summary>
        /// Gets the path of the associated JSON atlas file.
        /// </summary>
        private string GetJsonFilePath()
        {
            var fileName = Path.GetFileName(assetPath);
            var fileNameWOExt = Path.GetFileNameWithoutExtension(assetImporter.assetPath);

            var atlasFilePath = assetPath;
            atlasFilePath = atlasFilePath.Replace(fileName, "");
            atlasFilePath += fileNameWOExt + ".atlas.json";

            return atlasFilePath;
        }
    }
}