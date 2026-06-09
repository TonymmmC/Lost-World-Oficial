using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Arma a Wukong como jugador: crea un Animator Controller unificado con todos sus
// estados, el prefab del efecto de curacion, y configura el PlayerMovement/PlayerBlock
// del jugador en la escena. Menu: Tools > Jugador > Configurar Wukong.
public static class ConfigurarWukong
{
    private const string AnimDir = "Assets/Wukong/Animations";
    private const string CtrlPath = "Assets/Wukong/Wukong.controller";
    private const string HealSprite = "Assets/Tiny Swords/Units/Extra/Heal Effect/Heal_Effect.png";
    private const string HealPrefabPath = "Assets/Prefabs/Effects/Heal_Effect.prefab";

    // Estado base -> nombre (coincide con el clip .anim y con lo que espera PlayerMovement).
    private static readonly string[] Estados =
    {
        "Wuko_Idle", "Wuko_Run", "Wuko_attack1", "Wuko_Attack2",
        "Wuko_Heavy_Attack", "Wuko_Guard", "Wuko_heal",
    };

    [MenuItem("Tools/Jugador/Configurar Wukong")]
    public static void Configurar()
    {
        AnimatorController ctrl = CrearController();
        GameObject heal = CrearHealEffect();
        ConfigurarJugador(ctrl, heal);
    }

    private static AnimatorController CrearController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath) != null)
            AssetDatabase.DeleteAsset(CtrlPath);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (string nombre in Estados)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimDir}/{nombre}.anim");
            if (clip == null) { Debug.LogWarning($"Falta el clip {nombre}.anim en {AnimDir}"); continue; }
            AnimatorState st = sm.AddState(nombre);
            st.motion = clip;
            if (nombre == "Wuko_Idle") sm.defaultState = st;
        }
        Debug.Log("Wukong.controller creado con " + Estados.Length + " estados.");
        return ctrl;
    }

    private static GameObject CrearHealEffect()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
        }
        GameObject go = new GameObject("Heal_Effect");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite(HealSprite);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 70;
        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = BuscarController("Heal");

        EfectoTemporal ef = go.AddComponent<EfectoTemporal>();
        SerializedObject so = new SerializedObject(ef);
        so.FindProperty("duracion").floatValue = 0.9f; // estado vacio = usa el default del controller
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, HealPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void ConfigurarJugador(AnimatorController ctrl, GameObject heal)
    {
        PlayerMovement pm = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PlayerMovement>() : null;
        if (pm == null) pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) { Debug.LogError("No encontre un PlayerMovement en la escena. Selecciona el jugador y reintenta."); return; }

        GameObject player = pm.gameObject;
        Animator anim = player.GetComponent<Animator>();
        if (anim != null) anim.runtimeAnimatorController = ctrl;

        SerializedObject so = new SerializedObject(pm);
        so.FindProperty("idleState").stringValue = "Wuko_Idle";
        so.FindProperty("runState").stringValue = "Wuko_Run";
        so.FindProperty("attackState").stringValue = "Wuko_attack1";
        so.FindProperty("attackState2").stringValue = "Wuko_Attack2";
        so.FindProperty("heavyAttackState").stringValue = "Wuko_Heavy_Attack";
        so.FindProperty("healState").stringValue = "Wuko_heal";
        so.FindProperty("healEffectPrefab").objectReferenceValue = heal;
        so.ApplyModifiedPropertiesWithoutUndo();

        PlayerBlock pb = player.GetComponent<PlayerBlock>();
        if (pb != null)
        {
            SerializedObject sob = new SerializedObject(pb);
            sob.FindProperty("shieldState").stringValue = "Wuko_Guard";
            sob.ApplyModifiedPropertiesWithoutUndo();
        }

        PlayerWeapons pw = player.GetComponent<PlayerWeapons>();
        if (pw != null) pw.enabled = false; // formas de lado: Wukong standalone

        EditorUtility.SetDirty(player);
        Selection.activeObject = player;
        Debug.Log($"Jugador '{player.name}' configurado como Wukong. Guarda la escena (Ctrl+S).");
    }

    private static RuntimeAnimatorController BuscarController(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{nombre} t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == nombre)
                return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        }
        Debug.LogWarning("No encontre el controller " + nombre);
        return null;
    }

    private static Sprite CargarSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite sub) return sub;
        Debug.LogWarning("No encontre el sprite en " + path);
        return null;
    }
}
