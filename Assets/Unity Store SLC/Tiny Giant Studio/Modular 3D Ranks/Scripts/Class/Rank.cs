// Ignore Spelling: addons

using UnityEngine;

namespace TinyGiantStudio.Ranks
{
    //Note to future self:
    //Updating a state updates each variables manually to avoid overriding variables that aren't modifiable via sets tab
    //When creating a new variable, it needs to be set manually in the constructor and the ApplyState() method

    /// <summary>
    ///
    /// </summary>
    [System.Serializable]
    public class Rank
    {
        public string name; //todo: add a warning when two states have the same name

        #region Icon

        public int icon;
        public Material iconPrimaryMat;
        public Material iconSecondaryMat;

        /// <summary>
        /// After applying the rank, how long should it wait before removing the previous icon.
        /// </summary>
        public float oldIconExitDelay;

        /// <summary>
        /// After removing the previous icon, how long it should wait before applying this rank's icon. The minimum should be 0.25
        /// </summary>
        public float newIconEntryDelay;

        public AudioClip oldIconExitAudioClip;
        public AudioClip newIconEntryAudioClip;

        #endregion Icon

        #region Border

        public int border;
        public Material borderMat;

        /// <summary>
        /// After applying the rank, how long should it wait before removing the previous border.
        /// </summary>
        public float oldBorderExitDelay = 0f;

        public AudioClip oldBorderExitAudioClip;
        public float oldBorderExitAudioClipDelay;

        public AudioClip newBorderEntryAudioClip;

        public ExitAnimationType oldBorderExitAnimationType;

        /// <summary>
        /// How long fractured pieces last after using physics to exit border.
        /// Here, x is the minimum and y is the maximum lifetime of the pieces.
        /// </summary>
        public Vector2 oldBorderExitAnimation_physics_fracturedPiecesLifeTime = new(20, 30); //The default value is set by the editor script if value is 0,0

        /// <summary>
        /// When using physics explosion to exit border, how powerful the explosion should be.
        /// </summary>
        public float oldBorderExitAnimation_physics_explosionForce = 10;

        /// <summary>
        /// When using physics explosion to exit border, what should be the radius of the explosion
        /// </summary>
        public float oldBorderExitAnimation_physics_explosionRadius = 3;

        /// <summary>
        /// When using physics explosion to exit border, what should be the upwards force of the explosion
        /// </summary>
        public float oldBorderExitAnimation_physics_explosionUpwardsForce = 0.3f;

        /// <summary>
        /// The delay before applying this rank's border after removing the previous one.
        /// </summary>
        public float newBorderEntryDelay = 0f;

        public EntryAnimationType newBorderEntryAnimationType;

        /// <summary>
        /// When this is false, if this rank has the same border as the previous one, animation will be skipped.
        /// </summary>
        public bool transitionToSameBorder = false;

        #endregion Border

        #region Inside

        public Inside inside;
        public Material insideMat;

        /// <summary>
        /// After applying the rank, how long should it wait before removing the previous inside.
        /// </summary>
        public float oldInsideExitDelay = 0f;

        public AudioClip oldInsideExitAudioClip;
        public float oldInsideExitAudioClipDelay;
        public AudioClip newInsideEntryAudioClip;

        public ExitAnimationType oldInsideExitAnimationType;

        /// <summary>
        /// How long do fractured pieces last after using physics to exit the inside.
        /// Here, x is the minimum and y is the maximum lifetime of the pieces.
        /// </summary>
        public Vector2 oldInsideExitAnimation_physics_fracturedPiecesLifeTime = new(20, 30); //The default value is set by the editor script if value is 0,0

        /// <summary>
        /// When using physics explosion to exit inside, how powerful the explosion should be.
        /// </summary>
        public float oldInsideExitAnimation_physics_explosionForce = 10;

        /// <summary>
        /// When using physics explosion to exit inside, what should be the radius of the explosion.
        /// </summary>
        public float oldInsideExitAnimation_physics_explosionRadius = 3;

        /// <summary>
        /// When using physics explosion to exit inside, what should be the upwards force of the explosion
        /// </summary>
        public float oldInsideExitAnimation_physics_explosionUpwardsForce = 0.3f;

        public float newInsideEntryDelay = 0f;
        public EntryAnimationType newInsideEntryAnimationType;

        /// <summary>
        /// When this is false, if this rank has the same inside as the previous one, animation will be skipped.
        /// </summary>
        public bool transitionToSameInside = false;

        #endregion Inside

        #region Wings

        public int wings;
        public Material wingsMat;

        public AudioClip oldWingsExitAudioClip;
        public AudioClip newWingsEntryAudioClip;

