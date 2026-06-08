using System.Collections.Generic;
using UnityEngine;

// Pool simple para reusar proyectiles en vez de instanciar/destruir en caliente.
// No es MonoBehaviour: cada enemigo a distancia crea el suyo en Awake y lo usa.
public class ProjectilePool
{
    private readonly EnemyProjectile prefab;
    private readonly Queue<EnemyProjectile> libres = new();

    public ProjectilePool(EnemyProjectile prefab)
    {
        this.prefab = prefab;
    }

    public EnemyProjectile Obtener(Vector3 pos, Quaternion rot)
    {
        EnemyProjectile p = libres.Count > 0 ? libres.Dequeue() : Object.Instantiate(prefab);
        p.transform.SetPositionAndRotation(pos, rot);
        p.gameObject.SetActive(true);
        return p;
    }

    public void Devolver(EnemyProjectile p)
    {
        p.gameObject.SetActive(false);
        libres.Enqueue(p);
    }
}
