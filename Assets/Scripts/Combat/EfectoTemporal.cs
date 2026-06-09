using UnityEngine;

// Efecto visual de un solo uso: al aparecer reproduce un estado del Animator y se
// autodestruye tras una duracion. Para explosiones, golpes, etc.
[RequireComponent(typeof(Animator))]
public class EfectoTemporal : MonoBehaviour
{
    [SerializeField] private string estado;
    [SerializeField] private float duracion = 0.6f;

    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(estado))
            anim.Play(estado, 0, 0f);
        Destroy(gameObject, duracion);
    }
}
