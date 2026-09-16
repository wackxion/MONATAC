// ============================================================
//  AnimacionCartaGrupal.cs  —  Capa: VISUAL (presentación / Unity)
//  Muestra la carta grupal comprada en el centro de la pantalla
//  con su reverso, mientras el fondo se oscurece. Después de una
//  espera, ejecuta el efecto de la carta.
//
//  CÓMO USARLO:
//    1) Agregar este objeto a la escena (o al Canvas).
//    2) Asignar las Images: cartaReverso (centro) y panelOscuro (fondo).
//    3) Llamar IniciarAnimacion() pasando el sprite del reverso y un callback.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimacionCartaGrupal : MonoBehaviour
{
    // --- Configuración desde el Inspector ---

    [Header("Elementos UI")]
    [Tooltip("Image en el centro de la pantalla para mostrar el reverso de la carta")]
    public Image cartaReverso;

    [Tooltip("Image que cubre toda la pantalla con negro semitransparente")]
    public Image panelOscuro;

    [Header("Configuración de la animación")]
    [Tooltip("Tiempo que se muestra la carta antes de desaparecer")]
    public float duracionEspera = 1.0f;

    [Tooltip("Duración del fade in/out del reverso")]
    public float duracionFadeReverso = 0.3f;

    [Tooltip("Duración del fade in/out del panel oscuro")]
    public float duracionFadePanel = 0.3f;

    [Tooltip("Opacidad máxima del panel oscuro")]
    public float opacidadPanel = 0.7f;

    // --- Variables internas ---
    private bool animacionEnCurso = false;
    private System.Action callbackAlTerminar;

    private void Start()
    {
        // Asegurar que al inicio estén desactivados
        if (cartaReverso != null) cartaReverso.gameObject.SetActive(false);
        if (panelOscuro != null) panelOscuro.gameObject.SetActive(false);
    }

    // Llamá este método desde el GameManager para arrancar la animación.
    // reversoSprite: sprite del reverso de la carta grupal
    // callback: función que se ejecuta cuando termina la animación (aplica el efecto)
    public void IniciarAnimacion(Sprite reversoSprite, System.Action callback)
    {
        if (animacionEnCurso) return;
        if (reversoSprite == null)
        {
            callback?.Invoke();
            return;
        }

        callbackAlTerminar = callback;
        StartCoroutine(AnimacionCoroutine(reversoSprite));
    }

    private IEnumerator AnimacionCoroutine(Sprite reversoSprite)
    {
        animacionEnCurso = true;

        // Preparar elementos
        Color colorReverso = Color.white;
        colorReverso.a = 0f;
        cartaReverso.color = colorReverso;
        cartaReverso.sprite = reversoSprite;
        cartaReverso.gameObject.SetActive(true);

        Color colorPanel = panelOscuro.color;
        colorPanel.a = 0f;
        panelOscuro.color = colorPanel;
        panelOscuro.gameObject.SetActive(true);

        // --- FADE IN: panel oscuro + reverso ---
        float tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionFadePanel)
        {
            float t = (Time.time - tiempoInicio) / duracionFadePanel;
            colorPanel.a = Mathf.Lerp(0f, opacidadPanel, t);
            panelOscuro.color = colorPanel;
            yield return null;
        }
        colorPanel.a = opacidadPanel;
        panelOscuro.color = colorPanel;

        tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionFadeReverso)
        {
            float t = (Time.time - tiempoInicio) / duracionFadeReverso;
            colorReverso.a = Mathf.Lerp(0f, 1f, t);
            cartaReverso.color = colorReverso;
            yield return null;
        }
        colorReverso.a = 1f;
        cartaReverso.color = colorReverso;

        // --- ESPERA ---
        yield return new WaitForSeconds(duracionEspera);

        // --- FADE OUT: reverso + panel oscuro ---
        tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionFadeReverso)
        {
            float t = (Time.time - tiempoInicio) / duracionFadeReverso;
            colorReverso.a = Mathf.Lerp(1f, 0f, t);
            cartaReverso.color = colorReverso;
            yield return null;
        }
        colorReverso.a = 0f;
        cartaReverso.color = colorReverso;
        cartaReverso.gameObject.SetActive(false);

        tiempoInicio = Time.time;
        while (Time.time - tiempoInicio < duracionFadePanel)
        {
            float t = (Time.time - tiempoInicio) / duracionFadePanel;
            colorPanel.a = Mathf.Lerp(opacidadPanel, 0f, t);
            panelOscuro.color = colorPanel;
            yield return null;
        }
        colorPanel.a = 0f;
        panelOscuro.color = colorPanel;
        panelOscuro.gameObject.SetActive(false);

        animacionEnCurso = false;

        // Avisamos al GameManager que la animación terminó
        callbackAlTerminar?.Invoke();
    }

    // ¿Está mostrando la animación ahora mismo?
    public bool EstaAnimando()
    {
        return animacionEnCurso;
    }
}