using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }
}
