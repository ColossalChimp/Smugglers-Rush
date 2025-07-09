using UnityEngine;

namespace TinyGiantStudio.Ranks
{
    /// <summary>
    /// Contains information regarding border, usable insides for that border and wing position, rotations.
    /// </summary>
    [System.Serializable]
    public struct Body
    {
        public Mesh border;
        public GameObject borderFractured;

        /// <summary>
        /// A list of mesh that fit inside the border.
        /// </summary>
        public Inside[] inside;

        public Vector3 leftWing_position;
        public Vector3 rightWing_position;
        public Vector3 leftWing_rotation;
        public Vector3 rightWing_rotation;
    }
}