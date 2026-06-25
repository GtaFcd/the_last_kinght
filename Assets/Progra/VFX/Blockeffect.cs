using System.Collections;
using UnityEngine;

public class BlockEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparks;

    private void Start()
    {
        if (sparks != null) sparks.Play();
        StartCoroutine(WaitAndDestroy());
    }

    private IEnumerator WaitAndDestroy()
    {
        if (sparks != null)
            yield return new WaitUntil(() => !sparks.isPlaying);

        Destroy(gameObject);
    }
}