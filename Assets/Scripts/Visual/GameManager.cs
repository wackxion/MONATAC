// ============================================================
//  GameManager.cs  —  Capa: VISUAL (presentación / Unity)
//  Coordina la partida y conecta la lógica (Jugador, Dado) con
//  la interfaz de Unity. Ahora muestra las 4 barras a la vez:
//  cada jugador tiene su barra fija, y se MARCA quién está en
//  turno "(turno)" y a quién apunta el ataque "(objetivo)".
//
//  FLUJO DE UN TURNO:
//    1) Elegir acción            -> OnAtacar / OnCurarse / OnRecolectar
//    2) (si atacás) elegir a quién -> OnCambiarObjetivo
//    3) Lanzar los dados          -> OnLanzarDados
//    4) Pasar el turno            -> OnPasarTurno
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Las 4 barras y los 4 nombres, EN ORDEN: [0]=Jugador 1, [1]=Jugador 2, etc.
    // En el Inspector se ponen como arreglos de tamaño 4.
    [Header("Barras de HP (en orden J1, J2, J3, J4)")]
    public Image[] barras;
    public TextMeshProUGUI[] nombres;

    [Header("Del jugador en turno")]
    public TextMeshProUGUI textoMonedas;

    [Header("Turno")]
    public TextMeshProUGUI textoTurno;
    public TextMeshProUGUI textoEstado;

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    [Header("Mano de cartas (textos de las cartas de abajo)")]
    public TextMeshProUGUI[] cartasTexto;

    private int hpInicial = 40;          // 40 para probar rápido; el real es 100
    private int cantidadJugadores;       // lo define el menú (Config.cantidadJugadores)

    // --- Datos de la partida ---
    private List<Jugador> jugadores = new List<Jugador>();
    private int indiceActual = 0;     // quién juega ahora
    private int indiceObjetivo = 0;   // a quién apunta el ataque
    private Dado dado;
    private Mazo mazo;                // el mazo de cartas
    private PilaDescarte descarte;    // la pila de descarte

    // --- Estado del turno ---
    private TipoAccion accionElegida;
    private bool haElegido = false;
    private bool yaTiro = false;
    private bool puedeComprar = false;    // ¿puede comprar cartas este turno? (solo si Recolectó)
    private bool juegoTerminado = false;

    // --- PATRÓN SINGLETON ---
    // Garantiza que exista UNA sola instancia del GameManager, accesible
    // globalmente con GameManager.Instance desde cualquier otro script.
    public static GameManager Instance { get; private set; }

    // Awake() lo llama Unity ANTES que Start(). Acá aseguramos la única instancia.
    void Awake()
    {
        // Si ya existía otro GameManager, este sobra y se destruye.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;   // este pasa a ser LA instancia única
    }

    void Start()
    {
        // La cantidad la eligió el menú. Si se juega esta escena directo,
        // usa el valor por defecto de Config. Clamp la mantiene entre 2 y 4.
        cantidadJugadores = Mathf.Clamp(Config.cantidadJugadores, 2, 4);

        for (int i = 0; i < cantidadJugadores; i++)
        {
            jugadores.Add(new Jugador("Jugador " + (i + 1), hpInicial));
        }

        OcultarBarrasSobrantes();   // esconde las barras de los jugadores que no juegan

        dado = new Dado();
        descarte = new PilaDescarte();
        mazo = FabricaDeCartas.CrearMazo();   // la Fábrica arma el mazo
        mazo.Mezclar();

        indiceActual = 0;
        IniciarTurno();
    }

    // Muestra solo las barras/nombres de los jugadores que hay en la partida
    // y oculta las que sobran (ej: si son 2, esconde las de J3 y J4).
    private void OcultarBarrasSobrantes()
    {
        if (barras == null) return;
        for (int i = 0; i < barras.Length; i++)
        {
            bool existe = i < cantidadJugadores;
            // La barra vive dentro de BarraHP_Fondo (su "padre"): ocultamos todo el conjunto.
            if (barras[i] != null) barras[i].transform.parent.gameObject.SetActive(existe);
            if (nombres != null && i < nombres.Length && nombres[i] != null)
                nombres[i].gameObject.SetActive(existe);
        }
    }

    private Jugador Actual()   { return jugadores[indiceActual]; }
    private Jugador Objetivo() { return jugadores[indiceObjetivo]; }

    // --- Botones de acción: solo eligen ---
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

    // --- Botón CAMBIAR OBJETIVO: rota entre rivales vivos ---
    public void OnCambiarObjetivo()
    {
        if (juegoTerminado) return;
        indiceObjetivo = SiguienteRivalVivo(indiceObjetivo);
        ActualizarUI();
        Mensaje("Objetivo: " + Objetivo().nombre);
    }

    // --- Botón LANZAR DADOS: aplica la acción ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; }

        Jugador jugador = Actual();
        // Bonus que aportan las cartas de la mano para esta acción (polimorfismo).
        int bonus = BonusDeCartas(jugador, accionElegida);

        if (accionElegida == TipoAccion.Atacar)
        {
            int total = TirarDados(3) + bonus;     // dados + bonus de cartas
            Objetivo().RecibirDanio(total);
            Mensaje(jugador.nombre + " ataca a " + Objetivo().nombre + " por " + total + " (bonus +" + bonus + ").");
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            int total = TirarDados(2) + bonus;
            jugador.Curar(total);
            Mensaje(jugador.nombre + " se cura " + total + " HP.");
        }
        else // Recolectar
        {
            int total = TirarDados(3) + bonus;
            jugador.GanarMonedas(total);
            puedeComprar = true;   // habilita el botón Comprar carta este turno
            Mensaje(jugador.nombre + " recolecta " + total + " monedas. Podés comprar cartas (6 c/u) o acumular.");
        }

        DescartarUsadas(jugador);   // saca de la mano las cartas de un solo uso ya gastadas
        yaTiro = true;
        ActualizarUI();
        VerificarVictoria();
    }

    // Suma los bonus que dan las cartas de la mano para una acción.
    // Cada carta responde con su propio BonusPara() (polimorfismo).
    private int BonusDeCartas(Jugador j, TipoAccion accion)
    {
        int total = 0;
        foreach (Carta c in j.mano) total += c.BonusPara(accion);
        return total;
    }

    // --- Botón COMPRAR CARTA ---
    // Comprás si querés (una o varias veces), pero SOLO en el turno en que
    // Recolectaste. Si no comprás, las monedas quedan acumuladas para después.
    public void OnComprarCarta()
    {
        if (juegoTerminado) return;
        if (!puedeComprar) { Mensaje("Solo podés comprar cartas el turno que Recolectás."); return; }

        Jugador j = Actual();
        if (j.monedas < 6)     { Mensaje("Te faltan monedas (cada carta cuesta 6)."); return; }
        if (j.mano.Count >= 5) { Mensaje("Tu mano está llena (máximo 5 cartas)."); return; }

        if (mazo.EstaVacio()) mazo.Reciclar(descarte);   // mazo circular
        Carta comprada = mazo.Robar();
        j.GastarMonedas(6);
        j.mano.Add(comprada);
        Mensaje(j.nombre + " compró: " + comprada.nombre + " (te quedan " + j.monedas + " monedas).");
        ActualizarUI();
    }

    // Manda al descarte las cartas de un solo uso que ya se usaron.
    private void DescartarUsadas(Jugador j)
    {
        for (int i = j.mano.Count - 1; i >= 0; i--)
        {
            CartaUnUso u = j.mano[i] as CartaUnUso;   // ¿es de un solo uso?
            if (u != null && u.usada)
            {
                descarte.Agregar(j.mano[i]);
                j.mano.RemoveAt(i);
            }
        }
    }

    // --- Botón PASAR TURNO ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }
        indiceActual = SiguienteJugadorVivo(indiceActual);
        IniciarTurno();
    }

    private void IniciarTurno()
    {
        haElegido = false;
        yaTiro = false;
        puedeComprar = false;   // cada turno arranca sin poder comprar (hasta que Recolectes)
        indiceObjetivo = SiguienteRivalVivo(indiceActual);
        ActualizarUI();
        Mensaje("Turno de " + Actual().nombre + ": elegí una acción.");
    }

    private int SiguienteJugadorVivo(int desde)
    {
        int i = desde;
        for (int intento = 0; intento < jugadores.Count; intento++)
        {
            i = (i + 1) % jugadores.Count;
            if (jugadores[i].EstaVivo()) return i;
        }
        return desde;
    }

    private int SiguienteRivalVivo(int desde)
    {
        int i = desde;
        for (int intento = 0; intento < jugadores.Count; intento++)
        {
            i = (i + 1) % jugadores.Count;
            if (i != indiceActual && jugadores[i].EstaVivo()) return i;
        }
        return indiceActual;
    }

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

    // Refresca las 4 barras. Cada barra muestra SIEMPRE a su jugador,
    // y el nombre lleva la marca "(turno)" o "(objetivo)" según corresponda.
    void ActualizarUI()
    {
        for (int i = 0; i < jugadores.Count; i++)
        {
            Jugador j = jugadores[i];

            // La barra: llena según su HP (0 a 1).
            if (barras != null && i < barras.Length && barras[i] != null)
                barras[i].fillAmount = (float)j.hp / j.hpMaximo;

            // El nombre con su HP y la marca de turno/objetivo.
            if (nombres != null && i < nombres.Length && nombres[i] != null)
            {
                string marca = "";
                if (!j.EstaVivo())            marca = " (eliminado)";
                else if (i == indiceActual)   marca = " (turno)";
                else if (i == indiceObjetivo) marca = " (objetivo)";
                nombres[i].text = j.nombre + ": " + j.hp + " HP" + marca;
            }
        }

        // Monedas del jugador en turno.
        if (textoMonedas != null) textoMonedas.text = "Monedas: " + Actual().monedas;

        if (textoTurno != null && !juegoTerminado) textoTurno.text = "Turno de " + Actual().nombre;

        // Mano de cartas del jugador en turno: muestra el nombre de cada carta,
        // o "-" si ese slot está vacío.
        if (cartasTexto != null)
        {
            Jugador enTurno = Actual();
            for (int i = 0; i < cartasTexto.Length; i++)
            {
                if (cartasTexto[i] == null) continue;
                cartasTexto[i].text = (i < enTurno.mano.Count) ? enTurno.mano[i].nombre : "-";
            }
        }
    }

    private void Mensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }
}
