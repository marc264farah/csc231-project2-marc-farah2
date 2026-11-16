using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NeonVeil.Editor
{
    // Applies the same painted interface to new chapters and the existing saved scene.
    public static class NeonUIArt
    {
        private const string MenuPath = "Assets/Sprites/UI/IndustrialMenuPanel.png";
        private const string HudPath = "Assets/Sprites/UI/IndustrialHudPanel.png";
        private const string ButtonPath = "Assets/Sprites/UI/IndustrialButton.png";

        [MenuItem("NEON VEIL/Apply painted interface to open scene")]
        public static void ApplyToOpenScene()
        {
            ChapterUI ui = Object.FindAnyObjectByType<ChapterUI>();
            if (ui == null)
            {
                Debug.LogError("Open the Chapter 1 scene before applying interface art.");
                return;
            }
            Apply(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
        }

        public static void Apply(ChapterUI ui)
        {
            Sprite menu = LoadSprite(MenuPath, "IndustrialMenuPanel");
            Sprite hud = LoadSprite(HudPath, "IndustrialHudPanel");
            Sprite buttonArt = LoadSprite(ButtonPath, "IndustrialButton");
            if (menu == null || hud == null || buttonArt == null)
            {
                Debug.LogError("Import the painted UI sprites before applying the interface.");
                return;
            }

            Transform root = ui.transform;
            EnsureStoryIntro(ui, menu, buttonArt);
            PaintCard(root.Find("Title/Title card"), menu);
            PaintCard(root.Find("Story Intro/Story card"), menu);
            PaintCard(root.Find("Pause/Pause card"), menu);
            PaintCard(root.Find("Game Over/Game over card"), menu);
            PaintCard(root.Find("Level Complete/Extraction card"), menu);
            PaintHud(root.Find("HUD/Vitals"), hud);
            PaintHud(root.Find("HUD/Location and controls"), hud);
            SeparateHealth(root.Find("HUD"), hud);

            Move(root.Find("Title/Title card/Chapter"), new Vector2(0, 115));
            Move(root.Find("Title/Title card/Title"), new Vector2(0, 50));
            Text objective = root.Find("Title/Title card/Objective")?.GetComponent<Text>();
            if (objective != null) objective.text = "Complete your objectives, then reach\nthe extraction beacon.";
            RectTransform pauseCard = root.Find("Pause/Pause card") as RectTransform;
            if (pauseCard != null)
            {
                pauseCard.sizeDelta = new Vector2(550, 550);
                Move(pauseCard.Find("Heading"), new Vector2(0, 145));
                Transform oldMute = pauseCard.Find("Mute");
                if (oldMute != null) oldMute.gameObject.SetActive(false);
            }
            RectTransform gameOverCard = root.Find("Game Over/Game over card") as RectTransform;
            if (gameOverCard != null) gameOverCard.sizeDelta = new Vector2(620, 430);
            Move(root.Find("Game Over/Game over card/Heading"), new Vector2(0, 75));
            Move(root.Find("Game Over/Game over card/Restart chapter"), new Vector2(0, -30));
            Move(root.Find("Game Over/Game over card/Quit"), new Vector2(0, -83));

            RectTransform completeCard = root.Find("Level Complete/Extraction card") as RectTransform;
            if (completeCard != null) completeCard.sizeDelta = new Vector2(650, 470);
            Text completeHeading = root.Find("Level Complete/Extraction card/Heading")?.GetComponent<Text>();
            if (completeHeading != null)
            {
                completeHeading.fontSize = 47;
                completeHeading.resizeTextMaxSize = 47;
            }
            Text completeResults = root.Find("Level Complete/Extraction card/Run results")?.GetComponent<Text>();
            if (completeResults != null)
            {
                completeResults.fontSize = 17;
                completeResults.resizeTextMaxSize = 17;
            }
            Move(root.Find("Level Complete/Extraction card/Heading"), new Vector2(0, 50));
            Move(root.Find("Level Complete/Extraction card/Run results"), new Vector2(0, -5));
            Move(root.Find("Level Complete/Extraction card/Play again"), new Vector2(0, -49));
            Move(root.Find("Level Complete/Extraction card/Quit"), new Vector2(0, -100));

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                Image image = button.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = buttonArt;
                image.type = Image.Type.Simple;
                image.color = Color.white;
                button.targetGraphic = image;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.75f, 0.98f, 1f);
                colors.pressedColor = new Color(0.42f, 0.79f, 0.9f);
                colors.selectedColor = Color.white;
                button.colors = colors;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.color = new Color32(231, 247, 255, 255);
            }

            Text titleAudio = AudioButton(root.Find("Title"), "Title music toggle",
                new Vector2(1, 1), new Vector2(-118, -43),
                new Vector2(184, 42), buttonArt, ui);
            Text hudAudio = AudioButton(root.Find("HUD"), "HUD music toggle",
                new Vector2(0.5f, 1), new Vector2(0, -48),
                new Vector2(178, 40), buttonArt, ui);
            Text pauseAudio = AudioButton(pauseCard, "Pause music toggle",
                new Vector2(0.5f, 0.5f), new Vector2(0, -181),
                new Vector2(228, 44), buttonArt, ui);

            SerializedObject fields = new SerializedObject(ui);
            fields.FindProperty("titleAudioLabel").objectReferenceValue = titleAudio;
            fields.FindProperty("hudAudioLabel").objectReferenceValue = hudAudio;
            fields.FindProperty("pauseAudioLabel").objectReferenceValue = pauseAudio;
            fields.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void EnsureStoryIntro(ChapterUI ui, Sprite menu, Sprite buttonArt)
        {
            Transform root = ui.transform;
            RectTransform screen = root.Find("Story Intro") as RectTransform;
            if (screen == null)
            {
                GameObject screenObject = new GameObject("Story Intro", typeof(RectTransform), typeof(Image));
                screen = screenObject.GetComponent<RectTransform>();
                screen.SetParent(root, false);
                screen.anchorMin = Vector2.zero;
                screen.anchorMax = Vector2.one;
                screen.offsetMin = Vector2.zero;
                screen.offsetMax = Vector2.zero;
                screen.GetComponent<Image>().color = new Color32(4, 10, 21, 211);

                GameObject cardObject = new GameObject("Story card", typeof(RectTransform), typeof(Image));
                RectTransform card = cardObject.GetComponent<RectTransform>();
                card.SetParent(screen, false);
                card.anchorMin = Vector2.one * 0.5f;
                card.anchorMax = Vector2.one * 0.5f;
                card.anchoredPosition = Vector2.zero;
                card.sizeDelta = new Vector2(720, 570);
                Image cardImage = card.GetComponent<Image>();
                cardImage.sprite = menu;
                cardImage.type = Image.Type.Sliced;
                cardImage.color = Color.white;

                StoryText(card, "Heading", "VEYRA-9  /  2196", 36,
                    new Color32(54, 215, 241, 255), new Vector2(0, 175), new Vector2(590, 55));
                StoryText(card, "Story",
                    @"The quarantine of Veyra-9 has failed.
Alien Mimics stalk the dead district. Security Sentinels still hunt survivors.

You are Subject 09, a fallen operative rebuilt with cybernetics.
The extraction tower is your only way out.",
                    21, new Color32(231, 247, 255, 255), Vector2.zero, new Vector2(570, 260));

                GameObject buttonObject = new GameObject("Enter the Dead District",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.SetParent(card, false);
                buttonRect.anchorMin = Vector2.one * 0.5f;
                buttonRect.anchorMax = Vector2.one * 0.5f;
                buttonRect.anchoredPosition = new Vector2(0, -185);
                buttonRect.sizeDelta = new Vector2(334, 47);
                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.sprite = buttonArt;
                buttonImage.color = Color.white;
                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonImage;
                ChapterGame game = Object.FindAnyObjectByType<ChapterGame>();
                UnityEventTools.AddPersistentListener(button.onClick, game.BeginLevel);
                StoryText(buttonRect, "Label", "Enter the Dead District", 21,
                    new Color32(231, 247, 255, 255), Vector2.zero, new Vector2(330, 45));
                screen.gameObject.SetActive(false);
            }

            SerializedObject fields = new SerializedObject(ui);
            fields.FindProperty("introPanel").objectReferenceValue = screen.gameObject;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text StoryText(Transform parent, string name, string content, int size,
            Color color, Vector2 position, Vector2 bounds)
        {
            GameObject holder = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = holder.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = bounds;
            Text text = holder.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(15, size - 5);
            text.resizeTextMaxSize = size;
            return text;
        }
        private static Sprite LoadSprite(string path, string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == name);
        }

        private static void PaintCard(Transform card, Sprite sprite)
        {
            if (card == null) return;
            Image image = card.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            foreach (string name in new[] { "Top edge", "Bottom edge", "Left signal", "Right signal" })
            {
                Transform oldEdge = card.Find(name);
                if (oldEdge != null) oldEdge.gameObject.SetActive(false);
            }
        }

        private static void SeparateHealth(Transform hudRoot, Sprite sprite)
        {
            if (hudRoot == null) return;
            RectTransform stats = hudRoot.Find("Vitals") as RectTransform;
            if (stats == null) return;
            stats.anchoredPosition = new Vector2(380, -70);
            stats.sizeDelta = new Vector2(340, 98);

            RectTransform healthPanel = hudRoot.Find("Health panel") as RectTransform;
            if (healthPanel == null)
            {
                GameObject holder = new GameObject("Health panel", typeof(RectTransform), typeof(Image));
                healthPanel = holder.GetComponent<RectTransform>();
                healthPanel.SetParent(hudRoot, false);
            }
            healthPanel.anchorMin = new Vector2(0, 1);
            healthPanel.anchorMax = new Vector2(0, 1);
            healthPanel.anchoredPosition = new Vector2(105, -70);
            healthPanel.sizeDelta = new Vector2(190, 98);
            PaintHud(healthPanel, sprite);

            RectTransform health = stats.Find("Health") as RectTransform;
            if (health != null) health.SetParent(healthPanel, false);
            health = healthPanel.Find("Health") as RectTransform;
            if (health != null)
            {
                health.anchorMin = Vector2.one * 0.5f;
                health.anchorMax = Vector2.one * 0.5f;
                health.anchoredPosition = Vector2.zero;
                health.sizeDelta = new Vector2(166, 36);
            }

            RectTransform cells = stats.Find("Energy cells") as RectTransform;
            if (cells != null)
            {
                cells.anchoredPosition = new Vector2(0, 28);
                cells.sizeDelta = new Vector2(300, 28);
            }
        }
        private static void PaintHud(Transform panel, Sprite sprite)
        {
            if (panel == null) return;
            Image image = panel.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            foreach (Transform child in panel)
                if (child.name.Contains("signal edge")) child.gameObject.SetActive(false);
        }

        private static void Move(Transform target, Vector2 position)
        {
            if (target is RectTransform rect) rect.anchoredPosition = position;
        }

        private static Text AudioButton(Transform parent, string name, Vector2 anchor,
            Vector2 position, Vector2 size, Sprite sprite, ChapterUI ui)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponentInChildren<Text>(true);
            GameObject holder = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = holder.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = holder.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            Button button = holder.GetComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, ui.ToggleAudio);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 17;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color32(225, 245, 255, 255);
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "MUSIC ON";
            label.raycastTarget = false;
            return label;
        }
    }
}