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
using System.Collections.Generic;   // List<>
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
    public TMP_InputField inputCartas;   // índices de cartas a usar (ej: "0" o "0,2") · Etapa 6B
    public TMP_Text textoFin;         // cartel de fin de partida (el ganador) · Etapa 7

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
    private PilaDescarte descarte;   // pila de descarte (para reciclar el mazo si se vacía · Etapa 6)
    private GestorCartas gestorCartas;   // resuelve el efecto de las cartas: bonus, dados, multiplicador
    private bool yaActuo = false;   // ¿el jugador en turno ya hizo su acción? (1 acción por turno · 7C)

    void Awake()
    {
        jugadores = new NetworkList<EstadoJugador>();
    }

    public override void OnNetworkSpawn()
    {
        turnoActual.OnValueChanged += AlCambiarTurno;
        jugadores.OnListChanged += AlCambiarJugadores;

        // Nos enteramos cuando un cliente se cae (lo maneja el server · Etapa 7B).
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += ManejarDesconexion;

        // Solo el server crea la Partida real y la espeja a la red.
        if (IsServer)
        {
            partida = new Partida(cantidadJugadores, hpInicial);   // reusa /Rules tal cual
            dado = new Dado(Reglas.CarasDado);                      // el dado vive en el server
            mazo = FabricaDeCartas.CrearMazo();                     // el mazo también (reusa la Fábrica)
            mazo.Mezclar();
            descarte = new PilaDescarte();                          // para reciclar si el mazo se vacía
            gestorCartas = new GestorCartas(descarte);              // resuelve las cartas (reusa /Rules)
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
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= ManejarDesconexion;
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
    // Saltea las GRUPALES: no deben estar en la mano (se activan al comprarse, no al tenerlas).
    private void RepartirManoInicial(int cantidadPorJugador)
    {
        foreach (Jugador j in partida.Jugadores)
        {
            int dadas = 0;
            int intentos = 0;   // tope de seguridad
            while (dadas < cantidadPorJugador && !mazo.EstaVacio() && intentos < 100)
            {
                intentos++;
                Carta c = mazo.Robar();
                if (c is CartaGrupal) { descarte.Agregar(c); continue; }   // grupal → al descarte, no a la mano
                j.mano.Add(c);
                dadas++;
            }
        }
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

        if (partida.Terminada) return;   // la partida ya terminó

        partida.PasarTurno();                 // lógica real de /Rules (saltea muertos, cuenta rondas...)
        partida.ElegirObjetivoPorDefecto();   // re-elige el rival para el nuevo turno (como IniciarTurno)
        yaActuo = false;                      // nuevo turno: el jugador puede volver a actuar
        EspejarPartida();
        RevisarFin();                         // PasarTurno pudo terminar por límite de rondas
    }

    // ---------------- Acciones reales (3C) + usar cartas (6B) ----------------
    // Cada botón pide su acción, llevando QUÉ cartas usar (los índices del input).
    public void PedirAtacar()     { AccionRpc(TipoAccion.Atacar,     LeerSeleccion()); }
    public void PedirCurarse()    { AccionRpc(TipoAccion.Curarse,    LeerSeleccion()); }
    public void PedirRecolectar() { AccionRpc(TipoAccion.Recolectar, LeerSeleccion()); }

    // Lee del input los índices de cartas a usar: "0" o "0,2" (vacío = ninguna).
    private int[] LeerSeleccion()
    {
        if (inputCartas == null || string.IsNullOrWhiteSpace(inputCartas.text))
            return new int[0];
        List<int> indices = new List<int>();
        foreach (string p in inputCartas.text.Split(','))
            if (int.TryParse(p.Trim(), out int n)) indices.Add(n);
        return indices.ToArray();
    }

    [Rpc(SendTo.Server)]
    private void AccionRpc(TipoAccion tipo, int[] cartasUsadas, RpcParams rpcParams = default)
    {
        int numeroJugador = NumeroDelQuePide(rpcParams);

        // Mismo gating que el turno: solo el jugador en turno puede actuar.
        if (numeroJugador != partida.IndiceActual)
        {
            Debug.Log("[EstadoOnline] Acción rechazada: no es el turno del Jugador " + numeroJugador + ".");
            return;
        }

        if (partida.Terminada) return;   // la partida ya terminó: no se aceptan más acciones

        if (yaActuo) { Debug.Log("[EstadoOnline] Ya actuaste este turno; pasá el turno."); return; }

        // Ley Marcial (grupal): mientras está activa, solo se puede Atacar.
        if (partida.LeyMarcialActiva() && tipo != TipoAccion.Atacar)
        {
            Debug.Log("[EstadoOnline] Ley Marcial activa: este round solo se puede Atacar.");
            return;
        }

        Jugador jugador = partida.Jugadores[numeroJugador];
        gestorCartas.Mensajes.Clear();

        // Las cartas que el jugador eligió usar (por índice en SU mano).
        List<Carta> seleccionadas = new List<Carta>();
        foreach (int idx in cartasUsadas)
            if (idx >= 0 && idx < jugador.mano.Count) seleccionadas.Add(jugador.mano[idx]);

        // Igual que el GameManager, pero en el SERVER:
        Accion accion     = CrearAccion(tipo);                                // acción (polimorfismo)
        int dadosExtra    = gestorCartas.DadosExtra(seleccionadas, tipo);      // comodines de dado
        int multiplicador = gestorCartas.Multiplicador(seleccionadas, tipo);  // comodines de multiplicador
        int bonus         = gestorCartas.Bonus(seleccionadas, jugador, tipo); // bonus fijo de cartas

        int[] valores = TirarDados(accion.CantidadDados + dadosExtra);        // tira (base + extra)
        int total = Sumar(valores) * multiplicador + bonus;                   // total CON cartas
        int totalMostrar = total;                                             // para el cartel (antes de absorber)

        if (tipo == TipoAccion.Atacar)
        {
            // Defensivas del objetivo (6C): absorbe, recibe el daño restante, y refleja.
            Jugador defensor = partida.Objetivo();
            total = gestorCartas.Absorber(defensor, total);   // Escudo de Monedas (absorbe)
            accion.Aplicar(partida, total);                   // daño restante al objetivo
            gestorCartas.Reflejar(defensor, jugador);         // Espejo de Sangre / Vampirismo Defensivo
            gestorCartas.DescartarUsadas(defensor);           // descarta las defensivas usadas del objetivo
        }
        else
        {
            accion.Aplicar(partida, total);                   // Curarse / Recolectar (a uno mismo)
        }

        gestorCartas.DescartarUsadas(jugador);                // descarta las de un solo uso del que actuó
        yaActuo = true;                                       // una acción por turno (7C)

        MostrarTiradaRpc(numeroJugador, tipo, valores, totalMostrar);   // difunde la tirada (dados + total final)
        EspejarPartida();                                     // estado público (HP, monedas, cantidad)
        EnviarManoATodos();                                   // varias manos pudieron cambiar (atacante y defensor)
        RevisarFin();                                         // ¿quedó un solo vivo? → fin
    }

    // Etapa 4: el server DIFUNDE la tirada a TODOS (patrón inverso al ServerRpc: server → todos).
    [Rpc(SendTo.Everyone)]
    private void MostrarTiradaRpc(int numeroJugador, TipoAccion tipo, int[] valores, int totalFinal)
    {
        if (textoTirada == null) return;
        string detalle = "";
        int sumaDados = 0;
        for (int i = 0; i < valores.Length; i++)
        {
            detalle += valores[i];
            sumaDados += valores[i];
            if (i < valores.Length - 1) detalle += " + ";
        }
        string txt = "Jugador " + numeroJugador + " — " + tipo + ":  " + detalle + " = " + sumaDados;
        if (totalFinal != sumaDados) txt += "  →  " + totalFinal + " (con cartas)";   // bonus/multiplicador
        textoTirada.text = txt;
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
        EnviarManoA(sender, numero);
    }

    // Arma la mano del jugador 'numero' y se la manda SOLO a 'clientId' (ClientRpc dirigido).
    // Se usa al pedir la mano (5B) y después de comprar (6A).
    private void EnviarManoA(ulong clientId, int numero)
    {
        if (numero < 0 || numero >= partida.Jugadores.Count) return;

        List<Carta> mano = partida.Jugadores[numero].mano;
        string lista = "";
        for (int i = 0; i < mano.Count; i++)
            lista += i + ") " + mano[i].nombre + "\n";   // el número es el índice para usarla (6B)

        FixedString512Bytes texto = lista;   // los string no se serializan solos en RPCs
        ManoPrivadaRpc(texto, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    // Le reenvía a CADA jugador conectado su mano privada (cuando varias manos pudieron cambiar · 6C).
    private void EnviarManoATodos()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int numero = (identidad != null) ? identidad.NumeroDe(clientId) : -1;
            EnviarManoA(clientId, numero);
        }
    }

    // Llega SOLO al dueño (por SendTo.SpecifiedInParams + RpcTarget.Single).
    [Rpc(SendTo.SpecifiedInParams)]
    private void ManoPrivadaRpc(FixedString512Bytes texto, RpcParams rpcParams)
    {
        if (textoMiMano != null) textoMiMano.text = "Tu mano:\n" + texto.ToString();
    }

    // ---------------- Comprar carta (6A) ----------------
    // Lo llama el botón "Comprar": le pide al server comprar una carta.
    public void PedirComprar() { ComprarRpc(); }

    [Rpc(SendTo.Server)]
    private void ComprarRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numero = (identidad != null) ? identidad.NumeroDe(sender) : -1;
        if (numero != partida.IndiceActual)
        {
            Debug.Log("[EstadoOnline] Compra rechazada: no es el turno del Jugador " + numero + ".");
            return;
        }

        if (partida.Terminada) return;   // la partida ya terminó

        Jugador j = partida.Jugadores[numero];
        if (j.monedas < Reglas.CostoCarta)     { Debug.Log("[EstadoOnline] Sin monedas para comprar."); return; }
        if (j.mano.Count >= Reglas.ManoMaxima) { Debug.Log("[EstadoOnline] Mano llena.");               return; }

        if (mazo.EstaVacio()) mazo.Reciclar(descarte);   // mazo circular (reusa /Data)
        Carta comprada = mazo.Robar();
        j.GastarMonedas(Reglas.CostoCarta);

        // Grupal: se activa al instante para TODOS y NO va a la mano (6C).
        CartaGrupal grupal = comprada as CartaGrupal;
        if (grupal != null)
        {
            ContextoGrupal ctx = new ContextoGrupal {
                jugadores = partida.Jugadores, mazo = mazo, descarte = descarte, comprador = j, partida = partida
            };
            grupal.AplicarATodos(ctx);                 // efecto grupal (reusa /Rules)
            descarte.Agregar(comprada);
            Debug.Log("[EstadoOnline] Se jugó la carta grupal: " + grupal.nombre + ".");
        }
        else
        {
            j.mano.Add(comprada);   // carta normal: va a la mano
        }

        EspejarPartida();       // monedas y cantidad de cartas → público (todos lo ven)
        EnviarManoATodos();     // una grupal pudo cambiar las manos de todos
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
        RevisarFin();   // así el botón de prueba también puede terminar la partida
    }

    // ---------------- Fin de partida (7A) ----------------
    // Revisa si hay ganador y, si la partida terminó, lo anuncia a todos.
    private void RevisarFin()
    {
        partida.VerificarVictoria();   // marca Terminada si queda un solo vivo
        if (partida.Terminada)         // (o se acabaron las rondas, que ya lo marca PasarTurno)
        {
            int ganador = (partida.Ganador != null) ? partida.Jugadores.IndexOf(partida.Ganador) : -1;
            FinRpc(ganador);
        }
    }

    // Anuncia el fin a TODOS (server → todos). Usa el índice (0,1,2...) igual que el resto de la UI.
    [Rpc(SendTo.Everyone)]
    private void FinRpc(int numeroGanador)
    {
        if (textoFin == null) return;
        textoFin.text = (numeroGanador >= 0)
            ? "FIN DE LA PARTIDA · Ganó el Jugador " + numeroGanador
            : "FIN DE LA PARTIDA";
    }

    // ---------------- Desconexiones (7B) ----------------
    // Lo llama NetworkManager cuando un cliente se cae (corre en el server).
    // El que se va queda ELIMINADO (HP a 0) → reusa la lógica de "muerto" de /Rules.
    private void ManejarDesconexion(ulong clientId)
    {
        if (!IsServer || partida == null || partida.Terminada) return;

        int numero = (identidad != null) ? identidad.NumeroDe(clientId) : -1;
        if (numero < 0 || numero >= partida.Jugadores.Count) return;

        Jugador jugador = partida.Jugadores[numero];
        if (!jugador.EstaVivo()) return;   // ya estaba fuera

        jugador.RecibirDanio(jugador.hp);   // lo dejamos en 0 HP (eliminado)
        Debug.Log("[EstadoOnline] El Jugador " + numero + " se desconectó → eliminado.");

        // Si justo era su turno, pasamos al siguiente vivo (si no, la partida quedaría trabada).
        if (partida.IndiceActual == numero)
        {
            partida.PasarTurno();
            partida.ElegirObjetivoPorDefecto();
        }

        EspejarPartida();
        RevisarFin();   // puede quedar un solo vivo → fin
    }
}
