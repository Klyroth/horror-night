using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Flashlight — Horror Night (NEW, 2026-11-21)
// A SpotLight mounted on the player camera. The night city is dark;
// the flashlight is the player's main light source.
//  - Default ON when the scene starts.
//  - Toggle on PC with KeyCode.F.
//  - Toggle on Android with the LIGHT button in TouchControls, which
//    sets the static touchFlashlightToggle flag (consumed + reset here).
// Attach to the Player camera (or the Player) and assign the SpotLight
// in the Inspector. Everything is null-guarded: with no light assigned
// the script does nothing instead of throwing.
public class Flashlight : MonoBehaviour
{
    [Header("Flashlight")]
    public Light spotLight;
    public float intensity = 4f;

    // Android touch toggle (written by TouchControls LIGHT button)
    public static bool touchFlashlightToggle;

    void Start()
    {
        // Default ON when the scene starts.
        if (spotLight != null)
        {
            spotLight.intensity = intensity;
            spotLight.enabled = true;
        }
    }

    void Update()
    {
        bool toggle = Input.GetKeyDown(KeyCode.F) || touchFlashlightToggle;
        // Consume the touch flag either way so one tap = one toggle.
        touchFlashlightToggle = false;

        if (toggle && spotLight != null)
        {
            spotLight.enabled = !spotLight.enabled;
            if (spotLight.enabled)
            {
                spotLight.intensity = intensity;
            }
        }
    }
}
