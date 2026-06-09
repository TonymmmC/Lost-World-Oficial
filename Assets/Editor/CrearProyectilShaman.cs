using UnityEditor;
using UnityEngine;

// Crea el proyectil magico del Shaman (sprite Shaman_Projectile, explota en area al
// impactar) y su efecto de explosion (Proyectile_Explosion). Menu: Tools > Crear
// proyectil Shaman. Despues asignar Proyectil_Shaman en el Projectile Prefab del Shaman.
public static class CrearProyectilShaman
{
    private const string BaseShaman = "Assets/Tiny Swords - Enemy Pack/Enemies/Shaman";
    private const string Carpeta = "Assets/Prefabs/Projectiles";
    private const string RutaExplosion = "Assets/Prefabs/Projectiles/Explosion_Shaman.prefab";
    private const string RutaProyectil = "Assets/Prefabs/Projectiles/Proyectil_Shaman.prefab";

    [MenuItem("Tools/Proyectiles/Magia Shaman")]
    public static void Crear()
    {
        AsegurarCarpeta();
        GameObject explosion = CrearExplosion();
        CrearProyectil(explosion);
        Debug.Log("Proyectil del Shaman creado. Asignalo en Enemy_Shaman > Enemy A Distancia > Projectile Prefab.");
    }

    private static GameObject CrearExplosion()
    {
        GameObject go = new GameObject("Explosion_Shaman");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite($"{BaseShaman}/Shaman_Explosion.png");
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 60;
        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = BuscarController("Proyectile_Controller");

        EfectoTemporal ef = go.AddComponent<EfectoTemporal>();
        SerializedObject so = new SerializedObject(ef);
        so.FindProperty("estado").stringValue = "Proyectile_Explosion";
        so.FindProperty("duracion").floatValue = 0.6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaExplosion);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void CrearProyectil(GameObject explosion)
    {
        GameObject go = new GameObject("Proyectil_Shaman");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite($"{BaseShaman}/Shaman_Projectile.png");
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 55;
        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = BuscarController("Proyectile_Controller");

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.2f;

        EnemyProjectile proj = go.AddComponent<EnemyProjectile>();
        SerializedObject so = new SerializedObject(proj);
        so.FindProperty("explosionRadius").floatValue = 1.5f;
        so.FindProperty("explosionEffectPrefab").objectReferenceValue = explosion;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaProyectil);
        Object.DestroyImmediate(go);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
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
