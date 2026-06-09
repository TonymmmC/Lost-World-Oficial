using UnityEditor;
using UnityEngine;

// Crea (o reusa) un GameObject "_MusicaAmbiente" en la escena con un AudioSource en loop
// reproduciendo el clip de ambiente de la zona. Sin script de runtime: AudioSource con
// playOnAwake + loop alcanza para una zona. El volumen queda bajo para no tapar SFX.
public static class ConfigurarMusicaAmbiente
{
    private const string ClipPath = "Assets/Audio/Music/Ambient_Zone_Wukong.mp3";
    private const string ObjetoNombre = "_MusicaAmbiente";

    [MenuItem("Tools/Audio/Musica Ambiente Zona Wukong")]
    private static void Configurar()
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null) { Debug.LogError("No encontre el clip en " + ClipPath); return; }

        GameObject go = GameObject.Find(ObjetoNombre);
        if (go == null)
        {
            go = new GameObject(ObjetoNombre);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + ObjetoNombre);
        }

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(go);

        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.volume = 0.4f;
        src.spatialBlend = 0f; // 2D, se oye igual en toda la zona

        EditorUtility.SetDirty(go);
        Selection.activeObject = go;
        Debug.Log("Musica ambiente configurada en " + ObjetoNombre + ".");
    }
}
