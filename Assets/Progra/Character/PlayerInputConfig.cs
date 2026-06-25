using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInputConfig", menuName = "Combat/PlayerInputConfig")]
public class PlayerInputConfig : ScriptableObject
{
    [Header("Movimiento")]
    public KeyCode moveLeft  = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode jump      = KeyCode.Space;
    public KeyCode flipKey   = KeyCode.Q;

    [Header("Combate")]
    public KeyCode attackButton = KeyCode.Mouse0;
    public KeyCode dodgeButton  = KeyCode.Mouse1;
    public KeyCode shoveKey     = KeyCode.E;

    [Header("Zona de ataque manual (opcional - para jugar sin mouse)")]
    [Tooltip("Si ambas están en None, usa el mouse para determinar la zona")]
    public KeyCode zoneUp   = KeyCode.None;
    public KeyCode zoneDown = KeyCode.None;

    [Header("Zona de ataque (solo se usa si NO hay teclas de zona asignadas)")]
    public float highThreshold = 0.8f;
    public float lowThreshold  = -0.5f;

    // ── Helpers de movimiento ──
    public float GetMoveInput()
    {
        float val = 0f;
        if (Input.GetKey(moveRight)) val += 1f;
        if (Input.GetKey(moveLeft))  val -= 1f;
        return val;
    }

    public bool GetJumpDown()   => Input.GetKeyDown(jump);
    public bool GetJumpHeld()   => Input.GetKey(jump);
    public bool GetFlipDown()   => Input.GetKeyDown(flipKey);
    public bool GetAttackDown() => Input.GetKeyDown(attackButton);
    public bool GetAttackHeld() => Input.GetKey(attackButton);
    public bool GetAttackUp()   => Input.GetKeyUp(attackButton);
    public bool GetDodgeDown()  => Input.GetKeyDown(dodgeButton);
    public bool GetShoveDown()  => Input.GetKeyDown(shoveKey);

    // ── Zona manual ──
    public bool UsesManualZone => zoneUp != KeyCode.None || zoneDown != KeyCode.None;

    public bool GetZoneUpDown()   => zoneUp   != KeyCode.None && Input.GetKeyDown(zoneUp);
    public bool GetZoneDownDown() => zoneDown != KeyCode.None && Input.GetKeyDown(zoneDown);
}