using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire d'espaces de travail amélioré pour VR
/// - Transitions fluides entre espaces
/// - Système d'événements
/// - Persistence d'état
/// - Support des animations
/// </summary>
public class WorkspaceManager : MonoBehaviour
{
    [System.Serializable]
    public enum WorkspaceType
    {
        Home = 0,
        Work = 1,
        Entertainment = 2
    }

    [System.Serializable]
    private class WorkspaceConfig
    {
        public WorkspaceType type;
        public GameObject prefab;
        public float transitionDuration = 1f;
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    [SerializeField] private WorkspaceConfig[] workspaces = new WorkspaceConfig[3];
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool persistState = true;
    [SerializeField] private bool debugMode = false;

    private Dictionary<WorkspaceType, GameObject> workspaceInstances = new Dictionary<WorkspaceType, GameObject>();
    private WorkspaceType currentWorkspace = WorkspaceType.Home;
    private bool isTransitioning = false;
    private CanvasGroup fadeOverlay;

    // Événements
    public event Action<WorkspaceType> OnWorkspaceChanging;
    public event Action<WorkspaceType> OnWorkspaceChanged;
    public event Action<float> OnTransitionProgress;

    void Start()
    {
        InitializeWorkspaces();
        LoadPersistedState();
        ShowWorkspace(currentWorkspace);
    }

    /// <summary>
    /// Initialise tous les espaces de travail
    /// </summary>
    private void InitializeWorkspaces()
    {
        Log("Initialisation des espaces de travail...");

        // Créer le fade overlay
        CreateFadeOverlay();

        // Instantier tous les espaces
        foreach (var config in workspaces)
        {
            if (config.prefab != null)
            {
                GameObject instance = Instantiate(config.prefab, transform);
                instance.name = $"Workspace_{{config.type}}";
                instance.SetActive(false);
                workspaceInstances[config.type] = instance;

                Log($"✓ {{config.type}} initialisé");
            }
        }
    }

    /// <summary>
    /// Crée le panneau de fade (transition visuelle)
    /// </summary>
    private void CreateFadeOverlay()
    {
        GameObject overlayGO = new GameObject("FadeOverlay");
        overlayGO.transform.SetParent(transform);
        overlayGO.transform.localPosition = Vector3.zero;

        Canvas canvas = overlayGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        RawImage overlay = overlayGO.AddComponent<RawImage>();
        overlay.color = Color.black;

        fadeOverlay = overlayGO.AddComponent<CanvasGroup>();
        fadeOverlay.alpha = 0;

        RectTransform rect = overlayGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Affiche un espace de travail avec transition
    /// </summary>
    public void ShowWorkspace(WorkspaceType type)
    {
        if (currentWorkspace == type || isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionToWorkspace(type));
    }

    /// <summary>
    /// Coroutine de transition entre espaces
    /// </summary>
    private IEnumerator TransitionToWorkspace(WorkspaceType targetType)
    {
        isTransitioning = true;
        OnWorkspaceChanging?.Invoke(targetType);

        Log($"Transition vers {{targetType}}...");

        // Phase 1: Fade Out
        yield return StartCoroutine(FadeOut(fadeDuration));

        // Phase 2: Désactiver l'espace actuel
        if (workspaceInstances.ContainsKey(currentWorkspace))
        {
            workspaceInstances[currentWorkspace].SetActive(false);
        }

        // Phase 3: Activer le nouvel espace
        if (workspaceInstances.ContainsKey(targetType))
        {
            workspaceInstances[targetType].SetActive(true);
            
            // Exécuter les animations du nouvel espace
            ExecuteWorkspaceAnimation(workspaceInstances[targetType]);
        }

        currentWorkspace = targetType;

        // Phase 4: Fade In
        yield return StartCoroutine(FadeIn(fadeDuration));

        // Sauvegarder l'état
        if (persistState)
        {
            SavePersistedState();
        }

        isTransitioning = false;
        OnWorkspaceChanged?.Invoke(targetType);

        Log($"✓ Transition vers {{targetType}} complète");
    }

    /// <summary>
    /// Exécute les animations du nouvel espace
    /// </summary>
    private void ExecuteWorkspaceAnimation(GameObject workspace)
    {
        Animator animator = workspace.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Enter");
        }

        // Animer les enfants progressivement
        CanvasGroup[] childGroups = workspace.GetComponentsInChildren<CanvasGroup>();
        foreach (var group in childGroups)
        {
            group.alpha = 0;
            StartCoroutine(FadeInChild(group, fadeDuration * 0.5f));
        }
    }

    /// <summary>
    /// Fade out progressif
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(elapsed / duration);
            OnTransitionProgress?.Invoke(fadeOverlay.alpha);
            yield return null;
        }
        fadeOverlay.alpha = 1;
    }

    /// <summary>
    /// Fade in progressif
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(1 - (elapsed / duration));
            OnTransitionProgress?.Invoke(1 - fadeOverlay.alpha);
            yield return null;
        }
        fadeOverlay.alpha = 0;
    }

    /// <summary>
    /// Fade in d'un enfant
    /// </summary>
    private IEnumerator FadeInChild(CanvasGroup group, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        group.alpha = 1;
    }

    /// <summary>
    /// Sauvegarde l'état persistant
    /// </summary>
    private void SavePersistedState()
    {
        PlayerPrefs.SetInt("LastWorkspace", (int)currentWorkspace);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Charge l'état persistant
    /// </summary>
    private void LoadPersistedState()
    {
        if (persistState && PlayerPrefs.HasKey("LastWorkspace"))
        {
            int savedState = PlayerPrefs.GetInt("LastWorkspace");
            currentWorkspace = (WorkspaceType)savedState;
        }
    }

    /// <summary>
    /// Retourne l'espace de travail actuel
    /// </summary>
    public WorkspaceType GetCurrentWorkspace()
    {
        return currentWorkspace;
    }

    /// <summary>
    /// Vérifie si une transition est en cours
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// Change vers l'espace suivant (cycle)
    /// </summary>
    public void NextWorkspace()
    {
        int nextIndex = ((int)currentWorkspace + 1) % System.Enum.GetValues(typeof(WorkspaceType)).Length;
        ShowWorkspace((WorkspaceType)nextIndex);
    }

    /// <summary>
    /// Change vers l'espace précédent (cycle inverse)
    /// </summary>
    public void PreviousWorkspace()
    {
        int prevIndex = ((int)currentWorkspace - 1 + System.Enum.GetValues(typeof(WorkspaceType)).Length) 
                        % System.Enum.GetValues(typeof(WorkspaceType)).Length;
        ShowWorkspace((WorkspaceType)prevIndex);
    }

    /// <summary>
    /// Affiche les informations de débogage
    /// </summary>
    public void DebugInfo()
    {
        Log($"Current Workspace: {{currentWorkspace}}");
        Log($"Is Transitioning: {{isTransitioning}}");
        Log($"Fade Overlay Alpha: {{fadeOverlay.alpha}}");
    }

    /// <summary>
    /// Enregistre un log (si debug activé)
    /// </summary>
    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[WorkspaceManager] {{message}}");
        }
    }
}