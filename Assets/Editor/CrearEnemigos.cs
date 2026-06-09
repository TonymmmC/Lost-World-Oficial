using UnityEditor;
using UnityEngine;

// Generador de enemigos por menu (Tools/Enemigos/). Cada bicho se arma completo: sprite,
// Animator, Health, DamageFeedback, YSort, su IA y barra de vida copiada del Enemy_Skull.
// Tres tipos de IA: Cono (EnemyMeleeCono), Caja (SkullAI) y Distancia (EnemyADistancia).
public static class CrearEnemigos
{
    private enum Tipo { Cono, Caja, Distancia }

    private class Def
    {
        public string nombre, controller, sprite, idle, run, attack, death = "";
        public Tipo tipo = Tipo.Cono;
        public int danio = 1;
        public float attackRange = 0.9f;
        public float chaseSpeed = 3f;
        public string proyectil = FlechaPrefab;
    }

    private const string Pack = "Assets/Tiny Swords - Enemy Pack/Enemies";
    private const string Blue = "Assets/Tiny Swords/Units/Blue Units";
    private const string SkullPrefab = "Assets/Prefabs/Enemies/Enemy_Skull.prefab";
    private const string FlechaPrefab = "Assets/Prefabs/Projectiles/Flecha.prefab";

    // --- Melee de cono ---
    [MenuItem("Tools/Enemigos/Gnome")]
    private static void Gnome() => Crear(P("Enemy_Gnome", "Gnome", "Gnome_Idle", "Gnome_Run", "Gnome_Attack", Tipo.Cono));

    [MenuItem("Tools/Enemigos/Lizard")]
    private static void Lizard() => Crear(P("Enemy_Lizard", "Lizard", "Lizard_Idle", "Lizard_Run", "Lizard_Attack", Tipo.Cono));

    [MenuItem("Tools/Enemigos/Thief")]
    private static void Thief() => Crear(P("Enemy_Thief", "Thief", "Thief_Idle", "Thief_Run", "Thief_Attack", Tipo.Cono));

    [MenuItem("Tools/Enemigos/Panda")]
    private static void Panda() => Crear(P("Enemy_Panda", "Panda", "Panda_Idle", "Panda_Run", "Panda_Attack", Tipo.Cono));

    [MenuItem("Tools/Enemigos/Spider")]
    private static void Spider() { Def d = P("Enemy_Spider", "Spider", "Spider_Idle", "Spider_Run", "Spider_Attack", Tipo.Cono); d.chaseSpeed = 3.5f; Crear(d); }

    [MenuItem("Tools/Enemigos/Troll")]
    private static void Troll() { Def d = P("Enemy_Troll", "Troll", "Troll_Idle", "Troll_Walk", "Troll_Attack", Tipo.Cono); d.danio = 3; d.attackRange = 1.4f; d.chaseSpeed = 2.5f; Crear(d); }

    // --- Melee de caja ---
    [MenuItem("Tools/Enemigos/Skull")]
    private static void Skull() { Def d = P("Enemy_Skull_Nuevo", "Skull", "Skull_Idle", "Skull_Run", "Skull_Attack", Tipo.Caja); d.death = "Skull_dead"; d.attackRange = 1f; Crear(d); }

    // --- A distancia ---
    [MenuItem("Tools/Enemigos/Archer")]
    private static void Archer()
    {
        Def d = new Def { nombre = "Enemy_Archer", controller = "Archer_Blue", sprite = $"{Blue}/Archer/Archer_Idle.png",
            idle = "Archer_Idle_Blue", run = "Archer_Run_Blue", attack = "Archer_Shoot_Blue", tipo = Tipo.Distancia, attackRange = 5f };
        Crear(d);
    }

    [MenuItem("Tools/Enemigos/Gnoll (hueso)")]
    private static void Gnoll() { Def d = P("Enemy_Gnoll", "Gnoll", "Gnoll_Idle", "Gnoll_Run", "Gnoll_Throw", Tipo.Distancia); d.attackRange = 5f; d.proyectil = "Assets/Prefabs/Projectiles/Hueso.prefab"; Crear(d); }

