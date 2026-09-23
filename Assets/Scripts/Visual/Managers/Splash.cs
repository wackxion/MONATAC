// ============================================================
//  Splash.cs  —  Capa: VISUAL (Unity)
//  Pantalla de presentación: el video se reproduce EN BUCLE.
//  Cuando termina la PRIMERA vuelta, aparece el botón "Jugar",
//  que es el único que lleva al menú (el video sigue en loop).
//  Es puro flujo de escenas: no toca reglas del juego.
// ============================================================

using UnityEngine;
using UnityEngine.Video;             // VideoPlayer
using UnityEngine.SceneManagement;   // cambiar de escena

public class Splash : MonoBehaviour
{
    public VideoPlayer video;                 // el reproductor del video (se asigna en el Inspector)
    public GameObject botonJugar;             // el botón "Jugar" (arranca OCULTO)
    public string escenaSiguiente = "MENU";   // a qué escena ir al tocar Jugar

    void Start()
    {
        // El video se repite solo (por las dudas lo forzamos por código también).
        if (video != null)
        {
            video.isLooping = true;
            // loopPointReached se dispara cada vez que el video llega al final.
            // Como está en loop, lo usamos para saber que terminó la PRIMERA vuelta.
            video.loopPointReached += AlTerminarVuelta;
        }

        // El botón arranca oculto: recién aparece cuando termina la primera reproducción.
        if (botonJugar != null) botonJugar.SetActive(false);
    }

    // Se llama cuando el video llega al final (fin de una vuelta del loop).
    private void AlTerminarVuelta(VideoPlayer vp)
    {
        // Mostramos el botón Jugar. Ya no necesitamos escuchar más el evento.
        if (botonJugar != null) botonJugar.SetActive(true);
        video.loopPointReached -= AlTerminarVuelta;   // solo la primera vez
    }

    // Botón "Jugar" → conectar a este método. Lleva al menú.
    public void Jugar()
    {
        SceneManager.LoadScene(escenaSiguiente);
    }
}
