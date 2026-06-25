using UnityEngine;

/// <summary>
/// Componente central de audio de combate.
/// Ponlo en el mismo GameObject que PlayerCombat, PlayerDodge y PlayerHealth.
/// Llama sus métodos desde los demás scripts cuando ocurre cada acción.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CombatAudio : MonoBehaviour
{
    [Header("Ataques")]
    [SerializeField] private AudioClip sfxAttackHigh;
    [SerializeField] private AudioClip sfxAttackMid;
    [SerializeField] private AudioClip sfxAttackLow;

    [Header("Ataques cargados (opcional — si es null usa el normal)")]
    [SerializeField] private AudioClip sfxChargedHigh;
    [SerializeField] private AudioClip sfxChargedMid;
    [SerializeField] private AudioClip sfxChargedLow;

    [Header("Esquive")]
    [SerializeField] private AudioClip sfxDodge;
    [SerializeField] private AudioClip sfxCrouch;   // dodge en zona Mid

    [Header("Golpe recibido")]
    [SerializeField] private AudioClip sfxHitReceived;

    [Header("Bloqueo & Choque")]
    [SerializeField] private AudioClip sfxBlock;
    [SerializeField] private AudioClip sfxClash;

    [Header("Volumen global")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    // ── Ataques normales ──────────────────────────────────────
    public void PlayAttack(AttackZone zone)
    {
        AudioClip clip = zone switch
        {
            AttackZone.High => sfxAttackHigh,
            AttackZone.Low  => sfxAttackLow,
            _               => sfxAttackMid,
        };
        Play(clip);
    }

    // ── Ataques cargados ──────────────────────────────────────
    public void PlayChargedAttack(AttackZone zone)
    {
        AudioClip clip = zone switch
        {
            AttackZone.High => sfxChargedHigh != null ? sfxChargedHigh : sfxAttackHigh,
            AttackZone.Low  => sfxChargedLow  != null ? sfxChargedLow  : sfxAttackLow,
            _               => sfxChargedMid  != null ? sfxChargedMid  : sfxAttackMid,
        };
        Play(clip);
    }

    // ── Esquive ───────────────────────────────────────────────
    public void PlayDodge()   => Play(sfxDodge);
    public void PlayCrouch()  => Play(sfxCrouch != null ? sfxCrouch : sfxDodge);

    // ── Golpe recibido ────────────────────────────────────────
    public void PlayHitReceived() => Play(sfxHitReceived);

    // ── Bloqueo & Choque ──────────────────────────────────────
    public void PlayBlock() => Play(sfxBlock);
    public void PlayClash() => Play(sfxClash);

    // ── Helper interno ────────────────────────────────────────
    private void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}