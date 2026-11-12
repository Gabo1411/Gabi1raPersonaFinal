using UnityEditor;
using UnityEngine;

// Los shaders que el código busca por nombre (Shader.Find) no entran en la build si ningún material los usa,
// y entonces los efectos se ven rosas. Este script los añade a la lista de shaders siempre incluidos.
[InitializeOnLoad]
public static class EnsureRuntimeShaders
{
    static readonly string[] Names =
    {
        "Legacy Shaders/Particles/Additive",
        "Legacy Shaders/Particles/Alpha Blended",
        "Sprites/Default",
        "Universal Render Pipeline/Unlit",
        "Universal Render Pipeline/Lit",
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
            Debug.Log("Shaders añadidos a 'Always Included Shaders' para que los efectos se vean en la build.");
        }
    }
}
