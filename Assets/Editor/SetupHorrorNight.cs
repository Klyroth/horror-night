// SetupHorrorNight — one-shot Editor setup for 'Horror Night'.
// Run ONLY in batch mode after the Unity licence/login is recovered:
//   Unity -batchmode -quit -projectPath <project> -executeMethod SetupHorrorNight.SetupAll
// (Also available in the Editor menu: HorrorNight/Setup All.)
//
// DO NOT run this against The real SK scenes. It opens NightCity.unity
// (BINARY scene — never text-edited; every change below goes through
// the Editor API) and MainMenu.unity, wires the Inspector fields listed
// in Assets/Scripts/HORROR_SCRIPTS_README.md, and sets the Android
// Player Settings from qa-checklist.md sections 1, 2, 5, 8.
//
// Fail-loudly rule: any object this script EXPECTS to exist and cannot
// find throws an Exception, so the batch log shows the failure instead
// of silently producing a half-wired scene. The two deliberate
// exceptions are documented inline: (a) PlayerMovement.healthText has
// NO existing UI object — the old builder never created a "HealthText"
// (only "HealthLabel"); the script CREATES it instead of throwing, or
// SETUP COMPLETE could never be reached; (b) the DeathScreen panel +
// buttons also never existed in NightCity, so step 6 CREATES them.
//
// Hierarchy names used (all proven, not guessed):
//   Player / MainCamera / Weapon_Sniper / Weapon_ScarH / GroundCheck /
//   LeftFoot / RightFoot / PlayerUI(HealthSlider, HealthLabel, ScoreText,
//   HighScoreText, WaveNumberText, WaveTimerText, AmmoText, CrosshairText,
//   DeathScreen/DeathText) / Environment / SpawnPoints(SpawnPoint1,2,3,
//   6,7,8) / Directional Light / TouchControls / EventSystem
//     — BuildTheRealSK_Scenes.cs BuildPlayer()/BuildPlayerUI()/BuildMap()
//       (Map2 branch) + FixTouchControls.cs (close spawns 6,7,8; the
//       "SpawnPoint6" add was skipped as a duplicate, so the scene holds
//       SpawnPoint1,2,3,6,7,8 — verified by strings on NightCity.unity).
//   MainMenu: MenuCanvas / ContinueButton / Map1Button / Map2Button /
//   SettingsButton / BackButton / QuitButton / SettingsPanel / Credits /
//   Main_Menu (SettingsManager) / EventSystem
//     — BuildTheRealSK_Scenes.cs BuildMainMenu() + MainMenu.unity YAML.
//   MainMenu's EventSystem uses StandaloneInputModule (MainMenu.unity
//   YAML: UnityEngine.EventSystems.StandaloneInputModule; ProjectSettings
//   activeInputHandler = 0, old Input Manager; no Input System package
//   in Packages/manifest.json) — so new EventSystems use the same type.
//
// NEVER referenced here: Map1/Map2 scene paths, GameManager.weapons
// (removed), ScopeSystem / ScopeCameraController as types. The two scope
// components are removed by TYPE NAME (string match) so this file has
// no compile-time dependency on those legacy scripts; deleting them
// later will not break this script.

