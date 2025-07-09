using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Linq;
using System;
using Random = UnityEngine.Random;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace TinyGiantStudio.Ranks
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class RanksAnimationController : MonoBehaviour
    {
        #region Variables

        #region Public Variables

        /// <summary>
        /// The default animation that plays when this GameObject spawns or becomes active.
        /// 0 is none, 1 is breathing, and others are different spawn animations.
        /// </summary>
        public int defaultSpawnAnimation = 0; //if renamed, remember to update UI binding path

        /// <summary>
        /// List of all the ranks saved.
        /// </summary>
        public List<Rank> ranks = new();

        /// <summary>
        /// When a new rank is applied, its index in the ranks list is compared to this value and is used to determine whether to call Upgrade or Downgrade particles.
        /// </summary>
        [Tooltip("When a new rank is applied, its index in the ranks list is compared to this value and is used to determine whether to call Upgrade or Downgrade particles.")]
        public int currentRankIndex;

        /// <summary>
        /// Last applied rank plus any modification done manually.When applying a new rank, some rank variables don't need to be copied from the ranks list. So, if you require accurate information, getting the rank from the ranks list is better.
        /// </summary>
        public Rank currentRank;

        /// <summary>
        /// A list of all borders with their supported insides and correct position, and rotation for wings.
        /// </summary>
        public Body[] bodies;

        /// <summary>
        /// A list of everything required for wings to work.
        /// </summary>
        public Wing[] wings;

        public bool enableUpgradeEffect = true;
        public ParticleSystem upgradeParticle;
        public VisualEffect upgradeVisualEffect;
        public bool enableDowngradeEffect = true;
        public ParticleSystem downgradeParticle;
        public VisualEffect downgradeVisualEffect;

        #endregion Public Variables

        [SerializeField] private Transform root; //This is referenced via the Reference Tab

        #region GetComponents

        private Animator animator;
        private AudioSource audioSource;

        #endregion GetComponents

        #region References

        [SerializeField] private MeshRenderer[] iconsPrimary;
        [SerializeField] private MeshRenderer[] iconsSecondary;

        [SerializeField] private Animator iconAnimator;

        [SerializeField] private MeshFilter border;
        [SerializeField] private MeshRenderer borderRenderer;

        [SerializeField] private MeshFilter inside;
        [SerializeField] private MeshRenderer insideRenderer;

        //public GameObject[] addons;

        /// <summary>
        /// An array of materials that are available as a shortcut in the customize tab.
        /// Different methods can use the index of these materials to set them to the border, the inside, and the wings.
        /// </summary>
        public Material[] materials;

        #endregion References

        /// <summary>
        /// In-case, this gameObject is disabled in the middle of the animation, storing them here helps to remove all the pieces.
        /// </summary>
        private GameObject newFractured_border;

        private GameObject oldFractured_border;

        private GameObject oldFractured_Inside;
        private GameObject newFractured_Inside;

        private Coroutine iconSwitchRoutine;
        private Coroutine enableBorderRoutine;
        private Coroutine enableInsideRoutine;
        private Coroutine wingsRoutine;

        #region Editor Only

#if UNITY_EDITOR

        /// <summary>
        /// This keeps a reference to all the clips for icon animation and switch to it in editor.
        /// </summary>
        [SerializeField] private AnimationClip[] iconAnimationClips;

#pragma warning disable 0168 // Unused variable warning
#pragma warning disable CS0414

        /// <summary>
        /// Used to store how many buttons to create in the editor
        /// </summary>
        [SerializeField] private int baseAnimationsCount = 4;
        [SerializeField] private int iconsAmount = 11;

#pragma warning restore CS0414
#pragma warning restore 0168

#endif

        #endregion Editor Only

        #endregion Variables

        #region Unity Stuff

        private void Awake()
        {
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            PlaySpawnAnimation(defaultSpawnAnimation);
            SetIconDirectly(currentRank.icon);
        }

        private void OnDisable()
        {
            //If it was in the middle of animations, make sure the correct icon is saved
            if (iconSwitchRoutine != null)
                CleanupIconRoutine();

            //If it was in the middle of animations, make sure the correct border is saved
            RemoveOldFracturedBorder();
            if (currentRank.border >= 0 && currentRank.border < bodies.Length)
                border.sharedMesh = bodies[currentRank.border].border;
            else
                border.sharedMesh = null;

            //If it was in the middle of animations, make sure the correct inside is saved
            CleanupInside();

            //If it was in the middle of animations, make sure the correct wings is saved
            if (wingsRoutine != null)
            {
                StopCoroutine(wingsRoutine);

                for (int index = 0; index < wings.Length; index++)
                {
                    if (wings[index].holder == null)
                        continue;

                    //This is the wing that should be on
                    if (index == currentRank.wings)
                        wings[index].holder.SetActive(true);
                    else
                        wings[index].holder.SetActive(false);
                }

                DirectUpdateWingPosition(currentRank.border, currentRank.wings);
            }
        }

        #endregion Unity Stuff

        #region Cleanup

        private void CleanupIconRoutine()
        {
            StopCoroutine(iconSwitchRoutine);
            SetIconPrimaryMaterial(currentRank.iconPrimaryMat);
            SetIconSecondaryMaterial(currentRank.iconSecondaryMat);
            SetIconDirectly(currentRank.icon);
        }

        private void RemoveOldFracturedBorder()
        {
            if (oldFractured_border != null)
                Destroy(oldFractured_border);

            if (newFractured_border != null)
                Destroy(newFractured_border);
        }

        private void CleanupInside()
        {
            if (enableInsideRoutine != null)
                StopCoroutine(enableInsideRoutine);

            if (oldFractured_Inside != null)
                Destroy(oldFractured_Inside);

            if (newFractured_Inside != null)
                Destroy(newFractured_Inside);

            //In case the previous inside animation wasn't completed and user changed rank too quickly, make sure the current rank's inside is applied before switching to new one.
            if (currentRank.inside != null && currentRank.inside.mesh != null)
            {
                inside.sharedMesh = currentRank.inside.mesh;
                inside.gameObject.SetActive(true);
            }
            else
            {
                inside.sharedMesh = null;
                inside.gameObject.SetActive(false);
            }
        }

        #endregion Cleanup

        /// <summary>
        ///  Plays an animation on the animator on the root of the game object.
        /// </summary>
        /// <param name="target"></param>
        public void PlaySpawnAnimation(int target) => animator.SetTrigger(target.ToString());

        #region Apply Rank

        /// <summary>
        /// Retrieves the rank from the ranks list at the specified index and applies it.
        /// If an invalid index is passed, it logs a warning message and returns. Doesn't print an error.
        /// </summary>
        /// <param name="index">The index of the rank in ranks list.</param>
        public void ApplyRank(int index)
        {
            if (index < 0 || index >= ranks.Count)
            {
                Debug.LogWarning("Invalid rank " + index + " is passed to apply rank.");
                return;
            }

            ApplyRank(ranks[index]);
        }

        /// <summary>
        /// Applies a rank from the ranks list by its exact name.
        /// </summary>
        /// <param name="rankName">The name of the rank to apply.</param>
        /// <remarks>
        /// If the rank is not found or the name is invalid, a warning is logged.
        /// </remarks>
        /// <example>
        /// <code>
        /// ApplyRank("Bronze 1"); // Applies the rank with the name "Bronze 1" if it exists.
        /// </code>
        /// </example>
        public void ApplyRank(string rankName)
        {
            if (string.IsNullOrEmpty(rankName))
            {
                Debug.LogWarning("Invalid rank string is passed to apply rank.");
                return;
            }

            Rank rank = ranks.FirstOrDefault(r => r.name.Equals(rankName, StringComparison.OrdinalIgnoreCase));

            if (rank != null)
                ApplyRank(rank);
            else
                Debug.LogWarning($"Rank '{rankName}' not found. Ensure the name matches exactly.");
        }

        /// <summary>
        /// Applies the rank passed as parameter
        /// </summary>
        /// <param name="targetRank"></param>
        public void ApplyRank(Rank targetRank)
        {
            int newRankIndex = ranks.IndexOf(targetRank);
            if (newRankIndex > currentRankIndex)
            {
                if (enableUpgradeEffect)
                {
                    if (upgradeParticle != null)
                        upgradeParticle.Play();
                    if (upgradeVisualEffect != null)
                        upgradeVisualEffect.Play();
                }
            }
            else if (newRankIndex < currentRankIndex)
            {
                if (enableDowngradeEffect)
                {
                    if (downgradeParticle != null)
                        downgradeParticle.Play();
                    if (downgradeVisualEffect != null)
                        downgradeVisualEffect.Play();
                }
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, gameObject.name + " rank updated.");
#endif
            currentRankIndex = newRankIndex;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            if (enableBorderRoutine != null)
                StopCoroutine(enableBorderRoutine);
            if (wingsRoutine != null)
                StopCoroutine(wingsRoutine);

            RemoveOldFracturedBorder();

            CleanupInside();

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                if (enableBorderRoutine != null)
                    StopCoroutine(enableBorderRoutine);
                if (enableInsideRoutine != null)
                    StopCoroutine(enableInsideRoutine);
                if (wingsRoutine != null)
                    StopCoroutine(wingsRoutine);
            }

            Undo.RecordObject(this, gameObject.name + " rank updated.");
#else
            if (enableBorderRoutine != null)
                StopCoroutine(enableBorderRoutine);
            if (enableInsideRoutine != null)
                StopCoroutine(enableInsideRoutine);
            if (wingsRoutine != null)
                StopCoroutine(wingsRoutine);
#endif
            currentRank.name = targetRank.name;

            SetIcon(targetRank);

            SetBorder(targetRank);

            SetInside(targetRank);

            SetWings(targetRank);

            //SetAddons(targetRank.addons);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        #endregion Apply Rank

        #region Icon

        #region Public

        /// <summary>
        /// Applies icon with the new rank's settings.
        /// </summary>
        /// <param name="targetRank"></param>
        public void SetIcon(Rank targetRank)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SetIconPrimaryMaterial(targetRank.iconPrimaryMat);
                SetIconSecondaryMaterial(targetRank.iconSecondaryMat);
                SetIconDirectly(targetRank.icon);
                return;
            }
