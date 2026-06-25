using UnityEngine;

/// <summary>Se destruye solo cuando el ParticleSystem termina de reproducirse.</summary>
public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem ps;
    private void Awake() { ps = GetComponent<ParticleSystem>(); }
    private void Update()
    {
        if (ps != null && !ps.IsAlive()) Destroy(gameObject);
    }
}
