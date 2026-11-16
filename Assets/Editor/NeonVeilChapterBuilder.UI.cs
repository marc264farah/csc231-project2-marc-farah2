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
        private static ChapterUI CreateUI(ChapterGame game, Subject09Controller player)
        {
            GameObject canvasObject = new GameObject("Chapter Interface", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.55f;

            GameObject eventObject = new GameObject("Event System", typeof(EventSystem));
            InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
            InputActionReference[] refs = AssetDatabase.LoadAllAssetsAtPath(UIInputPath).OfType<InputActionReference>().ToArray();
            module.actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(UIInputPath);
            module.point = UIAction(refs, "Point");
            module.leftClick = UIAction(refs, "Click");
            module.move = UIAction(refs, "Navigate");
            module.submit = UIAction(refs, "Submit");
            module.cancel = UIAction(refs, "Cancel");
            module.scrollWheel = UIAction(refs, "ScrollWheel");

            ChapterUI ui = canvasObject.AddComponent<ChapterUI>();
            RectTransform title = Screen(canvas.transform, "Title");
            RectTransform intro = Screen(canvas.transform, "Story Intro");
            RectTransform hud = Rect(canvas.transform, "HUD", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform pause = Screen(canvas.transform, "Pause");
            RectTransform gameOver = Screen(canvas.transform, "Game Over");
            RectTransform complete = Screen(canvas.transform, "Level Complete");
            RectTransform titleCard = Card(title, "Title card", new Vector2(652, 470));
            TextAt(titleCard, "Chapter", "CHAPTER 1  /  DEAD DISTRICT", 22, Cyan, new Vector2(0, 154), new Vector2(550, 38));
            TextAt(titleCard, "Title", "NEON VEIL", 83, TextLight, new Vector2(0, 85), new Vector2(580, 102));
            TextAt(titleCard, "Objective", "Complete your objectives, then reach\nthe extraction beacon.", 23, TextLight,
                new Vector2(0, -3), new Vector2(570, 46));
            ButtonAt(titleCard, "Start chapter", new Vector2(0, -91), game.StartChapter, true);
            ButtonAt(titleCard, "Quit", new Vector2(0, -154), game.Quit);

            RectTransform storyCard = Card(intro, "Story card", new Vector2(720, 570));
            TextAt(storyCard, "Heading", "VEYRA-9  /  2196", 36, Cyan,
                new Vector2(0, 175), new Vector2(590, 55));
            TextAt(storyCard, "Story",
                @"The quarantine of Veyra-9 has failed.
Alien Mimics stalk the dead district. Security Sentinels still hunt survivors.

You are Subject 09, a fallen operative rebuilt with cybernetics.
The extraction tower is your only way out.",
                21, TextLight, new Vector2(0, 0), new Vector2(570, 260));
            ButtonAt(storyCard, "Enter the Dead District", new Vector2(0, -185), game.BeginLevel, true);
            RectTransform left = Rect(hud, "Vitals", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(197, -70), new Vector2(346, 98));
            ImageAt(left, new Color32(6, 13, 27, 223));
            ImageAt(CenterRect(left, "Vitals signal edge", new Vector2(0, 49), new Vector2(346, 3)), Cyan);
            Text health = TextAt(left, "Health", "HEALTH", 20, TextLight, new Vector2(-82, 28), new Vector2(170, 27));
            Text cells = TextAt(left, "Energy cells", "ENERGY CELLS", 18, Cyan, new Vector2(74, 28), new Vector2(168, 27));
            Text phase = TextAt(left, "Phase cooldown", "PHASE  READY", 17, TextLight, new Vector2(0, 0), new Vector2(260, 25));
            Text defeats = TextAt(left, "Enemy defeats", "ENEMIES  0/7", 17, new Color32(255, 182, 99, 255),
                new Vector2(0, -28), new Vector2(260, 25));

            RectTransform right = Rect(hud, "Location and controls", Vector2.one, Vector2.one,
                new Vector2(-269, -69), new Vector2(488, 96));
            ImageAt(right, new Color32(6, 13, 27, 223));
            ImageAt(CenterRect(right, "Location signal edge", new Vector2(0, 48), new Vector2(488, 3)), Cyan);
            Text section = TextAt(right, "Location", "01  AWAKENING", 21, Cyan, new Vector2(0, 27), new Vector2(470, 27));
            Text instruction = TextAt(right, "Hint", "A / D or arrows  •  Space jump  •  Left click attack", 16, TextLight,
                new Vector2(0, 0), new Vector2(470, 28));
            TextAt(right, "Pause hint", "Press Esc to pause", 14, new Color32(255, 182, 99, 255),
                new Vector2(0, -29), new Vector2(470, 24));
            Text checkpoint = TextAt(hud, "Checkpoint saved", "CHECKPOINT  SAVED", 25, Cyan, new Vector2(0, 159), new Vector2(444, 39));
            checkpoint.gameObject.SetActive(false);

            RectTransform pauseCard = Card(pause, "Pause card", new Vector2(550, 485));
            TextAt(pauseCard, "Heading", "PAUSED", 56, TextLight, new Vector2(0, 176), new Vector2(460, 78));
            ButtonAt(pauseCard, "Resume", new Vector2(0, 99), game.Resume, true);
            ButtonAt(pauseCard, "Restart chapter", new Vector2(0, 38), game.RestartChapter);
            ButtonAt(pauseCard, "Quit", new Vector2(0, -23), game.Quit);
            TextAt(pauseCard, "Volume label", "VOLUME", 17, Cyan, new Vector2(-123, -115), new Vector2(140, 30));
            Slider volume = VolumeControl(pauseCard, new Vector2(69, -115), ui.SetVolume);
            Toggle mute = MuteControl(pauseCard, new Vector2(-66, -174), ui.SetMuted);

            RectTransform overCard = Card(gameOver, "Game over card", new Vector2(620, 370));
            TextAt(overCard, "Heading", "SIGNAL LOST", 56, TextLight, new Vector2(0, 106), new Vector2(540, 80));
            ButtonAt(overCard, "Respawn at checkpoint", new Vector2(0, 24), game.RespawnAtCheckpoint, true);
            ButtonAt(overCard, "Restart chapter", new Vector2(0, -37), game.RestartChapter);
            ButtonAt(overCard, "Quit", new Vector2(0, -98), game.Quit);

            RectTransform completeCard = Card(complete, "Extraction card", new Vector2(650, 365));
            TextAt(completeCard, "Heading", "LEVEL COMPLETE", 47, TextLight, new Vector2(0, 110), new Vector2(570, 80));
            Text result = TextAt(completeCard, "Run results", "", 17, Cyan, new Vector2(0, 44), new Vector2(570, 52));
            ButtonAt(completeCard, "Play again", new Vector2(0, -31), game.RestartChapter, true);
            ButtonAt(completeCard, "Quit", new Vector2(0, -92), game.Quit);

            Set(ui, "game", game, "player", player, "combat", player.GetComponent<Subject09Combat>(),
                "titlePanel", title.gameObject, "introPanel", intro.gameObject, "hudPanel", hud.gameObject, "pausePanel", pause.gameObject,
                "gameOverPanel", gameOver.gameObject, "completePanel", complete.gameObject,
                "healthLabel", health, "cellLabel", cells, "defeatLabel", defeats, "phaseLabel", phase, "sectionLabel", section,
                "instructionLabel", instruction, "checkpointLabel", checkpoint, "resultLabel", result,
                "volume", volume, "mute", mute);
            intro.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);
            complete.gameObject.SetActive(false);
            NeonUIArt.Apply(ui);
            return ui;
        }

        private static InputActionReference UIAction(InputActionReference[] actions, string name)
        {
            return actions.First(a => a != null && a.action != null && a.action.actionMap.name == "UI" && a.action.name == name);
        }

        private static RectTransform Screen(Transform parent, string name)
        {
            RectTransform screen = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ImageAt(screen, new Color32(4, 10, 21, 211));
            return screen;
        }

        private static RectTransform Card(Transform screen, string name, Vector2 size)
        {
            RectTransform card = CenterRect(screen, name, Vector2.zero, size);
            ImageAt(card, new Color32(9, 21, 38, 246));
            RectTransform topRule = CenterRect(card, "Top edge", Vector2.up * (size.y * 0.5f), new Vector2(size.x, 3f));
            ImageAt(topRule, Cyan);
            ImageAt(CenterRect(card, "Bottom edge", Vector2.down * (size.y * 0.5f),
                new Vector2(size.x, 2f)), new Color32(41, 104, 127, 235));
            ImageAt(CenterRect(card, "Left signal", new Vector2(-size.x * 0.5f, size.y * 0.29f),
                new Vector2(4f, size.y * 0.16f)), Cyan);
            ImageAt(CenterRect(card, "Right signal", new Vector2(size.x * 0.5f, -size.y * 0.29f),
                new Vector2(4f, size.y * 0.16f)), new Color32(255, 121, 91, 255));
            return card;
        }

        private static Button ButtonAt(RectTransform parent, string name, Vector2 position, UnityAction callback,
            bool primary = false)
        {
            RectTransform rect = CenterRect(parent, name, position, new Vector2(334, 47));
            Image image = ImageAt(rect, primary ? Cyan : new Color32(22, 45, 63, 255), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(181, 234, 255, 255);
            colors.pressedColor = new Color32(102, 192, 214, 255);
            button.colors = colors;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, callback);
            TextAt(rect, "Label", name, 22, primary ? Ink : TextLight, Vector2.zero, new Vector2(328, 47));
            return button;
        }

        private static Slider VolumeControl(RectTransform parent, Vector2 position, UnityAction<float> callback)
        {
            RectTransform rect = CenterRect(parent, "Volume", position, new Vector2(221, 35));
            ImageAt(CenterRect(rect, "Track", Vector2.zero, new Vector2(221, 7)), new Color32(34, 60, 80, 255));
            RectTransform travel = CenterRect(rect, "Handle area", Vector2.zero, new Vector2(204, 30));
            RectTransform handle = CenterRect(travel, "Handle", Vector2.zero, new Vector2(17, 25));
            Image image = ImageAt(handle, Cyan, true);
            Slider slider = rect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = image;
            slider.value = 0.65f;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged, callback);
            return slider;
        }

        private static Toggle MuteControl(RectTransform parent, Vector2 position, UnityAction<bool> callback)
        {
            RectTransform rect = CenterRect(parent, "Mute", position, new Vector2(235, 34));
            Image target = ImageAt(CenterRect(rect, "Mute box", new Vector2(-80, 0), new Vector2(24, 24)),
                new Color32(36, 73, 97, 255), true);
            Image check = ImageAt(CenterRect(target.rectTransform, "Checked", Vector2.zero, new Vector2(13, 13)), Cyan);
            TextAt(rect, "Mute text", "Mute audio", 21, TextLight, new Vector2(0, 0), new Vector2(120, 34));
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = target;
            toggle.graphic = check;
            toggle.isOn = false;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(toggle.onValueChanged, callback);
            return toggle;
        }

        private static Image ImageAt(RectTransform rect, Color color, bool raycast = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text TextAt(Transform parent, string name, string label, int size, Color color,
            Vector2 position, Vector2 bounds)
        {
            RectTransform rect = CenterRect(parent, name, position, bounds);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = size;
            text.fontStyle = size >= 25 ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, size - 7);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static RectTransform CenterRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            return Rect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Transform Group(string name, Transform parent = null)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static SpriteRenderer Block(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, Sprite sprite, int order)
        {
            Transform block = Group(name, parent);
            block.localPosition = position;
            block.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = block.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void Set(Object target, params object[] fields)
        {
            SerializedObject serialized = new SerializedObject(target);
            for (int i = 0; i < fields.Length; i += 2)
            {
                SerializedProperty property = serialized.FindProperty((string)fields[i]);
                switch (fields[i + 1])
                {
                    case Object value: property.objectReferenceValue = value; break;
                    case int value: property.intValue = value; break;
                    case float value: property.floatValue = value; break;
                    case Vector2 value: property.vector2Value = value; break;
                    case string value: property.stringValue = value; break;
                    case System.Enum value: property.enumValueIndex = System.Convert.ToInt32(value); break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
