// ============================================================
//  BotonSonido.cs  —  Capa: VISUAL (Unity)
//  Botón que silencia / activa el sonido. Le habla al GestorAudio
//  (que es quien maneja el mute) y cambia su ícono según el estado.
//  Es solo UI: no toca reglas.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class BotonSonido : MonoBehaviour
{
    public Image icono;               // el Image del botón (el dibujo que cambia)
    public Sprite spriteConSonido;    // ícono cuando HAY sonido (parlante)
    public Sprite spriteSilenciado;   // ícono cuando está MUTEADO (parlante tachado)

    void Start()
    {
        // Al arrancar, muestra el ícono según cómo quedó el sonido la última vez.
        Actualizar();
    }

    // Conectar este método al On Click () del botón.
    public void AlternarSonido()
    {
        if (GestorAudio.Instance != null) GestorAudio.Instance.AlternarSonido();
        Actualizar();
    }

    // Pone el ícono correcto según si está silenciado o no.
    private void Actualizar()
    {
        if (icono == null || GestorAudio.Instance == null) return;
        icono.sprite = GestorAudio.Instance.silenciado ? spriteSilenciado : spriteConSonido;
    }
}
