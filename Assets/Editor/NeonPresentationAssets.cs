using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NeonVeil.Editor
{
    public static class NeonPresentationAssets
    {
        private const string AnimationFolder = "Assets/Animations/Chapter1";
        private const string ControllerPath = AnimationFolder + "/Subject09Placeholder.controller";
        private const string BladePath = "Assets/Sprites/Prototype/BladeArcPlaceholder.png";

        // Every clip moves only the Pose child. Rigidbody and the Visual facing root stay separate.
        public static RuntimeAnimatorController GetSubjectController()
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(AnimationFolder);
            AssetDatabase.Refresh();
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Pose", AnimatorControllerParameterType.Int);
            AnimatorStateMachine states = controller.layers[0].stateMachine;
            states.defaultState = AddPose(states, Subject09Presentation.Pose.Idle, true,
                new PoseKey(0f, 0f, 0f, 1f, 1f, 0f),
                new PoseKey(0.55f, 0f, 0.035f, 0.98f, 1.03f, -1.5f),
                new PoseKey(1.10f, 0f, 0f, 1f, 1f, 0f));
            AddPose(states, Subject09Presentation.Pose.Run, true,
                new PoseKey(0f, 0.04f, -0.045f, 1.05f, 0.95f, -10f),
                new PoseKey(0.13f, 0.09f, 0.085f, 0.96f, 1.04f, -15f),
                new PoseKey(0.26f, 0.04f, -0.045f, 1.05f, 0.95f, -10f));
            AddPose(states, Subject09Presentation.Pose.Jump, false,
                new PoseKey(0f, 0f, -0.02f, 1.06f, 0.93f, 6f),
                new PoseKey(0.18f, 0.04f, 0.075f, 0.89f, 1.12f, -7f));
            AddPose(states, Subject09Presentation.Pose.Fall, false,
                new PoseKey(0f, 0.04f, 0.075f, 0.89f, 1.12f, -7f),
                new PoseKey(0.18f, 0f, 0f, 1.12f, 0.94f, 10f));
            AddPose(states, Subject09Presentation.Pose.Hurt, false,
                new PoseKey(0f, -0.10f, 0.035f, 1.13f, 0.91f, 17f),
                new PoseKey(0.10f, -0.15f, -0.06f, 0.91f, 0.95f, 23f),
                new PoseKey(0.29f, 0f, 0f, 1f, 1f, 0f));
            AddPose(states, Subject09Presentation.Pose.Death, false,
                new PoseKey(0f, 0f, 0f, 1f, 1f, 0f),
                new PoseKey(0.13f, -0.18f, -0.21f, 0.93f, 0.91f, 38f),
                new PoseKey(0.42f, -0.28f, -0.58f, 0.84f, 0.78f, 86f));
            AddPose(states, Subject09Presentation.Pose.Attack, false,
                new PoseKey(0f, -0.07f, -0.035f, 1.07f, 0.97f, 11f),
                new PoseKey(0.08f, 0.21f, 0.04f, 1.14f, 0.91f, -21f),
                new PoseKey(0.25f, 0f, 0f, 1f, 1f, 0f));
            AddPose(states, Subject09Presentation.Pose.Phase, false,
                new PoseKey(0f, 0.14f, -0.02f, 1.26f, 0.88f, -22f),
                new PoseKey(0.12f, 0.20f, -0.06f, 1.32f, 0.85f, -23f),
                new PoseKey(0.22f, 0f, 0f, 1f, 1f, 0f));
            AssetDatabase.SaveAssets();
            return controller;
        }

        public static Sprite GetBladeArc()
        {
            if (!File.Exists(BladePath))
            {
                const int size = 96;
                Directory.CreateDirectory(Path.GetDirectoryName(BladePath));
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / size - 0.46f, (y + 0.5f) / size - 0.49f);
                    float angle = Mathf.Atan2(point.y, point.x);
                    float taper = Mathf.Clamp01(1f - Mathf.Abs((angle - 0.16f) / 1.14f));
                    float distance = Mathf.Abs(point.magnitude - (0.40f + angle * 0.035f));
                    float glow = Mathf.Clamp01(1f - distance / 0.085f) * taper * 0.55f;
                    float core = Mathf.Clamp01(1f - distance / 0.032f) * Mathf.Sqrt(taper);
                    pixels[y * size + x] = new Color(0.55f + core * 0.45f, 0.86f + core * 0.14f, 1f,
                        Mathf.Clamp01(core + glow));
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(BladePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(BladePath, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BladePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(BladePath);
        }

        public static NeonAudio ConfigureAudio(GameObject gameObject)
        {
            NeonAudio audio = gameObject.AddComponent<NeonAudio>();
            AudioSource music = gameObject.AddComponent<AudioSource>();
            AudioSource effects = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.loop = true;
            music.clip = PolishedAudio("BleakTerminal_Theme.mp3") ?? PolishedAudio("Infestation_ControlRoom_Balanced.wav") ?? NeonPlaceholderAudio.GetAmbience();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;

            SerializedObject fields = new SerializedObject(audio);
            fields.FindProperty("music").objectReferenceValue = music;
            fields.FindProperty("effects").objectReferenceValue = effects;
            fields.FindProperty("jump").objectReferenceValue = PolishedAudio("Jump_CyberTakeoff.wav") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Jump);
            fields.FindProperty("land").objectReferenceValue = PolishedAudio("Land_MetalFoot.wav") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Land);
            fields.FindProperty("collect").objectReferenceValue = PolishedAudio("Collect_Energy.wav") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Collect);
            fields.FindProperty("hurt").objectReferenceValue = PolishedAudio("Hurt_Suit.wav") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Hurt);
            fields.FindProperty("win").objectReferenceValue = PolishedAudio("Win_Extraction.mp3") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Win);
            fields.FindProperty("attack").objectReferenceValue = PolishedAudio("Attack_Energy.ogg") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Attack);
            fields.FindProperty("phase").objectReferenceValue = PolishedAudio("Phase_Forcefield.ogg") ?? NeonPlaceholderAudio.GetCue(NeonAudio.Cue.Phase);
            fields.FindProperty("metalImpact").objectReferenceValue = PolishedAudio("Land_ArmorImpact.ogg");
            fields.FindProperty("collectResonance").objectReferenceValue = PolishedAudio("Collect_Resonance.wav");
            fields.ApplyModifiedPropertiesWithoutUndo();
            return audio;
        }

        public static void ApplyEffectArt(Subject09Presentation presentation)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Sprites/Effects/ImpactParticlesArt.png").OfType<Sprite>().ToArray();
            SerializedObject fields = new SerializedObject(presentation);
            fields.FindProperty("landingArt").objectReferenceValue =
                sprites.FirstOrDefault(sprite => sprite.name == "LandingImpact");
            fields.FindProperty("pickupArt").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/EnergyPickupBurst.png")
                ?? sprites.FirstOrDefault(sprite => sprite.name == "EnergyPickup");
            fields.FindProperty("hitArt").objectReferenceValue =
                sprites.FirstOrDefault(sprite => sprite.name == "BladeHit");
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
        private static AudioClip PolishedAudio(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Chapter1/Polished/" + fileName);
        }

        private static AnimatorState AddPose(AnimatorStateMachine states, Subject09Presentation.Pose pose,
            bool loop, params PoseKey[] keys)
        {
            string path = AnimationFolder + "/Subject09_" + pose + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { frameRate = 60f };
                AnimationCurve positionX = new AnimationCurve();
                AnimationCurve positionY = new AnimationCurve();
                AnimationCurve scaleX = new AnimationCurve();
                AnimationCurve scaleY = new AnimationCurve();
                AnimationCurve rotationZ = new AnimationCurve();
                AnimationCurve rotationW = new AnimationCurve();
                foreach (PoseKey key in keys)
                {
                    positionX.AddKey(key.Time, key.X);
                    positionY.AddKey(key.Time, key.Y);
                    scaleX.AddKey(key.Time, key.Width);
                    scaleY.AddKey(key.Time, key.Height);
                    Quaternion rotation = Quaternion.Euler(0f, 0f, key.Angle);
                    rotationZ.AddKey(key.Time, rotation.z);
                    rotationW.AddKey(key.Time, rotation.w);
                }
                clip.SetCurve("Pose", typeof(Transform), "localPosition.x", positionX);
                clip.SetCurve("Pose", typeof(Transform), "localPosition.y", positionY);
                clip.SetCurve("Pose", typeof(Transform), "localScale.x", scaleX);
                clip.SetCurve("Pose", typeof(Transform), "localScale.y", scaleY);
                clip.SetCurve("Pose", typeof(Transform), "localRotation.x", AnimationCurve.Constant(0f, clip.length, 0f));
                clip.SetCurve("Pose", typeof(Transform), "localRotation.y", AnimationCurve.Constant(0f, clip.length, 0f));
                clip.SetCurve("Pose", typeof(Transform), "localRotation.z", rotationZ);
                clip.SetCurve("Pose", typeof(Transform), "localRotation.w", rotationW);
                clip.EnsureQuaternionContinuity();
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, path);
            }

            AnimatorState state = states.AddState(pose.ToString(),
                new Vector3(340 + (int)pose % 2 * 210, 10 + (int)pose / 2 * 80));
            state.motion = clip;
            AnimatorStateTransition transition = states.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.045f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, (int)pose, "Pose");
            return state;
        }

        private readonly struct PoseKey
        {
            public readonly float Time;
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;
            public readonly float Angle;

            public PoseKey(float time, float x, float y, float width, float height, float angle)
            {
                Time = time;
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Angle = angle;
            }
        }
    }
}
