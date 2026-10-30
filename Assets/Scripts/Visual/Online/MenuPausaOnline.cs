// ============================================================
//  MenuPausaOnline.cs  —  Capa: VISUAL (UI del juego online · B2.4)
//  Menú de pausa para la partida ONLINE: muestra/oculta un panel
//  con "Reanudar" y "Volver al menú". A diferencia de HotSeat, NO
//  toca Time.timeScale: en red el server y los RPCs siguen corriendo,
//  y como el juego es por turnos, no pasa nada si el panel queda abierto.
//
//  "Volver al menú" delega en EstadoOnline.VolverAlMenu() (corta la red):
//    · si lo hace el host, se cierra para todos;
//    · si lo hace un cliente, se desconecta y el server lo da por eliminado.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;   // Input System nuevo (el proyecto usa este)

public class MenuPausaOnline : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject panelPausa;   // panel que aparece al pausar (arranca APAGADO en la escena)
    public EstadoOnline estado;     // para "Volver al menú" (corta la red y carga el menú)

    [Header("Opciones")]
    public bool permitirEsc = true; // alternar el panel con la tecla Esc

    private bool enPausa = false;

    void Update()
    {
        // Keyboard.current puede ser null (sin teclado); por eso el guard.
        if (permitirEsc && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (enPausa) Reanudar();
            else Pausar();
        }
    }

    // Muestra el panel (NO congela el tiempo: en online no corresponde).
    public void Pausar()
    {
        enPausa = true;
        if (panelPausa != null) panelPausa.SetActive(true);
    }

    // Oculta el panel y sigue jugando.
    public void Reanudar()
    {
        enPausa = false;
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    // Vuelve al menú cortando la red (lo resuelve EstadoOnline, igual que el botón del panel de fin).
    public void VolverAlMenu()
    {
        if (estado != null) estado.VolverAlMenu();
    }
}
