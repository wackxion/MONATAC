// ============================================================
//  PanelSonido.cs  —  Capa: VISUAL (Unity)
// ------------------------------------------------------------
//  QUÉ ES:
//    El panel de "Opciones de sonido". Tiene dos sliders (uno para
//    la MÚSICA y otro para los EFECTOS) que dejan al jugador regular
//    el volumen de cada uno por separado.
//
//  CÓMO ENCAJA EN LA ARQUITECTURA:
//    Es SOLO interfaz (capa /Visual). No sabe nada de reglas del juego.
//    No maneja el audio directamente: le pasa el pedido al GestorAudio
//    (el Singleton que sí controla las AudioSource). Así respetamos la
//    separación de responsabilidades (SRP): este script solo conecta
//    los sliders del panel con el gestor de audio.
//
//  CÓMO SE CABLEA EN UNITY:
//    - Este componente va en un objeto SIEMPRE ACTIVO (ej. el Canvas).
//    - El botón de sonido llama a Abrir(); la X del panel llama a Cerrar().
//    - Los sliders NO se conectan a mano: este script les engancha el
//      evento solo, en Start().
// ============================================================

using UnityEngine;
using UnityEngine.UI;   // para usar Slider (control de barra deslizante)

public class PanelSonido : MonoBehaviour
{
    // El panel que se muestra/oculta. Arranca DESACTIVADO en la escena.
    public GameObject panel;

    // Slider que controla el volumen de la MÚSICA. Su rango debe ser 0 a 1
    // (0 = mudo, 1 = volumen máximo). Se configura en el Inspector del Slider.
    public Slider sliderMusica;

    // Slider que controla el volumen de los EFECTOS (SFX). También de 0 a 1.
    public Slider sliderSFX;

    // Start() lo llama Unity una vez, al activarse el objeto por primera vez.
    void Start()
    {
        // 1) Dejar cada slider en el volumen que estaba guardado.
        //    Usamos SetValueWithoutNotify para MOVER la barra SIN disparar el
        //    evento onValueChanged (si no, al posicionarla se volvería a
        //    "guardar" el valor y podría pisar cosas). Solo mueve la barra visualmente.
        if (GestorAudio.Instance != null)
        {
            if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(GestorAudio.Instance.volumenMusica);
            if (sliderSFX != null)    sliderSFX.SetValueWithoutNotify(GestorAudio.Instance.volumenSFX);
        }

        // 2) Suscribirse al evento de cada slider: cada vez que el jugador
        //    ARRASTRA la barra, Unity dispara onValueChanged(nuevoValor) y
        //    nosotros avisamos al GestorAudio para que aplique el volumen.
        //    (AddListener = "cuando pase esto, llamá a esta función".)
        if (sliderMusica != null) sliderMusica.onValueChanged.AddListener(CambiarMusica);
        if (sliderSFX != null)    sliderSFX.onValueChanged.AddListener(CambiarSFX);
    }

    // --- Mostrar / ocultar el panel ---
    // Se conectan al On Click () de los botones:
    //   Botón de sonido -> Abrir()   |   Botón X del panel -> Cerrar()

    // Muestra el panel de sonido.
    public void Abrir()  { if (panel != null) panel.SetActive(true); }

    // Oculta el panel de sonido.
    public void Cerrar() { if (panel != null) panel.SetActive(false); }

    // --- Reacción a los sliders (privadas: solo las llama el propio evento) ---

    // Se ejecuta al mover el slider de música. 'v' es el nuevo valor (0 a 1).
    private void CambiarMusica(float v)
    {
        if (GestorAudio.Instance != null) GestorAudio.Instance.SetVolumenMusica(v);
    }

    // Se ejecuta al mover el slider de efectos. 'v' es el nuevo valor (0 a 1).
    private void CambiarSFX(float v)
    {
        if (GestorAudio.Instance != null) GestorAudio.Instance.SetVolumenSFX(v);
    }
}
