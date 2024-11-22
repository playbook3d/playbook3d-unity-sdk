using System;
using UnityEngine;

namespace PlaybookUnitySDK.Scripts
{
    [Serializable]
    public class PlaybookMaskGroup
    {
        [SerializeField] private GameObject[] maskObjects;

        public GameObject[] MaskObjects => maskObjects;
    }
}

