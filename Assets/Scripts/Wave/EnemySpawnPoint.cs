using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int maxSpawnCount = 10;
    public float positionJitterRadius = 0.5f;

    private int spawnedCount;

    public GameObject SpawnOne(EnemyEncounterZone owner)
    {
        if(enemyPrefab == null)
        {
            return null;
        }

        if(maxSpawnCount > 0)
        {
            if(spawnedCount >= maxSpawnCount)
            {
                return null;
            }
        }

        Vector3 basePos = transform.position;
        Vector2 rand = Random.insideUnitCircle * positionJitterRadius;
        Vector3 spawnPos = new Vector3(basePos.x + rand.x, basePos.y, basePos.z + rand.y);

        Quaternion rot = transform.rotation;

        GameObject obj = Instantiate(enemyPrefab, spawnPos, rot);
        ++spawnedCount;

        EnemyLifetimeReporter reporter = obj.GetComponent<EnemyLifetimeReporter>();
        if(reporter != null)
        {
            reporter.Initialize(owner);
        }

        return obj;
    }

    public bool CanSpawnMore()
    {
        if(maxSpawnCount <= 0)
        {
            return true;
        }

        if(spawnedCount < maxSpawnCount)
        {
            return true;
        }

        return false;
    }
}
