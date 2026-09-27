// ============================================================
//  PanelSonido.cs  —  Capa: VISUAL (Unity)
//  Panel de opciones de sonido: dos sliders (música y efectos)
//  que regulan el volumen a través del GestorAudio. Es solo UI.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class PanelSonido : MonoBehaviour
{
    public GameObject panel;        // el panel que se muestra/oculta (arranca oculto)
    public Slider sliderMusica;     // slider de música (rango 0 a 1)
    public Slider sliderSFX;        // slider de efectos (rango 0 a 1)

    void Start()
    {
        if (GestorAudio.Instance != null)
        {
            // Pone cada slider en el volumen guardado, SIN disparar el evento (para no re-guardar).
            if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(GestorAudio.Instance.volumenMusica);
            if (sliderSFX != null)    sliderSFX.SetValueWithoutNotify(GestorAudio.Instance.volumenSFX);
        }
        // Cuando el jugador mueve un slider, avisamos al GestorAudio.
        if (sliderMusica != null) sliderMusica.onValueChanged.AddListener(CambiarMusica);
        if (sliderSFX != null)    sliderSFX.onValueChanged.AddListener(CambiarSFX);
    }

    // El botón de sonido llama a Abrir(); la X del panel llama a Cerrar().
    public void Abrir()  { if (panel != null) panel.SetActive(true); }
    public void Cerrar() { if (panel != null) panel.SetActive(false); }

    private void CambiarMusica(float v) { if (GestorAudio.Instance != null) GestorAudio.Instance.SetVolumenMusica(v); }
    private void CambiarSFX(float v)    { if (GestorAudio.Instance != null) GestorAudio.Instance.SetVolumenSFX(v); }
}
