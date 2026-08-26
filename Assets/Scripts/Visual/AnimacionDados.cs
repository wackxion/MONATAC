// ============================================================
//  AnimacionDados.cs  —  Capa: VISUAL (presentación / Unity)
//  Animación estilo casino para los dados. Cuando el jugador
//  lanza los dados, los números giran rápido y se detienen
//  uno por uno con un pequeño delay, como una tragamonedas.
//
//  CÓMO USARLO:
//    1) Agregar este objeto a la escena (o al GameManager).
//    2) Asignar los 3 TextMeshPro de los dados.
//    3) Llamar IniciarAnimacion() pasando los valores finales.
// ============================================================

using UnityEngine;
using TMPro;
using System.Collections;

public class AnimacionDados : MonoBehaviour
{
    // --- Configuración desde el Inspector ---

    [Header("Textos de los dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    [Header("Configuración de la animación")]
    [Tooltip("Cuánto tiempo giran los números antes de detenerse")]
    public float duracionGiro = 1.2f;

    [Tooltip("Delay entre cada dado al detenerse")]
    public float delayEntreDados = 0.3f;

    [Tooltip("Cada cuántos segundos cambia el número mientras gira")]
    public float intervaloCambio = 0.05f;

    // --- Variables internas ---
    private TextMeshProUGUI[] textosDados;
    private bool animacionEnCurso = false;
    private System.Action callbackAlTerminar;

    private void Start()
    {
        // Armamos el array con los 3 dados para recorrerlos fácil
        textosDados = new TextMeshProUGUI[] { dado1Texto, dado2Texto, dado3Texto };
    }

    // Llamá este método desde el GameManager para arrancar la animación.
    //valoresFinales: array con los 3 resultados finales (ej: [3, 4, 2])
    // callback: función que se ejecuta cuando termina la animación
    public void IniciarAnimacion(int[] valoresFinales, System.Action callback)
    {
        if (animacionEnCurso) return;  // no arrancar dos veces

        callbackAlTerminar = callback;
        StartCoroutine(AnimacionCoroutine(valoresFinales));
    }

    // Corutina principal de la animación
    private IEnumerator AnimacionCoroutine(int[] valoresFinales)
    {
        animacionEnCurso = true;
        int cantidadDados = Mathf.Min(valoresFinales.Length, textosDados.Length);

        // Fase 1: Todos los dados giran al mismo tiempo
        float tiempoInicio = Time.time;
        bool[] detenido = new bool[cantidadDados];

        while (Time.time - tiempoInicio < duracionGiro)
        {
            for (int i = 0; i < cantidadDados; i++)
            {
                if (!detenido[i] && textosDados[i] != null)
                {
                    // Mostramos un número random mientras gira
                    textosDados[i].text = Random.Range(1, 5).ToString();
                }
            }
            yield return new WaitForSeconds(intervaloCambio);
        }

        // Fase 2: Los dados se detienen uno por uno
        for (int i = 0; i < cantidadDados; i++)
        {
            if (textosDados[i] != null)
            {
                textosDados[i].text = valoresFinales[i].ToString();
            }
            yield return new WaitForSeconds(delayEntreDados);
        }

        animacionEnCurso = false;

        // Avisamos al GameManager que la animación terminó
        callbackAlTerminar?.Invoke();
    }

    // ¿Está girando ahora mismo?
    public bool EstaAnimando()
    {
        return animacionEnCurso;
    }
}
