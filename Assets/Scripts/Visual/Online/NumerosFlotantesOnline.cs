// ============================================================
//  NumerosFlotantesOnline.cs  —  Capa: VISUAL (UI del juego online · B2.3)
//  Spawnea un número flotante (-N daño / +N curación) sobre el
//  PANEL de un jugador. Reusa el prefab 'NumeroFlotante' de HotSeat,
//  pero trabaja con RectTransform de UI (no avatares en el mundo).
//
//  Lo llama VistaOnline cuando detecta que cambió el HP de un slot.
//  El 'slot' es el panel en pantalla (ya rotado): el número aparece
//  donde el jugador se ve en ESTA pantalla.
// ============================================================

using UnityEngine;

public class NumerosFlotantesOnline : MonoBehaviour
{
    [Header("Prefab (reusa el NumeroFlotante de HotSeat)")]
    public GameObject prefabNumeroFlotante;

    [Header("Canvas (opcional: si se deja vacío, se busca solo)")]
    [Tooltip("El número se cuelga del Canvas (al frente de todo), posicionado sobre el ancla. " +
             "Si está vacío, se toma el Canvas del propio ancla.")]
    public Canvas canvas;

    [Header("Anclas: una por SLOT (mismo orden que VistaOnline.paneles)")]
    [Tooltip("Dónde aparece el número de cada slot. Puede ser el panel, o mejor un " +
             "RectTransform vacío puesto arriba del avatar (sin Layout Group encima).")]
    public RectTransform[] anclas;

    [Header("Colores")]
    public Color colorDanio = new Color(1f, 0.3f, 0.3f);
    public Color colorCuracion = new Color(0.3f, 1f, 0.3f);

    // delta < 0 = daño (perdió HP) · delta > 0 = curación (ganó HP) · 0 = no hace nada.
    public void Spawn(int slot, int delta)
    {
        if (prefabNumeroFlotante == null || anclas == null) return;
        if (slot < 0 || slot >= anclas.Length || anclas[slot] == null) return;
        if (delta == 0) return;

        // Padre = Canvas raíz (así NINGÚN panel lo tapa ni lo recorta), pero posicionado SOBRE el ancla.
        Transform padre = anclas[slot];
        Canvas c = (canvas != null) ? canvas : anclas[slot].GetComponentInParent<Canvas>();
        if (c != null) padre = c.transform;

        GameObject obj = Instantiate(prefabNumeroFlotante, padre);
        obj.transform.position = anclas[slot].position;   // lo pongo justo donde está el ancla (en el mundo)
        obj.transform.SetAsLastSibling();                 // al frente de todo lo del canvas
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.one;

        NumeroFlotante nf = obj.GetComponent<NumeroFlotante>();
        if (nf == null) { Destroy(obj); return; }

        bool esDanio = delta < 0;
        string mensaje = esDanio ? delta.ToString() : "+" + delta;   // daño ya trae el "-"
        Color color = esDanio ? colorDanio : colorCuracion;
        nf.Iniciar(mensaje, color, esDanio);
    }
}