using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SetupHorrorNight
{
    private const string NightCityPath = "Assets/Scenes/NightCity.unity";
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string GunshotPath = "Assets/Audio/710084__felix-blume__rifle-gunshot-one-shot.wav";
    private const string GroanPath = "Assets/Audio/246487__137sounds__freesound-single-zombie-groan.wav";
    private const string BloodParticlePath = "Assets/Prefabs/BloodParticle.prefab";
    private const string BloodScreenPath = "Assets/Prefabs/BloodScreen.prefab";

    // Task step 4: the four Horror Night zombie prefabs. Zombie_Arm is
    // deliberately NOT in this list (it exists on disk but is not one
    // of the four the wave system should spawn).
    private static readonly string[] ZombiePrefabPaths =
    {
        "Assets/Prefabs/Zombie_Basic.prefab",
        "Assets/Prefabs/Zombie_Chubby.prefab",
        "Assets/Prefabs/Zombie_Ribcage.prefab",
        "Assets/Prefabs/SkeletonZombie.prefab",
    };

    [MenuItem("HorrorNight/Setup All")]
    public static void SetupAll()
    {
        // ---- Step 1+2: NightCity lighting + player ----
        Scene night = EditorSceneManager.OpenScene(NightCityPath, OpenSceneMode.Single);
        Debug.Log("SetupHorrorNight: opened " + NightCityPath);
        SetupNightLighting(night);
        SetupPlayer(night);
        EnsureEventSystem(night, NightCityPath);

        // Save before leaving NightCity so a later failure cannot lose
        // the work above; the MainMenu EventSystem check (step 3, second
        // half) needs MainMenu open, then we come straight back.
        EditorSceneManager.SaveScene(night);
        Debug.Log("SetupHorrorNight: NightCity saved (steps 1-3, NightCity half)");

        Scene menu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        EnsureEventSystem(menu, MainMenuPath);
        EditorSceneManager.SaveScene(menu);
        Debug.Log("SetupHorrorNight: MainMenu EventSystem checked + saved");

        // ---- Steps 4-6: zombies, HUD, death screen (NightCity) ----
        night = EditorSceneManager.OpenScene(NightCityPath, OpenSceneMode.Single);
        SetupZombies(night);
        SetupHUD(night);
        SetupDeathScreen(night);
        EditorSceneManager.SaveScene(night);
        Debug.Log("SetupHorrorNight: NightCity saved (steps 4-6)");

        // ---- Step 7: menu cleanup (MainMenu) ----
        menu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        CleanupMainMenu(menu);
        EditorSceneManager.SaveScene(menu);
        Debug.Log("SetupHorrorNight: MainMenu saved (step 7)");

        // ---- Step 8: Player Settings (scene-independent) ----
        SetupPlayerSettings();

        // ---- Step 9: NavMesh rebake (NightCity must be active) ----
        night = EditorSceneManager.OpenScene(NightCityPath, OpenSceneMode.Single);
        RebakeNavMesh(night);

        // ---- Step 10: save everything ----
        EditorSceneManager.SaveScene(night);
        AssetDatabase.SaveAssets();
        Debug.Log("SETUP COMPLETE");
    }

    // ================= Step 1: night lighting =================
    private static void SetupNightLighting(Scene scene)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.05f); // very dark bluish-black
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.03f);
        Debug.Log("SetupHorrorNight: night lighting set (ambient 0.02/0.02/0.05, fog exponential density 0.02)");

        List<Light> lights = FindAllInScene<Light>(scene);
        if (lights.Count == 0)
            Fail("no Light at all in NightCity — cannot set moonlight (expected 'Directional Light')");
        Light dir = null;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional) { dir = l; break; }
        }
        if (dir == null)
        {
            // Defensive per spec: some light exists, just no directional one.
            Debug.LogWarning("SetupHorrorNight: no Directional Light in NightCity ("
                + lights.Count + " other light(s) present) — moonlight step skipped");
            return;
        }
        dir.color = new Color(0.45f, 0.55f, 0.85f); // dim blue moonlight
        dir.intensity = 0.15f;
        EditorUtility.SetDirty(dir);
        Debug.Log("SetupHorrorNight: Directional Light '" + dir.gameObject.name
            + "' set to dim blue moonlight (intensity 0.15)");
    }

    // ================= Step 2: player =================
    private static void SetupPlayer(Scene scene)
    {
        GameObject player = RequireGO(scene, "Player");
        if (player.tag != "Player")
        {
            player.tag = "Player";
            Debug.Log("SetupHorrorNight: Player tag fixed to 'Player'");
        }
        else
        {
            Debug.Log("SetupHorrorNight: Player tag already 'Player'");
        }

        // Main Camera tag (FixCameraTags.cs pattern).
        GameObject camGo = RequireGO(scene, "MainCamera");
        if (camGo.tag != "MainCamera")
        {
            camGo.tag = "MainCamera";
            Debug.Log("SetupHorrorNight: MainCamera tag fixed");
        }
        Camera cam = camGo.GetComponent<Camera>();
        if (cam == null) Fail("MainCamera GameObject has no Camera component");

        // --- Scope removal: ScopeSystem / ScopeCameraController must not
        // be attached anywhere in NightCity (HORROR_SCRIPTS_README.md:
        // "Scope is gone at runtime. No SCOPE button, no ScopeCamera, no
        // ScopeCanvas"). Removed by type NAME so this file never
        // references those legacy types (they exist only because the old
        // Editor builders reference them). The ScopeCamera / ScopeCanvas
        // GameObjects are kept but deactivated, not deleted.
        RemoveComponentsByTypeName(camGo, "ScopeSystem");
        GameObject scopeCamGo = FindGameObject(scene, "ScopeCamera");
        if (scopeCamGo != null)
        {
            RemoveComponentsByTypeName(scopeCamGo, "ScopeCameraController");
            scopeCamGo.SetActive(false);
            EditorUtility.SetDirty(scopeCamGo);
            Debug.Log("SetupHorrorNight: ScopeCamera deactivated (component removed by name)");
        }
        GameObject scopeCanvas = FindGameObject(scene, "ScopeCanvas");
        if (scopeCanvas != null)
        {
            scopeCanvas.SetActive(false);
            EditorUtility.SetDirty(scopeCanvas);
            Debug.Log("SetupHorrorNight: ScopeCanvas deactivated");
        }

        // --- Flashlight: attach to Main Camera if missing; SpotLight
        // child of Main Camera pointing forward (README: spotLight field,
        // intensity 4 default).
        Flashlight flash = camGo.GetComponent<Flashlight>();
        if (flash == null)
        {
            flash = camGo.AddComponent<Flashlight>();
            Debug.Log("SetupHorrorNight: Flashlight added to MainCamera");
        }
        Transform spotT = camGo.transform.Find("Flashlight");
        Light spot = null;
        if (spotT != null) spot = spotT.GetComponent<Light>();
        if (spot == null)
        {
            GameObject spotGo = new GameObject("Flashlight");
            spotGo.transform.SetParent(camGo.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            spotGo.transform.localRotation = Quaternion.identity; // +Z = camera forward
            spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            Debug.Log("SetupHorrorNight: SpotLight child 'Flashlight' created under MainCamera");
        }
        spot.spotAngle = 40f;
        spot.range = 25f;
        spot.intensity = 4f;
        spot.color = new Color(1f, 0.96f, 0.88f);
        spot.enabled = true;
        flash.spotLight = spot;
        flash.intensity = 4f;
        EditorUtility.SetDirty(spot);
        EditorUtility.SetDirty(flash);
        Debug.Log("SetupHorrorNight: Flashlight wired (spot 40 deg, range 25, intensity 4)");

        // --- TouchControls: NightCity only, never MainMenu.
        // (FixTouchControls.cs pattern: root GO "TouchControls".)
        GameObject tcGo = FindGameObject(scene, "TouchControls");
        if (tcGo == null)
        {
            tcGo = new GameObject("TouchControls");
            tcGo.AddComponent<TouchControls>();
            Debug.Log("SetupHorrorNight: TouchControls GameObject created in NightCity");
        }
        else if (tcGo.GetComponent<TouchControls>() == null)
        {
            tcGo.AddComponent<TouchControls>();
            Debug.Log("SetupHorrorNight: TouchControls component added to existing 'TouchControls' GameObject");
        }
        else
        {
            Debug.Log("SetupHorrorNight: TouchControls already present in NightCity");
        }

        // --- Sniper: the ONE weapon. Weapon_ScarH is deactivated (one
        // weapon only, no switching — HORROR_SCRIPTS_README.md).
        GameObject scar = FindGameObject(scene, "Weapon_ScarH");
        if (scar != null && scar.activeSelf)
        {
            scar.SetActive(false);
            EditorUtility.SetDirty(scar);
            Debug.Log("SetupHorrorNight: Weapon_ScarH deactivated (sniper-only game)");
        }
        GameObject sniper = RequireGO(scene, "Weapon_Sniper");
        if (!sniper.activeSelf)
        {
            sniper.SetActive(true);
            Debug.Log("SetupHorrorNight: Weapon_Sniper activated");
        }
        ShootingController sc = sniper.GetComponent<ShootingController>();
        if (sc == null)
        {
            sc = sniper.AddComponent<ShootingController>();
            Debug.Log("SetupHorrorNight: ShootingController added to Weapon_Sniper");
        }
        sc.isAuto = false;
        sc.fireRate = 1f;
        sc.fireRange = 60f;
        sc.damagePerShot = 50;
        sc.maxAmmo = 6;
        sc.currentAmmo = 6;
        sc.reserveAmmo = 24;
        sc.maxReserveAmmo = 48;
        sc.reloadTime = 2.5f;
        // README: animator LEAVE EMPTY — the sniper has no animator and
        // every animator call in ShootingController is null-guarded.
        // (The old builder assigned a SniperController animator; clear it.)
        sc.animator = null;

        Transform fp = sniper.transform.Find("FirePoint");
        if (fp == null)
        {
            GameObject fpGo = new GameObject("FirePoint");
            fpGo.transform.SetParent(sniper.transform, false);
            fpGo.transform.localPosition = new Vector3(0.05f, 0f, 1.1f); // old builder's sniper muzzle
            fp = fpGo.transform;
            Debug.Log("SetupHorrorNight: FirePoint created on Weapon_Sniper");
        }
        sc.firePoint = fp;

        ParticleSystem muzzle = sniper.GetComponentInChildren<ParticleSystem>(true);
        if (muzzle == null)
        {
            // Same helper the old builder used for both weapons.
            muzzle = BuildTheRealSK_Prefabs.AddMuzzleFlash(sniper, new Vector3(0.05f, 0f, 1.1f));
            Debug.Log("SetupHorrorNight: MuzzleFlash created on Weapon_Sniper");
        }
        sc.muzzleFlash = muzzle;

        AudioSource gunAudio = sniper.GetComponentInChildren<AudioSource>(true);
        if (gunAudio == null)
        {
            GameObject snd = new GameObject("GunAudio");
            snd.transform.SetParent(sniper.transform, false);
            gunAudio = snd.AddComponent<AudioSource>();
            Debug.Log("SetupHorrorNight: GunAudio AudioSource created on Weapon_Sniper");
        }
        sc.soundAudioSource = gunAudio;
        sc.shootingSoundClip = RequireAudio(GunshotPath);
        // reloadSoundClip intentionally left as-is (may stay empty —
        // README: no reload clip in the approved audio; null-guarded).
        GameObject bloodParticle = RequirePrefab(BloodParticlePath);
        sc.bloodParticlePrefab = bloodParticle;

        GameObject playerUI = RequireGO(scene, "PlayerUI");
        sc.currentAmmoText = RequireText(playerUI.transform, "AmmoText");
        EditorUtility.SetDirty(sc);
        Debug.Log("SetupHorrorNight: ShootingController wired on Weapon_Sniper "
            + "(semi-auto, rate 1, range 60, dmg 50, mag 6, reserve 24/48, reload 2.5s, ammo text AmmoText)");
    }

    // ================= Step 3: EventSystem (exactly one) =================
    private static void EnsureEventSystem(Scene scene, string scenePath)
    {
        List<EventSystem> systems = FindAllInScene<EventSystem>(scene);
        if (systems.Count == 0)
        {
            // Same input module type MainMenu uses (StandaloneInputModule —
            // see file header; FixMenuButtons.EnsureEventSystem pattern).
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("SetupHorrorNight: EventSystem created in " + scenePath + " (StandaloneInputModule)");
            return;
        }
        for (int i = 1; i < systems.Count; i++)
        {
            Debug.LogWarning("SetupHorrorNight: deleting duplicate EventSystem '"
                + systems[i].gameObject.name + "' in " + scenePath);
            Object.DestroyImmediate(systems[i].gameObject);
        }
        EventSystem es = systems[0];
        if (es.GetComponent<BaseInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
            Debug.Log("SetupHorrorNight: StandaloneInputModule added to existing EventSystem in " + scenePath);
        }
        EditorUtility.SetDirty(es.gameObject);
        Debug.Log("SetupHorrorNight: exactly one EventSystem in " + scenePath);
    }

    // ================= Step 4: zombies =================
    private static void SetupZombies(Scene scene)
    {
        GameObject player = RequireGO(scene, "Player");
        if (player.tag != "Player") Fail("Player GameObject tag is '" + player.tag + "', expected 'Player'");

        // Spawn points: objects with "Spawn" in the name. NightCity holds
        // SpawnPoint1,2,3,6,7,8 (verified from the binary scene); the
        // three close ones (6,7,8) are ~20 m from the (0,0,8) player spawn.
        List<Transform> spawns = new List<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectSpawnPoints(root.transform, spawns);
        if (spawns.Count == 0)
        {
            GameObject spawnRoot = FindGameObject(scene, "SpawnPoints");
            if (spawnRoot == null)
            {
                spawnRoot = new GameObject("SpawnPoints");
                Debug.Log("SetupHorrorNight: SpawnPoints root created");
            }
            Vector3[] close =
            {
                new Vector3(14f, 0f, -14f),
                new Vector3(-14f, 0f, -14f),
                new Vector3(0f, 0f, -18f),
            };
            for (int i = 0; i < close.Length; i++)
            {
                GameObject sp = new GameObject("SpawnPoint" + (i + 1));
                sp.transform.SetParent(spawnRoot.transform, false);
                sp.transform.position = close[i];
                spawns.Add(sp.transform);
            }
            Debug.Log("SetupHorrorNight: no spawn points existed — created 3 close to the player spawn");
        }
        else
        {
            Debug.Log("SetupHorrorNight: found " + spawns.Count + " existing spawn point(s)");
        }
        // Snap every spawn point onto the NavMesh (qa-checklist.md §5).
        // A failed snap only warns: the point stays where it was and the
        // step-9 rebake uses the same geometry, so a genuinely off-mesh
        // point would need a human to move it.
        foreach (Transform sp in spawns)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(sp.position, out hit, 5f, NavMesh.AllAreas))
            {
                sp.position = hit.position;
                EditorUtility.SetDirty(sp.gameObject);
            }
            else
            {
                Debug.LogWarning("SetupHorrorNight: spawn '" + sp.name + "' at "
                    + sp.position + " is NOT on the NavMesh (SamplePosition failed) — left in place");
            }
        }

        ZombieWaveSystem ws = FindInScene<ZombieWaveSystem>(scene);
        if (ws == null) Fail("ZombieWaveSystem not found in NightCity (expected on 'Environment')");
        GameObject[] prefs = new GameObject[ZombiePrefabPaths.Length];
        for (int i = 0; i < ZombiePrefabPaths.Length; i++)
            prefs[i] = RequirePrefab(ZombiePrefabPaths[i]);
        ws.zombiePrefabs = prefs;
        ws.spawnPoints = spawns.ToArray();
        ws.timeBetweenWaves = 10f;
        ws.zombiesPerWave = 4;
        ws.maxAlive = 10;
        GameObject playerUI = RequireGO(scene, "PlayerUI");
        ws.WaveNumber = RequireText(playerUI.transform, "WaveNumberText");
        ws.WaveTimer = RequireText(playerUI.transform, "WaveTimerText");
        EditorUtility.SetDirty(ws);
        Debug.Log("SetupHorrorNight: ZombieWaveSystem wired (4 prefabs, "
            + spawns.Count + " spawn points, 10s between waves, 4/wave +2, maxAlive 10)");

        // ZombieGroan on EACH zombie prefab ASSET (not scene instances).
        AudioClip groan = RequireAudio(GroanPath);
        foreach (string path in ZombiePrefabPaths)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ZombieGroan zg = contents.GetComponent<ZombieGroan>();
                if (zg == null)
                {
                    zg = contents.AddComponent<ZombieGroan>();
                    Debug.Log("SetupHorrorNight: ZombieGroan added to prefab " + path);
                }
                zg.groanClip = groan;
                EditorUtility.SetDirty(zg);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("SetupHorrorNight: ZombieGroan.groanClip set on prefab " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }

    // ================= Step 5: HUD =================
    private static void SetupHUD(Scene scene)
    {
        GameObject playerUI = RequireGO(scene, "PlayerUI");

        GameManager gm = FindInScene<GameManager>(scene);
        if (gm == null) Fail("GameManager not found in NightCity (expected on 'Environment')");
        gm.highScoreText = RequireText(playerUI.transform, "HighScoreText");
        gm.currentScoreText = RequireText(playerUI.transform, "ScoreText");
        EditorUtility.SetDirty(gm);
        Debug.Log("SetupHorrorNight: GameManager wired (highScoreText=HighScoreText, currentScoreText=ScoreText)");

        GameObject player = RequireGO(scene, "Player");
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm == null) Fail("PlayerMovement not found on 'Player'");
        pm.healthSlider = RequireSlider(playerUI.transform, "HealthSlider");
        pm.healthText = GetOrCreateHealthText(playerUI.transform);
        pm.bloodScreenEffect = RequirePrefab(BloodScreenPath);
        // pm.deathScreen is wired in step 6 (SetupDeathScreen) once the
        // DeathScreen is guaranteed to exist; it is only the fallback
        // path (death normally goes through GameManager.OnPlayerDied).
        EditorUtility.SetDirty(pm);
        Debug.Log("SetupHorrorNight: PlayerMovement wired (healthSlider=HealthSlider, "
            + "healthText=HealthText, bloodScreenEffect=BloodScreen.prefab)");

        // Wave texts are wired in step 4 (SetupZombies); re-verify here
        // so a missing text fails loudly even if step 4 is ever skipped.
        ZombieWaveSystem ws = FindInScene<ZombieWaveSystem>(scene);
        if (ws == null) Fail("ZombieWaveSystem not found in NightCity during HUD wiring");
        if (ws.WaveNumber == null || ws.WaveTimer == null)
            Fail("ZombieWaveSystem WaveNumber/WaveTimer texts are null after step 4");
        Debug.Log("SetupHorrorNight: ZombieWaveSystem texts verified (WaveNumberText, WaveTimerText)");
    }

    // The old builder created "HealthLabel" ("HEALTH") but never a
    // "HealthText" object, so there is nothing to find: create it next
    // to the health slider (top-left, right of the slider) instead of
    // throwing. Documented deviation from the "throw if missing" rule —
    // throwing here would make SETUP COMPLETE unreachable.
    private static Text GetOrCreateHealthText(Transform canvasT)
    {
        Text existing = FindText(canvasT, "HealthText");
        if (existing != null) return existing;
        // HealthSlider spans x 20-240 at the 1920x1080 reference res, so
        // the text sits clear of it at x 250-330.
        Text t = MakeText(canvasT, "HealthText", "100", 24, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(290, -30),
            TextAnchor.MiddleLeft, 80f);
        Debug.Log("SetupHorrorNight: HealthText did not exist (old builder only made HealthLabel) — created at top-left next to HealthSlider");
        return t;
    }

    // ================= Step 6: DeathScreen =================
    private static void SetupDeathScreen(Scene scene)
    {
        GameObject playerUI = RequireGO(scene, "PlayerUI");
        DeathScreen ds = FindInScene<DeathScreen>(scene);
        GameObject dsGo;
        if (ds == null)
        {
            // Fallback only: the old builder put DeathScreen on the
            // fade-overlay GameObject under PlayerUI, so this path
            // should not trigger. If it does, build a manager object
            // that stays active (README: NOT the panel itself, because
            // Start() disables the panel). It is a full-screen
            // RectTransform UNDER PlayerUI so its fade Image/Text render.
            dsGo = new GameObject("DeathScreen", typeof(RectTransform));
            dsGo.transform.SetParent(playerUI.transform, false);
            RectTransform drt = dsGo.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            ds = dsGo.AddComponent<DeathScreen>();
            GameObject imgGo = new GameObject("DeathImage", typeof(RectTransform));
            imgGo.transform.SetParent(dsGo.transform, false);
            RectTransform irt = imgGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
            Image dim = imgGo.AddComponent<Image>();
            dim.color = new Color(0.4f, 0f, 0f, 0f);
            ds.targetImage = dim;
            ds.targetText = MakeText(dsGo.transform, "DeathText", "YOU DIED", 64,
                new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            ds.duration = 5f;
            Debug.LogWarning("SetupHorrorNight: no DeathScreen existed — created manager GameObject 'DeathScreen'");
        }
        else
        {
            dsGo = ds.gameObject;
            Debug.Log("SetupHorrorNight: DeathScreen found on '" + dsGo.name + "'");
        }

        // Verify the fade overlay fields the old builder assigned. If a
        // field is null, recover it from the existing objects; if that
        // fails, throw (a death screen with no overlay is broken).
        if (ds.targetImage == null)
        {
            ds.targetImage = dsGo.GetComponent<Image>();
            if (ds.targetImage == null) Fail("DeathScreen.targetImage is null and no Image on '" + dsGo.name + "'");
        }
        if (ds.targetText == null)
        {
            Transform dt = dsGo.transform.Find("DeathText");
            if (dt != null) ds.targetText = dt.GetComponent<Text>();
            if (ds.targetText == null) Fail("DeathScreen.targetText is null and no 'DeathText' child on '" + dsGo.name + "'");
        }
        ds.duration = 5f;
        // QA §1: the full-screen fade Image must not eat taps while it
        // is transparent — it never needs input itself.
        ds.targetImage.raycastTarget = false;

        // Game-over panel + texts + buttons: these never existed in
        // NightCity (the old builder made only the fade overlay), so
        // create them under PlayerUI. The panel starts hidden;
        // DeathScreen.Start() also disables it at runtime.
        Transform panelT = playerUI.transform.Find("GameOverPanel");
        GameObject panel;
        if (panelT != null)
        {
            panel = panelT.gameObject;
            Debug.Log("SetupHorrorNight: GameOverPanel already exists — reusing it");
        }
        else
        {
            panel = new GameObject("GameOverPanel", typeof(RectTransform));
            panel.transform.SetParent(playerUI.transform, false);
            RectTransform prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0f, 0f, 0.75f);
            Debug.Log("SetupHorrorNight: GameOverPanel created under PlayerUI");
        }
        ds.gameOverPanel = panel;

        ds.finalScoreText = GetOrCreatePanelText(panel.transform, "FinalScoreText", "Score: 0", new Vector2(0, 60), 32);
        ds.waveReachedText = GetOrCreatePanelText(panel.transform, "WaveReachedText", "Wave: 1", new Vector2(0, 0), 28);
        ds.restartButton = GetOrCreatePanelButton(panel.transform, "RestartButton", "RESTART", new Vector2(0, -90));
        ds.quitButton = GetOrCreatePanelButton(panel.transform, "QuitButton", "QUIT", new Vector2(0, -160));

        // SERIALIZED onClick wiring (UnityEventTools.AddPersistentListener).
        // NEVER runtime AddListener in editor code — that is what killed
        // The real SK Build #2's menu buttons.
        WireButtonPersistent(ds.restartButton, ds, "RestartGame");
        WireButtonPersistent(ds.quitButton, ds, "QuitToMenu");

        panel.SetActive(false);
        EditorUtility.SetDirty(ds);
        EditorUtility.SetDirty(panel);

        // Fallback wiring for PlayerMovement (see step 5 note).
        GameObject player = FindGameObject(scene, "Player");
        if (player != null)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null && pm.deathScreen == null)
            {
                pm.deathScreen = ds;
                EditorUtility.SetDirty(pm);
                Debug.Log("SetupHorrorNight: PlayerMovement.deathScreen wired (fallback path)");
            }
        }
        Debug.Log("SetupHorrorNight: DeathScreen wired (panel + finalScoreText + waveReachedText + Restart/Quit serialized onClick)");
    }

    private static Text GetOrCreatePanelText(Transform panelT, string name, string content, Vector2 pos, int fontSize)
    {
        Text existing = FindText(panelT, name);
        if (existing != null) return existing;
        return MakeText(panelT, name, content, fontSize, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, TextAnchor.MiddleCenter, 400f);
    }

    private static Button GetOrCreatePanelButton(Transform panelT, string name, string label, Vector2 pos)
    {
        Transform existing = panelT.Find(name);
        if (existing != null)
        {
            Button b = existing.GetComponent<Button>();
            if (b == null) Fail("GameObject '" + name + "' in GameOverPanel has no Button component");
            return b;
        }
        GameObject go = DefaultControls.CreateButton(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(panelT, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(260, 56);
        Text t = go.GetComponentInChildren<Text>();
        if (t != null) { t.text = label; t.fontSize = 22; }
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        return go.GetComponent<Button>();
    }

    // Adds ONE persistent (serialized) listener and removes any stale
    // persistent listeners pointing at the same method, so re-running
    // this script can never stack duplicate Restart/Quit calls.
    private static void WireButtonPersistent(Button button, DeathScreen ds, string methodName)
    {
        if (button == null) Fail("Cannot wire onClick: Button is null (expected " + methodName + ")");
        UnityAction action = methodName == "RestartGame"
            ? (UnityAction)ds.RestartGame
            : (UnityAction)ds.QuitToMenu;
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName
                && button.onClick.GetPersistentTarget(i) == ds)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }
        UnityEventTools.AddPersistentListener(button.onClick, action);
        int index = button.onClick.GetPersistentEventCount() - 1;
        // Unity 6 removed UnityEventTools.SetPersistentListenerState; the
        // instance method on UnityEventBase is the equivalent.
        button.onClick.SetPersistentListenerState(index, UnityEventCallState.RuntimeOnly);
        EditorUtility.SetDirty(button);
        Debug.Log("SetupHorrorNight: '" + button.gameObject.name
            + "' onClick persistently wired to DeathScreen." + methodName + "()");
    }

    // ================= Step 7: menu cleanup =================
    private static void CleanupMainMenu(Scene scene)
    {
        SettingsManager sm = FindInScene<SettingsManager>(scene);
        if (sm == null) Fail("SettingsManager not found in MainMenu (expected on 'Main_Menu')");

        // Disable Map1Button. This is SAFE for SettingsManager: both map
        // buttons call SelectMap("NightCity") and selectedMap defaults to
        // "NightCity" (SettingsManager.cs header + Start()), so no map
        // selection can break; and Start() calls
        // map1Button.onClick.AddListener(...) on the Button COMPONENT,
        // which still exists on the inactive GameObject — deactivating
        // only removes it from clicks, it does NOT null the field, so
        // no NullReferenceException. The field is therefore deliberately
        // LEFT assigned (clearing it to null WOULD break Start()).
        GameObject map1 = RequireGO(scene, "Map1Button");
        map1.SetActive(false);
        EditorUtility.SetDirty(map1);
        if (sm.map1Button == null) Fail("SettingsManager.map1Button became null after disabling Map1Button");
        Debug.Log("SetupHorrorNight: Map1Button disabled (SettingsManager.map1Button field left assigned — see comment)");

        // Relabel the remaining map button (Map2Button) to PLAY.
        GameObject map2 = RequireGO(scene, "Map2Button");
        Text map2Text = map2.GetComponentInChildren<Text>(true);
        if (map2Text == null) Fail("Map2Button has no child Text to relabel");
        map2Text.text = "PLAY";
        EditorUtility.SetDirty(map2Text);
        if (sm.map2Button == null) Fail("SettingsManager.map2Button is null in MainMenu");
        Debug.Log("SetupHorrorNight: Map2Button relabelled to 'PLAY' (still loads NightCity)");

        // QuitButton must keep working: its field is assigned and
        // SettingsManager.Start() wires it at runtime to
        // Application.Quit(). Verify the field; do not touch the wiring.
        GameObject quit = RequireGO(scene, "QuitButton");
        if (sm.quitButton == null) Fail("SettingsManager.quitButton is null in MainMenu");
        if (sm.quitButton.gameObject != quit)
            Fail("SettingsManager.quitButton does not point at the 'QuitButton' GameObject");
        Debug.Log("SetupHorrorNight: QuitButton verified (SettingsManager.quitButton assigned, runtime-wired to Quit)");

        // Credits text: keep as-is, verify it still exists.
        GameObject credits = RequireGO(scene, "Credits");
        Text creditsText = credits.GetComponent<Text>();
        if (creditsText == null) creditsText = credits.GetComponentInChildren<Text>(true);
        if (creditsText == null) Fail("'Credits' has no Text component in MainMenu");
        Debug.Log("SetupHorrorNight: Credits kept (" + creditsText.text.Length + " chars)");

        EditorUtility.SetDirty(sm);
    }

    // ================= Step 8: Player Settings =================
    private static void SetupPlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.santosh.horrornight");
        Debug.Log("SetupHorrorNight: applicationIdentifier (Android) = "
            + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));

        PlayerSettings.productName = "Horror Night";
        Debug.Log("SetupHorrorNight: productName = " + PlayerSettings.productName);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        Debug.Log("SetupHorrorNight: defaultInterfaceOrientation = " + PlayerSettings.defaultInterfaceOrientation);

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        Debug.Log("SetupHorrorNight: scriptingBackend (Android) = "
            + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android));

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        Debug.Log("SetupHorrorNight: targetArchitectures (Android) = "
            + PlayerSettings.Android.targetArchitectures);

        // QA §8: both scenes must be in the build list (already set by
        // SetupTheRealSK.cs: MainMenu 0, NightCity 1) — verify, don't move.
        bool hasMenu = false, hasNight = false;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            Debug.Log("SetupHorrorNight: build scene: " + s.path + " (enabled=" + s.enabled + ")");
            if (s.enabled && s.path == MainMenuPath) hasMenu = true;
            if (s.enabled && s.path == NightCityPath) hasNight = true;
        }
        if (!hasMenu || !hasNight)
            Fail("Build Settings is missing an enabled scene (MainMenu=" + hasMenu + ", NightCity=" + hasNight + ")");
        Debug.Log("SetupHorrorNight: Player Settings verified (com.santosh.horrornight, Horror Night, LandscapeLeft, IL2CPP, ARM64)");
    }

    // ================= Step 9: NavMesh rebake =================
    private static void RebakeNavMesh(Scene scene)
    {
        // Same mechanism The real SK used: a NavMeshSurface component
        // (com.unity.ai.navigation 2.0.0) on 'Environment', baked with
        // BuildNavMesh() — BuildTheRealSK_Scenes.cs BuildMap() and
        // FixTouchControls.cs step 7 (player hidden during the bake so
        // its weapon meshes are not baked in).
        GameObject env = RequireGO(scene, "Environment");
        NavMeshSurface surface = env.GetComponent<NavMeshSurface>();
        if (surface == null) Fail("NavMeshSurface not found on 'Environment' in NightCity — cannot rebake NavMesh");
        GameObject player = FindGameObject(scene, "Player");
        bool wasActive = player != null && player.activeSelf;
        if (player != null) player.SetActive(false);
        try
        {
            surface.BuildNavMesh();
        }
        finally
        {
            if (player != null) player.SetActive(wasActive);
        }
        EditorUtility.SetDirty(surface);
        Debug.Log("SetupHorrorNight: NavMesh rebaked in NightCity (NavMeshSurface on Environment)");
    }

    // ================= helpers =================
    private static void Fail(string message)
    {
        throw new System.Exception("SetupHorrorNight FAILED: " + message);
    }

    private static GameObject RequireGO(Scene scene, string name)
    {
        GameObject go = FindGameObject(scene, name);
        if (go == null) Fail("GameObject '" + name + "' not found in scene '" + scene.name + "'");
        return go;
    }

    private static GameObject RequirePrefab(string path)
    {
        GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (p == null) Fail("Prefab not found: " + path);
        return p;
    }

    private static AudioClip RequireAudio(string path)
    {
        AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Fail("AudioClip not found: " + path);
        return c;
    }

    private static Text RequireText(Transform parent, string name)
    {
        Text t = FindText(parent, name);
        if (t == null) Fail("UI Text '" + name + "' not found under '" + parent.name + "'");
        return t;
    }

    private static Slider RequireSlider(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null) Fail("UI Slider '" + name + "' not found under '" + parent.name + "'");
        Slider s = t.GetComponent<Slider>();
        if (s == null) Fail("GameObject '" + name + "' has no Slider component");
        return s;
    }

    private static Text FindText(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null) return null;
        return t.GetComponent<Text>();
    }

    // Old builder's MakeText (BuildTheRealSK_Scenes.cs), reused for the
    // HealthText / panel texts this script creates.
    private static Text MakeText(Transform parent, string name, string content, int fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter, float width = 400f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 50);
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }

    private static void RemoveComponentsByTypeName(GameObject go, string typeName)
    {
        if (go == null) return;
        // Snapshot first: DestroyImmediate mutates the component list.
        Component[] comps = go.GetComponents<Component>();
        foreach (Component c in comps)
        {
            if (c != null && c.GetType().Name == typeName)
            {
                Debug.Log("SetupHorrorNight: removed " + typeName + " from '" + go.name + "'");
                Object.DestroyImmediate(c);
            }
        }
    }

    private static void CollectSpawnPoints(Transform t, List<Transform> outList)
    {
        // StartsWith, NOT Contains("Spawn"): the container "SpawnPoints"
        // itself contains "Spawn" and must NOT become a spawn point.
        if (t.name.StartsWith("SpawnPoint")) outList.Add(t);
        foreach (Transform child in t) CollectSpawnPoints(child, outList);
    }

    private static GameObject FindGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindGameObjectRecursive(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindGameObjectRecursive(GameObject go, string name)
    {
        if (go.name == name) return go;
        foreach (Transform child in go.transform)
        {
            GameObject found = FindGameObjectRecursive(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = FindInChildren<T>(root.transform);
            if (found != null) return found;
        }
        return null;
    }

    private static T FindInChildren<T>(Transform t) where T : Object
    {
        T c = t.GetComponent<T>();
        if (c != null) return c;
        foreach (Transform child in t)
        {
            T found = FindInChildren<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static List<T> FindAllInScene<T>(Scene scene) where T : Object
    {
        List<T> list = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            CollectInChildren<T>(root.transform, list);
        return list;
    }

    private static void CollectInChildren<T>(Transform t, List<T> outList) where T : Object
    {
        T c = t.GetComponent<T>();
        if (c != null) outList.Add(c);
        foreach (Transform child in t) CollectInChildren<T>(child, outList);
    }
}
