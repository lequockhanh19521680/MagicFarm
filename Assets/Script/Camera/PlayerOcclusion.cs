using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; 

/// <summary>
/// Manages the occlusion of objects between the Cinemachine camera and the player target
/// by hooking into the CinemachineCore's static update event.
/// Uses SphereCast for smoother, more robust edge detection.
/// </summary>
public class PlayerOcclusion : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The main camera (must have a CinemachineBrain)")]
    [SerializeField] private Camera gameCamera;
    [Tooltip("The player's transform (target)")]
    [SerializeField] private Transform playerTarget;
    [Tooltip("Offset from the player's pivot to target the raycast (e.g., 0, 1.5, 0 for the chest)")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1f, 0);

    [Header("Settings")]
    [Tooltip("Layers that contain occludable objects (walls, trees, etc.)")]
    [SerializeField] private LayerMask occluderLayer;
    [Tooltip("Maximum number of occluders to check simultaneously")]
    [SerializeField] private int maxHits = 10;
    [Tooltip("The radius of the spherecast, helps catch edges smoothly")]
    [SerializeField] private float occlusionRadius = 0.5f; 

    // Pre-allocated buffer for the cast
    private RaycastHit[] hitsBuffer;
    
    // HashSets for efficient tracking
    private readonly HashSet<Occludable> objectsCurrentlyFaded = new HashSet<Occludable>();
    private readonly HashSet<Occludable> objectsHitThisFrame = new HashSet<Occludable>();
    
    /// <summary>
    /// Initializes the hits buffer and verifies scene setup.
    /// </summary>
    private void Awake()
    {
        hitsBuffer = new RaycastHit[maxHits];
        
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (gameCamera.GetComponent<CinemachineBrain>() == null)
        {
             Debug.LogError("PlayerOcclusion requires a CinemachineBrain on the main camera!", this);
        }
    }

    /// <summary>
    /// Subscribes to the static CinemachineCore update event.
    /// </summary>
    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(ProcessOcclusion);
    }

    /// <summary>
    /// Unsubscribes from the static CinemachineCore update event.
    /// </summary>
    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(ProcessOcclusion);
    }

    /// <summary>
    /// Performs the occlusion spherecast and commands objects to fade.
    /// This is called by the CinemachineCore.CameraUpdatedEvent.
    /// </summary>
    private void ProcessOcclusion(CinemachineBrain brain)
    {
        if (brain == null) return; 

        objectsHitThisFrame.Clear();

        Vector3 playerWorldPos = playerTarget.position + targetOffset;
        Vector3 camPos = brain.OutputCamera.transform.position; 
        Vector3 direction = (playerWorldPos - camPos).normalized;
        float distance = Vector3.Distance(camPos, playerWorldPos);

        // Sử dụng SphereCastNonAlloc để bắt các cạnh mượt mà hơn
        int hitCount = Physics.SphereCastNonAlloc(
            camPos,             // Vị trí bắt đầu
            occlusionRadius,    // Bán kính của "tia dày"
            direction,          // Hướng bắn
            hitsBuffer,         // Nơi lưu kết quả
            distance,           // Khoảng cách
            occluderLayer       // Layer cần kiểm tra
        );

        for (int i = 0; i < hitCount; i++)
        {
            Occludable occludable = hitsBuffer[i].collider.GetComponent<Occludable>();
            
            if (occludable != null)
            {
                objectsHitThisFrame.Add(occludable);

                if (!objectsCurrentlyFaded.Contains(occludable))
                {
                    occludable.FadeOut();
                    objectsCurrentlyFaded.Add(occludable);
                }
            }
        }

        objectsCurrentlyFaded.RemoveWhere(occludable =>
        {
            if (!objectsHitThisFrame.Contains(occludable))
            {
                occludable.FadeIn();
                return true; 
            }
            return false;
        });
    }
}