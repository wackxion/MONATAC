// ============================================================
//  NumeroFlotante.cs  —  Capa: VISUAL (presentación / Unity)
//  Muestra un número que sube y se desvanece sobre un avatar.
//  Se usa para mostrar daño recibido o curación.
// ============================================================

using UnityEngine;
using TMPro;
using System.Collections;

public class NumeroFlotante : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidadSubida = 60f;
    public float duracion = 1.0f;

    private TextMeshProUGUI texto;
    private float tiempoInicio;
    private Color colorInicial;

    public void Iniciar(string mensaje, Color color, bool esDanio)
    {
        texto = GetComponent<TextMeshProUGUI>();
        if (texto == null) return;

        texto.text = mensaje;
        colorInicial = color;
        colorInicial.a = 1f;
        texto.color = colorInicial;
        tiempoInicio = Time.time;

        StartCoroutine(AnimacionCoroutine());
    }

    private IEnumerator AnimacionCoroutine()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 posicionInicial = rt.anchoredPosition;

        while (Time.time - tiempoInicio < duracion)
        {
            float t = (Time.time - tiempoInicio) / duracion;

            // Subir
            rt.anchoredPosition = posicionInicial + Vector2.up * (velocidadSubida * t);

            // Fade out
            Color c = colorInicial;
            c.a = Mathf.Lerp(1f, 0f, t);
            texto.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}