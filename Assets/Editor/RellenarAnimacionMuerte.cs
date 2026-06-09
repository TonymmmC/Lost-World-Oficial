using System.Linq;
using UnityEditor;
using UnityEngine;

// Llena un clip de animacion con los frames de un sprite sheet ya cortado. Util cuando
// el clip existe pero esta vacio (sin keyframes). Menu: Tools/Animaciones/...
public static class RellenarAnimacionMuerte
{
    private const float Fps = 12f;

    [MenuItem("Tools/Animaciones/Rellenar Skull_Death")]
    private static void Skull()
    {
        Rellenar(
            "Assets/Tiny Swords - Enemy Pack/Enemies/Skull/Skull_Death.png",
            "Assets/Tiny Swords - Enemy Pack/Enemies/Skull/Skull Animations/Skull_Death.anim");
    }

    private static void Rellenar(string pngPath, string clipPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToArray();
        if (sprites.Length <= 1) { Debug.LogError("El sheet no esta cortado en frames: " + pngPath); return; }
        System.Array.Sort(sprites, (a, b) => NumeroFrame(a.name).CompareTo(NumeroFrame(b.name)));

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) { Debug.LogError("No encontre el clip: " + clipPath); return; }

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / Fps, value = sprites[i] };

        // Binding al SpriteRenderer del mismo objeto que el Animator (path vacio = raiz).
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        clip.frameRate = Fps;
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false; // la muerte no se repite
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"{clip.name} rellenado con {sprites.Length} frames a {Fps} fps, loop off.");
    }

    private static int NumeroFrame(string nombre)
    {
        int i = nombre.LastIndexOf('_');
        return (i >= 0 && int.TryParse(nombre.Substring(i + 1), out int n)) ? n : 0;
    }
}
