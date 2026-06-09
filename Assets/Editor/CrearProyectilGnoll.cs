using UnityEditor;
using UnityEngine;

// Crea el proyectil hueso del Gnoll (sprite Gnoll_Bone girando, vuela en arco, sin
// explosion). Menu: Tools > Crear proyectil Gnoll. Luego el Gnoll lo usa automaticamente.
public static class CrearProyectilGnoll
{
    private const string BaseGnoll = "Assets/Tiny Swords - Enemy Pack/Enemies/Gnoll";
    private const string Carpeta = "Assets/Prefabs/Projectiles";
    private const string RutaHueso = "Assets/Prefabs/Projectiles/Hueso.prefab";

    [MenuItem("Tools/Proyectiles/Hueso Gnoll")]
    public static void Crear()
    {
        AsegurarCarpeta();

        GameObject raiz = new GameObject("Hueso");
        Rigidbody2D rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        BoxCollider2D col = raiz.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.3f, 0.3f);
        EnemyProjectile proj = raiz.AddComponent<EnemyProjectile>();

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform);
        visual.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite($"{BaseGnoll}/Gnoll_Bone.png");
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 50;
        Animator anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = BuscarController("Bone_Controller");

        SerializedObject so = new SerializedObject(proj);
        so.FindProperty("visual").objectReferenceValue = visual.transform;
        so.FindProperty("arcoAltura").floatValue = 1f;
        so.FindProperty("arcoDuracion").floatValue = 0.7f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaHueso);
        Object.DestroyImmediate(raiz);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Hueso creado en " + RutaHueso + ". El Gnoll lo usa al recrearlo, o asignalo a mano.");
    }

    private static void AsegurarCarpeta()
    {
        if (AssetDatabase.IsValidFolder(Carpeta)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
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
