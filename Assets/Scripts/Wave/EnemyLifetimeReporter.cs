using UnityEngine;

public class EnemyLifetimeReporter : MonoBehaviour
{
    private EnemyEncounterZone ownerZone;
    private Health health;
    private bool reported;

    public void Initialize(EnemyEncounterZone zone)
    {
        ownerZone = zone;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        if(health != null)
        {
            health.onDeath.AddListener(OnDeath);
        }
    }

    void OnDeath()
    {
        ReportIfNeeded();
    }

    void ReportIfNeeded()
    {
        if(reported == true)
        {
            return;
        }

        reported = true;

        if(ownerZone != null)
        {
            ownerZone.OnEnemyDead(this);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