#endif

            if (iconSwitchRoutine != null)
                CleanupIconRoutine();

            //currentRank.icon = targetRank.icon;
            currentRank.oldIconExitAudioClip = targetRank.oldIconExitAudioClip;
            currentRank.newIconEntryAudioClip = targetRank.newIconEntryAudioClip;
            currentRank.oldIconExitDelay = targetRank.oldIconExitDelay;
            currentRank.newIconEntryDelay = targetRank.newIconEntryDelay;
            currentRank.iconPrimaryMat = targetRank.iconPrimaryMat;
            currentRank.iconSecondaryMat = targetRank.iconSecondaryMat;

            iconSwitchRoutine = StartCoroutine(SetIconRoutine(currentRank.icon, targetRank.icon));
        }

        /// <summary>
        /// Applies icon with the currentRank's settings.
        /// </summary>
        /// <param name="icon"></param>
        public void SetIcon(int icon)
        {
            if (!Application.isPlaying)
            {
                SetIconDirectly(icon);
            }
            else
            {
                if (iconSwitchRoutine != null)
                    CleanupIconRoutine();

                iconSwitchRoutine = StartCoroutine(SetIconRoutine(currentRank.icon, icon));
            }
        }

        /// <summary>
        /// Removes icon. This is same as calling  SetIcon(0);
        /// </summary>
        public void DisableIcon() => SetIcon(0);

        /// <summary>
        /// It applies the icon skipping any animation settings in the current rank.
        /// </summary>
        /// <param name="icon"></param>
        public void SetIconDirectly(int icon)
        {
            iconAnimator.SetInteger("Icon", icon);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                SetRankFinalRank(icon);

            Undo.RecordObject(this, gameObject.name + " rank updated.");
#endif
            currentRank.icon = icon;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Retrieves the appropriate material from the materials list based on the given index and applies it to the icon.
        /// </summary>
        /// <param name="i"></param>
        public void SetIconPrimaryMaterial(int i)
        {
            if (!ValidMaterialIndex(i))
                return;

            if (iconsPrimary == null)
            {
                Debug.LogWarning("icons array is null.");
                return;
            }

            if (iconsPrimary.Length == 0)
            {
                Debug.LogWarning("icons array is empty.");
                return;
            }

#if UNITY_EDITOR
            string undoGroupName = gameObject.name + " icons material update";
            Undo.SetCurrentGroupName(undoGroupName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(iconsPrimary, undoGroupName);
#endif
            for (int index = 0; index < iconsPrimary.Length; index++)
            {
                if (iconsPrimary[index])
                    iconsPrimary[index].sharedMaterial = materials[i];
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroupName);
#endif
            currentRank.iconPrimaryMat = materials[i];
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        /// <summary>
        /// Applies the material passed as parameter to the icon.
        /// </summary>
        /// <param name="material"></param>
        public void SetIconPrimaryMaterial(Material material)
        {
            if (iconsPrimary == null)
            {
                Debug.LogWarning("icons array is null.");
                return;
            }

            if (iconsPrimary.Length == 0)
            {
                Debug.LogWarning("icons array is empty.");
                return;
            }

#if UNITY_EDITOR
            string undoGroupName = gameObject.name + " icons material update";
            Undo.SetCurrentGroupName(undoGroupName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(iconsPrimary, undoGroupName);
#endif
            for (int index = 0; index < iconsPrimary.Length; index++)
            {
                iconsPrimary[index].sharedMaterial = material;
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroupName);
#endif
            currentRank.iconPrimaryMat = material;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        /// <summary>
        ///  Retrieves the appropriate material from the materials list based on the given index and applies it to the icon.
        /// </summary>
        /// <param name="i"></param>
        public void SetIconSecondaryMaterial(int i)
        {
            if (!ValidMaterialIndex(i))
                return;

            if (iconsSecondary == null)
            {
                Debug.LogWarning("icons secondary array is null.");
                return;
            }

            if (iconsPrimary.Length == 0)
            {
                Debug.LogWarning("icons secondary array is empty.");
                return;
            }

#if UNITY_EDITOR
            string undoGroupName = gameObject.name + " icons secondary material update";
            Undo.SetCurrentGroupName(undoGroupName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(iconsSecondary, undoGroupName);
#endif
            for (int index = 0; index < iconsSecondary.Length; index++)
            {
                if (iconsSecondary[index])
                    iconsSecondary[index].sharedMaterial = materials[i];
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroupName);
#endif
            currentRank.iconSecondaryMat = materials[i];
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        /// <summary>
        /// Applies the material passed as parameter to the icon.
        /// </summary>
        /// <param name="material"></param>
        public void SetIconSecondaryMaterial(Material material)
        {
            if (iconsSecondary == null)
            {
                Debug.LogWarning("icons secondary array is null.");
                return;
            }

            if (iconsSecondary.Length == 0)
            {
                Debug.LogWarning("icons secondary array is empty.");
                return;
            }

#if UNITY_EDITOR
            string undoGroupName = gameObject.name + " icons secondary material update";
            Undo.SetCurrentGroupName(undoGroupName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(iconsSecondary, undoGroupName);
#endif
            for (int index = 0; index < iconsSecondary.Length; index++)
            {
                iconsSecondary[index].sharedMaterial = material;
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroupName);
#endif
            currentRank.iconSecondaryMat = material;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        #endregion Public

        private IEnumerator SetIconRoutine(int oldIcon, int newIcon)
        {
            //Wait out how long the old icon should stay for
            yield return new WaitForSeconds(currentRank.oldIconExitDelay);

            if (currentRank.newIconEntryDelay >= 0.25f)
            {
                if (audioSource != null && currentRank.oldIconExitAudioClip != null && oldIcon != newIcon)
                    audioSource.PlayOneShot(currentRank.oldIconExitAudioClip);

                iconAnimator.SetInteger("Icon", 0);
                yield return new WaitForSeconds(currentRank.newIconEntryDelay);
            }

            SetIconDirectly(newIcon);

            yield return new WaitForSeconds(0.25f);

            SetIconPrimaryMaterial(currentRank.iconPrimaryMat);
            SetIconSecondaryMaterial(currentRank.iconSecondaryMat);

            yield return new WaitForSeconds(0.3f);

            if (audioSource != null && currentRank.newIconEntryAudioClip != null && oldIcon != newIcon)
                audioSource.PlayOneShot(currentRank.newIconEntryAudioClip);
        }

        #endregion Icon

        #region Border

        #region Public

        /// <summary>
        /// Removes border and inside
        /// </summary>
        public void DisableBorder()
        {
            //No need for warning when disabling.
            if (border == null)
                return;

            if (Application.isPlaying)
                StartOldBorderRemoveAnimation();

#if UNITY_EDITOR
            string undoOperationName = gameObject.name + " border Update.";
            Undo.SetCurrentGroupName(undoOperationName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObject(this, undoOperationName);
#endif

            currentRank.border = -1;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.RecordObject(border.gameObject, undoOperationName);
#endif
            border.gameObject.SetActive(false);

#if UNITY_EDITOR
            if (Application.isPlaying)
                DisableInside();
            else
                EditMode_DisableInside_combineUndo(undoOperationName);
#else
            DisableInside();
#endif
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateWingPosition(undoOperationName); //Why this runs only in Edit mode?

#else
            //UpdateWingPosition();
#endif

#if UNITY_EDITOR
            Undo.CollapseUndoOperations(group);
#endif
        }

        public void SetBorder(Rank targetRank)
        {
            currentRank.transitionToSameBorder = targetRank.transitionToSameBorder;

            currentRank.oldBorderExitDelay = targetRank.oldBorderExitDelay;

            currentRank.oldBorderExitAudioClip = targetRank.oldBorderExitAudioClip;
            currentRank.oldBorderExitAudioClipDelay = targetRank.oldBorderExitAudioClipDelay;
            currentRank.newBorderEntryAudioClip = targetRank.newBorderEntryAudioClip;

            currentRank.oldBorderExitAnimationType = targetRank.oldBorderExitAnimationType;
            currentRank.oldBorderExitAnimation_physics_fracturedPiecesLifeTime = targetRank.oldBorderExitAnimation_physics_fracturedPiecesLifeTime;
            currentRank.oldBorderExitAnimation_physics_explosionForce = targetRank.oldBorderExitAnimation_physics_explosionForce;
            currentRank.oldBorderExitAnimation_physics_explosionRadius = targetRank.oldBorderExitAnimation_physics_explosionRadius;
            currentRank.oldBorderExitAnimation_physics_explosionUpwardsForce = targetRank.oldBorderExitAnimation_physics_explosionUpwardsForce;

            currentRank.newBorderEntryDelay = targetRank.newBorderEntryDelay;
            currentRank.newBorderEntryAnimationType = targetRank.newBorderEntryAnimationType;
            SetBorder(targetRank.border, false, false); //This updates wings position
            SetBorderMaterials(targetRank.borderMat);
        }

        /// <summary>
        /// Will disable border if negative value is passed as a parameter.
        /// Border Animation type needs to be set before this is called.
        /// </summary>
        /// <param name="newBorder">Will disable border if negative value is passed as a parameter.</param>
        /// <param name="autoUpdateInside">If the current inside isn't viable for the new border, auto update will assign a new one.</param>
        /// <param name="autoUpdateWingsPosition">If wings are changed along with this, set it false so that this doesn't handle updating the position and changing wings updates that.</param>
        public void SetBorder(int newBorder, bool autoUpdateInside = true, bool autoUpdateWingsPosition = true)
        {
            if (newBorder < 0)
            {
                DisableBorder();
                return;
            }

            if (border == null)
            {
                Debug.LogWarning("Border filter is not set");
                return;
            }

            if (bodies.Length == 0)
            {
                Debug.LogWarning("bodies is not set");
                return;
            }

            if (newBorder < 0 || newBorder >= bodies.Length)
            {
                Debug.LogError("Invalid index " + newBorder + " is being passed to the method SetBorder(int i). It should be between 0 and " + (bodies.Length - 1));
                return;
            }

#if UNITY_EDITOR
            string undoOperationName = gameObject.name + " border Update.";
            Undo.SetCurrentGroupName(undoOperationName);
            int group = Undo.GetCurrentGroup();

#endif
            if (Application.isPlaying)
            {
                if (currentRank.border >= 0 && (currentRank.transitionToSameBorder == false && newBorder >= 0 && bodies[newBorder].border == bodies[currentRank.border].border) == false)
                    StartOldBorderRemoveAnimation();

                if (newBorder >= 0) //If the new border isn't null
                {
                    if (bodies[newBorder].borderFractured != null)
                    {
                        if (enableBorderRoutine != null)
                            StopCoroutine(enableBorderRoutine);

                        if (newBorder >= 0 && (currentRank.transitionToSameBorder == false && currentRank.border >= 0 && bodies[newBorder].border == bodies[currentRank.border].border) == false)
                            enableBorderRoutine = StartCoroutine(BorderEnableAnimation(newBorder));
                        else
                            border.gameObject.SetActive(true);
                    }
                }
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoOperationName);
#endif
            currentRank.border = newBorder;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.RecordObject(border.gameObject, undoOperationName);
#endif
            if (!Application.isPlaying)
                border.gameObject.SetActive(true);
#if UNITY_EDITOR
            Undo.RecordObject(border, undoOperationName);
#endif
            border.sharedMesh = bodies[newBorder].border;

#if UNITY_EDITOR
            if (autoUpdateInside)
            {
                if (!Application.isPlaying)
                    UpdateInsideUatomaticallyAfterBorderChange(newBorder, undoOperationName, group);
                else
                    UpdateInsideUatomaticallyAfterBorderChange(newBorder);
            }

#else
            if (autoUpdateInside)
                UpdateInsideUatomaticallyAfterBorderChange(newBorder);
#endif

            if (autoUpdateWingsPosition)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UpdateWingPosition(undoOperationName);
                else
                    UpdateWingPosition();
#else
                UpdateWingPosition();
#endif
            }
        }

        /// <summary>
        /// Retrieves the appropriate material from the materials list based on the given index and applies it to the border.
        /// </summary>
        /// <param name="i">Index of the material in materials list</param>
        public void SetBorderMaterials(int i)
        {
            if (borderRenderer == null)
            {
                Debug.LogWarning(gameObject.name + " Border renderer is not set");
                return;
            }

            if (!ValidMaterialIndex(i))
                return;
#if UNITY_EDITOR
            string undoOperationName = gameObject.name + " Material Update";
            Undo.RecordObject(this, undoOperationName);
#endif
            currentRank.borderMat = materials[i];
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.RecordObject(borderRenderer, undoOperationName);
#endif
            borderRenderer.sharedMaterial = materials[i];
        }

        /// <summary>
        /// Applies the material passed as parameter to the border.
        /// </summary>
        /// <param name="material"></param>
        public void SetBorderMaterials(Material material)
        {
            if (borderRenderer == null)
            {
                Debug.LogWarning(gameObject.name + " Border renderer is not set");
                return;
            }

#if UNITY_EDITOR
            string undoOperationName = gameObject.name + " Material Update";
            Undo.RecordObject(this, undoOperationName);
#endif
            currentRank.borderMat = material;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.RecordObject(borderRenderer, undoOperationName);
#endif
            borderRenderer.sharedMaterial = material;
        }

        #endregion Public

        private void StartOldBorderRemoveAnimation()
        {
            if (currentRank.border >= 0) //If it is less than zero, there was no border before and no need to play animation
            {
                if (bodies[currentRank.border].borderFractured != null)
                {
                    oldFractured_border = Instantiate(bodies[currentRank.border].borderFractured, root);
                    oldFractured_border.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    if (oldFractured_border.TryGetComponent<PieceAnimationController>(out PieceAnimationController pieceAnimationController))
                        pieceAnimationController.BeginExitAnimation(
                            currentRank.borderMat,
                            currentRank.oldBorderExitAnimationType,
                            currentRank.oldBorderExitAnimation_physics_fracturedPiecesLifeTime,
                            currentRank.oldBorderExitAnimation_physics_explosionForce,
                            currentRank.oldBorderExitAnimation_physics_explosionRadius,
                            currentRank.oldBorderExitAnimation_physics_explosionUpwardsForce,
                            currentRank.oldBorderExitDelay,
                            audioSource,
                    currentRank.oldBorderExitAudioClip,
                    currentRank.oldBorderExitAudioClipDelay
                            );
                }
            }
        }

        private void StartOldInsideRemoveAnimation(Inside oldInside)
        {
            if (oldInside == null)
                return;

            if (oldInside.fractured != null)
            {
                if (oldFractured_Inside != null)
                    Destroy(oldFractured_Inside);

                oldFractured_Inside = Instantiate(oldInside.fractured, root);
                oldFractured_Inside.transform.localPosition = Vector3.zero;
                oldFractured_Inside.transform.localRotation = Quaternion.identity;
                if (oldFractured_Inside.TryGetComponent<PieceAnimationController>(out PieceAnimationController pieceAnimationController))
                    pieceAnimationController.BeginExitAnimation(
                        currentRank.insideMat,
                        currentRank.oldInsideExitAnimationType,
                        currentRank.oldInsideExitAnimation_physics_fracturedPiecesLifeTime,
                        currentRank.oldInsideExitAnimation_physics_explosionForce,
                        currentRank.oldInsideExitAnimation_physics_explosionRadius,
                        currentRank.oldInsideExitAnimation_physics_explosionUpwardsForce,
                        currentRank.oldInsideExitDelay,
                        audioSource,
                        currentRank.oldInsideExitAudioClip,
                        currentRank.oldInsideExitAudioClipDelay
                        );
            }
        }

        private IEnumerator BorderEnableAnimation(int i)
        {
            border.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame(); //Allow time pass for new material to be assigned
            yield return new WaitForSeconds(currentRank.newBorderEntryDelay);
            //
            if (newFractured_border != null)
                Destroy(newFractured_border);

            newFractured_border = Instantiate(bodies[i].borderFractured, root);
            newFractured_border.transform.localPosition = Vector3.zero;
            newFractured_border.transform.localRotation = Quaternion.identity;

            if (newFractured_border.TryGetComponent<PieceAnimationController>(out PieceAnimationController pieceAnimationController))
                pieceAnimationController.BeginEntryAnimation(currentRank.borderMat, currentRank.newBorderEntryAnimationType);

            //Destroy(newFractured_border, 2);
            yield return new WaitForSeconds(2);
            border.gameObject.SetActive(true);
            Destroy(newFractured_border);
        }

        #endregion Border

        #region Inside

        #region Public

        /// <summary>
        /// Removes current inside
        /// </summary>
        public void DisableInside()
        {
            SetInside((Inside)null);
        }

        /// <summary>
        /// Applies new inside according to currentRank's animation settings.
        /// </summary>
        /// <param name="i"></param>
        public void SetInside(int i)
        {
            if (currentRank.border < 0)
                return;

            if (i < 0 || i >= bodies[currentRank.border].inside.Length)
                SetInside((Inside)null);
            else
                SetInside(bodies[currentRank.border].inside[i]);
        }

        /// <summary>
        /// Applies new inside according to currentRank's animation settings.
        /// </summary>
        /// <param name="newInside"></param>
        public void SetInside(Inside newInside)
        {
            if (bodies.Length == 0)
            {
                Debug.LogWarning("bodies is not set");
                return;
            }

            if (Application.isPlaying)
            {
                //if (currentRank.inside != null && newInside != null && (currentRank.transitionToSameInside == false && newInside.mesh == currentRank.inside.mesh) == false)
                if (CanRemoveOldInside(newInside))
                    StartOldInsideRemoveAnimation(currentRank.inside);

                if (newInside != null && newInside.mesh != null && newInside.fractured != null
                    && (currentRank.transitionToSameInside == false && newInside.mesh == currentRank.inside.mesh) == false)
                {
                    if (enableInsideRoutine != null)
                        StopCoroutine(enableInsideRoutine);

                    enableInsideRoutine = StartCoroutine(InsideEnableAnimation(newInside));
                }
                else
                {
                    if (enableInsideRoutine != null)
                        StopCoroutine(enableInsideRoutine);

                    if (newInside != null)
                        inside.sharedMesh = newInside.mesh;
                    else
                        inside.sharedMesh = null;
                }
            }
            else
            {
                if (newInside == null)
                    inside.sharedMesh = null;
                else
                {
                    inside.sharedMesh = newInside.mesh;
                    inside.gameObject.SetActive(true);
                }
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, "Inside Updated.");
#endif
            currentRank.inside ??= new Inside();
            if (newInside != null)
            {
                //Directly assigning newInside to inside often leaves a direct reference in the editor, causing weird behavior when modifying in customize tab
                currentRank.inside.mesh = newInside.mesh;
                currentRank.inside.fractured = newInside.fractured;
            }
            else
            {
                currentRank.inside.mesh = null;
                currentRank.inside.fractured = null;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private bool CanRemoveOldInside(Inside newInside)
        {
            //If there is no old inside
            if (currentRank.inside == null)
                return false;

            if (newInside != null && !currentRank.transitionToSameInside && (newInside.mesh == currentRank.inside.mesh))
                return false;

            return true;
        }

        /// <summary>
        /// Retrieves the appropriate material from the materials list based on the given index and applies it to the inside
        /// </summary>
        /// <param name="i"></param>
        public void SetInsideMaterial(int i)
        {
            if (insideRenderer == null)
            {
                Debug.LogWarning(gameObject.name + "Inside renderer is not set", gameObject);
                return;
            }

            if (!ValidMaterialIndex(i))
                return;

#if UNITY_EDITOR
            Undo.SetCurrentGroupName("Inside Materials Update");
            int group = Undo.GetCurrentGroup();
            string undoOperationName = gameObject.name + " Wings Update.";
#endif
            currentRank.insideMat = materials[i];
            insideRenderer.sharedMaterial = materials[i];
        }

        /// <summary>
        /// Applies the material passed as parameter to the inside.
        /// </summary>
        /// <param name="material"></param>
        public void SetInsideMaterial(Material material)
        {
            if (insideRenderer == null)
            {
                Debug.LogWarning(gameObject.name + " Inside renderer is not set", gameObject);
                return;
            }

#if UNITY_EDITOR
            string undoOperationName = gameObject.name + " Inside Material Update";
            Undo.RecordObject(this, undoOperationName);
#endif
            currentRank.insideMat = material;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.RecordObject(borderRenderer, undoOperationName);
#endif
            insideRenderer.sharedMaterial = material;
        }

        #endregion Public

        private void SetInside(Rank targetRank)
        {
            currentRank.transitionToSameInside = targetRank.transitionToSameInside;

            currentRank.oldInsideExitDelay = targetRank.oldInsideExitDelay;
            currentRank.oldInsideExitAudioClip = targetRank.oldInsideExitAudioClip;
            currentRank.oldInsideExitAnimationType = targetRank.oldInsideExitAnimationType;
            currentRank.oldInsideExitAnimation_physics_fracturedPiecesLifeTime = targetRank.oldInsideExitAnimation_physics_fracturedPiecesLifeTime;
            currentRank.oldInsideExitAnimation_physics_explosionForce = targetRank.oldInsideExitAnimation_physics_explosionForce;
            currentRank.oldInsideExitAnimation_physics_explosionRadius = targetRank.oldInsideExitAnimation_physics_explosionRadius;
            currentRank.oldInsideExitAnimation_physics_explosionUpwardsForce = targetRank.oldInsideExitAnimation_physics_explosionUpwardsForce;

            currentRank.newInsideEntryAnimationType = targetRank.newInsideEntryAnimationType;
            currentRank.newInsideEntryDelay = targetRank.newInsideEntryDelay;
            SetInside(targetRank.inside);
            SetInsideMaterial(targetRank.insideMat);
        }

#if UNITY_EDITOR

        /// <summary>
        /// Doesn't run in play-mode, so it doesn't need to process animations
        /// </summary>
        /// <param name="undoOperationName"></param>
        private void EditMode_DisableInside_combineUndo(string undoOperationName)
        {
            if (inside == null)
            {
                Debug.LogWarning("Inside mesh filter is not set");
                return;
            }
            Undo.RecordObject(this, undoOperationName);

            currentRank.inside.mesh = null;
            currentRank.inside.fractured = null;

            EditorUtility.SetDirty(this);
            Undo.RecordObject(inside.gameObject, undoOperationName);
            inside.mesh = null;
            //inside.gameObject.SetActive(false);
        }

#endif

        private IEnumerator InsideEnableAnimation(Inside newInside)
        {
            inside.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame(); //Allow time pass for new material to be assigned
            yield return new WaitForSeconds(currentRank.newInsideEntryDelay);
            //
            if (newFractured_Inside != null)
                Destroy(newFractured_Inside);

            newFractured_Inside = Instantiate(newInside.fractured, root);
            newFractured_Inside.transform.localPosition = Vector3.zero;
            newFractured_Inside.transform.localRotation = Quaternion.identity;

            if (newFractured_Inside.TryGetComponent<PieceAnimationController>(out PieceAnimationController pieceAnimationController))
                pieceAnimationController.BeginEntryAnimation(currentRank.insideMat, currentRank.newInsideEntryAnimationType);

            yield return new WaitForSeconds(2);
            inside.sharedMesh = newInside.mesh;
            inside.gameObject.SetActive(true);
            Destroy(newFractured_Inside);
        }

        private void UpdateInsideUatomaticallyAfterBorderChange(int i, string undoOperationName = "", int group = 0)
        {
            //Already contains an acceptable mesh
            for (int j = 0; j < bodies[i].inside.Length; j++)
            {
                if (bodies[i].inside[j].mesh == inside.sharedMesh)
                {
#if UNITY_EDITOR
                    Undo.CollapseUndoOperations(group);
#endif
                    return;
                }
            }

            if (currentRank.inside.mesh != null)
            {
                for (int j = 0; j < bodies[i].inside.Length; j++)
                {
                    //If selected inside is applicable for this border, do not change selected item number
                    if (currentRank.inside.mesh == bodies[i].inside[j].mesh)
                        return;
                }
            }

            //If selected inside index is not applicable for this border, select the first inside available
            if (bodies[i].inside.Length > 0 && currentRank.inside.mesh != null) //If there were no inside selected, there is no need to select one now.
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, undoOperationName);
#endif
                //currentRank.inside = 0;
                currentRank.inside.mesh = bodies[i].inside[0].mesh;
                currentRank.inside.fractured = bodies[i].inside[0].fractured;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                Undo.RecordObject(inside, undoOperationName);
#endif
                inside.sharedMesh = bodies[i].inside[0].mesh;
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(group);
#endif
            }
            //If no inside is available, set it to null.
            else
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, undoOperationName);
#endif
                currentRank.inside.mesh = null;
                currentRank.inside.fractured = null;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                Undo.RecordObject(inside, undoOperationName);
#endif
                inside.sharedMesh = null;
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(group);
#endif
            }
        }

        #endregion Inside

        #region Wings

        #region Public

        public void DisableWings()
        {
            SetWing(-1);
        }

        private void SetWings(Rank targetRank)
        {
            currentRank.oldWingsExitAudioClip = targetRank.oldWingsExitAudioClip;
            currentRank.newWingsEntryAudioClip = targetRank.newWingsEntryAudioClip;
            currentRank.oldWingsExitAnimationStartDelay = targetRank.oldWingsExitAnimationStartDelay;
            currentRank.oldWingsExitPositionAnimationDuration = targetRank.oldWingsExitPositionAnimationDuration;
            currentRank.oldWingsExitMovementCurve = targetRank.oldWingsExitMovementCurve;
            currentRank.newWingsEntryAnimationStartDelay = targetRank.newWingsEntryAnimationStartDelay;
            currentRank.newWingsStartPositionAnimationDuration = targetRank.newWingsStartPositionAnimationDuration;
            currentRank.newWingsInMovementCurve = targetRank.newWingsInMovementCurve;
            SetWing(targetRank.wings); //This updates wings position too
            SetWingsMaterial(targetRank.wingsMat);
        }

        public void SetWing(int i)
        {
            if (wings == null)
            {
                Debug.LogWarning("Wings array is null.");
                return;
            }

            if (wings.Length == 0)
            {
                Debug.LogWarning("Wings array is empty.");
                return;
            }

            if (i >= wings.Length)
            {
                Debug.LogError("Invalid index " + i + " is being passed to the method SetWing(int i). It should be below" + (wings.Length - 1));
                return;
            }
#if UNITY_EDITOR
            Undo.SetCurrentGroupName("Wings material update");
            int group = Undo.GetCurrentGroup();
            string undoOperationName = gameObject.name + " Wings Update.";
            Undo.RecordObject(this, undoOperationName);
#endif

            int oldWings = currentRank.wings;
            currentRank.wings = i;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);

            if (Application.isPlaying)
                SetWingsMaterial(currentRank.wingsMat);
            else
                SetWingsMaterial_combineUndo(currentRank.wingsMat, undoOperationName);
#else
            SetWingsMaterial(currentRank.wingsMat);
#endif
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (oldWings >= 0 && oldWings < wings.Length)
                {
                    Undo.RecordObject(wings[oldWings].holder, undoOperationName);
                    wings[oldWings].holder.SetActive(false);
                }
                if (i >= 0 && i < wings.Length)
                {
                    Undo.RecordObject(wings[i].holder, undoOperationName);
                    wings[i].holder.SetActive(true);
                    UpdateWingPosition(undoOperationName);
                }
            }
            else
                UpdateWingPosition(oldWings);
#else
                UpdateWingPosition(oldWings);
#endif

#if UNITY_EDITOR

            Undo.CollapseUndoOperations(group);
#endif
        }

        /// <summary>
        /// Retrieves the appropriate material from the materials list based on the given index and applies it to the wings
        /// </summary>
        /// <param name="i"></param>
        public void SetWingsMaterial(int i)
        {
            if (!ValidMaterialIndex(i))
                return;

            if (wings == null)
            {
                Debug.LogWarning("Wings array is null.");
                return;
            }

            if (wings.Length == 0)
            {
                Debug.LogWarning("Wings array is empty.");
                return;
            }

            if (currentRank.wings < 0 || currentRank.wings >= wings.Length)
            {
                Debug.LogError("Invalid index " + currentRank.wings + " is saved in current rank when calling the method SetWing(int i). It should be between 0 and " + (wings.Length - 1));
                return;
            }

#if UNITY_EDITOR
            string undoGroupName = gameObject.name + " Wings Material update";
            Undo.SetCurrentGroupName(undoGroupName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(wings[currentRank.wings].renderers, undoGroupName);
#endif
            for (int index = 0; index < wings[currentRank.wings].renderers.Length; index++)
            {
                wings[currentRank.wings].renderers[index].sharedMaterial = materials[i];
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroupName);
#endif
            currentRank.wingsMat = materials[i];
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        public void SetWingsMaterial(Material mat)
        {
            if (wings == null)
            {
                Debug.LogWarning("Wings array is null.");
                return;
            }

            if (wings.Length == 0)
            {
                Debug.LogWarning("Wings array is empty.");
                return;
            }

            // negative if no wings is set. Do not do anything in that case
            if (currentRank.wings < 0 || currentRank.wings >= wings.Length)
            {
                return;
            }

#if UNITY_EDITOR
            string undoGroundName = gameObject.name + " wings material update";
            Undo.SetCurrentGroupName(undoGroundName);
            int group = Undo.GetCurrentGroup();

            Undo.RecordObjects(wings[currentRank.wings].renderers, undoGroundName);
#endif
            for (int index = 0; index < wings[currentRank.wings].renderers.Length; index++)
            {
                wings[currentRank.wings].renderers[index].sharedMaterial = mat;
            }
#if UNITY_EDITOR
            Undo.RecordObject(this, undoGroundName);
#endif
            currentRank.wingsMat = mat;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(group);
#endif
        }

        #endregion Public

#if UNITY_EDITOR

        private void UpdateWingPosition(string undoOperationName)
        {
            int currentBody = currentRank.border;
            int currentWings = currentRank.wings;

            if (currentWings >= 0)
            {
                if (currentBody >= 0 && currentBody < bodies.Length)
                {
                    Undo.RecordObject(wings[currentWings].leftWing, undoOperationName);
                    wings[currentWings].leftWing.localPosition = bodies[currentRank.border].leftWing_position;
                    wings[currentWings].leftWing.localEulerAngles = bodies[currentRank.border].leftWing_rotation;
                    Undo.RecordObject(wings[currentWings].rightWing, undoOperationName);
                    wings[currentWings].rightWing.localPosition = bodies[currentRank.border].rightWing_position;
                    wings[currentWings].rightWing.localEulerAngles = bodies[currentRank.border].rightWing_rotation;
                }
                else
                {
                    Undo.RecordObject(wings[currentWings].leftWing, undoOperationName);
                    wings[currentWings].leftWing.localPosition = Vector3.zero;
                    wings[currentWings].leftWing.localRotation = Quaternion.identity;
                    Undo.RecordObject(wings[currentWings].rightWing, undoOperationName);
                    wings[currentWings].rightWing.localPosition = Vector3.zero;
                    wings[currentWings].rightWing.localRotation = Quaternion.identity;
                }
            }
        }

#endif

        private void UpdateWingPosition(int oldWings)
        {
            int currentWings = currentRank.wings;
            int currentBorder = currentRank.border;

            if (!Application.isPlaying)
            {
                if (currentWings >= 0)
                    DirectUpdateWingPosition(currentBorder, currentWings);
            }
            else
            {
                Wing toDisable = null;
                if (oldWings >= 0 && oldWings < wings.Length)
                    toDisable = wings[oldWings];

                Vector3 leftWingPosition;
                Vector3 leftWingRotation;
                Vector3 rightWingPosition;
                Vector3 rightWingRotation;

                if (currentBorder >= 0)
                {
                    leftWingPosition = bodies[currentRank.border].leftWing_position;
                    leftWingRotation = bodies[currentRank.border].leftWing_rotation;
                    rightWingPosition = bodies[currentRank.border].rightWing_position;
                    rightWingRotation = bodies[currentRank.border].rightWing_rotation;
                }
                else
                {
                    leftWingPosition = Vector3.zero;
                    leftWingRotation = Vector3.zero;
                    rightWingPosition = Vector3.zero;
                    rightWingRotation = Vector3.zero;
                }

                wingsRoutine = StartCoroutine(UpdateWingPositionRoutine(
                     leftWingPosition,
                     leftWingRotation,
                     rightWingPosition,
                     rightWingRotation,
                     toDisable
                     ));
            }
        }

        private void UpdateWingPosition()
        {
            int currentBorder = currentRank.border;
            int currentWings = currentRank.wings;

            if (currentWings >= 0 && currentWings < wings.Length)
            {
                if (!Application.isPlaying)
                {
                    DirectUpdateWingPosition(currentBorder, currentWings);
                }
                else
                {
                    Vector3 leftWingPosition;
                    Vector3 leftWingRotation;
                    Vector3 rightWingPosition;
                    Vector3 rightWingRotation;

                    if (currentBorder >= 0)
                    {
                        leftWingPosition = bodies[currentRank.border].leftWing_position;
                        leftWingRotation = bodies[currentRank.border].leftWing_rotation;
                        rightWingPosition = bodies[currentRank.border].rightWing_position;
                        rightWingRotation = bodies[currentRank.border].rightWing_rotation;
                    }
                    else
                    {
                        leftWingPosition = Vector3.zero;
                        leftWingRotation = Vector3.zero;
                        rightWingPosition = Vector3.zero;
                        rightWingRotation = Vector3.zero;
                    }

                    if (wingsRoutine != null)
                        StopCoroutine(wingsRoutine);

                    wingsRoutine = StartCoroutine(UpdateWingPositionRoutine(
                        leftWingPosition,
                        leftWingRotation,
                        rightWingPosition,
                        rightWingRotation,
                        null,
                        false
                        ));
                }
            }
        }

        private void DirectUpdateWingPosition(int currentBorder, int currentWings)
        {
            if (currentWings < 0 || currentWings >= wings.Length)
                return;

            if (currentBorder >= 0 && currentBorder < bodies.Length)
            {
                wings[currentWings].leftWing.localPosition = bodies[currentBorder].leftWing_position;
                wings[currentWings].leftWing.localEulerAngles = bodies[currentBorder].leftWing_rotation;
                wings[currentWings].rightWing.localPosition = bodies[currentBorder].rightWing_position;
                wings[currentWings].rightWing.localEulerAngles = bodies[currentBorder].rightWing_rotation;
            }
            else
            {
                wings[currentWings].leftWing.localPosition = Vector3.zero;
                wings[currentWings].leftWing.localRotation = Quaternion.identity;
                wings[currentWings].rightWing.localPosition = Vector3.zero;
                wings[currentWings].rightWing.localRotation = Quaternion.identity;
            }
        }

        private IEnumerator UpdateWingPositionRoutine(Vector3 leftWingPosition, Vector3 leftWingRotation, Vector3 rightWingPosition, Vector3 rightWingRotation, Wing toDisable = null, bool scaleUpNewWing = true)
        {
            Transform leftWing = null;
            Transform rightWing = null;

            if (currentRank.wings >= 0 && currentRank.wings < wings.Length)
            {
                leftWing = wings[currentRank.wings].leftWing;
                rightWing = wings[currentRank.wings].rightWing;
            }

            Vector3 startPos1;
            Vector3 startPos2;

            Vector3 startRot1;
            Vector3 startRot2;

            float elapsed = 0f;

            if (audioSource != null && currentRank.oldWingsExitAudioClip != null)
                audioSource.PlayOneShot(currentRank.oldWingsExitAudioClip);

            //if (toDisable != null && (toDisable.leftWing != leftWing && toDisable.rightWing != rightWing))
            if (toDisable != null && (toDisable.leftWing != leftWing && toDisable.rightWing != rightWing))
            {
                startPos1 = toDisable.leftWing.localPosition;
                startPos2 = toDisable.rightWing.localPosition;

                startRot1 = NormalizeAngles(toDisable.leftWing.localEulerAngles);
                startRot2 = NormalizeAngles(toDisable.rightWing.localEulerAngles);

                var endPosition = new Vector3(0, 0, Random.Range(-2f, -1f));

                yield return new WaitForSeconds(currentRank.oldWingsExitAnimationStartDelay);

                while (elapsed < currentRank.oldWingsExitPositionAnimationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / currentRank.oldWingsExitPositionAnimationDuration);
                    float curveValue = currentRank.oldWingsExitMovementCurve.Evaluate(t); // Apply easing curve

                    // Move & Rotate both objects locally
                    toDisable.leftWing.localPosition = Vector3.Lerp(startPos1, endPosition, curveValue);
                    toDisable.leftWing.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, curveValue);
                    toDisable.leftWing.localEulerAngles = new Vector3(
                                                          Mathf.LerpAngle(startRot1.x, 0, curveValue),
                                                          Mathf.LerpAngle(startRot1.y, 0, curveValue),
                                                          Mathf.LerpAngle(startRot1.z, 0, curveValue)
                                                          );
                    toDisable.rightWing.localPosition = Vector3.Lerp(startPos2, endPosition, curveValue);
                    toDisable.rightWing.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, curveValue);
                    toDisable.rightWing.localEulerAngles = new Vector3(
                                                          Mathf.LerpAngle(startRot2.x, 0, curveValue),
                                                          Mathf.LerpAngle(startRot2.y, 0, curveValue),
                                                          Mathf.LerpAngle(startRot2.z, 0, curveValue)
                                                          );

                    toDisable.rightWing.localEulerAngles = Vector3.Lerp(startRot2, Vector3.zero, curveValue);

                    yield return null;
                }

                // Ensure final positions & rotations are exact
                toDisable.holder.SetActive(false);
            }

            yield return new WaitForSeconds(currentRank.newWingsEntryAnimationStartDelay);

            if (currentRank.wings < 0 || currentRank.wings >= wings.Length)
                yield break;

            leftWing = wings[currentRank.wings].leftWing;
            rightWing = wings[currentRank.wings].rightWing;

            Vector3 startScale;
            Vector3 endScale;

            if (audioSource != null && currentRank.newWingsEntryAudioClip != null)
                audioSource.PlayOneShot(currentRank.newWingsEntryAudioClip);

            //If switching to same wing
            if ((toDisable != null && toDisable.leftWing == leftWing && toDisable.rightWing == rightWing) || !scaleUpNewWing)
            {
                startPos1 = leftWing.localPosition;
                startPos2 = rightWing.localPosition;

                startRot1 = NormalizeAngles(leftWing.localEulerAngles);
                startRot2 = NormalizeAngles(rightWing.localEulerAngles);

                startScale = leftWing.localScale;
            }
            else
            {
                startPos1 = new Vector3(0, 0, Random.Range(-2f, -1f));
                startPos2 = new Vector3(0, 0, Random.Range(-2f, -1f));

                startRot1 = Vector3.zero;
                startRot2 = Vector3.zero;

                startScale = Vector3.zero;
            }

            endScale = Vector3.one;

            elapsed = 0f;

            wings[currentRank.wings].holder.SetActive(true);
            while (elapsed < currentRank.newWingsStartPositionAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / currentRank.newWingsStartPositionAnimationDuration);
                float curveValue = currentRank.newWingsInMovementCurve.Evaluate(t); // Apply easing curve

                // Move & Rotate both objects locally
                leftWing.localPosition = Vector3.Lerp(startPos1, leftWingPosition, curveValue);
                leftWing.localScale = Vector3.Lerp(startScale, endScale, curveValue);
                leftWing.localEulerAngles = new Vector3(
                                          Mathf.LerpAngle(startRot1.x, leftWingRotation.x, curveValue),
                                          Mathf.LerpAngle(startRot1.y, leftWingRotation.y, curveValue),
                                          Mathf.LerpAngle(startRot1.z, leftWingRotation.z, curveValue)
                                          );

                rightWing.localPosition = Vector3.Lerp(startPos2, rightWingPosition, curveValue);
                rightWing.localScale = Vector3.Lerp(startScale, endScale, curveValue);
                rightWing.localEulerAngles = new Vector3(
                                             Mathf.LerpAngle(startRot2.x, rightWingRotation.x, curveValue),
                                             Mathf.LerpAngle(startRot2.y, rightWingRotation.y, curveValue),
                                             Mathf.LerpAngle(startRot2.z, rightWingRotation.z, curveValue)
                                             );
                yield return null;
            }

            // Ensure final positions & rotations are exact
            leftWing.localPosition = leftWingPosition;
            leftWing.localEulerAngles = leftWingRotation;
            rightWing.localPosition = rightWingPosition;
            rightWing.localEulerAngles = rightWingRotation;

            leftWing.localScale = Vector3.one;
            rightWing.localScale = Vector3.one;
        }

        private Vector3 NormalizeAngles(Vector3 angles)
        {
            return new Vector3(
                NormalizeAngle(angles.x),
                NormalizeAngle(angles.y),
                NormalizeAngle(angles.z)
            );
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

#if UNITY_EDITOR

        /// <summary>
        /// This joins an undo operation and doesn't close it
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="operationName"></param>
        internal void SetWingsMaterial_combineUndo(Material mat, string operationName)
        {
            if (mat == null)
                return;

            if (wings == null)
            {
                Debug.LogWarning("Wings array is null.");
                return;
            }

            if (wings.Length == 0)
            {
                Debug.LogWarning("Wings array is empty.");
                return;
            }

            Undo.RecordObject(this, operationName);
            currentRank.wingsMat = mat;

            if (currentRank.wings < 0 || currentRank.wings >= wings.Length)
                return;

            Undo.RecordObjects(wings[currentRank.wings].renderers, operationName);
            for (int index = 0; index < wings[currentRank.wings].renderers.Length; index++)
            {
                wings[currentRank.wings].renderers[index].sharedMaterial = mat;
            }
        }

#endif

        #endregion Wings

        //        public void DisableAllAddon()
        //        {
        //            if (addons == null)
        //            {
        //                Debug.LogWarning("Add-ons array is null.");
        //                return;
        //            }

        //#if UNITY_EDITOR
        //            string groupName = gameObject.name + " add-ons disabled.";
        //            Undo.SetCurrentGroupName(groupName);
        //            int group = Undo.GetCurrentGroup();

        //            Undo.RecordObjects(addons, groupName);
        //#endif
        //            foreach (var addon in addons)
        //            {
        //                addon.SetActive(false);
        //            }

        //#if UNITY_EDITOR
        //            Undo.RecordObject(this, groupName);
        //#endif
        //            currentRank.addons.Clear();
        //#if UNITY_EDITOR
        //            EditorUtility.SetDirty(this);
        //            Undo.CollapseUndoOperations(group);
        //#endif
        //        }

        //        public void ToggleAddon(int i)
        //        {
        //            if (addons == null)
        //            {
        //                Debug.LogWarning("Add-ons array is null.");
        //                return;
        //            }

        //            if (addons.Length == 0)
        //            {
        //                Debug.LogWarning("Add-ons array is empty.");
        //                return;
        //            }

        //            if (i < 0 || i >= addons.Length)
        //            {
        //                Debug.LogError("Invalid index " + i + " is being passed to the method ToggleAddon(int i). It should be between 0 and " + (addons.Length - 1));
        //                return;
        //            }

        //            if (addons[i] == null)
        //            {
        //                Debug.LogError("Addon " + i + " is null and being passed to the method ToggleAddon(int i).");
        //                return;
        //            }

        //#if UNITY_EDITOR
        //            Undo.RecordObject(addons[i], addons[i].name + " toggle");
        //#endif
        //            addons[i].SetActive(!addons[i].activeSelf);

        //#if UNITY_EDITOR
        //            Undo.RecordObject(this, addons[i].name + " toggle");
        //#endif
        //            if (addons[i].activeSelf)
        //            {
        //                if (!currentRank.addons.Contains(addons[i]))
        //                    currentRank.addons.Add(addons[i]);
        //            }
        //            else //Addon got turned off
        //            {
        //                if (currentRank.addons.Contains(addons[i]))
        //                    currentRank.addons.Remove(addons[i]);
        //            }
        //#if UNITY_EDITOR
        //            EditorUtility.SetDirty(this);
        //#endif
        //        }

        //        public void SetAddons(List<GameObject> targetAddons)
        //        {
        //            if (addons == null)
        //            {
        //                Debug.LogWarning("Add-ons array is null.");
        //                return;
        //            }

        //            if (addons.Length == 0)
        //            {
        //                Debug.LogWarning("Add-ons array is empty.");
        //                return;
        //            }

        //#if UNITY_EDITOR
        //            Undo.RecordObject(this, addons + " updated");
        //#endif

        //            currentRank.addons.Clear();

        //#if UNITY_EDITOR
        //            Undo.RecordObjects(addons, addons + " updated");
        //#endif
        //            foreach (GameObject addon in addons)
        //            {
        //                if (targetAddons.Contains(addon))
        //                {
        //                    addon.SetActive(true);
        //                    currentRank.addons.Add(addon);
        //                }
        //                addon.SetActive(false);
        //            }
        //#if UNITY_EDITOR
        //            EditorUtility.SetDirty(this);
        //#endif
        //        }

        //Returns if the parameter is correct
        private bool ValidMaterialIndex(int i)
        {
            if (materials.Length == 0)
            {
                Debug.LogWarning("Materials array is empty.");
                return false;
            }

            if (i < 0 || i >= materials.Length)
            {
                Debug.LogError("Invalid index " + i + " is being passed. It should be between 0 and " + (materials.Length - 1));
                return false;
            }
            return true;
        }

        public void DisableEverything()
        {
            SetIconDirectly(0);
            DisableBorder();
            DisableInside();
            DisableWings();
            //DisableAllAddon();
        }

        public bool IsRankNameNew(string nameToCheck)
        {
            foreach (Rank rank in ranks)
            {
                if (rank.name == nameToCheck)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns -1 if rank with that name doesn't exist
        /// </summary>
        /// <param name="nameToCheck"></param>
        /// <returns></returns>
        public int IndexOfRank(string nameToCheck)
        {
            for (int i = 0; i < ranks.Count; i++)
            {
                if (ranks[i].name == nameToCheck)
                    return i;
            }
            return -1;
        }

#if UNITY_EDITOR

        /// <summary>
        /// This changes the current icon to the clip's final frame rank.\n
        /// Editor only
        /// </summary>
        /// <param name="target"></param>
        internal void SetRankFinalRank(int target)
        {
            if (iconAnimator == null)
                return;

            SetClipAsCurrentRank(iconAnimationClips[target]);
        }

        private void SetClipAsCurrentRank(AnimationClip targetClip)
        {
            // Calculate the end time of the animation clip
            float animationEndTime = targetClip.length;

            // Apply the animation rank to the GameObject
            targetClip.SampleAnimation(iconAnimator.gameObject, animationEndTime);
        }

#endif
    }
}