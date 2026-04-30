using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int maxEnemies = 4;
    [SerializeField] private Vector2 spawnArea = new Vector2(10f, 10f);
    [SerializeField] private float respawnDelay = 5f;

    private readonly List<GameObject> aliveEnemies = new();

    private void Start()
    {
        for (int i = 0; i < maxEnemies; i++)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        float x = Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f);
        float y = Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f);
        Vector3 pos = transform.position + new Vector3(x, y, 0f);
        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        aliveEnemies.Add(enemy);

        Health health = enemy.GetComponent<Health>();
        if (health != null)
            health.OnDeath += () => StartCoroutine(RespawnAfterDelay(enemy));
    }

    private IEnumerator RespawnAfterDelay(GameObject deadEnemy)
    {
        aliveEnemies.Remove(deadEnemy);
        yield return new WaitForSeconds(respawnDelay);
        SpawnEnemy();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnArea.x, spawnArea.y, 0f));
    }
}
