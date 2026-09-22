// ============================================================
//  MenuPausa.cs  —  Capa: VISUAL (Unity)
//  Maneja SOLO el menú de pausa: muestra/oculta el panel y
//  congela el juego. No toca las reglas (SRP): es un componente
//  aparte para no volver a llenar de UI al GameManager.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;   // para volver al menú
using UnityEngine.InputSystem;       // Input System nuevo (el proyecto usa este, no el viejo)

public class MenuPausa : MonoBehaviour
{
    // El panel que aparece al pausar. Arranca APAGADO (SetActive(false)) en la escena.
    public GameObject panelPausa;

    // Si es true, se puede pausar/reanudar con la tecla Esc (además de los botones).
    public bool permitirEsc = true;

    // Bandera interna: ¿estamos en pausa ahora mismo?
    private bool enPausa = false;

    // Se revisa cada frame si se apretó Esc para alternar pausa.
    void Update()
    {
        // Keyboard.current puede ser null (sin teclado); por eso el guard.
        if (permitirEsc && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (enPausa) Reanudar();
            else Pausar();
        }
    }

    // Congela el juego y muestra el panel.
    public void Pausar()
    {
        enPausa = true;
        Time.timeScale = 0f;   // 0 = tiempo detenido: animaciones y corrutinas con WaitForSeconds se frenan
        if (panelPausa != null) panelPausa.SetActive(true);
    }

    // Reanuda el juego y oculta el panel.
    public void Reanudar()
    {
        enPausa = false;
        Time.timeScale = 1f;   // 1 = velocidad normal
        if (panelPausa != null) panelPausa.SetActive(false);
    }

    // Vuelve al menú principal.
    public void VolverAlMenu()
    {
        // IMPORTANTE: Time.timeScale se conserva entre escenas. Si volvemos al menú
        // con el tiempo en 0, el menú quedaría congelado. Por eso lo reseteamos a 1.
        Time.timeScale = 1f;
        SceneManager.LoadScene("MENU");
    }
}
