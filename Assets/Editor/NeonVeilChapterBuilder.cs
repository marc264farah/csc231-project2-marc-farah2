using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonVeil.Editor
{
    public static partial class NeonVeilChapterBuilder
    {
        public const string ScenePath = "Assets/Scenes/NeonVeilChapter1.unity";
        public const string BuildPath = "Builds/NeonVeilChapter1/NeonVeil.exe";

        private const string SpritePath = "Assets/Sprites/Prototype/PrototypeSquare.png";
        private const string MaterialPath = "Assets/Materials/PrototypeNoFriction.physicsMaterial2D";
        private const string InputPath = "Assets/Settings/NeonVeilChapter1.inputactions";
        private const string UIInputPath = "Assets/Settings/InputSystem_Actions.inputactions";
        private const int ActorLayer = 2;
        private const int WorldLayers = 1 << 0;
        private const int ActorLayers = 1 << ActorLayer;
        private static readonly Color Ink = new Color32(6, 13, 27, 255);
        private static readonly Color TextLight = new Color32(231, 245, 255, 255);
        private static readonly Color Cyan = new Color32(67, 226, 255, 255);
        private static readonly Color Copper = new Color32(255, 173, 85, 255);
        private static readonly Color Violet = new Color32(186, 155, 255, 255);

        [MenuItem("NEON VEIL/Create Chapter 1")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                Debug.LogWarning("Open the existing Chapter 1 scene.");
                return;
            }

            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            PhysicsMaterial2D physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
            if (input == null || sprite == null || physicsMaterial == null)
            {
                Debug.LogError("Chapter 1 requires the chapter input asset, prototype square and physics material.");
                return;
            }

            RuntimeAnimatorController animator = NeonPresentationAssets.GetSubjectController();
            Sprite blade = NeonPresentationAssets.GetBladeArc();
            Scene priorScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                GameObject systems = new GameObject("Chapter Systems");
                ChapterGame game = systems.AddComponent<ChapterGame>();
                Transform startPoint = Group("Awakening Spawn", systems.transform);
                startPoint.position = new Vector2(2f, 0.92f);

                Subject09Controller player = CreatePlayer(game, startPoint.position, input, sprite, physicsMaterial, animator, blade);
                FollowCamera2D camera = CreateCamera(player.transform);
                NeonPresentationAssets.ConfigureAudio(new GameObject("Music and Sound"));
                ChapterUI ui = CreateUI(game, player);
                CreateBackground(camera.transform, sprite);
                CreateRoute(game, player, sprite, physicsMaterial);
                Set(game, "inputActions", input, "player", player, "startPoint", startPoint, "cameraFollow", camera, "ui", ui);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save Chapter 1.");

                AssetDatabase.SaveAssets();
                Debug.Log("Saved NEON VEIL Chapter 1 — Dead District.");
            }
            catch
            {
                EditorSceneManager.CloseScene(scene, true);
                if (priorScene.IsValid()) SceneManager.SetActiveScene(priorScene);
                throw;
            }
        }

        [MenuItem("NEON VEIL/Build Chapter 1 for Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Create the Chapter 1 scene before building.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, BuildPath,
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException($"Chapter 1 Windows build: {report.summary.result}.");

            Debug.Log($"Windows build succeeded. {report.summary.totalSize:N0} bytes at {report.summary.outputPath}");
        }

        private static Subject09Controller CreatePlayer(ChapterGame game, Vector2 spawn, InputActionAsset input,
            Sprite sprite, PhysicsMaterial2D material, RuntimeAnimatorController controller, Sprite blade)
        {
            GameObject actor = new GameObject("Subject 09");
            actor.layer = ActorLayer;
            actor.transform.position = spawn;
            Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D capsule = actor.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.8f, 1.8f);
            capsule.sharedMaterial = material;

            Transform visual = Group("Visual", actor.transform);
            Transform pose = Group("Pose", visual);
            SpriteRenderer armor = Block(pose, "Graphite armor", new Vector2(-0.07f, -0.18f),
                new Vector2(0.63f, 0.94f), Color.white, sprite, 10);
            Block(pose, "Helmet", new Vector2(-0.03f, 0.49f), new Vector2(0.62f, 0.45f),
                new Color32(103, 145, 170, 255), sprite, 11);
            SpriteRenderer eye = Block(pose, "Blue optical implant", new Vector2(0.21f, 0.54f),
                new Vector2(0.19f, 0.08f), Cyan, sprite, 12);
            Block(pose, "Cybernetic arm", new Vector2(0.30f, -0.18f), new Vector2(0.22f, 0.73f),
                new Color32(47, 134, 216, 255), sprite, 12).transform.localRotation = Quaternion.Euler(0, 0, 11);
            Block(pose, "Boot", new Vector2(-0.19f, -0.73f), new Vector2(0.23f, 0.27f), Ink, sprite, 11);
            Block(pose, "Boot", new Vector2(0.12f, -0.73f), new Vector2(0.23f, 0.27f), Ink, sprite, 11);

            Animator animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            Subject09Presentation presentation = actor.AddComponent<Subject09Presentation>();
            presentation.Setup(animator, armor, eye, sprite, blade);
            NeonPresentationAssets.ApplyEffectArt(presentation);
            Subject09Controller player = actor.AddComponent<Subject09Controller>();
            Subject09Combat combat = actor.AddComponent<Subject09Combat>();
            Set(player, "inputActions", input, "game", game, "visual", visual, "presentation", presentation,
                "groundLayers", WorldLayers);
            Set(combat, "presentation", presentation, "enemyLayers", ActorLayers, "obstacleLayers", WorldLayers);
            return player;
        }

        private static FollowCamera2D CreateCamera(Transform target)
        {
            GameObject cameraObject = new GameObject("Chapter Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(4f, 2.7f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            FollowCamera2D follow = cameraObject.AddComponent<FollowCamera2D>();
            follow.SetTarget(target);
            Set(follow, "offset", new Vector2(2.4f, 1.8f), "minimumY", 2.7f);
            return follow;
        }

        private static void CreateBackground(Transform camera, Sprite sprite)
        {
            Transform root = Group("District Parallax");
            Transform far = Group("Far Skyline", root);
            Transform middle = Group("Mid Skyline", root);
            Transform near = Group("Near Infrastructure", root);
            Sprite cityArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/CityMidParallax.png");
            Sprite infrastructureArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/InfrastructureParallax.png");
            Block(far, "Horizon", new Vector2(130, 0), new Vector2(360, 5),
                new Color32(14, 24, 47, 255), sprite, -33);
            Block(far, "Upper haze", new Vector2(130, 5), new Vector2(360, 6),
                new Color32(10, 17, 37, 255), sprite, -34);
            if (cityArt != null && infrastructureArt != null)
            {
                for (int i = 0; i < 22; i++)
                {
                    SpriteRenderer city = Block(middle, "Painted city facades", new Vector2(-13f + i * 25.5f, 1.35f),
                        Vector2.one, Color.white, cityArt, -26);
                    city.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                    SpriteRenderer supports = Block(near, "Painted infrastructure", new Vector2(-13f + i * 25.5f, 1.2f),
                        Vector2.one, Color.white, infrastructureArt, -23);
                    supports.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                }
            }
            else for (int i = 0; i < 25; i++)
            {
                float x = -20f + i * 11f;
                float height = 2.5f + i % 5 * 0.8f;
                Block(far, "Skyline slab", new Vector2(x, -1.1f + height * 0.5f),
                    new Vector2(7f, height), new Color32(21, 32, 61, 255), sprite, -32);
                Block(far, "Roof signal", new Vector2(x + 1.9f, height - 0.92f),
                    new Vector2(0.13f, 0.22f), new Color32(95, 85, 134, 255), sprite, -31);
            }
            if (cityArt == null || infrastructureArt == null) for (int i = 0; i < 38; i++)
            {
                float x = -19f + i * 11.5f;
                float height = 3.6f + i % 4 * 1.05f;
                Block(middle, "Building silhouette", new Vector2(x, -2f + height * 0.5f),
                    new Vector2(6.2f + i % 3, height), new Color32(17, 34, 54, 255), sprite, -26);
                for (int w = 0; w < 3; w++)
                    Block(middle, "Dark window strip", new Vector2(x + (w - 1) * 1.4f, 0.85f + i % 3 * 0.48f),
                        new Vector2(0.6f, 0.08f), new Color32(35, 77, 100, 255), sprite, -25);
            }
            if (cityArt == null || infrastructureArt == null) for (int i = 0; i < 36; i++)
            {
                float x = -15f + i * 17f;
                Block(near, "Support truss", new Vector2(x, 0.5f), new Vector2(0.44f, 8.5f),
                    new Color32(23, 43, 57, 255), sprite, -17);
                Block(near, "Overhead conduit", new Vector2(x + 7.5f, 4.35f), new Vector2(16.8f, 0.16f),
                    new Color32(24, 49, 65, 255), sprite, -18);
            }
            root.gameObject.AddComponent<NeonParallax>().Setup(camera, far, middle, near);
        }

        private static void CreateRoute(ChapterGame game, Subject09Controller player, Sprite sprite, PhysicsMaterial2D material)
        {
            Transform route = Group("Dead District");
            Transform awakening = Group("01 Awakening", route);
            CreateDecks(awakening, sprite, Cyan, -8, 23, 25.7f, 64, 66.9f, 128);
            Raised(awakening, 9, 13, 0.9f, Cyan, sprite);
            Raised(awakening, 48, 54, 1.05f, Cyan, sprite);
            Raised(awakening, 96, 103, 1.12f, Cyan, sprite);
            Cell(awakening, game, 12, 1.82f, sprite);
            Cell(awakening, game, 47, 1.05f, sprite);
            Cell(awakening, game, 100, 2.05f, sprite);
            Enemy(awakening, game, player, 37, ChapterEnemy.EnemyKind.TrainingDrone, sprite, material);
            Enemy(awakening, game, player, 83, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Section(awakening, game, 1, 1.5f, "01  AWAKENING",
                "A / D or arrows to move  •  Hold Space to jump higher  •  Left click to cut", sprite, Cyan);

            Transform metro = Group("02 Abandoned Metro", route);
            CreateDecks(metro, sprite, new Color32(96, 180, 240, 255), 128, 153, 156.2f, 192, 195.4f, 231, 234.2f, 272);
            Section(metro, game, 2, 130, "02  ABANDONED METRO",
                "Jump broken tracks. Sentinels flash amber before striking.", sprite, Cyan);
            Checkpoint(metro, game, 1, 137, 0, sprite);
            Laser(metro, game, 147, sprite, 2.7f);
            Raised(metro, 167, 173, 0.95f, Cyan, sprite);
            Raised(metro, 220, 225, 1f, Cyan, sprite);
            Cell(metro, game, 170, 1.9f, sprite);
            Cell(metro, game, 211, 1.05f, sprite);
            Cell(metro, game, 250, 1.05f, sprite);
            Enemy(metro, game, player, 182, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(metro, game, player, 208, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(metro, game, player, 247, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(metro, game, player, 260, ChapterEnemy.EnemyKind.Sentinel, sprite, material);

            Transform industrial = Group("03 Industrial Sector", route);
            CreateDecks(industrial, sprite, Copper, 272, 303, 305.9f, 347, 350.8f, 386, 389.3f, 424);
            Section(industrial, game, 3, 274, "03  INDUSTRIAL SECTOR",
                "Amber warns of a live beam. Left Shift performs Phase Strike.", sprite, Copper);
            Checkpoint(industrial, game, 2, 280, 0, sprite);
            Laser(industrial, game, 291, sprite, 2.35f);
            Laser(industrial, game, 310, sprite, 2.45f);
            Laser(industrial, game, 320, sprite, 2.05f);
            Laser(industrial, game, 365, sprite, 1.4f);
            Laser(industrial, game, 399, sprite, 2.25f);
            Raised(industrial, 333, 338, 1.05f, Copper, sprite);
            Raised(industrial, 378, 382, 1.05f, Copper, sprite);
            Cell(industrial, game, 334.7f, 1.98f, sprite);
            Cell(industrial, game, 371, 1.05f, sprite);
            Cell(industrial, game, 411, 1.05f, sprite);
            Enemy(industrial, game, player, 357, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(industrial, game, player, 409, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(industrial, game, player, 340, ChapterEnemy.EnemyKind.Sentinel, sprite, material);

            Transform quarantine = Group("04 Quarantine Zone", route);
            CreateDecks(quarantine, sprite, Violet, 424, 463, 466.4f, 500, 503.6f, 545, 548.2f, 577);
            Section(quarantine, game, 4, 426, "04  QUARANTINE ZONE",
                "Mimics wake nearby and lunge. Keep space for a counterattack.", sprite, Violet);
            Checkpoint(quarantine, game, 3, 433, 0, sprite);
            Laser(quarantine, game, 472, sprite, 2.5f);
            Enemy(quarantine, game, player, 448, ChapterEnemy.EnemyKind.Mimic, sprite, material);
            Enemy(quarantine, game, player, 481, ChapterEnemy.EnemyKind.Mimic, sprite, material);
            Enemy(quarantine, game, player, 519, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(quarantine, game, player, 533, ChapterEnemy.EnemyKind.Mimic, sprite, material);
            Enemy(quarantine, game, player, 561, ChapterEnemy.EnemyKind.Mimic, sprite, material);
            Laser(quarantine, game, 556, sprite, 1.85f);
            Raised(quarantine, 455, 459, 1.05f, Violet, sprite);
            Raised(quarantine, 493, 498, 1f, Violet, sprite);
            Cell(quarantine, game, 457, 1.98f, sprite);
            Cell(quarantine, game, 510, 1.05f, sprite);
            Cell(quarantine, game, 567, 1.05f, sprite);

            Transform tower = Group("05 Extraction Tower", route);
            CreateDecks(tower, sprite, Cyan, 577, 611, 614.3f, 640, 643.1f, 710);
            Section(tower, game, 5, 579, "05  EXTRACTION TOWER",
                "Defeat 7 real enemies, then climb to the cyan extraction beacon.", sprite, Cyan);
            Checkpoint(tower, game, 4, 586, 0, sprite);
            Laser(tower, game, 594, sprite, 2.7f);
            Laser(tower, game, 604, sprite, 1.55f);
            Enemy(tower, game, player, 625, ChapterEnemy.EnemyKind.Sentinel, sprite, material);
            Enemy(tower, game, player, 648, ChapterEnemy.EnemyKind.Mimic, sprite, material);
            Raised(tower, 659, 675, 1.05f, Cyan, sprite);
            Raised(tower, 671, 687, 2.15f, Cyan, sprite);
            Raised(tower, 683, 710, 3.2f, Cyan, sprite);
            Cell(tower, game, 664, 1.98f, sprite);
            Cell(tower, game, 677, 3.1f, sprite);
            Cell(tower, game, 690, 4.15f, sprite);
            Extraction(tower, game, 704, 3.2f, sprite);
            Raised(tower, 709.6f, 711.1f, 6.4f, Cyan, sprite);

            float[] lampPositions = { 10f, 61f, 113f, 142f, 252f, 287f, 414f, 439f, 568f, 587f, 698f };
            foreach (float x in lampPositions)
            {
                Transform section = x < 128f ? awakening : x < 272f ? metro :
                    x < 424f ? industrial : x < 577f ? quarantine : tower;
                StreetLight(section, x);
            }

            CeilingBorder(awakening, 0, -8f, 128f);
            CeilingBorder(metro, 1, 128f, 272f);
            CeilingBorder(industrial, 2, 272f, 424f);
            CeilingBorder(quarantine, 3, 424f, 577f);
            CeilingBorder(tower, 4, 577f, 711f);
        }

        private static void CreateDecks(Transform parent, Sprite sprite, Color accent, params float[] edges)
        {
            for (int i = 0; i < edges.Length; i += 2)
            {
                float left = edges[i];
                float right = edges[i + 1];
                Deck(parent, left, right, 0f, 3.2f, accent, sprite);
                if (i + 2 >= edges.Length) continue;
                Block(parent, "Gap marker", new Vector2(right - 0.48f, -0.16f), new Vector2(0.72f, 0.13f), Copper, sprite, 3);
                Block(parent, "Gap marker", new Vector2(edges[i + 2] + 0.48f, -0.16f), new Vector2(0.72f, 0.13f), Copper, sprite, 3);
            }
        }

        private static void Raised(Transform parent, float left, float right, float top, Color accent, Sprite sprite)
        {
            Deck(parent, left, right, top, top + 0.1f, accent, sprite);
        }

        private static void Deck(Transform parent, float left, float right, float top, float height, Color accent, Sprite sprite)
        {
            float length = right - left;
            float center = (left + right) * 0.5f;
            SpriteRenderer surface = Block(parent, "Solid deck", new Vector2(center, top - height * 0.5f),
                new Vector2(length, height), new Color32(29, 47, 62, 255), sprite, 1);
            surface.gameObject.AddComponent<BoxCollider2D>();
            string groundName = parent.name.StartsWith("01") ? "AwakeningDeck" :
                parent.name.StartsWith("02") ? "MetroDeck" :
                parent.name.StartsWith("03") ? "IndustrialDeck" :
                parent.name.StartsWith("04") ? "QuarantineDeck" : "ExtractionDeck";
            Sprite groundArt = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Ground/{groundName}.png");
            if (groundArt != null)
            {
                surface.enabled = false;
                SpriteRenderer painted = Block(parent, "Painted deck face", surface.transform.position,
                    Vector2.one, Color.white, groundArt, 2);
                painted.drawMode = top > 0.01f ? SpriteDrawMode.Sliced : SpriteDrawMode.Tiled;
                painted.size = new Vector2(length, height);
            }
            else
                Block(parent, "Deck edge", new Vector2(center, top - 0.06f), new Vector2(length, 0.12f),
                    Color.Lerp(new Color32(49, 72, 88, 255), accent, 0.38f), sprite, 2);
            if (groundArt != null || top > 0.01f) return;
            for (float x = left + 4f; x < right - 2.5f; x += 10.5f)
            {
                Block(parent, "Maintenance seam", new Vector2(x, -0.62f), new Vector2(3.25f, 0.13f),
                    new Color32(14, 28, 43, 255), sprite, 2);
                Block(parent, "Guide light", new Vector2(x + 1.18f, -0.62f), new Vector2(0.26f, 0.095f),
                    accent, sprite, 3);
            }
        }

        private static void Cell(Transform parent, ChapterGame game, float x, float y, Sprite sprite)
        {
            Transform cell = Group("Energy Cell", parent);
            cell.position = new Vector2(x, y);
            Transform visual = Group("Cell Display", cell);
            SpriteRenderer shell = Block(visual, "Outer shell", Vector2.zero, new Vector2(0.40f, 0.40f), Cyan, sprite, 6);
            shell.transform.localRotation = Quaternion.Euler(0, 0, 45);
            SpriteRenderer core = Block(visual, "Energy core", Vector2.zero,
                new Vector2(0.17f, 0.17f), TextLight, sprite, 7);
            core.transform.localRotation = Quaternion.Euler(0, 0, 45);
            Sprite cellArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/EnergyCell_Art.png");
            if (cellArt != null)
            {
                shell.name = "Energy Cell art";
                shell.sprite = cellArt;
                shell.color = Color.white;
                shell.transform.localRotation = Quaternion.identity;
                shell.transform.localScale = Vector3.one * 0.8f;
                core.enabled = false;
            }
            CircleCollider2D collider = cell.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.38f;
            collider.isTrigger = true;
            Set(cell.gameObject.AddComponent<EnergyCell>(), "game", game, "visual", visual);
        }

        private static void Enemy(Transform parent, ChapterGame game, Subject09Controller player, float x,
            ChapterEnemy.EnemyKind kind, Sprite sprite, PhysicsMaterial2D material)
        {
            bool mimic = kind == ChapterEnemy.EnemyKind.Mimic;
            bool trainer = kind == ChapterEnemy.EnemyKind.TrainingDrone;
            Transform actor = Group(kind.ToString(), parent);
            actor.position = new Vector2(x, 0.65f);
            actor.gameObject.layer = ActorLayer;
            Rigidbody2D body = actor.gameObject.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            CapsuleCollider2D capsule = actor.gameObject.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(1f, 1.3f);
            capsule.sharedMaterial = material;

            Transform visual = Group("Visual", actor);
            Color shellColor = mimic ? new Color32(103, 63, 157, 255) : new Color32(156, 106, 63, 255);
            SpriteRenderer shell = Block(visual, "Shell", new Vector2(0f, -0.13f),
                mimic ? new Vector2(1.2f, 0.55f) : new Vector2(0.87f, 0.86f), shellColor, sprite, 8);
            SpriteRenderer housing = Block(visual, mimic ? "Crest" : "Sensor housing", new Vector2(0.19f, 0.35f),
                mimic ? new Vector2(0.42f, 0.37f) : new Vector2(0.57f, 0.29f), shellColor, sprite, 9);
            housing.transform.localRotation = Quaternion.Euler(0, 0, mimic ? 38 : 0);
            SpriteRenderer sensor = Block(visual, "Sensor", new Vector2(0.38f, 0.33f), new Vector2(0.14f, 0.11f),
                mimic ? Violet : Copper, sprite, 10);
            SpriteRenderer tread = Block(visual, mimic ? "Front claw" : "Tread", new Vector2(0f, -0.53f),
                new Vector2(0.88f, 0.21f), Ink, sprite, 8);
            if (trainer)
            {
                Sprite droneArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/TrainingDrone_Still.png");
                if (droneArt != null)
                {
                    shell.enabled = false;
                    housing.enabled = false;
                    sensor.enabled = false;
                    tread.enabled = false;
                    shell = Block(visual, "Training drone art", Vector2.zero,
                        new Vector2(1.7f, 1.7f), Color.white, droneArt, 9);
                }
                Transform label = Group("Tutorial enemy label", actor);
                label.localPosition = new Vector3(0f, 1.28f, 0f);
                TextMesh text = label.gameObject.AddComponent<TextMesh>();
                text.text = "TUTORIAL ENEMY";
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.045f;
                text.fontSize = 48;
                text.color = Cyan;
                label.GetComponent<MeshRenderer>().sortingOrder = 14;
            }
            else
            {
                string artName = mimic ? "Mimic_WalkAttackRebuilt" : "Sentinel_Rebuilt";
                Sprite enemyArt = mimic ? AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Mimic_WalkAttackRebuilt.png")
                    .OfType<Sprite>().FirstOrDefault(s => s.name == "Mimic_WalkAttackRebuilt_0") :
                    AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Sentinel_Rebuilt.png")
                        .OfType<Sprite>().FirstOrDefault(s => s.name == "Sentinel_Rebuilt_0");
                if (enemyArt != null)
                {
                    shell.enabled = false;
                    housing.enabled = false;
                    sensor.enabled = false;
                    tread.enabled = false;
                    shell = Block(visual, mimic ? "Mimic still art" : "Sentinel still art",
                        new Vector2(0f, mimic ? -0.20f : 0.16f),
                        Vector2.one * (mimic ? 1.75f / enemyArt.bounds.size.x : 1.65f / enemyArt.bounds.size.x), Color.white, enemyArt, 9);
                    shell.flipX = false;
                }
            }
            Sprite[] crawlFrames = null;
            Sprite[] sentinelFrames = null;
            if (!mimic && !trainer)
                sentinelFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Sentinel_Rebuilt.png")
                    .OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (mimic)
            {
                crawlFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Mimic_WalkAttackRebuilt.png")
                    .OfType<Sprite>().OrderBy(s => s.name).ToArray();
                if (crawlFrames.Length == 8)
                {
                    shell.sprite = crawlFrames[0];
                    shell.transform.localScale = Vector3.one * (1.75f / crawlFrames[0].bounds.size.x);
                }
            }
            SpriteRenderer strikeEffect = null;
            if (!trainer)
            {
                string effectName = mimic ? "MimicStrike" : "SentinelStrike";
                Sprite strikeArt = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Effects/{effectName}.png");
                if (strikeArt != null)
                {
                    float width = mimic ? 2.2f : 2.8f;
                    float scale = width / strikeArt.bounds.size.x;
                    strikeEffect = Block(visual, "Visible attack", new Vector2(mimic ? 0.85f : 1.5f, -0.05f),
                        new Vector2(scale, scale), Color.white, strikeArt, 12);
                    strikeEffect.enabled = false;
                }
            }
            SpriteRenderer warning = Block(actor, "Attack indicator", new Vector2(0f, 1.04f), new Vector2(0.16f, 0.36f), Copper, sprite, 12);
            warning.enabled = false;
            Block(actor, "Health tray", new Vector2(0f, 0.76f), new Vector2(0.84f, 0.12f), Ink, sprite, 10);
            SpriteRenderer fill = Block(actor, "Health fill", new Vector2(0f, 0.76f), new Vector2(0.77f, 0.06f),
                mimic ? Violet : Copper, sprite, 11);

            EnemyHealth health = actor.gameObject.AddComponent<EnemyHealth>();
            Set(health, "maximumHealth", trainer ? 2 : 3, "bodySprite", shell, "healthFill", fill.transform);
            ChapterEnemy enemy = actor.gameObject.AddComponent<ChapterEnemy>();
            Set(enemy, "kind", kind, "game", game, "player", player, "visual", visual, "warning", warning,
                "strikeEffect", strikeEffect,
                "groundLayers", WorldLayers);
            if (sentinelFrames != null && sentinelFrames.Length == 8)
                Set(enemy, "sentinelBody", shell, "sentinelFrames", sentinelFrames);
            if (mimic && crawlFrames != null && crawlFrames.Length == 8)
            {
                SerializedObject animation = new SerializedObject(enemy);
                animation.FindProperty("crawlBody").objectReferenceValue = shell;
                SerializedProperty frames = animation.FindProperty("crawlFrames");
                frames.arraySize = crawlFrames.Length;
                for (int i = 0; i < crawlFrames.Length; i++)
                    frames.GetArrayElementAtIndex(i).objectReferenceValue = crawlFrames[i];
                animation.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Laser(Transform parent, ChapterGame game, float x, Sprite sprite, float safeTime)
        {
            Transform root = Group("Security Laser", parent);
            root.position = new Vector2(x, 1.6f);
            SpriteRenderer beam = Block(root, "Laser beam", Vector2.zero, new Vector2(0.22f, 3.2f),
                new Color32(35, 83, 112, 150), sprite, 7);
            Sprite housing = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/LaserEmitter.png");
            if (housing != null)
            {
                float scale = 1.05f / housing.bounds.size.x;
                Block(root, "Painted emitter", new Vector2(0f, 1.64f), Vector2.one * scale,
                    Color.white, housing, 8);
                SpriteRenderer receiver = Block(root, "Painted receiver", new Vector2(0f, -1.52f),
                    Vector2.one * scale, Color.white, housing, 8);
                receiver.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                Block(root, "Emitter", new Vector2(0f, 1.64f), new Vector2(0.56f, 0.30f), Copper, sprite, 8);
                Block(root, "Receiver", new Vector2(0f, -1.52f), new Vector2(0.57f, 0.17f), Copper, sprite, 8);
            }
            BoxCollider2D trigger = root.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.26f, 3.2f);
            Set(root.gameObject.AddComponent<SecurityLaser>(), "game", game, "beam", beam, "safeTime", safeTime);
        }

        private static void Section(Transform parent, ChapterGame game, int number, float x, string title, string instructions,
            Sprite sprite, Color accent)
        {
            if (number > 1)
            {
                string path = "Assets/Sprites/Backgrounds/SectionTransitionFrames.png";
                Sprite frame = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .FirstOrDefault(s => s.name == "SectionTransitionFrames_" + (number - 2));
                if (frame != null)
                {
                    float scale = 5.2f / frame.bounds.size.y;
                    Block(parent, "Painted location transition", new Vector2(x - 2f, 2.6f),
                        Vector2.one * scale, Color.white, frame, -6);
                }
            }
            Transform marker = Group("Section threshold", parent);
            marker.position = new Vector2(x, 1.8f);
            BoxCollider2D collider = marker.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.2f, 5f);
            Set(marker.gameObject.AddComponent<ChapterMarker>(), "kind", ChapterMarker.MarkerKind.Section, "game", game,
                "number", number, "sectionName", title, "instruction", instructions);
        }

        private static void StreetLight(Transform parent, float x)
        {
            Sprite lamp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/DistrictStreetLamp.png");
            if (lamp == null) return;
            float scale = 3.8f / lamp.bounds.size.y;
            Block(parent, "Painted street light", new Vector2(x, 1.9f),
                Vector2.one * scale, Color.white, lamp, -6);
        }

        private static void CeilingBorder(Transform parent, int section, float left, float right)
        {
            string path = "Assets/Sprites/Backgrounds/SectionCeilingBorders.png";
            Sprite border = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .FirstOrDefault(s => s.name == "SectionCeiling_" + section);
            if (border == null) return;
            float width = (right - left) / 5f;
            for (int tile = 0; tile < 5; tile++)
            {
                float x = left + width * (tile + 0.5f);
                Block(parent, "Painted ceiling border " + (tile + 1), new Vector2(x, 7.85f),
                    new Vector2(width / border.bounds.size.x, 1.42f), Color.white, border, -9);
            }
        }

        private static void Checkpoint(Transform parent, ChapterGame game, int number, float x, float y, Sprite sprite)
        {
            Transform marker = Group("Checkpoint " + number, parent);
            marker.position = new Vector2(x, y + 1.4f);
            Sprite terminal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/CheckpointTerminal.png");
            SpriteRenderer beacon;
            if (terminal != null)
            {
                float scale = 1.9f / terminal.bounds.size.y;
                beacon = Block(marker, "Painted checkpoint terminal", new Vector2(0f, -0.45f),
                    Vector2.one * scale, Color.white, terminal, 5);
            }
            else
            {
                beacon = Block(marker, "Checkpoint light", new Vector2(0f, 0.88f),
                    new Vector2(0.46f, 0.15f), Cyan, sprite, 5);
                Block(marker, "Checkpoint post", new Vector2(0f, 0.08f), new Vector2(0.18f, 1.6f),
                    new Color32(36, 104, 137, 255), sprite, 3);
            }
            Transform spawn = Group("Respawn", marker);
            spawn.position = new Vector2(x + 0.72f, y + 0.93f);
            BoxCollider2D collider = marker.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.25f, 4f);
            Set(marker.gameObject.AddComponent<ChapterMarker>(), "kind", ChapterMarker.MarkerKind.Checkpoint, "game", game,
                "number", number, "spawnPoint", spawn, "beacon", beacon);
        }

        private static void Extraction(Transform parent, ChapterGame game, float x, float floor, Sprite sprite)
        {
            Transform extraction = Group("Extraction beacon", parent);
            extraction.position = new Vector2(x, floor + 1.55f);
            Block(extraction, "Beacon glow", Vector2.zero, new Vector2(2.5f, 3.15f),
                new Color32(38, 212, 241, 43), sprite, 5);
            Sprite gate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/ExtractionGate.png");
            if (gate != null)
            {
                float scale = 3.2f / gate.bounds.size.y;
                Block(extraction, "Painted extraction gate", Vector2.zero, Vector2.one * scale,
                    Color.white, gate, 6);
            }
            else
            {
                Block(extraction, "Left guide", new Vector2(-1.18f, 0f), new Vector2(0.12f, 3.1f), Cyan, sprite, 7);
                Block(extraction, "Right guide", new Vector2(1.18f, 0f), new Vector2(0.12f, 3.1f), Cyan, sprite, 7);
                Block(extraction, "Header", new Vector2(0f, 1.55f), new Vector2(2.5f, 0.16f), Cyan, sprite, 7);
            }
            BoxCollider2D collider = extraction.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2.4f, 3.2f);
            Set(extraction.gameObject.AddComponent<ChapterMarker>(), "kind", ChapterMarker.MarkerKind.Extraction, "game", game);
        }

    }
}
