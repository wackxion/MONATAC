// ============================================================
//  GameManager.cs  —  Capa: VISUAL (presentación / Unity)
//  Es el "cerebro" que coordina la partida y hace de PUENTE
//  entre la lógica (Jugador, Dado) y la interfaz de Unity
//  (barras, textos, botones).
//
//  FLUJO DE UN TURNO:
//    1) El jugador elige una acción  -> OnAtacar / OnCurarse / OnRecolectar
//    2) Lanza los dados              -> OnLanzarDados (recién acá se aplica el efecto)
//    3) Pasa el turno                -> OnPasarTurno (le toca al siguiente)
//  Cuando queda un solo jugador vivo, VerificarVictoria() termina el juego.
// ============================================================

using UnityEngine;
using UnityEngine.UI;                 // para usar Image (las barras de vida)
using TMPro;                          // para usar los textos de TextMeshPro
using System.Collections.Generic;     // para usar List<> (la lista de jugadores)

public class GameManager : MonoBehaviour
{
    // --- Referencias a la UI ---
    // Son "public" para que aparezcan como casilleros en el Inspector
    // y ahí arrastramos los objetos de la escena (barras, textos, dados).

    [Header("Jugador en turno (abajo)")]
    public Image barraVos;                // la barra de vida del jugador actual
    public TextMeshProUGUI textoMonedas;  // sus monedas

    [Header("Oponente (arriba)")]
    public Image barraRival;              // la barra de vida del rival
    public TextMeshProUGUI textoRival;    // nombre y HP del rival

    [Header("Turno")]
    public TextMeshProUGUI textoTurno;    // "Turno de Jugador X"
    public TextMeshProUGUI textoEstado;   // mensajes de guía / ganador

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;    // número del dado 1
    public TextMeshProUGUI dado2Texto;    // número del dado 2
    public TextMeshProUGUI dado3Texto;    // número del dado 3

    // HP con el que arrancan. 40 para probar la victoria rápido; el real es 100.
    private int hpInicial = 40;

    // --- Datos de la partida (lógica) ---
    private List<Jugador> jugadores = new List<Jugador>();  // todos los jugadores
    private int indiceActual = 0;   // posición del jugador que está jugando ahora
    private Dado dado;              // un dado reutilizable para tirar

    // --- Estado del turno (banderas que controlan qué se puede hacer) ---
    private TipoAccion accionElegida;   // qué acción eligió el jugador
    private bool haElegido = false;     // ¿ya eligió una acción?
    private bool yaTiro = false;        // ¿ya lanzó los dados este turno?
    private bool juegoTerminado = false;// ¿terminó la partida?

    // Start() lo llama Unity una sola vez, al arrancar el juego.
    void Start()
    {
        // Creamos los jugadores y los guardamos en la lista.
        jugadores.Add(new Jugador("Jugador 1", hpInicial));
        jugadores.Add(new Jugador("Jugador 2", hpInicial));
        dado = new Dado();      // creamos el dado
        indiceActual = 0;       // empieza el primero
        IniciarTurno();         // preparamos su turno
    }

    // Atajo para obtener al jugador que está jugando ahora.
    private Jugador Actual() { return jugadores[indiceActual]; }

    // Atajo para obtener al oponente (con 2 jugadores, es el otro).
    // El "% jugadores.Count" hace que después del último se vuelva al primero.
    private Jugador Oponente() { return jugadores[(indiceActual + 1) % jugadores.Count]; }

    // --- Los 3 botones de acción: SOLO eligen (todavía no resuelven) ---
    public void OnAtacar()     { ElegirAccion(TipoAccion.Atacar); }
    public void OnCurarse()    { ElegirAccion(TipoAccion.Curarse); }
    public void OnRecolectar() { ElegirAccion(TipoAccion.Recolectar); }

    // Guarda la acción elegida y avisa al jugador que tire los dados.
    private void ElegirAccion(TipoAccion accion)
    {
        // Si el juego terminó, o ya tiró este turno, no deja elegir de nuevo.
        if (juegoTerminado || yaTiro) return;
        accionElegida = accion;
        haElegido = true;
        Mensaje("Elegiste " + accion + ". Ahora lanzá los dados.");
    }

