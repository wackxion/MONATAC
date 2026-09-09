// ============================================================
//  AnimacionBarraHP.cs  —  Capa: VISUAL (presentación / Unity)
//  Anima la transición de la barra de HP cuando cambia el valor.
//  En vez de saltar instantáneamente, hace un fade suave.
//
//  CÓMO USARLO:
//    1) Agregar este componente al GameObject de la barra (la Image fill).
//    2) Asignar la Image (o se busca automáticamente en el mismo objeto).
//    3) Llamar SetHP() con el valor actual y el máximo.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimacionBarraHP : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Duración de la transición en segundos")]
    public float duracion = 0.5f;

    [Tooltip("Curva de easing (opcional)")]
    public AnimationCurve curva = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image imagenBarra;
    private float valorActual = 1f;
    private float valorObjetivo = 1f;
    private Coroutine coroutineActiva;

    private void Awake()
    {
        imagenBarra = GetComponent<Image>();
    }

    private void Start()
    {
        if (imagenBarra != null)
            valorActual = imagenBarra.fillAmount;
    }

    // Llamá este método para actualizar la barra con animación.
    // hpActual: HP nuevo del jugador
    // hpMaximo: HP máximo del jugador
    public void SetHP(int hpActual, int hpMaximo)
    {
        if (imagenBarra == null) return;

        float nuevoValor = (hpMaximo > 0) ? (float)hpActual / hpMaximo : 0f;
        nuevoValor = Mathf.Clamp01(nuevoValor);

        // Si el valor no cambió, no hacer nada
        if (Mathf.Approximately(valorObjetivo, nuevoValor)) return;

        valorObjetivo = nuevoValor;

        // Si ya hay una corutina corriendo, pararla
        if (coroutineActiva != null)
            StopCoroutine(coroutineActiva);

        // Arrancar nueva corutina desde el valor actual (para interpolar desde donde está)
        coroutineActiva = StartCoroutine(AnimarCoroutine());
    }

    // Versión para usar sin animación (ej: al iniciar la partida)
    public void SetHPInmediato(int hpActual, int hpMaximo)
    {
        if (imagenBarra == null) return;

        float nuevoValor = (hpMaximo > 0) ? (float)hpActual / hpMaximo : 0f;
        imagenBarra.fillAmount = Mathf.Clamp01(nuevoValor);
        valorActual = imagenBarra.fillAmount;
        valorObjetivo = imagenBarra.fillAmount;
    }

    private IEnumerator AnimarCoroutine()
    {
        float tiempoInicio = Time.time;

        while (Time.time - tiempoInicio < duracion)
        {
            float t = (Time.time - tiempoInicio) / duracion;
            float curvaT = curva.Evaluate(t);
            imagenBarra.fillAmount = Mathf.Lerp(valorActual, valorObjetivo, curvaT);
            yield return null;
        }

        // Asegurar que llegue al valor exacto
        imagenBarra.fillAmount = valorObjetivo;
        valorActual = valorObjetivo;
        coroutineActiva = null;
    }
}