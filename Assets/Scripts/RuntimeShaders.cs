using UnityEngine;

// Busca shaders por nombre con alternativas: Shader.Find devuelve null si el shader no entró en la build,
// y un material sin shader se ve rosa. Así siempre se obtiene alguno válido.
public static class RuntimeShaders
{
    public static Shader Find(params string[] names)
    {
        foreach (string name in names)
        {
            Shader shader = Shader.Find(name);
            if (shader != null) return shader;
        }

        Shader fallback = Shader.Find("Sprites/Default");
        if (fallback == null) fallback = Shader.Find("Universal Render Pipeline/Lit");
        return fallback;
    }

    public static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
    }
}
