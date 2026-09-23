// ============================================================
//  Splash.cs  —  Capa: VISUAL (Unity)
//  Pantalla de presentación: reproduce un video corto y, al
//  terminar, carga el menú. Se puede saltar con clic o tecla.
//  Es puro flujo de escenas: no toca reglas del juego.
// ============================================================

using UnityEngine;
using UnityEngine.Video;             // VideoPlayer
using UnityEngine.SceneManagement;   // cambiar de escena
using UnityEngine.InputSystem;       // Input System nuevo (para saltar)

public class Splash : MonoBehaviour
{
    public VideoPlayer video;                 // el reproductor del video (se asigna en el Inspector)
    public string escenaSiguiente = "MENU";   // a qué escena ir cuando termina
    public bool sePuedeSaltar = true;         // permitir saltar con clic o tecla

    private bool yaCargo = false;             // evita cargar la escena dos veces

    void Start()
    {
        // loopPointReached se dispara cuando el video LLEGA AL FINAL.
        if (video != null) video.loopPointReached += AlTerminar;
        else Continuar();   // si no hay video asignado, no nos trabamos: vamos directo al menú
    }

    void Update()
    {
        // Saltar la intro con cualquier tecla o clic.
        if (sePuedeSaltar && SeApretoAlgo()) Continuar();
    }

    private bool SeApretoAlgo()
    {
        // Guards por si no hay teclado/mouse conectado (evita null).
        bool tecla = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool clic  = Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame;
        return tecla || clic;
    }

    // Se llama solo cuando el video termina.
    private void AlTerminar(VideoPlayer vp) { Continuar(); }

    private void Continuar()
    {
        if (yaCargo) return;   // si ya saltamos (o ya terminó), no cargamos de nuevo
        yaCargo = true;
        SceneManager.LoadScene(escenaSiguiente);
    }
}
