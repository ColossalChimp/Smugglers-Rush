using System.Collections;
using UnityEngine;

namespace TinyGiantStudio.Ranks
{
    [RequireComponent(typeof(Animator))]
    public class PieceAnimationController : MonoBehaviour
    {
        [Space(10)]
        [Tooltip("Should be all child objects")]
        [SerializeField] private GameObject[] fragments;

        [Tooltip("Should be all child objects. Instead of getting renderer from GameObjects, caching it improves performance slightly.")]
        [SerializeField] private Renderer[] fragmentRenderers;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void BeginEntryAnimation(Material material, EntryAnimationType entryAnimationType)
        {
            gameObject.SetActive(true); //Just in-case

            UpdateMaterials(material);

            switch (entryAnimationType)
            {
                case EntryAnimationType.Grow:

                    foreach (GameObject go in fragments)
                        StartCoroutine(GrowFromZero(go, Random.Range(0.25f, 2f)));

                    break;

                case EntryAnimationType.ComeUp:
                    if (animator != null)
                    {
                        animator.enabled = true;
                        animator.SetTrigger("Come Up");
                    }

                    foreach (GameObject go in fragments)
                        StartCoroutine(GrowFromZero(go, Random.Range(0.25f, 2f)));

                    //Destroy(gameObject, 2f); Destroy is handled by ranks animation controller
                    break;
            }
        }

        public void BeginExitAnimation(Material material, ExitAnimationType exitAnimationType, Vector2 physicsPiecesLifeTime, float explosionForce, float explosionRadius, float upwardsModifier, float animationStartDelay, AudioSource audioSource = null, AudioClip audioClip = null, float audioDelay = 0)
        {
            StartCoroutine(ExitCoroutine(material, exitAnimationType, physicsPiecesLifeTime, explosionForce, explosionRadius, upwardsModifier, animationStartDelay, audioSource, audioClip, audioDelay));
        }

        private IEnumerator ExitCoroutine(Material material, ExitAnimationType exitAnimationType, Vector2 physicsPiecesLifeTime, float explosionForce, float explosionRadius, float upwardsModifier, float animationStartDelay, AudioSource audioSource, AudioClip audioClip, float audioDelay)
        {
            gameObject.SetActive(true); //Just in-case

            UpdateMaterials(material);

            StartCoroutine(AudioRoutine(audioSource, audioClip, audioDelay));

            yield return new WaitForSeconds(animationStartDelay);

            switch (exitAnimationType)
            {
                case ExitAnimationType.DropByPhysics:

                    if (animator != null)
                        animator.enabled = false;

                    foreach (GameObject go in fragments)
                    {
                        if (go.GetComponent<Collider>() != null)
                            go.GetComponent<Collider>().enabled = true;
                        else
                            go.AddComponent<BoxCollider>();

                        Rigidbody rb = go.GetComponent<Rigidbody>();
                        //?? go.AddComponent<Rigidbody>();
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        //rb.mass = Random.Range(0.1f, 1f);
                        rb.AddForce(new Vector3(MinorForce(), MinorForce(), MinorForce()));

                        StartCoroutine(FadeMaterial(go, 1, 0, Vector3.one, Vector3.zero, FragmentDestroyDelay(physicsPiecesLifeTime)));

                        Destroy(gameObject, physicsPiecesLifeTime.y);
                    }
                    break;

                case ExitAnimationType.ExplodeByPhysics:

                    if (animator != null)
                        animator.enabled = false;

                    foreach (GameObject go in fragments)
                    {
                        if (!go.TryGetComponent<Collider>(out var col))
                            col = go.AddComponent<BoxCollider>();

                        col.enabled = true;

                        if (!go.TryGetComponent<Rigidbody>(out var rb))
                            rb = go.AddComponent<Rigidbody>();

                        rb.isKinematic = false;
                        rb.useGravity = true;

                        rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);

                        StartCoroutine(FadeMaterial(go, 1, 0, Vector3.one, Vector3.zero, FragmentDestroyDelay(physicsPiecesLifeTime)));
                    }
                    Destroy(gameObject, physicsPiecesLifeTime.y);

                    break;

                case ExitAnimationType.DropByAnimation:
                    if (animator != null)
                    {
                        animator.enabled = true;
                        animator.SetTrigger("Drop");
                    }

                    foreach (GameObject go in fragments)
                    {
                        StartCoroutine(FadeMaterial(go, 1, 0, Vector3.one, Vector3.zero, Random.Range(0.5f, 1.5f)));
                    }
                    Destroy(gameObject, 2);
                    break;

                case ExitAnimationType.ExplodeByAnimation:
                    if (animator != null)
                    {
                        animator.enabled = true;
                        animator.SetTrigger("Explode");
                    }

                    foreach (GameObject go in fragments)
                    {
                        StartCoroutine(FadeMaterial(go, 1, 0, Vector3.one, Vector3.zero, Random.Range(0.5f, 1.5f)));
                    }
                    Destroy(gameObject, 2f);
                    break;
            }
        }

        private IEnumerator AudioRoutine(AudioSource audioSource, AudioClip clip, float audioDelay)
        {
            yield return new WaitForSeconds(audioDelay);

            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private float MinorForce()
        {
            return Random.Range(-10f, 10f);
        }

        private void UpdateMaterials(Material material)
        {
            if (fragmentRenderers.Length == 0)
                return;

            foreach (var renderer in fragmentRenderers)
            {
                renderer.sharedMaterial = material;
            }
        }

        private IEnumerator GrowFromZero
           (
           GameObject target,
           float totalTime
           )
        {
            float time = 0f;

            while (time < totalTime)
            {
                time += Time.deltaTime;
                target.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time / totalTime);

                yield return null;
            }

            target.transform.localScale = Vector3.one;
        }

        private IEnumerator FadeMaterial
            (
            GameObject target,
            float startAlpha, float targetAlpha,
            Vector3 startSize, Vector3 targetSize,
            float totalTime
            )
        {
            float fadeDuration = Random.Range(1, 30);
            if (fadeDuration > totalTime / 2f) fadeDuration = totalTime / 2f;
            yield return new WaitForSeconds(totalTime - fadeDuration);
            var mat = target.GetComponent<MeshRenderer>().material;
            float time = 0f;
            Color color = mat.color;

            while (time < fadeDuration && target != null)
            {
                time += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
                mat.color = new Color(color.r, color.g, color.b, alpha);
                target.transform.localScale = Vector3.Lerp(startSize, targetSize, time / fadeDuration);

                yield return null;
            }

            if (target != null)
            {
                // Ensure final alpha value is set
                mat.color = new Color(color.r, color.g, color.b, targetAlpha);
                target.transform.localScale = targetSize;
                yield return null;
                Destroy(target);
            }
        }

        private float FragmentDestroyDelay(Vector2 physicsPiecesLifeTime)
        {
            return Random.Range(physicsPiecesLifeTime.x, physicsPiecesLifeTime.y);
        }
    }
}