    [MenuItem("Tools/Enemigos/Shaman (magia)")]
    private static void Shaman() { Def d = P("Enemy_Shaman", "Shaman", "Shaman_Idle", "Shaman_Run", "Shaman_Attack", Tipo.Distancia); d.attackRange = 5f; d.proyectil = "Assets/Prefabs/Projectiles/Proyectil_Shaman.prefab"; Crear(d); }

    private static Def P(string nombre, string carpeta, string idle, string run, string attack, Tipo tipo)
    {
        return new Def { nombre = nombre, controller = carpeta + "_Controller",
            sprite = $"{Pack}/{carpeta}/{idle}.png", idle = idle, run = run, attack = attack, tipo = tipo };
    }

    private static void Crear(Def d)
    {
        GameObject go = new GameObject(d.nombre);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + d.nombre);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CargarSprite(d.sprite);
        sr.sortingLayerName = "Characters";

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.6f, 0.7f);

        Animator anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = BuscarController(d.controller);

        go.AddComponent<Health>();
        go.AddComponent<DamageFeedback>();
        go.AddComponent<YSort>();
        ConfigurarIA(go, d);
        ConfigHealth(go.GetComponent<Health>());
        AgregarBarra(go);

        Emparentar(go);
        Colocar(go);
        Selection.activeObject = go;
        Debug.Log(d.nombre + " creado.");
    }

    private static void ConfigurarIA(GameObject go, Def d)
    {
        Component ia = d.tipo switch
        {
            Tipo.Caja => go.AddComponent<SkullAI>(),
            Tipo.Distancia => go.AddComponent<EnemyADistancia>(),
            _ => go.AddComponent<EnemyMeleeCono>(),
        };
        SerializedObject so = new SerializedObject(ia);
        so.FindProperty("idleAnim").stringValue = d.idle;
        so.FindProperty("runAnim").stringValue = d.run;
        so.FindProperty("attackAnim").stringValue = d.attack;
        so.FindProperty("deathAnim").stringValue = d.death;
        so.FindProperty("attackRange").floatValue = d.attackRange;
        so.FindProperty("chaseSpeed").floatValue = d.chaseSpeed;

        if (d.tipo == Tipo.Distancia)
        {
            so.FindProperty("detectionRange").floatValue = 8f;
            so.FindProperty("giveUpRange").floatValue = 12f;
            so.FindProperty("velocidadProyectil").floatValue = 9f;
            so.FindProperty("liberarEn").floatValue = 0.5f;
            so.FindProperty("danioProyectil").intValue = 2;
            EnemyProjectile p = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(d.proyectil);
            if (p != null) so.FindProperty("projectilePrefab").objectReferenceValue = p;
            else Debug.LogWarning($"{d.nombre}: no encontre el proyectil en {d.proyectil}. Crealo y reasignalo.");
        }
        else
        {
            so.FindProperty("danioAtaque").intValue = d.danio;
            so.FindProperty("cooldownAtaque").floatValue = 1f;
            so.FindProperty("golpeEn").floatValue = 0.5f;
            so.FindProperty("attackFrontDot").floatValue = 0.3f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigHealth(Health health)
    {
        SerializedObject so = new SerializedObject(health);
        so.FindProperty("maxHealth").intValue = 3;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AgregarBarra(GameObject go)
    {
        GameObject skull = AssetDatabase.LoadAssetAtPath<GameObject>(SkullPrefab);
        Transform fuente = skull != null ? skull.transform.Find("HealthBar") : null;
        if (fuente == null) { Debug.LogWarning("No encontre HealthBar en Enemy_Skull; queda sin barra."); return; }
        GameObject hb = Object.Instantiate(fuente.gameObject, go.transform);
        hb.name = "HealthBar";
        hb.transform.localPosition = fuente.localPosition;
    }

    private static void Emparentar(GameObject go)
    {
        GameObject padre = GameObject.Find("_Enemies");
        if (padre != null) go.transform.SetParent(padre.transform, true);
    }

    private static void Colocar(GameObject go)
    {
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 p = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(p.x, p.y, 0f);
        }
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
