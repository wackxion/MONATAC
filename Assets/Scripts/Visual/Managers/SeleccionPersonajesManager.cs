// ============================================================
//  SeleccionPersonajesManager.cs  —  Capa: VISUAL (Unity)
//  Controla la pantalla de selección de personajes: cada jugador
//  elige su personaje y nombre antes de empezar la partida.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SeleccionPersonajesManager : MonoBehaviour
{
    // Los 4 slots de la pantalla: imagen del personaje, campo del nombre y texto del nombre del personaje.
    [Header("Slots de jugadores (4)")]
    public Image[] imagenesPersonaje;
    public TMP_InputField[] inputsNombre;
    public TextMeshProUGUI[] textosIndice;

    // Los personajes disponibles para elegir (sprite + nombre), en el mismo orden.
    [Header("Personajes disponibles")]
    public Sprite[] spritesPersonajes;
    public string[] nombresPersonajes;

    [Header("Botón jugar")]
    public GameObject botonJugar;

    // Qué personaje tiene elegido cada jugador ahora mismo (índice dentro de spritesPersonajes).
    private int[] seleccionActual = new int[4];

    private void Start()
    {
        // Para cada slot: le da un personaje inicial distinto y carga el nombre guardado en Config.
        for (int i = 0; i < 4; i++)
        {
            seleccionActual[i] = i % spritesPersonajes.Length;   // reparte los personajes iniciales (% = circular)

            if (i < inputsNombre.Length && inputsNombre[i] != null)
                inputsNombre[i].text = Config.nombresJugadores[i];   // muestra el nombre guardado

            ActualizarSlot(i);   // dibuja el sprite y el nombre del personaje
        }
    }

    // Cambia el personaje de un jugador rotando +1 (derecha) o -1 (izquierda).
    public void CambiarPersonaje(int indiceJugador, int direccion)
    {
        if (indiceJugador < 0 || indiceJugador >= 4) return;

        seleccionActual[indiceJugador] += direccion;

        // Rotación circular: si se pasa del último vuelve al primero, y viceversa.
        if (seleccionActual[indiceJugador] >= spritesPersonajes.Length)
            seleccionActual[indiceJugador] = 0;
        else if (seleccionActual[indiceJugador] < 0)
            seleccionActual[indiceJugador] = spritesPersonajes.Length - 1;

        ActualizarSlot(indiceJugador);
    }

    // Un método por flecha (sin parámetros) para poder conectarlos fácil en el OnClick del Inspector.
    public void CambiarPersonaje0Izq() { CambiarPersonaje(0, -1); }
    public void CambiarPersonaje0Der() { CambiarPersonaje(0, 1); }
    public void CambiarPersonaje1Izq() { CambiarPersonaje(1, -1); }
    public void CambiarPersonaje1Der() { CambiarPersonaje(1, 1); }
    public void CambiarPersonaje2Izq() { CambiarPersonaje(2, -1); }
    public void CambiarPersonaje2Der() { CambiarPersonaje(2, 1); }
    public void CambiarPersonaje3Izq() { CambiarPersonaje(3, -1); }
    public void CambiarPersonaje3Der() { CambiarPersonaje(3, 1); }

    // Dibuja en el slot el sprite y el nombre del personaje que tiene elegido ese jugador.
    private void ActualizarSlot(int indice)
    {
        if (indice >= imagenesPersonaje.Length || imagenesPersonaje[indice] == null) return;
        if (seleccionActual[indice] >= spritesPersonajes.Length) return;

        imagenesPersonaje[indice].sprite = spritesPersonajes[seleccionActual[indice]];   // muestra el dibujo

        if (textosIndice != null && indice < textosIndice.Length && textosIndice[indice] != null)
            textosIndice[indice].text = nombresPersonajes[seleccionActual[indice]];       // muestra el nombre
    }

    // Botón JUGAR: guarda lo elegido en Config y arranca la partida.
    public void Jugar()
    {
        // Por cada jugador que va a jugar, guarda su personaje y su nombre en Config
        // (para que la Partida y la VistaJuegoUI los usen en la escena del juego).
        for (int i = 0; i < Config.cantidadJugadores; i++)
        {
            Config.personajesElegidos[i] = seleccionActual[i];

            // Solo pisa el nombre si el jugador escribió algo (si no, deja el que había).
            if (i < inputsNombre.Length && inputsNombre[i] != null && inputsNombre[i].text.Length > 0)
                Config.nombresJugadores[i] = inputsNombre[i].text;
        }

        SceneManager.LoadScene("juego");
    }

    // Botón VOLVER: regresa al menú principal sin empezar la partida.
    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MENU");
    }
}