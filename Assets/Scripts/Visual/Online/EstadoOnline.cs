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
using Unity.Collections;   // FixedString: así los nombres de las cartas viajan por la red
using UnityEngine;
using TMPro;

// El estado de UN jugador que viaja por la red.
//  - INetworkSerializable: le dice a Netcode CÓMO mandarlo (qué campos).
//  - IEquatable: para detectar si un elemento cambió (y no sincronizar de más).
public struct EstadoJugador : INetworkSerializable, System.IEquatable<EstadoJugador>
{
    public int hp;
    public int monedas;
    public int cartas;   // cuántas cartas en mano (la ven todos; el CONTENIDO es privado · Etapa 5)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref monedas);
        serializer.SerializeValue(ref cartas);
    }

    public bool Equals(EstadoJugador otro) => hp == otro.hp && monedas == otro.monedas && cartas == otro.cartas;
}

public class EstadoOnline : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text textoTurno;       // de quién es el turno
    public TMP_Text textoJugadores;   // HP y monedas de cada jugador
    public TMP_Text textoTirada;      // la última tirada de dados (la ve todo el mundo · Etapa 4)
    public TMP_Text textoMiMano;      // MI mano (privada · solo la ve su dueño · Etapa 5B)

    [Header("Partida (la crea el server)")]
    public int cantidadJugadores = 2;
    public int hpInicial = 40;

    [Header("Identidad (arrastrar el objeto Red)")]
    public IdentidadJugadores identidad;   // el mapa clientId → Jugador (Etapa 1), para validar el turno

    // --- Estado sincronizado (server escribe, todos leen) ---
    public NetworkVariable<int> turnoActual = new NetworkVariable<int>(0);
    public NetworkList<EstadoJugador> jugadores;

    // La Partida REAL (de /Rules). Solo existe en el server; el cliente la deja en null.
    private IPartida partida;
    private Dado dado;   // el dado del server: las tiradas corren acá (nadie las puede falsear)
    private Mazo mazo;   // el mazo del server (de acá se reparten las cartas · Etapa 5)

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
            dado = new Dado(Reglas.CarasDado);                      // el dado vive en el server
            mazo = FabricaDeCartas.CrearMazo();                     // el mazo también (reusa la Fábrica)
            mazo.Mezclar();
            RepartirManoInicial(3);                                 // 3 cartas a cada jugador
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
                jugadores.Add(new EstadoJugador { hp = j.hp, monedas = j.monedas, cartas = j.mano.Count });
        }
        else
        {
            // Ya existe: actualizamos solo lo que cambió (por eso el struct es IEquatable).
            for (int i = 0; i < partida.Jugadores.Count; i++)
            {
                Jugador j = partida.Jugadores[i];
                EstadoJugador nuevo = new EstadoJugador { hp = j.hp, monedas = j.monedas, cartas = j.mano.Count };
                if (!jugadores[i].Equals(nuevo)) jugadores[i] = nuevo;
            }
        }
    }

    // Reparte 'cantidadPorJugador' cartas a cada jugador desde el mazo (solo server · Etapa 5).
    private void RepartirManoInicial(int cantidadPorJugador)
    {
        foreach (Jugador j in partida.Jugadores)
            for (int c = 0; c < cantidadPorJugador; c++)
                if (!mazo.EstaVacio()) j.mano.Add(mazo.Robar());
    }

    // ---------------- Turno ----------------
    private void AlCambiarTurno(int anterior, int nuevo) => MostrarTurno(nuevo);

    private void MostrarTurno(int valor)
    {
        if (textoTurno != null) textoTurno.text = "Turno del Jugador " + valor;
    }

    // --- Botón "Siguiente turno" (Etapa 3: ahora el cliente le PIDE al server) ---

    // Lo llama el botón, en el host O en el cliente. No ejecuta: manda el pedido.
    public void PedirSiguienteTurno()
    {
        SiguienteTurnoRpc();
    }

    // Corre SIEMPRE en el server (venga del host o de un cliente).
    [Rpc(SendTo.Server)]
    private void SiguienteTurnoRpc(RpcParams rpcParams = default)
    {
        int numeroJugador = NumeroDelQuePide(rpcParams);

        // GATING (3B): solo el jugador EN TURNO puede avanzar. Si no, se rechaza el pedido.
        if (numeroJugador != partida.IndiceActual)
        {
            Debug.Log("[EstadoOnline] Pedido rechazado: pidió el Jugador " + numeroJugador
                      + " pero es el turno del Jugador " + partida.IndiceActual + ".");
            return;
        }

        partida.PasarTurno();                 // lógica real de /Rules (saltea muertos, cuenta rondas...)
        partida.ElegirObjetivoPorDefecto();   // re-elige el rival para el nuevo turno (como IniciarTurno)
        EspejarPartida();
    }

    // ---------------- Acciones reales (3C) ----------------
    // Cada botón pide su acción; el server valida el turno, tira los dados y aplica el efecto.
    public void PedirAtacar()     { AccionRpc(TipoAccion.Atacar); }
    public void PedirCurarse()    { AccionRpc(TipoAccion.Curarse); }
    public void PedirRecolectar() { AccionRpc(TipoAccion.Recolectar); }

    [Rpc(SendTo.Server)]
    private void AccionRpc(TipoAccion tipo, RpcParams rpcParams = default)
    {
        int numeroJugador = NumeroDelQuePide(rpcParams);

        // Mismo gating que el turno: solo el jugador en turno puede actuar.
        if (numeroJugador != partida.IndiceActual)
        {
            Debug.Log("[EstadoOnline] Acción rechazada: no es el turno del Jugador " + numeroJugador + ".");
            return;
        }

        // Igual que el GameManager, pero corriendo en el SERVER:
        Accion accion = CrearAccion(tipo);                // 1) crea la acción (polimorfismo de /Rules)
        int[] valores = TirarDados(accion.CantidadDados); // 2) tira los dados EN EL SERVER (anti-trampa)
        int total = Sumar(valores);
        accion.Aplicar(partida, total);                   // 3) aplica el efecto sobre la Partida real

        MostrarTiradaRpc(numeroJugador, tipo, valores);   // 4) DIFUNDE la tirada a todos (Etapa 4)
        EspejarPartida();                                 // 5) todos ven el estado nuevo
    }

    // Etapa 4: el server DIFUNDE la tirada a TODOS (patrón inverso al ServerRpc: server → todos).
    [Rpc(SendTo.Everyone)]
    private void MostrarTiradaRpc(int numeroJugador, TipoAccion tipo, int[] valores)
    {
        if (textoTirada == null) return;
        string detalle = "";
        int total = 0;
        for (int i = 0; i < valores.Length; i++)
        {
            detalle += valores[i];
            total += valores[i];
            if (i < valores.Length - 1) detalle += " + ";
        }
        textoTirada.text = "Jugador " + numeroJugador + " — " + tipo + ":  " + detalle + " = " + total;
    }

    // Crea la acción concreta según el tipo (como GameManager.CrearAccion).
    private Accion CrearAccion(TipoAccion tipo)
    {
        switch (tipo)
        {
            case TipoAccion.Atacar:  return new AccionAtacar();
            case TipoAccion.Curarse: return new AccionCurarse();
            default:                 return new AccionRecolectar();
        }
    }

    // Tira 'cantidad' dados en el server y devuelve los valores individuales.
    private int[] TirarDados(int cantidad)
    {
        int[] valores = new int[cantidad];
        for (int i = 0; i < cantidad; i++) valores[i] = dado.Tirar();
        return valores;
    }

    // Suma los valores de una tirada.
    private int Sumar(int[] valores)
    {
        int total = 0;
        foreach (int v in valores) total += v;
        return total;
    }

    // Devuelve el número de Jugador de quien mandó el RPC (o -1 si no está registrado).
    private int NumeroDelQuePide(RpcParams rpcParams)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        return (identidad != null) ? identidad.NumeroDe(sender) : -1;
    }

    // ---------------- Mano privada (5B) ----------------
    // Lo llama el botón "Ver mi mano": le pide al server MI mano.
    public void PedirMiMano() { PedirMiManoRpc(); }

    // Corre en el server: arma la mano del que pidió y se la manda SOLO a él.
    [Rpc(SendTo.Server)]
    private void PedirMiManoRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numero = (identidad != null) ? identidad.NumeroDe(sender) : -1;
        if (numero < 0 || numero >= partida.Jugadores.Count) return;

        // Arma el texto de SU mano (solo nombres).
        string lista = "";
        foreach (Carta c in partida.Jugadores[numero].mano)
            lista += "• " + c.nombre + "\n";

        // FixedString: los string no se serializan solos en RPCs.
        FixedString512Bytes texto = lista;

        // ClientRpc DIRIGIDO: llega SOLO al clientId que pidió (su dueño).
        ManoPrivadaRpc(texto, RpcTarget.Single(sender, RpcTargetUse.Temp));
    }

    // Llega SOLO al dueño (por SendTo.SpecifiedInParams + RpcTarget.Single).
    [Rpc(SendTo.SpecifiedInParams)]
    private void ManoPrivadaRpc(FixedString512Bytes texto, RpcParams rpcParams)
    {
        if (textoMiMano != null) textoMiMano.text = "Tu mano:\n" + texto.ToString();
    }

    // ---------------- Jugadores ----------------
    private void AlCambiarJugadores(NetworkListEvent<EstadoJugador> evento) => MostrarJugadores();

    private void MostrarJugadores()
    {
        if (textoJugadores == null) return;
        string txt = "";
        for (int i = 0; i < jugadores.Count; i++)
            txt += "Jugador " + i + ": HP " + jugadores[i].hp + " · Monedas " + jugadores[i].monedas
                 + " · Cartas " + jugadores[i].cartas + "\n";
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