    // --- Botón LANZAR DADOS: acá recién se aplica el efecto de la acción ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }  // no eligió nada
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; } // no puede tirar 2 veces

        // Según la acción elegida, tira los dados y aplica el resultado.
        if (accionElegida == TipoAccion.Atacar)
        {
            int dano = TirarDados(3);              // atacar = 3 dados
            Oponente().RecibirDanio(dano);         // el rival recibe el daño
            Mensaje(Actual().nombre + " ataca por " + dano + " de daño.");
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            int cura = TirarDados(2);              // curarse = 2 dados
            Actual().Curar(cura);                  // te curás vos
            Mensaje(Actual().nombre + " se cura " + cura + " HP.");
        }
        else // Recolectar
        {
            int mon = TirarDados(3);              // recolectar = 3 dados
            Actual().GanarMonedas(mon);            // ganás monedas
            Mensaje(Actual().nombre + " recolecta " + mon + " monedas.");
        }

        yaTiro = true;        // marca que ya actuó este turno
        ActualizarUI();       // refresca lo que se ve en pantalla
        VerificarVictoria();  // ¿alguien ganó?
    }

    // --- Botón PASAR TURNO: le pasa el turno al siguiente jugador ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        // Obliga a hacer una acción antes de poder pasar.
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }

        // Avanza al siguiente jugador (y vuelve al primero después del último).
        indiceActual = (indiceActual + 1) % jugadores.Count;
        IniciarTurno();
    }

    // Prepara un turno nuevo: reinicia las banderas y refresca la pantalla.
    private void IniciarTurno()
    {
        haElegido = false;
        yaTiro = false;
        ActualizarUI();
        Mensaje("Turno de " + Actual().nombre + ": elegí una acción.");
    }

    // Revisa si quedó un solo jugador vivo. Si es así, ese ganó.
    private void VerificarVictoria()
    {
        int vivos = 0;
        Jugador ultimo = null;

        // Recorre todos los jugadores contando cuántos siguen vivos.
        foreach (Jugador j in jugadores)
        {
            if (j.EstaVivo()) { vivos++; ultimo = j; }
        }

        // Si queda 1 (o 0), el juego terminó.
        if (vivos <= 1)
        {
            juegoTerminado = true;
            Mensaje("FIN DEL JUEGO. Ganó " + ultimo.nombre + "!");
            if (textoTurno != null) textoTurno.text = "Ganó " + ultimo.nombre;
        }
    }

    // Tira 'cantidad' dados, muestra los números en pantalla y devuelve la suma.
    private int TirarDados(int cantidad)
    {
        int total = 0;
        string[] caras = { "", "", "" };   // arranca vacío (para los dados que no se usan)

        // Tira uno por uno y guarda el resultado.
        for (int i = 0; i < cantidad; i++)
        {
            int valor = dado.Tirar();
            total += valor;                 // suma al total
            caras[i] = valor.ToString();    // guarda el número como texto
        }

        // Muestra cada resultado en su dado (si el casillero está conectado).
        if (dado1Texto != null) dado1Texto.text = caras[0];
        if (dado2Texto != null) dado2Texto.text = caras[1];
        if (dado3Texto != null) dado3Texto.text = caras[2];

        return total;
    }

    // Refresca TODA la pantalla según el estado actual del juego.
    // Se llama después de cada cambio para que lo visual coincida con la lógica.
    void ActualizarUI()
    {
        Jugador actual = Actual();
        Jugador oponente = Oponente();

        // Abajo: siempre el jugador en turno.
        // fillAmount va de 0 (vacío) a 1 (lleno) = hp actual / hp máximo.
        if (barraVos != null) barraVos.fillAmount = (float)actual.hp / actual.hpMaximo;
        if (textoMonedas != null) textoMonedas.text = "Monedas: " + actual.monedas;

        // Arriba: el oponente.
        if (barraRival != null) barraRival.fillAmount = (float)oponente.hp / oponente.hpMaximo;
        if (textoRival != null) textoRival.text = oponente.nombre + ": " + oponente.hp + " HP";

        // Cartel de turno (mientras el juego no haya terminado).
        if (textoTurno != null && !juegoTerminado) textoTurno.text = "Turno de " + actual.nombre;
    }

    // Muestra un mensaje en la Console (para nosotros) y en el texto de estado (para el jugador).
    private void Mensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }
}
