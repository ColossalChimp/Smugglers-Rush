// Ignore Spelling: renderers

using UnityEngine;

namespace TinyGiantStudio.Ranks
{
    [System.Serializable]
    public class Wing
    {
        public GameObject holder;
        public Transform leftWing;
        public Transform rightWing;
        public Renderer[] renderers;
    }
}