using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake() => sr = GetComponent<SpriteRenderer>();

    private void Update() => sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
}
