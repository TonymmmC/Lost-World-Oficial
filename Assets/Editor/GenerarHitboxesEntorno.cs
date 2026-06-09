using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Agrega colliders 2D a los objetos del entorno (arboles, rocas, props) de forma masiva.
// En top-down el collider va en la BASE del sprite (el tronco), no en todo el sprite: asi el
// jugador puede caminar por detras de la copa. Tamano y posicion del collider son configurables
// como fraccion del sprite. Trabaja sobre la seleccion o sobre padres por nombre (_Map, _Environment).
public class GenerarHitboxesEntorno : EditorWindow
{
    private enum Forma { Capsula, Caja, PoligonoAuto }
    private enum Ambito { Seleccion, PorNombrePadre }

    private Ambito ambito = Ambito.PorNombrePadre;
    private string nombresPadres = "_Map,_Environment";
    private string filtroNombre = "";
    private Forma forma = Forma.Capsula;

    private float anchoFraccion = 0.45f;
    private float altoFraccion = 0.25f;
    private float desplazamientoY = 0f;
    private bool anclarEnBase = true;
    private bool reemplazarExistentes = false;
    private bool incluirTilemaps = false;

    [MenuItem("Tools/Entorno/Generar Hitboxes")]
    private static void Abrir()
    {
        GetWindow<GenerarHitboxesEntorno>("Hitboxes Entorno").minSize = new Vector2(340f, 340f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Ambito", EditorStyles.boldLabel);
        ambito = (Ambito)EditorGUILayout.EnumPopup("Aplicar a", ambito);
        if (ambito == Ambito.PorNombrePadre)
            nombresPadres = EditorGUILayout.TextField("Padres (coma)", nombresPadres);
        filtroNombre = EditorGUILayout.TextField("Filtro nombre (coma)", filtroNombre);
        if (filtroNombre.Trim().Length == 0)
            EditorGUILayout.LabelField(" ", "Vacio = todos. Ej: Rock,Bush", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
        forma = (Forma)EditorGUILayout.EnumPopup("Forma", forma);
        if (forma == Forma.PoligonoAuto)
        {
            EditorGUILayout.HelpBox("Poligono auto: traza el contorno real del sprite (ignora lo " +
                "transparente). Ideal para arbustos y piedras chicas. Las fracciones no aplican.", MessageType.None);
        }
        else
        {
            anchoFraccion = EditorGUILayout.Slider("Ancho (fraccion)", anchoFraccion, 0.05f, 1f);
            altoFraccion = EditorGUILayout.Slider("Alto (fraccion)", altoFraccion, 0.05f, 1f);
            anclarEnBase = EditorGUILayout.Toggle("Anclar en base", anclarEnBase);
            desplazamientoY = EditorGUILayout.FloatField("Desplazamiento Y", desplazamientoY);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Opciones", EditorStyles.boldLabel);
        reemplazarExistentes = EditorGUILayout.Toggle("Reemplazar existentes", reemplazarExistentes);
        incluirTilemaps = EditorGUILayout.Toggle("Incluir tilemaps", incluirTilemaps);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "El collider se coloca en la base del sprite (tronco). Ajusta las fracciones y usa " +
            "Gizmos en la escena para verificar. Salta objetos que ya tienen Collider2D salvo que " +
            "marques Reemplazar.", MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generar hitboxes", GUILayout.Height(32f)))
            Generar();
    }

    private void Generar()
    {
        List<SpriteRenderer> objetivos = RecolectarObjetivos();
        if (objetivos.Count == 0) { Debug.LogWarning("Hitboxes: no encontre objetos con SpriteRenderer."); return; }

        int creados = 0, saltados = 0;
        foreach (SpriteRenderer sr in objetivos)
        {
            if (sr.sprite == null) { saltados++; continue; }

            Collider2D existente = sr.GetComponent<Collider2D>();
            if (existente != null)
            {
                if (!reemplazarExistentes) { saltados++; continue; }
                Undo.DestroyObjectImmediate(existente);
            }

            AgregarCollider(sr);
            creados++;
        }

        Debug.Log($"Hitboxes: {creados} colliders creados, {saltados} saltados.");
    }

    private void AgregarCollider(SpriteRenderer sr)
    {
        if (forma == Forma.PoligonoAuto)
        {
            // PolygonCollider2D recien agregado a un SpriteRenderer se auto-genera desde el
            // physics shape del sprite, trazando solo los pixeles opacos.
            Undo.AddComponent<PolygonCollider2D>(sr.gameObject);
            return;
        }

        Bounds b = sr.sprite.bounds; // espacio local, ya considera pivot y pixels per unit
        Vector2 tam = new Vector2(b.size.x * anchoFraccion, b.size.y * altoFraccion);
        float centroY = anclarEnBase ? b.min.y + tam.y * 0.5f : b.center.y;
        Vector2 centro = new Vector2(b.center.x, centroY + desplazamientoY);

        if (forma == Forma.Capsula)
        {
            CapsuleCollider2D col = Undo.AddComponent<CapsuleCollider2D>(sr.gameObject);
            col.size = tam;
            col.offset = centro;
            col.direction = CapsuleDirection2D.Horizontal;
        }
        else
        {
            BoxCollider2D col = Undo.AddComponent<BoxCollider2D>(sr.gameObject);
            col.size = tam;
            col.offset = centro;
        }
    }

    private List<SpriteRenderer> RecolectarObjetivos()
    {
        List<SpriteRenderer> lista = new List<SpriteRenderer>();
        if (ambito == Ambito.Seleccion)
        {
            foreach (GameObject go in Selection.gameObjects)
                RecolectarDe(go.transform, lista);
            return lista;
        }

        foreach (string nombre in nombresPadres.Split(','))
        {
            string limpio = nombre.Trim();
            if (limpio.Length == 0) continue;
            GameObject padre = GameObject.Find(limpio);
            if (padre == null) { Debug.LogWarning($"Hitboxes: no encontre el padre '{limpio}'."); continue; }
            RecolectarDe(padre.transform, lista);
        }
        return lista;
    }

    private void RecolectarDe(Transform raiz, List<SpriteRenderer> lista)
    {
        foreach (SpriteRenderer sr in raiz.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!incluirTilemaps && sr.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>() != null) continue;
            if (sr.GetComponentInParent<UnityEngine.Tilemaps.Tilemap>() != null && !incluirTilemaps) continue;
            if (!lista.Contains(sr)) lista.Add(sr);
        }
    }
}
