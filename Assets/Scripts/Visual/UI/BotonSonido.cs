// ============================================================
//  BotonSonido.cs  —  Capa: VISUAL (Unity)
// ------------------------------------------------------------
//  QUÉ ES:
//    Un botón de MUTE: silencia o reactiva TODO el sonido de un toque.
//    Además cambia su ícono (parlante <-> parlante tachado) para que
//    se vea en qué estado está.
//
//  CÓMO ENCAJA:
//    Es solo UI. No maneja el audio: le pide al GestorAudio que alterne
//    el silencio (el gestor es quien tiene las AudioSource). Este script
//    solo dispara la acción y refresca el dibujo del ícono.
//
//  ES OPCIONAL:
//    El PanelSonido (con sliders) ya deja bajar el volumen a 0. Este botón
//    es un extra por si querés un mute rápido de un clic (por ejemplo,
//    dentro del panel o en una esquina de la pantalla).
//
//  CÓMO SE CABLEA:
//    - Va en el objeto del botón, junto al componente Button.
//    - On Click () del botón -> BotonSonido.AlternarSonido().
//    - Asignar 'icono' + los dos sprites en el Inspector (opcional).
// ============================================================

using UnityEngine;
using UnityEngine.UI;   // para usar Image (el dibujo del ícono)

public class BotonSonido : MonoBehaviour
{
    // El Image que muestra el ícono del botón (el dibujo que vamos a cambiar).
    public Image icono;

    // Ícono a mostrar cuando HAY sonido (parlante normal).
    public Sprite spriteConSonido;

    // Ícono a mostrar cuando está MUTEADO (parlante tachado).
    public Sprite spriteSilenciado;

    // Al activarse, muestra el ícono acorde a cómo quedó el sonido la última vez
    // (el GestorAudio recuerda el silencio entre sesiones con PlayerPrefs).
    void Start()
    {
        Actualizar();
    }

    // Se conecta al On Click () del botón.
    // Alterna el silencio en el GestorAudio y después refresca el ícono.
    public void AlternarSonido()
    {
        if (GestorAudio.Instance != null) GestorAudio.Instance.AlternarSonido();
        Actualizar();
    }

    // Pone el sprite correcto según si el sonido está silenciado o no.
    // Tiene guardas null: si no hay ícono asignado o no existe el gestor,
    // no hace nada (no rompe) — el mute igual funciona, solo no cambia el dibujo.
    private void Actualizar()
    {
        if (icono == null || GestorAudio.Instance == null) return;
        icono.sprite = GestorAudio.Instance.silenciado ? spriteSilenciado : spriteConSonido;
    }
}
