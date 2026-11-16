using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonVeil.Editor
{
    public static class NeonVeilPrototypeBuilder
    {
        private const string ScenePath = "Assets/Scenes/NeonVeilPrototype.unity";

        [MenuItem("NEON VEIL/Create Movement Prototype")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                Debug.LogWarning("Prototype already exists. Open it instead; no scene was overwritten.");
                return;
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            Directory.CreateDirectory("Assets/Sprites/Prototype");
            const string texturePath = "Assets/Sprites/Prototype/PrototypeSquare.png";
            if (!File.Exists(texturePath))
            {
                Texture2D texture = new Texture2D(8, 8);
                Color[] pixels = new Color[64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 8;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            Color floor = new Color(0.12f, 0.22f, 0.29f);
            Block("Awakening Floor", new Vector2(5, -0.5f), new Vector2(22, 1), floor, sprite, true);
            Block("Metro Landing", new Vector2(22, -0.5f), new Vector2(8, 1), floor, sprite, true);
            Block("Final Landing", new Vector2(36, -0.5f), new Vector2(14, 1), floor, sprite, true);
            Block("Training Step", new Vector2(7, 0.5f), new Vector2(3, 1), floor, sprite, true);
            Block("Training Platform", new Vector2(12, 1.6f), new Vector2(3, 0.5f), floor, sprite, true);
            Block("Gap Platform", new Vector2(27.5f, 1), new Vector2(2, 0.5f), floor, sprite, true);
            Block("Extraction Marker", new Vector2(40, 1.5f), new Vector2(0.4f, 3), Color.cyan, sprite, false);
            GameObject player = new GameObject("Subject 09");
            player.layer = 2; // Built-in Ignore Raycast layer; ground checks only inspect Default.
            player.transform.position = new Vector3(0, 1.1f, 0);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);
            PhysicsMaterial2D material = new PhysicsMaterial2D("Prototype No Friction");
            material.friction = 0;
            material.bounciness = 0;
            const string materialPath = "Assets/Materials/PrototypeNoFriction.physicsMaterial2D";
            if (!File.Exists(materialPath)) AssetDatabase.CreateAsset(material, materialPath);
            else Object.DestroyImmediate(material);
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            GameObject armor = Block("Armor Placeholder", Vector2.zero, new Vector2(0.8f, 1.8f),
                new Color(0.35f, 0.5f, 0.6f), sprite, false);
            armor.transform.SetParent(visual.transform, false);
            GameObject eye = Block("Blue Optical Implant", new Vector2(0.24f, 0.55f),
                new Vector2(0.2f, 0.15f), Color.cyan, sprite, false);
            eye.transform.SetParent(visual.transform, false);
            eye.GetComponent<SpriteRenderer>().sortingOrder = 1;
            Subject09Movement movement = player.AddComponent<Subject09Movement>();
            SerializedObject serialized = new SerializedObject(movement);
            serialized.FindProperty("visual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject cameraObject = new GameObject("Prototype Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.06f);
            cameraObject.transform.position = new Vector3(2, 2, -10);
            cameraObject.AddComponent<FollowCamera2D>().SetTarget(player.transform);
            new GameObject("Prototype Instructions", typeof(PrototypeInstructions));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("NEON VEIL prototype created and saved with movement, colliders and camera attached. Open it alone before Play.");
        }

        private static GameObject Block(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, bool solid)
        {
            GameObject block = new GameObject(name);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            if (solid) block.AddComponent<BoxCollider2D>();
            return block;
        }
    }
}
