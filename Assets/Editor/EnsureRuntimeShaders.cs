using UnityEditor;
using UnityEngine;

// Los shaders que el código busca por nombre (Shader.Find) no entran en la build si ningún material los usa,
// y entonces los efectos se ven rosas. Este script añade a 'Always Included Shaders' solo los shaders pequeños.
//
// Los shaders de URP (Lit, Unlit...) NO deben estar en esa lista: ahí Unity incluye todas sus variantes
// (más de dos millones para Lit) y la build falla. Los materiales que los necesitan están guardados como assets
// (por ejemplo Resources/Materials/SniperCrystal.mat), así Unity incluye justo las variantes que se usan.
[InitializeOnLoad]
public static class EnsureRuntimeShaders
{
    static readonly string[] Names =
    {
        "Legacy Shaders/Particles/Additive",
        "Legacy Shaders/Particles/Alpha Blended",
        "Sprites/Default",
    };

    // Estos se quitan de la lista si una versión anterior de este script los había añadido
    static readonly string[] Forbidden =
    {
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Unlit",
        "Universal Render Pipeline/Particles/Unlit",
    };

    static EnsureRuntimeShaders()
    {
        EditorApplication.delayCall += Ensure;
    }

    public static void RunNow()
    {
        Ensure();
    }

    static void Ensure()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;

        SerializedObject settings = new SerializedObject(assets[0]);
        SerializedProperty list = settings.FindProperty("m_AlwaysIncludedShaders");
        if (list == null) return;

        bool changed = false;

        // Quitar los shaders que hacen fallar la build
        foreach (string name in Forbidden)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) continue;

            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != shader) continue;

                // En un array de objetos, la primera llamada deja el elemento en null y la segunda lo elimina
                list.DeleteArrayElementAtIndex(i);
                if (i < list.arraySize && list.GetArrayElementAtIndex(i).objectReferenceValue == shader) list.DeleteArrayElementAtIndex(i);
                changed = true;
            }
        }

        // Añadir los pequeños
        foreach (string name in Names)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) continue;

            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
            }
            if (present) continue;

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            changed = true;
        }

        if (changed)
        {
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets[0]);
            // Los ajustes del proyecto no se guardan con SaveAssets: hay que serializarlos explícitamente
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { assets[0] }, "ProjectSettings/GraphicsSettings.asset", true);
            Debug.Log("Always Included Shaders actualizado (sin los shaders de URP).");
        }
    }
}
