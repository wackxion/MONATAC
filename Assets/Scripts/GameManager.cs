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
    public TextMeshProUGUI textoEstado;   // mensajes de guía (elegí acción, ganador, etc.)

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    // HP con el que arrancan. 40 para probar la victoria rápido; el valor real del juego es 100.
    private int hpInicial = 40;

    // --- La lógica del juego ---
    private List<Jugador> jugadores = new List<Jugador>();
    private int indiceActual = 0;
    private Dado dado;

    // Estado del turno
    private TipoAccion accionElegida;
    private bool haElegido = false;
    private bool yaTiro = false;
    private bool juegoTerminado = false;

    void Start()
    {
        jugadores.Add(new Jugador("Jugador 1", hpInicial));
        jugadores.Add(new Jugador("Jugador 2", hpInicial));
        dado = new Dado();
        indiceActual = 0;
        IniciarTurno();
    }

    private Jugador Actual() { return jugadores[indiceActual]; }
    private Jugador Oponente() { return jugadores[(indiceActual + 1) % jugadores.Count]; }

    // --- Los 3 botones de acción ahora SOLO eligen (no resuelven) ---
    public void OnAtacar()     { ElegirAccion(TipoAccion.Atacar); }
    public void OnCurarse()    { ElegirAccion(TipoAccion.Curarse); }
    public void OnRecolectar() { ElegirAccion(TipoAccion.Recolectar); }

    private void ElegirAccion(TipoAccion accion)
    {
        if (juegoTerminado || yaTiro) return;
        accionElegida = accion;
        haElegido = true;
        Mensaje("Elegiste " + accion + ". Ahora lanzá los dados.");
    }

    // --- Botón LANZAR DADOS: resuelve la acción elegida ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; }

        if (accionElegida == TipoAccion.Atacar)
        {
            int dano = TirarDados(3);
            Oponente().RecibirDanio(dano);
            Mensaje(Actual().nombre + " ataca por " + dano + " de daño.");
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            int cura = TirarDados(2);
            Actual().Curar(cura);
            Mensaje(Actual().nombre + " se cura " + cura + " HP.");
        }
        else // Recolectar
        {
            int mon = TirarDados(3);
            Actual().GanarMonedas(mon);
            Mensaje(Actual().nombre + " recolecta " + mon + " monedas.");
        }

        yaTiro = true;
        ActualizarUI();
        VerificarVictoria();
    }

    // --- Botón PASAR TURNO ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }
        indiceActual = (indiceActual + 1) % jugadores.Count;
        IniciarTurno();
    }

    // Prepara un turno nuevo
    private void IniciarTurno()
    {
        haElegido = false;
        yaTiro = false;
        ActualizarUI();
        Mensaje("Turno de " + Actual().nombre + ": elegí una acción.");
    }

    // ¿Queda un solo jugador vivo? -> ganó
    private void VerificarVictoria()
    {
        int vivos = 0;
        Jugador ultimo = null;
        foreach (Jugador j in jugadores)
        {
            if (j.EstaVivo()) { vivos++; ultimo = j; }
        }

        if (vivos <= 1)
        {
            juegoTerminado = true;
            Mensaje("FIN DEL JUEGO. Ganó " + ultimo.nombre + "!");
            if (textoTurno != null) textoTurno.text = "Ganó " + ultimo.nombre;
        }
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

    // Refresca barras y textos según de quién es el turno
    void ActualizarUI()
    {
        Jugador actual = Actual();
        Jugador oponente = Oponente();

        if (barraVos != null) barraVos.fillAmount = (float)actual.hp / actual.hpMaximo;
        if (textoMonedas != null) textoMonedas.text = "Monedas: " + actual.monedas;

        if (barraRival != null) barraRival.fillAmount = (float)oponente.hp / oponente.hpMaximo;
        if (textoRival != null) textoRival.text = oponente.nombre + ": " + oponente.hp + " HP";

        if (textoTurno != null && !juegoTerminado) textoTurno.text = "Turno de " + actual.nombre;
    }

    // Muestra un mensaje en la Console y (si existe) en el texto de estado
    private void Mensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }
}
