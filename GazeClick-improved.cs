using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Système de clic au regard amélioré pour VR
/// - Détection du regard avec feedback visuel
/// - Debounce pour éviter les clics multiples
/// - Support des interfaces UI et physiques
/// </summary>
public class GazeClick : MonoBehaviour
{
    [Header("Configuration du Regard")]
    [SerializeField] private float gazeTimeRequired = 1.5f;
    [SerializeField] private float gazeDistance = 1000f;
    [SerializeField] private LayerMask raycastMask = -1;
    
    [Header("Feedback Visuel")]
    [SerializeField] private GameObject gazeCursor;
    [SerializeField] private Color gazeActiveColor = Color.green;
    [SerializeField] private Color gazeInactiveColor = Color.gray;
    
    [Header("Débounce")]
    [SerializeField] private float clickDebounceTime = 0.5f;

    private GameObject currentTarget;
    private float gazeTimer;
    private float lastClickTime;
    private bool isGazing;
    private RaycastHit lastHit;

    void Start()
    {
        ValidateSetup();
    }

    void Update()
    {
        PerformRaycast();
        UpdateGazeTimer();
        UpdateCursorFeedback();
    }

    /// <summary>
    /// Effectue le raycast depuis la caméra
    /// </summary>
    private void PerformRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, gazeDistance, raycastMask))
        {
            HandleGazeHit(hit);
        }
        else
        {
            ResetGaze();
        }
    }

    /// <summary>
    /// Gère l'impact du regard sur un objet
    /// </summary>
    private void HandleGazeHit(RaycastHit hit)
    {
        if (currentTarget != hit.collider.gameObject)
        {
            // Nouveau target détecté
            ResetGaze();
            currentTarget = hit.collider.gameObject;
            lastHit = hit;
            isGazing = true;
        }

        gazeTimer += Time.deltaTime;

        if (gazeTimer >= gazeTimeRequired && CanClickNow())
        {
            PerformClick();
            lastClickTime = Time.time;
            gazeTimer = 0f;
        }
    }

    /// <summary>
    /// Effectue le clic sur la cible
    /// </summary>
    private void PerformClick()
    {
        if (currentTarget == null) return;

        // Essayer d'exécuter les événements UI d'abord
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        if (!ExecuteEvents.Execute(currentTarget, pointerData, ExecuteEvents.pointerClickHandler))
        {
            // Fallback: appeler OnClick si c'est un bouton ou collider
            currentTarget.SendMessage("OnGazeClick", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"[GazeClick] Clic sur: {currentTarget.name}");
    }

    /// <summary>
    /// Vérifie si on peut cliquer maintenant (débounce)
    /// </summary>
    private bool CanClickNow()
    {
        return Time.time - lastClickTime >= clickDebounceTime;
    }

    /// <summary>
    /// Réinitialise le regard
    /// </summary>
    private void ResetGaze()
    {
        currentTarget = null;
        gazeTimer = 0f;
        isGazing = false;
    }

    /// <summary>
    /// Met à jour le feedback visuel du curseur
    /// </summary>
    private void UpdateCursorFeedback()
    {
        if (gazeCursor == null) return;

        if (isGazing)
        {
            // Afficher le curseur avec progression
            gazeCursor.SetActive(true);
            
            // Mettre à jour la couleur selon la progression
            float progress = gazeTimer / gazeTimeRequired;
            Color targetColor = Color.Lerp(gazeInactiveColor, gazeActiveColor, progress);
            
            SetCursorColor(targetColor);
            SetCursorScale(1f + progress * 0.5f);
        }
        else
        {
            gazeCursor.SetActive(false);
        }
    }

    /// <summary>
    /// Met à jour la progression du timer
    /// </summary>
    private void UpdateGazeTimer()
    {
        // Optionnel: réduire le timer si on ne regarde rien
        if (!isGazing && gazeTimer > 0)
        {
            gazeTimer -= Time.deltaTime * 2f; // Decay rapide
        }
    }

    /// <summary>
    /// Change la couleur du curseur
    /// </summary>
    private void SetCursorColor(Color color)
    {
        Image cursorImage = gazeCursor.GetComponent<Image>();
        if (cursorImage != null)
        {
            cursorImage.color = color;
        }
    }

    /// <summary>
    /// Change l'échelle du curseur
    /// </summary>
    private void SetCursorScale(float scale)
    {
        gazeCursor.transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Valide la configuration au démarrage
    /// </summary>
    private void ValidateSetup()
    {
        if (GetComponent<Camera>() == null)
        {
            Debug.LogWarning("[GazeClick] Doit être attaché à une caméra!");
        }
    }

    /// <summary>
    /// Retourne les informations de débogage
    /// </summary>
    public void DebugInfo()
    {
        Debug.Log($"[GazeClick Debug] Target: {currentTarget?.name ?? "None"}, Timer: {gazeTimer:F2}s, Gazing: {isGazing}");
    }
} 