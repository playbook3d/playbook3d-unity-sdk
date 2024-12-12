using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    public enum MaskGroup
    {
        Mask1,
        Mask2,
        Mask3,
        Mask4,
        Mask5,
        Mask6,
        Mask7,
        CatchAll,
    }

    public class PlaybookMaskPass : MonoBehaviour
    {
        private Dictionary<MaskGroup, List<GameObject>> _maskGroups = new();
        private Material[] _maskMaterials;

        private Camera _renderCamera;
        private MaskGroup _backgroundMaskGroup = MaskGroup.CatchAll;
        private Color _originalBackgroundColor;
        private CameraClearFlags _originalClearFlags;
        private Dictionary<Renderer, Material[]> _originalMaterials = new();

        private List<Renderer> _visibleObjectRenderers;

        // private Color[] _maskColors =
        // {
        //     new(255 / 255f, 233 / 255f, 6 / 255f, 255 / 255f),
        //     new(5 / 255f, 137 / 255f, 214 / 255f, 255 / 255f),
        //     new(162 / 255f, 212 / 255f, 213 / 255f, 255 / 255f),
        //     new(0 / 255f, 0 / 255f, 22 / 255f, 255 / 255f),
        //     new(0 / 255f, 173 / 255f, 88 / 255f, 255 / 255f),
        //     new(240 / 255f, 132 / 255f, 207 / 255f, 255 / 255f),
        //     new(238 / 255f, 158 / 255f, 62 / 255f, 255 / 255f),
        //     new(230 / 255f, 0 / 255f, 12 / 255f, 255 / 255f),
        // };

        private Color32[] _maskColors =
        {
            new(255, 233, 6, 255),
            new(5, 137, 214, 255),
            new(162, 212, 213, 255),
            new(0, 0, 22, 255),
            new(0, 173, 88, 255),
            new(240, 132, 207, 255),
            new(238, 158, 62, 255),
            new(230, 0, 12, 255),
        };

        private static readonly int Color = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            InitializeProperties();
        }

        #region Private Methods

        private void InitializeProperties()
        {
            _renderCamera = GetComponent<Camera>();

            // Create an unlit colored material for each mask group
            _maskMaterials = new Material[_maskColors.Length];
            for (int i = 0; i < _maskColors.Length; i++)
            {
                Material material = new(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor(Color, _maskColors[i]);
                _maskMaterials[i] = material;
            }

            foreach (MaskGroup maskGroup in Enum.GetValues(typeof(MaskGroup)).Cast<MaskGroup>())
            {
                _maskGroups.Add(maskGroup, new List<GameObject>());
            }
        }

        /// <summary>
        /// Save the current camera properties and object materials so that they can be reset to
        /// their original values after the mask pass is complete.
        /// </summary>
        public void SaveProperties()
        {
            // Camera properties
            _originalBackgroundColor = _renderCamera.backgroundColor;
            _originalClearFlags = _renderCamera.clearFlags;

            // Keep track of all visible objects in the scene (visible = active + has a renderer)
            // and default them to the catch-all mask
            _visibleObjectRenderers = FindObjectsOfType<Renderer>(false).ToList();
            AddObjectsToCatchallGroup(
                _visibleObjectRenderers.Select(rend => rend.gameObject).ToList()
            );

            _originalMaterials.Clear();
            foreach (Renderer objectRenderer in _visibleObjectRenderers)
            {
                _originalMaterials.TryAdd(objectRenderer, objectRenderer.materials);
            }
        }

        /// <summary>
        /// Set the camera properties and object materials necessary for rendering the
        /// mask pass.
        /// </summary>
        public void SetProperties()
        {
            _renderCamera.clearFlags = CameraClearFlags.SolidColor;
            _renderCamera.backgroundColor = _maskMaterials[(int)_backgroundMaskGroup].color;

            SetObjectMaskGroups();
        }

        public void ResetProperties()
        {
            _renderCamera.clearFlags = _originalClearFlags;
            _renderCamera.backgroundColor = _originalBackgroundColor;

            foreach (Renderer objectRenderer in _visibleObjectRenderers)
            {
                objectRenderer.materials = _originalMaterials[objectRenderer];
            }
        }

        private void SetObjectMaskGroups()
        {
            foreach (KeyValuePair<MaskGroup, List<GameObject>> maskGroup in _maskGroups)
            {
                foreach (GameObject maskObject in maskGroup.Value)
                {
                    ApplyMaskColor((int)maskGroup.Key, maskObject);
                }
            }
        }

        /// <summary>
        /// Replace the given object's material(s) to the appropriate material required
        /// for the mask pass.
        /// </summary>
        private void ApplyMaskColor(int maskGroup, GameObject target)
        {
            Renderer rend = target.GetComponent<Renderer>();

            // Save materials before replacing to reset later
            _originalMaterials.TryAdd(rend, rend.materials);
            rend.material = _maskMaterials[maskGroup];
        }

        /// <summary>
        /// Add the given objects that are not already part of a different mask group
        /// to the catch-all mask group.
        /// </summary>
        private void AddObjectsToCatchallGroup(List<GameObject> visibleObjects)
        {
            foreach (GameObject visibleObject in visibleObjects)
            {
                // Object is not already part of a mask group. Add to catch-all
                if (!_maskGroups.Any(group => group.Value.Contains(visibleObject)))
                {
                    AddObjectToMaskGroup(visibleObject, MaskGroup.CatchAll);
                }
            }
        }

        #endregion

        #region Public Methods

        public void SetBackgroundMaskGroup(MaskGroup maskGroup)
        {
            _backgroundMaskGroup = maskGroup;
        }

        public void AddObjectToMaskGroup(GameObject maskObject, MaskGroup maskGroup)
        {
            // TODO: Should all children of an object be in the same group?
            if (!maskObject.TryGetComponent(out Renderer rend))
            {
                Debug.LogError($"{maskObject} does not have a renderer.");
                return;
            }

            // Remove the object from all mask groups to avoid duplicates
            RemoveObjectFromMaskGroup(maskObject);

            _maskGroups[maskGroup].Add(maskObject);
        }

        public void AddObjectsToMaskGroup(List<GameObject> objects, MaskGroup maskGroup)
        {
            foreach (GameObject maskObject in objects)
            {
                AddObjectToMaskGroup(maskObject, maskGroup);
            }
        }

        public void RemoveObjectFromMaskGroup(GameObject maskObject)
        {
            // Remove the object if it exists in any mask group
            foreach (List<GameObject> maskObjects in _maskGroups.Values)
            {
                maskObjects.Remove(maskObject);
            }
        }

        public void RemoveObjectsFromMaskGroup(List<GameObject> objects)
        {
            foreach (GameObject maskObject in objects)
            {
                RemoveObjectFromMaskGroup(maskObject);
            }
        }

        #endregion
    }
}
