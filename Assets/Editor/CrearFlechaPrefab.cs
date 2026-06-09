using UnityEditor;
using UnityEngine;

// Herramienta de editor: crea el prefab de la flecha ya configurado (raiz con Rigidbody,
// collider trigger y EnemyProjectile + hijo Visual con el sprite, en modo arco). Evita
// armarlo a mano en el Inspector. Menu: Tools > Crear prefab Flecha.
public static class CrearFlechaPrefab
{
    private const string SpritePath = "Assets/Tiny Swords/Units/Extra/Arrow/Arrow.png";
    private const string CarpetaPrefab = "Assets/Prefabs/Projectiles";
    private const string RutaPrefab = "Assets/Prefabs/Projectiles/Flecha.prefab";

    [MenuItem("Tools/Proyectiles/Flecha")]
    public static void Crear()
    {
        GameObject raiz = new GameObject("Flecha");
        Rigidbody2D rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        BoxCollider2D col = raiz.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.3f, 0.1f);
        EnemyProjectile proj = raiz.AddComponent<EnemyProjectile>();

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform);
        visual.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite();
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 50;

        ConfigurarProyectil(proj, visual.transform);
        GuardarPrefab(raiz);
    }

    private static void ConfigurarProyectil(EnemyProjectile proj, Transform visual)
    {
        SerializedObject so = new SerializedObject(proj);
        so.FindProperty("visual").objectReferenceValue = visual;
        so.FindProperty("arcoAltura").floatValue = 1f;
        so.FindProperty("arcoDuracion").floatValue = 0.7f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void GuardarPrefab(GameObject raiz)
    {
        if (!AssetDatabase.IsValidFolder(CarpetaPrefab))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "Projectiles");
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log(prefab != null
            ? "Flecha creada en " + RutaPrefab + ". Asignala en Projectile Prefab del arquero."
            : "No se pudo crear la Flecha.");
    }

    private static Sprite CargarSprite()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (s != null) return s;
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(SpritePath))
            if (obj is Sprite sub) return sub;
        Debug.LogWarning("No encontre el sprite Arrow en " + SpritePath);
        return null;
    }
}
