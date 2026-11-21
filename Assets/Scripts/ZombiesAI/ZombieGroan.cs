using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ZombieGroan — Horror Night (NEW, 2026-11-21)
// Self-contained groan audio for a zombie. Attach to any zombie prefab
// and assign groanClip (the groan WAV) in the Inspector.
//  - On Start it gets the AudioSource on this GameObject, or adds one:
//    3D sound (spatialBlend = 1), maxDistance 30, volume 0.8.
//  - Every 8-20 seconds (random) it groans — but only if the player
//    (tag "Player") is within 25 m, so distant zombies stay silent.
//  - Each groan plays with a random pitch 0.85-1.15 so zombies don't
//    all sound identical.
//  - Null-guarded: no clip, no player, or no AudioSource means silence,
//    never an exception. No other behaviour — movement, damage and
//    death stay in ZombieAI / WaypointZombieAI.
public class ZombieGroan : MonoBehaviour
{
    [Header("Groan Audio")]
    public AudioClip groanClip;

    private AudioSource audioSource;
    private Transform player;
    private float nextGroanTime;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 30f;
        audioSource.volume = 0.8f;
        audioSource.playOnAwake = false;

        FindPlayer();
        nextGroanTime = Time.time + Random.Range(8f, 20f);
    }

    void Update()
    {
        if (Time.time < nextGroanTime)
        {
            return;
        }
        nextGroanTime = Time.time + Random.Range(8f, 20f);

        if (groanClip == null || audioSource == null)
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();
        }
        if (player == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, player.position) <= 25f)
        {
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(groanClip);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }
}
