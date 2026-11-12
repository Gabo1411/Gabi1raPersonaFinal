using UnityEngine;

// Texturas, materiales y utilidades de brillo generados por código: halos de antorchas, brasas, chispas y bruma.
// Los shaders son los de partículas clásicos (siempre incluidos en la build gracias a EnsureRuntimeShaders).
public static class GlowKit
{
    static Texture2D softTexture, smokeTexture;
    static Material additiveMaterial, alphaMaterial;

    // Disco con borde muy suave (para halos, brasas y chispas)
    public static Texture2D Soft
    {
        get
        {
            if (softTexture != null) return softTexture;

            const int N = 64;
            softTexture = NewTexture("BrilloSuave", N);
            Color32[] pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(1f - r, 2.2f);
                    byte v = (byte)(a * 255f);
                    pixels[y * N + x] = new Color32(v, v, v, v);
                }
            }
            softTexture.SetPixels32(pixels);
            softTexture.Apply();
            return softTexture;
        }
    }

    // Mancha irregular y muy difusa (para la bruma a ras de suelo)
    public static Texture2D Smoke
    {
        get
        {
            if (smokeTexture != null) return smokeTexture;

            const int N = 128;
            smokeTexture = NewTexture("Bruma", N);
            Color32[] pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float falloff = Mathf.Pow(1f - r, 1.6f);
                    float noise = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.07f + 11f, y * 0.07f + 3f);
                    byte v = (byte)(Mathf.Clamp01(falloff * noise) * 255f);
                    pixels[y * N + x] = new Color32(v, v, v, v);
                }
            }
            smokeTexture.SetPixels32(pixels);
            smokeTexture.Apply();
            return smokeTexture;
        }
    }

    // Suma luz sobre lo que hay detrás (fuego, brasas, halos)
    public static Material Additive
    {
        get
        {
            if (additiveMaterial != null) return additiveMaterial;
            additiveMaterial = new Material(RuntimeShaders.Find("Legacy Shaders/Particles/Additive", "Sprites/Default"));
            additiveMaterial.mainTexture = Soft;
            RuntimeShaders.SetColor(additiveMaterial, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return additiveMaterial;
        }
    }

    // Mezcla normal con transparencia (bruma)
    public static Material Alpha
    {
        get
        {
            if (alphaMaterial != null) return alphaMaterial;
            alphaMaterial = new Material(RuntimeShaders.Find("Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Particles/Additive", "Sprites/Default"));
            alphaMaterial.mainTexture = Smoke;
            RuntimeShaders.SetColor(alphaMaterial, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return alphaMaterial;
        }
    }

    static Texture2D NewTexture(string name, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = name;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    // Sistema de partículas ya detenido y con valores base, listo para configurar y llamar a Play()
    public static ParticleSystem NewParticles(string name, Transform parent, Material material, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    // Desvanece las partículas hacia el final de su vida
    public static void FadeOut(ParticleSystem particles, float startAlpha = 1f)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(startAlpha, 0.15f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = gradient;
    }
}