        public float oldWingsExitAnimationStartDelay = 0f;
        public float oldWingsExitPositionAnimationDuration = 0.25f;
        public AnimationCurve oldWingsExitMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float newWingsEntryAnimationStartDelay = 0f;
        public float newWingsStartPositionAnimationDuration = 0.5f;
        public AnimationCurve newWingsInMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion Wings

        public void ResetWingsAnimationVariables()
        {
            this.oldWingsExitAnimationStartDelay = 0f;
            this.oldWingsExitPositionAnimationDuration = 0.25f;
            this.oldWingsExitMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            this.newWingsEntryAnimationStartDelay = 0f;
            this.newWingsStartPositionAnimationDuration = 0.5f;
            this.newWingsInMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        //public List<GameObject> addons = new();

        //This is used to create a new state to save by copying current state
        //This is used when caching current state

        /// <summary>
        ///
        /// </summary>
        /// <param name="currentState"></param>
        public Rank(Rank currentState)
        {
            this.name = currentState.name;

            #region Icon

            this.icon = currentState.icon;
            this.iconPrimaryMat = currentState.iconPrimaryMat;
            this.iconSecondaryMat = currentState.iconSecondaryMat;
            this.oldIconExitAudioClip = currentState.oldIconExitAudioClip;
            this.newIconEntryAudioClip = currentState.newIconEntryAudioClip;

            #endregion Icon

            #region Border

            this.border = currentState.border;
            this.borderMat = currentState.borderMat;

            this.oldIconExitDelay = currentState.oldIconExitDelay; //This was missing for some reason, so I added it. Check if this creates a bug

            this.oldBorderExitAudioClip = currentState.oldBorderExitAudioClip;
            this.oldBorderExitAudioClipDelay = currentState.oldBorderExitAudioClipDelay;
            this.newBorderEntryAudioClip = currentState.newBorderEntryAudioClip;

            this.oldBorderExitAnimationType = currentState.oldBorderExitAnimationType;
            this.oldBorderExitAnimation_physics_fracturedPiecesLifeTime = currentState.oldBorderExitAnimation_physics_fracturedPiecesLifeTime;
            this.oldBorderExitAnimation_physics_explosionForce = currentState.oldBorderExitAnimation_physics_explosionForce;
            this.oldBorderExitAnimation_physics_explosionRadius = currentState.oldBorderExitAnimation_physics_explosionRadius;
            this.oldBorderExitAnimation_physics_explosionUpwardsForce = currentState.oldBorderExitAnimation_physics_explosionUpwardsForce;

            this.newBorderEntryAnimationType = currentState.newBorderEntryAnimationType;

            #endregion Border

            #region Inside

            this.inside = new();
            if (currentState.inside != null)
                this.inside.mesh = currentState.inside.mesh;
            else
                this.inside.mesh = null;

            if (currentState.inside.fractured != null)
                this.inside.fractured = currentState.inside.fractured;
            else
                this.inside.fractured = null;

            this.insideMat = currentState.insideMat;

            this.oldInsideExitAudioClip = currentState.oldInsideExitAudioClip;
            this.oldInsideExitAudioClipDelay = currentState.oldInsideExitAudioClipDelay;
            this.newInsideEntryAudioClip = currentState.newInsideEntryAudioClip;

            this.oldInsideExitAnimationType = currentState.oldInsideExitAnimationType;
            this.oldInsideExitAnimation_physics_fracturedPiecesLifeTime = currentState.oldInsideExitAnimation_physics_fracturedPiecesLifeTime;
            this.oldInsideExitAnimation_physics_explosionForce = currentState.oldInsideExitAnimation_physics_explosionForce;
            this.oldInsideExitAnimation_physics_explosionRadius = currentState.oldInsideExitAnimation_physics_explosionRadius;
            this.oldInsideExitAnimation_physics_explosionUpwardsForce = currentState.oldInsideExitAnimation_physics_explosionUpwardsForce;

            this.newInsideEntryAnimationType = currentState.newInsideEntryAnimationType;

            #endregion Inside

            #region Wings
            this.wings = currentState.wings;
            this.wingsMat = currentState.wingsMat;

            this.oldWingsExitAudioClip = currentState.oldWingsExitAudioClip;
            this.newWingsEntryAudioClip = currentState.newWingsEntryAudioClip;
            this.oldWingsExitAnimationStartDelay = currentState.oldWingsExitAnimationStartDelay;
            this.oldWingsExitPositionAnimationDuration = currentState.oldWingsExitPositionAnimationDuration;
            this.oldWingsExitMovementCurve = currentState.oldWingsExitMovementCurve;
            this.newWingsEntryAnimationStartDelay = currentState.newWingsEntryAnimationStartDelay;
            this.newWingsStartPositionAnimationDuration = currentState.newWingsStartPositionAnimationDuration;
            this.newWingsInMovementCurve = currentState.newWingsInMovementCurve;

            #endregion Wings
            //this.addons = currentState.addons;
        }
    }
}