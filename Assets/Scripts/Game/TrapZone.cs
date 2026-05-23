using UnityEngine;

public enum TrapType
{
    Electricity,
    Lava,
    Laser
}

/// <summary>
/// Trigger zone for traps. When Vesper enters, he is instantly killed
/// (2-second death sequence: 1s hurt animation + 1s death animation).
/// </summary>
public class TrapZone : MonoBehaviour
{
    [Header("Trap Configuration")]
    [SerializeField] private TrapType trapType = TrapType.Lava;

    public TrapType Type => trapType;

    public void SetTrapType(TrapType type)
    {
        trapType = type;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var vesper = other.GetComponent<VesperController>();
            if (vesper != null && !vesper.IsDead)
            {
                vesper.KillFromTrap();
            }
        }
    }
}
