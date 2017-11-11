using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeGI : MonoBehaviour {
    public Player player;
    [Range(0f, 1f)]
    public float spaceShadowStrength = 1f;
    [Range(0f, 1f)]
    public float groundShadowStrength = .5f;
    public Color spaceAmbientColor = Color.black;
    public Color atmosphereAmbientColor = new Color(.5f, .5f, .5f, 1f);

    public Light scaledLight;
    public Light realLight;

    Gravity playerGravity;

    void Start() {
        playerGravity = player.GetComponent<Gravity>();
    }

    public void LateUpdate() {
        if (playerGravity.planet && playerGravity.planet.hasAtmosphere) {
            Shader.SetGlobalColor("_PlanetAmbient", atmosphereAmbientColor);
            float t = (float)playerGravity.planet.AtmosphereDensity(playerGravity.distToPlanet);

            Color a = Color.Lerp(spaceAmbientColor, atmosphereAmbientColor, t);
            RenderSettings.ambientLight = a;
            RenderSettings.ambientSkyColor = a;
            RenderSettings.ambientEquatorColor = a;
            RenderSettings.ambientGroundColor = a;
            realLight.shadowStrength = Mathf.Lerp(spaceShadowStrength, groundShadowStrength, t);
            scaledLight.shadowStrength = Mathf.Lerp(spaceShadowStrength, groundShadowStrength, t);
        } else
            Shader.SetGlobalColor("_PlanetAmbient", spaceAmbientColor);
    }
}
