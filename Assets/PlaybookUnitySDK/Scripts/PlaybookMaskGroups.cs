using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    [Serializable]
    public struct PlaybookMaskGroup
    {
        [SerializeField]
        private List<GameObject> maskObjects;

        public int MaskID { get; set; }

        public List<GameObject> MaskObjects => maskObjects;
    }

    public class PlaybookMaskGroups : MonoBehaviour
    {
        [SerializeField]
        private PlaybookMaskGroup[] maskGroups = new PlaybookMaskGroup[7];

        private static readonly int MaskIDProperty = Shader.PropertyToID("_ObjectID");

        private void Start()
        {
            SetObjectMaskGroups();
        }

        public void SetObjectMaskGroups()
        {
            for (int i = 0; i < maskGroups.Length; i++)
            {
                maskGroups[i].MaskID = i;
                SetMaterialPropertyBlockIDs(maskGroups[i]);
            }
        }

        private void SetMaterialPropertyBlockIDs(PlaybookMaskGroup maskGroup)
        {
            foreach (GameObject maskObject in maskGroup.MaskObjects)
            {
                if (!maskObject.TryGetComponent(out Renderer maskObjectRenderer))
                {
                    continue;
                }

                MaterialPropertyBlock block = new();
                maskObjectRenderer.GetPropertyBlock(block);
                Debug.Log($"<color=white>{maskGroup.MaskID}</color>");
                block.SetFloat(MaskIDProperty, maskGroup.MaskID);
                maskObjectRenderer.SetPropertyBlock(block);
                Debug.Log($"<color=red>{block.GetFloat(MaskIDProperty)}</color>");
            }
        }
    }
}
