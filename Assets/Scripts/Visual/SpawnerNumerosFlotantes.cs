// ============================================================
//  SpawnerNumerosFlotantes.cs  —  Capa: VISUAL (presentación / Unity)
//  Instancia números flotantes sobre los avatares cuando
//  un jugador recibe daño o se cura.
//  Funciona con SpriteRenderer (objetos en el mundo).
// ============================================================

using UnityEngine;
using TMPro;

public class SpawnerNumerosFlotantes : MonoBehaviour
{
    [Header("Prefab del número flotante")]
    public GameObject prefabNumeroFlotante;

    [Header("Canvas donde instanciar los números")]
    public Canvas canvas;

    [Header("Transforms de los avatares (en el mundo)")]
    public Transform[] avatares;

    [Header("Colores")]
    public Color colorDanio = new Color(1f, 0.3f, 0.3f);
    public Color colorCuracion = new Color(0.3f, 1f, 0.3f);

    private Camera cam;

    private void Start()
    {
        cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    public void Spawn(int indiceJugador, int cantidad)
    {
        if (prefabNumeroFlotante == null) return;
        if (indiceJugador < 0 || indiceJugador >= avatares.Length) return;
        if (avatares[indiceJugador] == null) return;

        // Convertir posición del mundo a posición local del Canvas
        Vector3 posicionMundo = avatares[indiceJugador].position;
        Vector2 posicionPantalla = cam.WorldToScreenPoint(posicionMundo);

        // Instanciar como hijo del Canvas
        GameObject obj = Instantiate(prefabNumeroFlotante, canvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        if (rt != null && canvasRT != null)
        {
            Vector2 posicionLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, posicionPantalla, cam, out posicionLocal);
            rt.anchoredPosition = posicionLocal;
        }

        NumeroFlotante nf = obj.GetComponent<NumeroFlotante>();
        if (nf == null) { Destroy(obj); return; }

        bool esDanio = cantidad > 0;
        string mensaje = esDanio ? "-" + cantidad : "+" + Mathf.Abs(cantidad);
        Color color = esDanio ? colorDanio : colorCuracion;

        nf.Iniciar(mensaje, color, esDanio);
    }
}