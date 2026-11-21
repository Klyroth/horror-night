using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ZombieWaveSystem — Horror Night
// From tutorial videos 30 (setup), 31 (logic), 37 (wave UI).
// Quirk kept deliberately (tutorial's own): zombiesPerWave += 2 runs
// BEFORE the first wave spawns, so wave 1 spawns 6 zombies, not 4.
// HORROR NIGHT adaptation (2026-11-21): waves run FOREVER — the
// wave-10 early return is removed. Alive cap: maxAlive limits how many
// zombies can be alive at once; StartNewWave only spawns up to that
// total (dead zombies, state Dead, don't count — they are never
// Destroyed, they just disable themselves).
// LoadSetting() reads saved difficulty from PlayerPrefs; the tutorial
// calls it from Update() every frame (video 49) — here it is called
// once from Start() (same result, no per-frame cost).
public class ZombieWaveSystem : MonoBehaviour
{
    public GameObject[] zombiePrefabs;
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 10f;
    [SerializeField] private float waveTimer = 0f;
    private int waveNumber = 1;
    public int zombiesPerWave = 4;
    public int maxAlive = 10;

    // Current wave, for the death screen's "wave reached" text.
    public int CurrentWave { get { return waveNumber; } }

    [Header("UI")]
    public Text WaveNumber;
    public Text WaveTimer;

    void Start()
    {
        LoadSetting();
        if (WaveNumber != null)
        {
            WaveNumber.text = waveNumber.ToString();
        }
    }

    void Update()
    {
        // Waves run forever — no wave-10 stop (Horror Night).
        waveTimer += Time.deltaTime;

        int intValue = Mathf.RoundToInt(waveTimer);

        if (WaveTimer != null)
        {
            WaveTimer.text = intValue.ToString();
        }

        if (waveTimer >= timeBetweenWaves)
        {
            StartNewWave();
        }
    }

    void StartNewWave()
    {
        waveTimer = 0f;

        zombiesPerWave += 2;

        // Alive cap: only spawn up to maxAlive living zombies in total.
        int alive = CountAliveZombies();
        int toSpawn = Mathf.Max(0, Mathf.Min(zombiesPerWave, maxAlive - alive));

        float minDistance = 4f;

        for (int i = 0; i < toSpawn; i++)
        {
            int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomSpawnIndex];

            GameObject randomZombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];

            Vector3 spawnPosition = spawnPoint.position + Random.insideUnitSphere * minDistance;
            spawnPosition.y = spawnPoint.position.y;

            Instantiate(randomZombiePrefab, spawnPosition, spawnPoint.rotation);
        }

        waveNumber++;

        if (WaveNumber != null)
        {
            WaveNumber.text = waveNumber.ToString();
        }
    }

    // Counts zombies that are still alive (both AI types). Dead zombies
    // are never Destroyed — they disable themselves — so state Dead is
    // excluded. Null-safe: a missing component or a null entry counts
    // as not alive.
    private int CountAliveZombies()
    {
        int count = 0;
        ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
        if (zombies != null)
        {
            foreach (ZombieAI zombie in zombies)
            {
                if (zombie != null && zombie.currentState != ZombieAI.ZombieState.Dead)
                {
                    count++;
                }
            }
        }
        WaypointZombieAI[] waypointZombies = FindObjectsOfType<WaypointZombieAI>();
        if (waypointZombies != null)
        {
            foreach (WaypointZombieAI zombie in waypointZombies)
            {
                if (zombie != null && zombie.currentState != WaypointZombieAI.ZombieState.Dead)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public void LoadSetting()
    {
        if (PlayerPrefs.HasKey("TimeBetweenWaves"))
        {
            timeBetweenWaves = PlayerPrefs.GetFloat("TimeBetweenWaves");
        }
        if (PlayerPrefs.HasKey("ZombiesPerWave"))
        {
            zombiesPerWave = PlayerPrefs.GetInt("ZombiesPerWave");
        }
    }
}
