# Horror Night — Scripts README (for the scene agent)

Adapted 2026-11-21 from The real SK scripts. Unity 6000.3.28f1 + URP.
No Unity Editor was run during this pass (no licence) — scripts were
verified by full re-read + brace-balance check only. `dotnet`/`csc`
are NOT available on this machine, so there was no syntax compile;
the first Editor open will be the real compile check.

## Rules the scene agent must keep

- **Buttons are wired ONLY via serialized Inspector onClick** — never
  via editor-code `AddListener` (not serialized; that bug killed
  The real SK Build #2's menu).
- **Scope is gone at runtime.** No SCOPE button, no ScopeCamera, no
  ScopeCanvas in NightCity. `ScopeSystem.cs` and
  `ScopeCameraController.cs` still exist ONLY because the legacy Editor
  build scripts (`Assets/Editor/BuildTheRealSK_Scenes.cs`,
  `FixTouchControls.cs`) reference those types — deleting them breaks
  Editor compilation. Do NOT attach them to anything in the scene.
- **One weapon only (sniper).** There is no weapon switching anywhere.
- Player GameObject must carry tag **"Player"** (ZombieGroan finds it
  by tag).

## Scripts — where they attach + Inspector fields

### Player (NightCity scene)

| Script | Attach to | Inspector fields to assign |
|---|---|---|
| `Player/PlayerMovement.cs` | Player root (has CharacterController) | `groundCheck`, `groundMask` (Ground layer), `leftFootAudioSource`, `rightFootAudioSource`, `footstepSounds`, `healthSlider` (UI Slider), `healthText` (UI Text, shows HP number), `bloodScreenEffect` = `Prefabs/BloodScreen.prefab`, `deathScreen` (the DeathScreen object; fallback only) |
| `Player/CameraController.cs` | Main Camera (child of Player) | (unchanged from The real SK — see its header) |
| `Player/Flashlight.cs` **NEW** | Main Camera (or Player) | `spotLight` = the SpotLight on the camera (make one: SpotLight, child of Main Camera, pointing forward), `intensity` = 4 (default) |
| `Player/TouchControls.cs` | Any GameObject in NightCity only (NOT MainMenu) | No Inspector fields — builds its own touch UI at runtime, only on touch devices |
| `Shooting/ShootingController.cs` | Sniper weapon object (child of Main Camera) | `firePoint` (empty child at muzzle), `fireRate` 1.0, `fireRange` 60, `isAuto` false, `damagePerShot` 50, `maxAmmo` 6, `reserveAmmo` 24, `maxReserveAmmo` 48, `reloadTime` 2.5, `animator` — LEAVE EMPTY (sniper has none; all calls null-guarded), `muzzleFlash` (ParticleSystem), `bloodParticlePrefab` = `Prefabs/BloodParticle.prefab`, `soundAudioSource`, `shootingSoundClip` = `Audio/710084__felix-blume__rifle-gunshot-one-shot.wav`, `reloadSoundClip` — may stay empty, `currentAmmoText` (UI Text; shows "6 / 24") |

### Environment / managers (NightCity scene)

| Script | Attach to | Inspector fields to assign |
|---|---|---|
| `GameManager.cs` | Environment/manager GameObject | `highScoreText`, `currentScoreText` (UI Texts). No weapons fields any more. Score = kills × 10 + seconds survived; best score saved to PlayerPrefs `HorrorNightBestScore` on death |
| `ZombiesAI/ZombieWaveSystem.cs` | Environment/manager GameObject | `zombiePrefabs` (zombie prefabs), `spawnPoints` (empty Transforms around the map), `timeBetweenWaves` 10, `zombiesPerWave` 4 (grows +2/wave), `maxAlive` 10, `WaveNumber`, `WaveTimer` (UI Texts) |
| `DeathScreen.cs` | Its OWN GameObject that stays active (e.g. a "DeathScreen" manager object — NOT the panel itself, because Start() disables the panel) | `targetImage` (full-screen dark-red Image, starts transparent), `targetText` ("YOU DIED" Text, starts transparent), `duration` 5, `gameOverPanel` (panel GameObject, starts hidden), `finalScoreText`, `waveReachedText` (UI Texts inside the panel), `restartButton`, `quitButton` (Buttons inside the panel — wire their onClick in the Inspector: Restart → `DeathScreen.RestartGame()`, Quit → `DeathScreen.QuitToMenu()`) |

### Zombies (on each zombie prefab)

| Script | Attach to | Inspector fields to assign |
|---|---|---|
| `ZombiesAI/ZombieAI.cs` / `WaypointZombieAI.cs` | Zombie prefab root | (unchanged — `bloodScreenEffect` = `Prefabs/BloodScreen.prefab`, damage, health; their `Die()` calls `GameManager.AddScore(1)` = 1 kill) |
| `ZombiesAI/ZombieGroan.cs` **NEW** | Same zombie prefab root (beside the AI script) | `groanClip` = `Audio/246487__137sounds__freesound-single-zombie-groan.wav`. No AudioSource needed — it gets one or adds one (3D, maxDistance 30, volume 0.8) |

### MainMenu scene

| Script | Attach to | Notes |
|---|---|---|
| `MainMenu/SettingsManager.cs` | (unchanged) | Unchanged from The real SK |
| DeathScreen / GameManager / TouchControls | — | Do NOT put these in MainMenu |

## How the pieces connect (no code changes needed)

- **Touch buttons (runtime-built):** joystick → `PlayerMovement.touchMoveInput`;
  look area → `CameraController.touchLookDelta`; FIRE → `ShootingController.touchFireHeld/touchFirePressed`;
  LIGHT → `Flashlight.touchFlashlightToggle`; RELOAD → `ShootingController.touchReloadPressed`.
  PC keys still work: F = flashlight, R = reload, mouse Fire1 (gated by `Input.touchCount == 0`).
- **Ammo:** magazine 6, reserve 24 (cap 48). Reload moves
  `min(magazine space, reserve)`; empty reserve = reload does nothing.
  Ammo text shows `current / reserve`.
- **Score:** `GameManager.currentScore = kills * 10 + (int)secondsSurvived`.
  `AddScore(1)` from a zombie death counts as 1 kill (kept for
  compatibility); `AddKill()` does the same directly.
- **Death:** zombie damage → `PlayerMovement.TakeDamage()` → HP 0 →
  `GameManager.OnPlayerDied()` (exactly once) → saves best score →
  `DeathScreen.Show()` → panel on, final score + wave filled in,
  `Time.timeScale = 0`. Restart reloads `NightCity`; Quit loads `MainMenu`
  (both reset `timeScale = 1` first).
- **Waves:** endless (no wave-10 stop). Each wave spawns only up to
  `maxAlive` living zombies total; dead zombies don't count.
- **Groans:** every 8–20 s per zombie, only if the player is within 25 m,
  random pitch 0.85–1.15.

## What changed in this pass (2026-11-21)

- **Changed:** `TouchControls.cs` (SCOPE→LIGHT, SWAP→RELOAD; joystick/look/FIRE untouched),
  `ShootingController.cs` (sniper-only, limited reserve ammo, touch reload),
  `GameManager.cs` (weapon switching removed; kills + time score; `OnPlayerDied()`, `AddKill()`),
  `ZombieWaveSystem.cs` (endless waves; `maxAlive` cap; `CurrentWave` getter),
  `DeathScreen.cs` (singleton `Instance`, `Show()` / `RestartGame()` / `QuitToMenu()`,
  final-score + wave texts, game-over panel; fade uses unscaled time so it plays while frozen),
  `PlayerMovement.cs` (health system adapted in place — see below; `healthText`,
  damage blood flash, once-only death via GameManager).
- **Added:** `Player/Flashlight.cs`, `ZombiesAI/ZombieGroan.cs`.
  No `.meta` files exist yet for these two new scripts — Unity will
  generate them on first import; that is normal.
- **NOT added:** `PlayerHealth.cs` — player health/damage already lived
  in `PlayerMovement.cs` (`TakeDamage`, maxHealth 100, health slider),
  so it was adapted there instead of duplicating it.
- **NOT deleted:** `ScopeSystem.cs` / `ScopeCameraController.cs` — after
  the TouchControls rewrite nothing in `Scripts/` references them, but
  the legacy Editor scripts `Assets/Editor/BuildTheRealSK_Scenes.cs`
  (adds both components) and `FixTouchControls.cs` (reads
  `ScopeCameraController`) still do. Deleting them would break Editor
  compilation, so they stay, unused at runtime.
- **Deleted:** `~/workspace/horror-night/library-backup.tar` (1.9 GB
  redundant copy; the original is safe in `~/workspace/the-real-sk/`).
- **One-line fix outside the task list (had to be done):**
  `Assets/Editor/BuildTheRealSK_Scenes.cs:536` — `gm.weapons = weapons;`
  commented out, because `GameManager.weapons` no longer exists and the
  line would have broken Editor compilation. Everything else in that
  legacy builder still compiles (all other fields it sets still exist).
- **Not touched:** `Packages/`, `ProjectSettings/`, scene files
  (`MainMenu.unity`, `NightCity.unity`).
