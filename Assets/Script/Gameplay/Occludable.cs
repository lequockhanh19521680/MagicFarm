using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to any object that can be occluded (faded).
/// Responds to commands from PlayerOcclusion.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Occludable : MonoBehaviour
{
    [SerializeField] private Material transparentMat;
    [SerializeField] private float fadeSpeed = 3.0f;
    [Tooltip("The alpha value when the object is faded")]
    [Range(0f, 1f)]
    [SerializeField] private float fadedAlpha = 0.2f;

    private Renderer objectRenderer;
    private Material originalMaterial;
    private Material transparentMaterialInstance; 
    private Coroutine currentFadeCoroutine;
    
    private static int _colorPropertyID;

    /// <summary>
    /// Caches the renderer, materials, and the shader's color property ID.
    /// </summary>
    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        originalMaterial = objectRenderer.material; 
        transparentMaterialInstance = new Material(transparentMat);

        if (_colorPropertyID == 0)
        {
            // Use "_BaseColor" if using URP/HDRP Lit shader
            _colorPropertyID = Shader.PropertyToID("_Color"); 
        }
    }

    /// <summary>
    /// Starts the fade-out process to make the object transparent.
    /// </summary>
    public void FadeOut()
    {
        StopFade();
        objectRenderer.material = transparentMaterialInstance;
        currentFadeCoroutine = StartCoroutine(FadeMaterial(fadedAlpha));
    }

    /// <summary>
    /// Starts the fade-in process to make the object opaque.
    /// </summary>
    public void FadeIn()
    {
        StopFade();
        currentFadeCoroutine = StartCoroutine(FadeMaterial(1.0f));
    }

    /// <summary>
    /// Stops any currently active fade coroutine.
    /// </summary>
    private void StopFade()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
    }

    /// <summary>
    /// Coroutine that smoothly interpolates the material's alpha to the target value.
    /// </summary>
    private IEnumerator FadeMaterial(float targetAlpha)
    {
        float time = 0;
        Color currentColor = objectRenderer.material.GetColor(_colorPropertyID); 
        float startAlpha = currentColor.a;

        while (!Mathf.Approximately(startAlpha, targetAlpha))
        {
            time += Time.deltaTime * fadeSpeed;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time);
            
            currentColor.a = newAlpha;
            objectRenderer.material.SetColor(_colorPropertyID, currentColor); 
            
            startAlpha = newAlpha;
            yield return null;
        }

        // Revert to the original opaque material for performance
        if (targetAlpha == 1.0f)
        {
            objectRenderer.material = originalMaterial;
        }
    }
}