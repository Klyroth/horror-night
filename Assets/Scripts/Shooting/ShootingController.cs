using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ShootingController — Horror Night
// From tutorial videos 12 (raycast shooting), 13 (rifle modes),
// 14 (ammo/reload), 15 (animator), 16 (muzzle flash), 17 (sounds),
// 23 + 31 (damage to ZombieAI and WaypointZombieAI), 33 (weapon stats),
// 38 (ammo UI text).
// HORROR NIGHT adaptation (2026-11-21): sniper ONLY, limited ammo.
//  - One weapon, no switching: isAuto=false, fireRate=1.0f (one shot
//    per second), fireRange=60f, damagePerShot=50, magazine maxAmmo=6,
//    reloadTime=2.5f.
//  - Limited reserve: reserveAmmo (default 24, cap maxReserveAmmo=48).
//    Reload transfers min(magazine space, reserveAmmo); with an empty
//    reserve Reload does nothing. Ammo text shows "current / reserve".
//  - Touch reload: TouchControls sets touchReloadPressed (RELOAD button),
//    consumed here; zero effect on PC (R key still reloads).
// Adaptations vs tutorial (kept):
//  - Animator trigger is "reload" (lowercase) everywhere — the tutorial
//    itself is inconsistent ("reload" in video 15, "Reload" in video 17),
//    so our controllers define the parameter as "reload".
//  - Muzzle flash: the tutorial's Legacy Particle Pack is deprecated, so
//    our flash is a built-in ParticleSystem made in the setup script.
//  - Reload sound: no reload clip in our approved audio; the field is
//    null-guarded and may stay empty.
//  - Touch fire (Phase 11): TouchControls sets touchFireHeld /
//    touchFirePressed; zero effect on PC.
//  - Blood particle: spawned at the hit point on a zombie hit (tutorial
//    video 24); the prefab is made in the setup script.
//  - All animator calls are null-guarded — the sniper has no animator.
public class ShootingController : MonoBehaviour
{
    [Header("Shooting")]
    public Transform firePoint;
    public float fireRate = 1.0f;
    public float fireRange = 60f;
    public bool isAuto = false;
    public int damagePerShot = 50;
    private float nextFireTime = 0f;

    [Header("Ammo & Reload")]
    public int maxAmmo = 6;
    public int currentAmmo;
    public int reserveAmmo = 24;
    public int maxReserveAmmo = 48;
    public float reloadTime = 2.5f;
    private bool isReloading = false;

    [Header("Animation & Effects")]
    public Animator animator;
    public ParticleSystem muzzleFlash;
    public GameObject bloodParticlePrefab;

    [Header("Sound Effects")]
    public AudioSource soundAudioSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadSoundClip;

    [Header("UI")]
    public Text currentAmmoText;

    // Android touch fire (written by TouchControls; unused on PC)
    public static bool touchFireHeld;
    public static bool touchFirePressed;
    // Android touch reload (written by TouchControls RELOAD button; unused on PC)
    public static bool touchReloadPressed;

    void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoText();
    }

    void Update()
    {
        // Touch reload (RELOAD button) — consume the flag first, even
        // while reloading, so a tap during a reload is dropped rather
        // than queued and firing a second reload when it finishes.
        if (touchReloadPressed)
        {
            touchReloadPressed = false;
            Reload();
        }

        if (isReloading)
        {
            return;
        }

        // Manual reload
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            Reload();
        }

        // Fire1 (mouse) only counts when no touch is active: on Android
        // ANY screen touch is reported as mouse button 0, so without this
        // gate every look-drag / joystick touch also fired the gun
        // (play-test #2: ammo 30 -> 11 without the player aiming).
        // On a phone, firing happens only via TouchControls (FIRE button).
        bool fireHeld = (Input.touchCount == 0 && Input.GetButton("Fire1")) || touchFireHeld;
        bool firePressed = (Input.touchCount == 0 && Input.GetButtonDown("Fire1")) || touchFirePressed;
        touchFirePressed = false;

        if (isAuto)
        {
            if (fireHeld && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (animator != null)
            {
                animator.SetBool("shoot", false);
            }
        }
        else
        {
            if (firePressed && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (animator != null)
            {
                animator.SetBool("shoot", false);
            }
        }
    }

    private void Shoot()
    {
        if (currentAmmo > 0)
        {
            if (animator != null)
            {
                animator.SetBool("shoot", true);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (soundAudioSource != null && shootingSoundClip != null)
            {
                soundAudioSource.PlayOneShot(shootingSoundClip);
            }

            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
            {
                // Damage the zombie (both zombie AI types — video 31 fix)
                ZombieAI zombieAI = hit.collider.GetComponent<ZombieAI>();
                if (zombieAI != null)
                {
                    zombieAI.TakeDamage(damagePerShot);
                    SpawnBlood(hit);
                }
                else
                {
                    WaypointZombieAI waypointZombie = hit.collider.GetComponent<WaypointZombieAI>();
                    if (waypointZombie != null)
                    {
                        waypointZombie.TakeDamage(damagePerShot);
                        SpawnBlood(hit);
                    }
                }
            }

            currentAmmo--;
            UpdateAmmoText();
        }
        else
        {
            //Reload
            Reload();
        }
    }

    private void SpawnBlood(RaycastHit hit)
    {
        if (bloodParticlePrefab != null)
        {
            GameObject blood = Instantiate(bloodParticlePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            ParticleSystem ps = blood.GetComponent<ParticleSystem>();
            Destroy(blood, ps != null ? ps.main.duration : 2f);
        }
    }

    private void Reload()
    {
        // No reserve ammo left: nothing to reload with.
        if (reserveAmmo <= 0)
        {
            return;
        }
        if (!isReloading && currentAmmo < maxAmmo)
        {
            if (animator != null)
            {
                animator.SetTrigger("reload");
            }
            isReloading = true;

            if (soundAudioSource != null && reloadSoundClip != null)
            {
                soundAudioSource.PlayOneShot(reloadSoundClip);
            }

            Invoke("FinishReloading", reloadTime);
        }
    }

    private void FinishReloading()
    {
        // Move only what fits in the magazine, and only what the
        // reserve still has.
        int transfer = Mathf.Min(maxAmmo - currentAmmo, reserveAmmo);
        currentAmmo += transfer;
        reserveAmmo -= transfer;
        isReloading = false;
        UpdateAmmoText();

        if (animator != null)
        {
            animator.ResetTrigger("reload");
        }
    }

    private void UpdateAmmoText()
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo + " / " + reserveAmmo;
        }
    }
}
