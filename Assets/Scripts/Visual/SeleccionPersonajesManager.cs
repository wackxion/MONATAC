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
    [Header("Slots de jugadores (4")]
    public Image[] imagenesPersonaje;
    public TMP_InputField[] inputsNombre;
    public TextMeshProUGUI[] textosIndice;

    [Header("Personajes disponibles")]
    public Sprite[] spritesPersonajes;
    public string[] nombresPersonajes;

    [Header("Botón jugar")]
    public GameObject botonJugar;

    private int[] seleccionActual = new int[4];

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            seleccionActual[i] = i % spritesPersonajes.Length;

            if (i < inputsNombre.Length && inputsNombre[i] != null)
                inputsNombre[i].text = Config.nombresJugadores[i];

            ActualizarSlot(i);
        }
    }

    public void CambiarPersonaje(int indiceJugador, int direccion)
    {
        if (indiceJugador < 0 || indiceJugador >= 4) return;

        seleccionActual[indiceJugador] += direccion;

        if (seleccionActual[indiceJugador] >= spritesPersonajes.Length)
            seleccionActual[indiceJugador] = 0;
        else if (seleccionActual[indiceJugador] < 0)
            seleccionActual[indiceJugador] = spritesPersonajes.Length - 1;

        ActualizarSlot(indiceJugador);
    }

    public void CambiarPersonaje0Izq() { CambiarPersonaje(0, -1); }
    public void CambiarPersonaje0Der() { CambiarPersonaje(0, 1); }
    public void CambiarPersonaje1Izq() { CambiarPersonaje(1, -1); }
    public void CambiarPersonaje1Der() { CambiarPersonaje(1, 1); }
    public void CambiarPersonaje2Izq() { CambiarPersonaje(2, -1); }
    public void CambiarPersonaje2Der() { CambiarPersonaje(2, 1); }
    public void CambiarPersonaje3Izq() { CambiarPersonaje(3, -1); }
    public void CambiarPersonaje3Der() { CambiarPersonaje(3, 1); }

    private void ActualizarSlot(int indice)
    {
        if (indice >= imagenesPersonaje.Length || imagenesPersonaje[indice] == null) return;
        if (seleccionActual[indice] >= spritesPersonajes.Length) return;

        imagenesPersonaje[indice].sprite = spritesPersonajes[seleccionActual[indice]];

        if (textosIndice != null && indice < textosIndice.Length && textosIndice[indice] != null)
            textosIndice[indice].text = nombresPersonajes[seleccionActual[indice]];
    }

    public void Jugar()
    {
        for (int i = 0; i < Config.cantidadJugadores; i++)
        {
            Config.personajesElegidos[i] = seleccionActual[i];

            if (i < inputsNombre.Length && inputsNombre[i] != null && inputsNombre[i].text.Length > 0)
                Config.nombresJugadores[i] = inputsNombre[i].text;
        }

        SceneManager.LoadScene("juego");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MENU");
    }
}