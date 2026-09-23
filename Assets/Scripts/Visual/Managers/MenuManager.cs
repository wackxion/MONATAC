// ============================================================
//  MenuManager.cs  —  Capa: VISUAL (Unity)
//  Controla la escena del Menú: guarda cuántos jugadores se
//  eligieron y carga la escena del juego.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;   // para cambiar de escena
using TMPro;

public class MenuManager : MonoBehaviour
{
    // (Opcional) un texto para el aviso "Próximamente" del botón online.
    public TextMeshProUGUI textoAviso;

    // Un método por botón, para conectarlos fácil (sin parámetros).
    public void Jugar2() { Jugar(2); }
    public void Jugar3() { Jugar(3); }
    public void Jugar4() { Jugar(4); }

    // Guarda la elección y carga la escena de carga.
    private void Jugar(int cantidad)
    {
        Config.cantidadJugadores = cantidad;   // se recuerda entre escenas
        SceneManager.LoadScene("Loading");     // pantalla de carga → luego carga "juego"
    }

    // El botón de multijugador online, por ahora, solo avisa.
    public void Online()
    {
        Debug.Log("Multijugador online: próximamente.");
        if (textoAviso != null) textoAviso.text = "Multijugador online: próximamente.";
    }

    //hecho por pilar
    // Botón de personalización que lleva a la escena correspondiente.
    public void Personalizacion()
    {
        SceneManager.LoadScene("Personalizacion");
    }

    // Botón "Salir": cierra el juego.
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        // Dentro del Editor, Application.Quit() no hace nada; frenamos el Play manualmente.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // En el build (.exe) sí cierra la aplicación.
        Application.Quit();
#endif
    }
}
