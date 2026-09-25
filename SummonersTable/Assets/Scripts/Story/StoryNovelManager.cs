using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SummonersTable.Story
{
    /// <summary>
    /// Контроллер 2D визуальной новеллы для сюжетного режима «Пир Хохота».
    /// Управляет диалогами, 2D-спрайтами (L, C, R), фонами, звуками (BGM/SFX) и боевыми триггерами.
    /// </summary>
    public sealed class StoryNovelManager : MonoBehaviour
    {
        public static StoryNovelManager Instance { get; private set; }

        [Header("Data")]
        public StoryConfig storyConfig;
        public int currentSceneIndex = 0;
        public int currentStepIndex = 0;

        [Header("UI Canvas Elements")]
        public Canvas novelCanvas;
        public RawImage backgroundImage;
        public RectTransform leftSpriteSlot;
        public RectTransform centerSpriteSlot;
        public RectTransform rightSpriteSlot;
        public Image leftSpriteImage;
        public Image centerSpriteImage;
        public Image rightSpriteImage;

        [Header("Dialogue Box")]
        public RectTransform dialoguePanel;
        public Text speakerNameText;
        public Text dialogueBodyText;
        public GameObject continueIndicator;

        [Header("Fade & Camera Effects")]
        public Image fadeOverlay;
        public Camera novelCamera;

        [Header("Audio")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("Settings")]
        public float typeWriterSpeed = 0.03f;

        private Coroutine _typewriterRoutine;
        private bool _isTyping = false;
        private string _fullCurrentText = "";

        // События для интеграции с боевым модулем
        public event Action<string, string> OnStartBattle; // battleId, enemyDeck
        public event Action OnShowDeckSelect;
        public event Action<int, string, string> OnWallOfShameWrite; // slot, name, title

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureAudioSources();
            EnsureUIHierarchy();
        }

        private void Start()
        {
            LoadConfigFromStreamingAssets();
        }

        private void Update()
        {
            if (!novelCanvas.gameObject.activeInHierarchy) return;

            // Клик мыши, Enter или Space для продвижения диалога
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceDialogue();
            }
        }

        /// <summary>
        /// Загрузка story.json из StreamingAssets/Config/story.json
        /// </summary>
        public void LoadConfigFromStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Config", "story.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                storyConfig = JsonUtility.FromJson<StoryConfig>(json);
                Debug.Log($"[StoryNovelManager] Успешно загружен сценарий: {storyConfig.title}, сцен: {storyConfig.scenes.Count}");
            }
            else
            {
                Debug.LogWarning($"[StoryNovelManager] Файл сценария не найден по пути: {path}");
            }
        }

        /// <summary>
        /// Запуск сцены по её идентификатору (e.g. "scene_00_prologue")
        /// </summary>
        public void PlayScene(string sceneId)
        {
            if (storyConfig == null || storyConfig.scenes == null) return;

            int index = storyConfig.scenes.FindIndex(s => s.sceneId == sceneId);
            if (index >= 0)
            {
                PlaySceneByIndex(index);
            }
            else
            {
                Debug.LogError($"[StoryNovelManager] Сцена '{sceneId}' не найдена в конфиге!");
            }
        }

        public void PlaySceneByIndex(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= storyConfig.scenes.Count) return;

            currentSceneIndex = sceneIndex;
            currentStepIndex = 0;
            novelCanvas.gameObject.SetActive(true);
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            var scene = storyConfig.scenes[currentSceneIndex];
            if (currentStepIndex >= scene.steps.Count)
            {
                // Завершение сцены
                OnSceneComplete();
                return;
            }

            var step = scene.steps[currentStepIndex];

            // 1. Установка фона
            ApplyBackground(step.background);

            // 2. Установка спрайтов
            ApplySprites(step.sprites);

            // 3. Звук (BGM & SFX)
            ApplyAudio(step.audio);

            // 4. Текст и имя спикера
            speakerNameText.text = step.speaker;
            _fullCurrentText = step.text;
            if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = StartCoroutine(TypeWriterRoutine(_fullCurrentText));

            // 5. Техническое действие
            ExecuteAction(step.action);
        }

        public void AdvanceDialogue()
        {
            if (_isTyping)
            {
                // Если текст еще печатается, мгновенно показать его целиком
                if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
                dialogueBodyText.text = _fullCurrentText;
                _isTyping = false;
                if (continueIndicator != null) continueIndicator.SetActive(true);
            }
            else
            {
                // Переход к следующей реплике
                currentStepIndex++;
                ShowCurrentStep();
            }
        }

        private IEnumerator TypeWriterRoutine(string targetText)
        {
            _isTyping = true;
            dialogueBodyText.text = "";
            if (continueIndicator != null) continueIndicator.SetActive(false);

            foreach (char c in targetText)
            {
                dialogueBodyText.text += c;
                yield return new WaitForSeconds(typeWriterSpeed);
            }

            _isTyping = false;
            if (continueIndicator != null) continueIndicator.SetActive(true);
        }

        private void ApplyBackground(string bgId)
        {
            if (string.IsNullOrEmpty(bgId)) return;
            // Попытка загрузить из Resources/Art/ или Resources/Backgrounds/
            var tex = Resources.Load<Texture2D>("Art/" + bgId) ?? Resources.Load<Texture2D>("Backgrounds/" + bgId);
            if (tex != null && backgroundImage != null)
            {
                backgroundImage.texture = tex;
                backgroundImage.color = Color.white;
            }
        }

        private void ApplySprites(List<StorySpriteSlot> spriteSlots)
        {
            // Скрыть все слоты по умолчанию
            if (leftSpriteImage) leftSpriteImage.gameObject.SetActive(false);
            if (centerSpriteImage) centerSpriteImage.gameObject.SetActive(false);
            if (rightSpriteImage) rightSpriteImage.gameObject.SetActive(false);

            if (spriteSlots == null) return;

            foreach (var slot in spriteSlots)
            {
                Image targetImage = null;
                switch (slot.position?.ToUpperInvariant())
                {
                    case "L": targetImage = leftSpriteImage; break;
                    case "C": targetImage = centerSpriteImage; break;
                    case "R": targetImage = rightSpriteImage; break;
                }

                if (targetImage != null)
                {
                    targetImage.gameObject.SetActive(true);
                    // Загрузка спрайта по персонажу и эмоции (e.g. "Characters/Yorik_panic")
                    string spritePath = $"Characters/{slot.character}_{slot.emotion}";
                    var sp = Resources.Load<Sprite>(spritePath) ?? Resources.Load<Sprite>($"Characters/{slot.character}");
                    if (sp != null)
                    {
                        targetImage.sprite = sp;
                    }
                }
            }
        }

        private void ApplyAudio(StoryAudio audio)
        {
            if (audio == null) return;

            // BGM
            if (!string.IsNullOrEmpty(audio.bgm))
            {
                if (audio.bgm.Equals("Mute", StringComparison.OrdinalIgnoreCase))
                {
                    bgmSource.Stop();
                }
                else
                {
                    PlayBGM(audio.bgm);
                }
            }

            // SFX
            if (!string.IsNullOrEmpty(audio.sfx))
            {
                PlaySFX(audio.sfx);
            }
        }

        public void PlayBGM(string clipName)
        {
            var clip = LoadAudioClip(clipName);
            if (clip != null && bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        public void PlaySFX(string clipName)
        {
            var clip = LoadAudioClip(clipName);
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private AudioClip LoadAudioClip(string name)
        {
            // 1. Попытка загрузки из Resources/Audio/
            string clearName = Path.GetFileNameWithoutExtension(name);
            var clip = Resources.Load<AudioClip>("Audio/" + clearName);
            if (clip != null) return clip;

            // 2. Попытка загрузки из Audio/Card_Game/...
            clip = Resources.Load<AudioClip>(clearName);
            return clip;
        }

        private void ExecuteAction(StoryAction action)
        {
            if (action == null || string.IsNullOrEmpty(action.actionType)) return;

            switch (action.actionType.ToUpperInvariant())
            {
                case "CAMERA_SHAKE":
                    StartCoroutine(CameraShakeRoutine(0.5f, 0.4f));
                    break;

                case "FADE_IN":
                    StartCoroutine(FadeRoutine(1f, 0f, 0.5f));
                    break;

                case "FADE_OUT":
                    StartCoroutine(FadeRoutine(0f, 1f, 0.5f));
                    break;

                case "START_BATTLE":
                    Debug.Log($"[StoryNovelManager] ТРИГГЕР БОЯ: {action.target}, колода врага: {action.parameter}");
                    novelCanvas.gameObject.SetActive(false);
                    OnStartBattle?.Invoke(action.target, action.parameter);
                    break;

                case "UI_SHOW_DECK_SELECT":
                    Debug.Log("[StoryNovelManager] ТРИГГЕР: Выбор колоды игроком");
                    OnShowDeckSelect?.Invoke();
                    break;

                case "WALL_OF_SHAME_WRITE":
                    int slot = 0;
                    int.TryParse(action.target, out slot);
                    Debug.Log($"[StoryNovelManager] СТЕНА ПОЗОРА: Слот {slot}, Кличка: {action.parameter}");
                    OnWallOfShameWrite?.Invoke(slot, action.target, action.parameter);
                    break;
            }
        }

        private IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            Transform camTransform = novelCamera != null ? novelCamera.transform : (Camera.main != null ? Camera.main.transform : null);
            if (camTransform == null) yield break;

            Vector3 originalPos = camTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            camTransform.localPosition = originalPos;
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
        {
            if (fadeOverlay == null) yield break;

            fadeOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeOverlay.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                fadeOverlay.color = c;
                yield return null;
            }

            c.a = endAlpha;
            fadeOverlay.color = c;
            if (Mathf.Approximately(endAlpha, 0f))
            {
                fadeOverlay.gameObject.SetActive(false);
            }
        }

        private void OnSceneComplete()
        {
            Debug.Log($"[StoryNovelManager] Сцена {currentSceneIndex} завершена!");
            // Переход к следующей сцене, если есть
            if (currentSceneIndex + 1 < storyConfig.scenes.Count)
            {
                currentSceneIndex++;
                currentStepIndex = 0;
                ShowCurrentStep();
            }
            else
            {
                novelCanvas.gameObject.SetActive(false);
                Debug.Log("[StoryNovelManager] Все сцены новеллы завершены!");
            }
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                var bgmObj = new GameObject("Novel_BGM");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
            }
            if (sfxSource == null)
            {
                var sfxObj = new GameObject("Novel_SFX");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }
        }

        private void EnsureUIHierarchy()
        {
            if (novelCanvas != null) return;

            var existingCanvas = GetComponentInChildren<Canvas>(true);
            if (existingCanvas != null)
            {
                novelCanvas = existingCanvas;
                return;
            }

            // Создание холста новеллы программно, если он еще не настроен в инспекторе
            var canvasGo = new GameObject("NovelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform);
            novelCanvas = canvasGo.GetComponent<Canvas>();
            novelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            novelCanvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            backgroundImage = bgGo.GetComponent<RawImage>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Sprite Slots
            leftSpriteSlot = CreateSlot(canvasGo.transform, "LeftSlot", new Vector2(0.2f, 0.4f), out leftSpriteImage);
            centerSpriteSlot = CreateSlot(canvasGo.transform, "CenterSlot", new Vector2(0.5f, 0.4f), out centerSpriteImage);
            rightSpriteSlot = CreateSlot(canvasGo.transform, "RightSlot", new Vector2(0.8f, 0.4f), out rightSpriteImage);

            // Dialogue Panel
            var dialGo = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            dialGo.transform.SetParent(canvasGo.transform, false);
            dialoguePanel = dialGo.GetComponent<RectTransform>();
            dialoguePanel.anchorMin = new Vector2(0.1f, 0.05f);
            dialoguePanel.anchorMax = new Vector2(0.9f, 0.32f);
            dialoguePanel.sizeDelta = Vector2.zero;
            dialGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.88f);

            // Speaker Name
            var spkGo = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text));
            spkGo.transform.SetParent(dialGo.transform, false);
            var spkRect = spkGo.GetComponent<RectTransform>();
            spkRect.anchorMin = new Vector2(0.03f, 0.78f);
            spkRect.anchorMax = new Vector2(0.5f, 0.95f);
            speakerNameText = spkGo.GetComponent<Text>();
            speakerNameText.fontSize = 28;
            speakerNameText.color = new Color(1f, 0.85f, 0.3f);
            var defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            speakerNameText.font = defaultFont;

            // Dialogue Body
            var bodyGo = new GameObject("DialogueText", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(dialGo.transform, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.03f, 0.1f);
            bodyRect.anchorMax = new Vector2(0.97f, 0.75f);
            dialogueBodyText = bodyGo.GetComponent<Text>();
            dialogueBodyText.fontSize = 24;
            dialogueBodyText.color = Color.white;
            dialogueBodyText.font = defaultFont;

            // Fade Overlay
            var fadeGo = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Image));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            var fadeRect = fadeGo.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeOverlay = fadeGo.GetComponent<Image>();
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.raycastTarget = false;
            fadeGo.SetActive(false);
        }

        private RectTransform CreateSlot(Transform parent, string name, Vector2 anchor, out Image img)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(400, 600);
            img = go.GetComponent<Image>();
            img.preserveAspect = true;
            go.SetActive(false);
            return rect;
        }
    }
}
