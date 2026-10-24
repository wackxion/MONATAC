// ============================================================
//  EstadoOnline.cs  —  Capa: VISUAL (red · Netcode)
//  Etapa 2: SINCRONIZAR el estado visible. El server escribe, todos leen.
//    - 2A: 'turnoActual' con NetworkVariable.
//    - 2B: HP y monedas por jugador con NetworkList.
//    - 2C: esos valores salen de una PARTIDA REAL (/Rules) que vive en el server.
//
//  Clave de arquitectura: la Partida es C# PURO y corre SOLO en el server.
//  EstadoOnline la "espeja" a variables de red para que todos la vean. El
//  cliente nunca tiene Partida: solo lee el espejo.
// ============================================================

using Unity.Netcode;
using UnityEngine;
using TMPro;

// El estado de UN jugador que viaja por la red.
//  - INetworkSerializable: le dice a Netcode CÓMO mandarlo (qué campos).
//  - IEquatable: para detectar si un elemento cambió (y no sincronizar de más).
public struct EstadoJugador : INetworkSerializable, System.IEquatable<EstadoJugador>
{
    public int hp;
    public int monedas;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref monedas);
    }

    public bool Equals(EstadoJugador otro) => hp == otro.hp && monedas == otro.monedas;
}

public class EstadoOnline : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text textoTurno;       // de quién es el turno
    public TMP_Text textoJugadores;   // HP y monedas de cada jugador

    [Header("Partida (la crea el server)")]
    public int cantidadJugadores = 2;
    public int hpInicial = 40;

    // --- Estado sincronizado (server escribe, todos leen) ---
    public NetworkVariable<int> turnoActual = new NetworkVariable<int>(0);
    public NetworkList<EstadoJugador> jugadores;

    // La Partida REAL (de /Rules). Solo existe en el server; el cliente la deja en null.
    private IPartida partida;

    void Awake()
    {
        jugadores = new NetworkList<EstadoJugador>();
    }

    public override void OnNetworkSpawn()
    {
        turnoActual.OnValueChanged += AlCambiarTurno;
        jugadores.OnListChanged += AlCambiarJugadores;

        // Solo el server crea la Partida real y la espeja a la red.
        if (IsServer)
        {
            partida = new Partida(cantidadJugadores, hpInicial);   // reusa /Rules tal cual
            EspejarPartida();
        }

        MostrarTurno(turnoActual.Value);
        MostrarJugadores();
    }

    public override void OnNetworkDespawn()
    {
        turnoActual.OnValueChanged -= AlCambiarTurno;
        jugadores.OnListChanged -= AlCambiarJugadores;
    }

    // Copia el estado de la Partida real a las variables de red (solo server).
    private void EspejarPartida()
    {
        if (!IsServer || partida == null) return;

        turnoActual.Value = partida.IndiceActual;   // de quién es el turno

        // HP y monedas de cada jugador.
        if (jugadores.Count != partida.Jugadores.Count)
        {
            // Primera vez: armamos la lista de red con los jugadores reales.
            jugadores.Clear();
            foreach (Jugador j in partida.Jugadores)
                jugadores.Add(new EstadoJugador { hp = j.hp, monedas = j.monedas });
        }
        else
        {
            // Ya existe: actualizamos solo lo que cambió (por eso el struct es IEquatable).
            for (int i = 0; i < partida.Jugadores.Count; i++)
            {
                Jugador j = partida.Jugadores[i];
                EstadoJugador nuevo = new EstadoJugador { hp = j.hp, monedas = j.monedas };
                if (!jugadores[i].Equals(nuevo)) jugadores[i] = nuevo;
            }
        }
    }

    // ---------------- Turno ----------------
    private void AlCambiarTurno(int anterior, int nuevo) => MostrarTurno(nuevo);

    private void MostrarTurno(int valor)
    {
        if (textoTurno != null) textoTurno.text = "Turno del Jugador " + valor;
    }

    // Botón "Siguiente turno": pasa el turno en la PARTIDA REAL y espeja.
    public void SiguienteTurno()
    {
        if (!IsServer) return;
        partida.PasarTurno();   // lógica real de /Rules (saltea muertos, cuenta rondas...)
        EspejarPartida();
    }

    // ---------------- Jugadores ----------------
    private void AlCambiarJugadores(NetworkListEvent<EstadoJugador> evento) => MostrarJugadores();

    private void MostrarJugadores()
    {
        if (textoJugadores == null) return;
        string txt = "";
        for (int i = 0; i < jugadores.Count; i++)
            txt += "Jugador " + i + ": HP " + jugadores[i].hp + " · Monedas " + jugadores[i].monedas + "\n";
        textoJugadores.text = txt;
    }

    // Botón de prueba: daña al Jugador 1 EN LA PARTIDA REAL (método controlado) y espeja.
    public void DanarJugador1()
    {
        if (!IsServer) return;
        if (partida.Jugadores.Count < 2) return;
        partida.Jugadores[1].RecibirDanio(5);   // respeta el encapsulamiento del Jugador (no baja de 0)
        EspejarPartida();
    }
}
