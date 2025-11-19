using UnityEngine;
using System.Collections.Generic;

namespace MagicFarm.Camera
{
    /// <summary>
    /// Detects objects obstructing the camera's view of the target player and applies fading effects.
    /// Works with isometric camera setups by casting rays from camera to player bounds.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraObstructionDetector : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Target Settings")]
        [Tooltip("The player transform to track and protect from obstruction.")]
        [SerializeField] private Transform targetPlayer;
        
        [Tooltip("Layer mask defining which objects can obstruct the view.")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Detection Settings")]
        [Tooltip("Radius of the sphere cast used for obstruction detection.")]
        [SerializeField] private float checkRadius = 0.2f;
        
        [Tooltip("Additional bounds expansion around the player for more accurate detection.")]
        [SerializeField] private float boundExpand = 0.1f;

        #endregion

        #region Private Fields

        private Collider _playerCollider;
        private UnityEngine.Camera _camera;
        private readonly HashSet<FadingObject> _objectsToFade = new HashSet<FadingObject>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
        }

        private void LateUpdate()
        {
            DetectAndHandleObstructions();
        }

        private void OnDrawGizmos()
        {
            DrawDebugRays();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes required components and validates setup.
        /// </summary>
        private void InitializeComponents()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            
            if (targetPlayer != null)
            {
                _playerCollider = targetPlayer.GetComponent<Collider>();
            }
            
            if (_camera == null)
            {
                Debug.LogError($"[{nameof(CameraObstructionDetector)}] Camera component is required but not found!");
            }
            
            if (_playerCollider == null)
            {
                Debug.LogWarning($"[{nameof(CameraObstructionDetector)}] Target player or player collider not found!");
            }
        }

        #endregion

        #region Obstruction Detection

        /// <summary>
        /// Detects obstructions between camera and player, then applies fading to obstructing objects.
        /// </summary>
        private void DetectAndHandleObstructions()
        {
            if (!ValidateSetup()) return;

            Bounds expandedBounds = GetExpandedPlayerBounds();
            List<Vector3> checkPoints = GenerateBoundCheckPoints(expandedBounds);

            _objectsToFade.Clear();

            foreach (Vector3 point in checkPoints)
            {
                DetectObstructionsAtPoint(point);
            }

            ApplyFadingToObstructions();
        }

        /// <summary>
        /// Validates that all required components are available.
        /// </summary>
        private bool ValidateSetup()
        {
            return targetPlayer != null && _playerCollider != null && _camera != null;
        }

        /// <summary>
        /// Gets the player's bounds with expansion applied.
        /// </summary>
        private Bounds GetExpandedPlayerBounds()
        {
            Bounds bounds = _playerCollider.bounds;
            bounds.Expand(boundExpand);
            return bounds;
        }

        /// <summary>
        /// Detects obstructions at a specific point using sphere cast.
        /// </summary>
        private void DetectObstructionsAtPoint(Vector3 point)
        {
            Vector3 screenPoint = _camera.WorldToScreenPoint(point);
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            float distance = Vector3.Distance(ray.origin, point);

            RaycastHit[] hits = Physics.SphereCastAll(ray, checkRadius, distance, obstacleLayer);

            foreach (RaycastHit hit in hits)
            {
                FadingObject fadingObject = hit.collider.GetComponent<FadingObject>();
                if (fadingObject != null)
                {
                    _objectsToFade.Add(fadingObject);
                }
            }
        }

        /// <summary>
        /// Applies fading effect to all detected obstructing objects.
        /// </summary>
        private void ApplyFadingToObstructions()
        {
            foreach (FadingObject fadingObject in _objectsToFade)
            {
                fadingObject.SetObstructing();
            }
        }

        #endregion

        #region Bounds Calculation

        /// <summary>
        /// Generates nine check points covering the bounds of the target.
        /// Includes center, corners, and edge midpoints for comprehensive coverage.
        /// </summary>
        private List<Vector3> GenerateBoundCheckPoints(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = bounds.center;

            return new List<Vector3>
            {
                center,
                min,
                max,
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z)
            };
        }

        #endregion

        #region Debug Visualization

        /// <summary>
        /// Draws debug rays in the scene view for visualization.
        /// </summary>
        private void DrawDebugRays()
        {
            if (_playerCollider == null || _camera == null) return;

            Gizmos.color = Color.cyan;
            Bounds expandedBounds = GetExpandedPlayerBounds();
            List<Vector3> points = GenerateBoundCheckPoints(expandedBounds);

            foreach (Vector3 point in points)
            {
                Vector3 screenPoint = _camera.WorldToScreenPoint(point);
                Ray ray = _camera.ScreenPointToRay(screenPoint);
                Gizmos.DrawLine(ray.origin, point);
            }
        }

        #endregion
    }
}