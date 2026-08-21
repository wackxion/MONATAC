// GameManager.cs — El "puente" entre la lógica del juego y la UI de Unity

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Jugador en turno (abajo)")]
    public Image barraVos;
    public TextMeshProUGUI textoMonedas;

    [Header("Oponente (arriba)")]
    public Image barraRival;
    public TextMeshProUGUI textoRival;

    [Header("Turno")]
    public TextMeshProUGUI textoTurno;

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    // --- La lógica del juego ---
    private List<Jugador> jugadores = new List<Jugador>();
    private int indiceActual = 0;
    private Dado dado;

    void Start()
    {
        // Por ahora 2 jugadores (más adelante sumamos los otros 2)
        jugadores.Add(new Jugador("Jugador 1", 100));
        jugadores.Add(new Jugador("Jugador 2", 100));
        dado = new Dado();
        indiceActual = 0;
        ActualizarUI();
    }

    // El jugador que está jugando ahora
    private Jugador Actual()
    {
        return jugadores[indiceActual];
    }

    // El oponente (con 2 jugadores, es el otro)
    private Jugador Oponente()
    {
        int otro = (indiceActual + 1) % jugadores.Count;
        return jugadores[otro];
    }

    // Botón ATACAR: daña al oponente y pasa el turno
    public void OnAtacar()
    {
        int dano = TirarDados(3);
        Oponente().RecibirDanio(dano);
        Debug.Log(Actual().nombre + " ataca por " + dano + ". HP oponente: " + Oponente().hp);
        PasarTurno();
    }

    // Botón CURARSE: te curás vos (el jugador en turno) y pasa el turno
    public void OnCurarse()
    {
        int cura = TirarDados(2);
        Actual().Curar(cura);
        Debug.Log(Actual().nombre + " se cura " + cura + ". HP: " + Actual().hp);
        PasarTurno();
    }

    // Botón RECOLECTAR: ganás monedas y pasa el turno
    public void OnRecolectar()
    {
        int monedas = TirarDados(3);
        Actual().GanarMonedas(monedas);
        Debug.Log(Actual().nombre + " recolecta " + monedas + ". Total: " + Actual().monedas);
        PasarTurno();
    }

    // Pasa al siguiente jugador y refresca la pantalla
    private void PasarTurno()
    {
        indiceActual = (indiceActual + 1) % jugadores.Count;
        ActualizarUI();
    }

    // Tira 'cantidad' dados, los muestra y devuelve la suma
    private int TirarDados(int cantidad)
    {
        int total = 0;
        string[] caras = { "", "", "" };

        for (int i = 0; i < cantidad; i++)
        {
            int valor = dado.Tirar();
            total += valor;
            caras[i] = valor.ToString();
        }

        if (dado1Texto != null) dado1Texto.text = caras[0];
        if (dado2Texto != null) dado2Texto.text = caras[1];
        if (dado3Texto != null) dado3Texto.text = caras[2];

        return total;
    }

    // Refresca toda la pantalla según de quién es el turno
    void ActualizarUI()
    {
        Jugador actual = Actual();
        Jugador oponente = Oponente();

        // Abajo: SIEMPRE el jugador en turno
        if (barraVos != null) barraVos.fillAmount = (float)actual.hp / actual.hpMaximo;
        if (textoMonedas != null) textoMonedas.text = "Monedas: " + actual.monedas;

        // Arriba: el oponente
        if (barraRival != null) barraRival.fillAmount = (float)oponente.hp / oponente.hpMaximo;
        if (textoRival != null) textoRival.text = oponente.nombre + ": " + oponente.hp + " HP";

        // Cartel de turno
        if (textoTurno != null) textoTurno.text = "Turno de " + actual.nombre;
    }
}
