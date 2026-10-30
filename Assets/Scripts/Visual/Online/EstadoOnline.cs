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
using UnityEngine.SceneManagement;   // volver al menú (B2.4)
using TMPro;

// El estado de UN jugador que viaja por la red.
//  - INetworkSerializable: le dice a Netcode CÓMO mandarlo (qué campos).
//  - IEquatable: para detectar si un elemento cambió (y no sincronizar de más).
public struct EstadoJugador : INetworkSerializable, System.IEquatable<EstadoJugador>
{
    public int hp;
    public int monedas;
    public int cartas;      // cuántas cartas en mano (el CONTENIDO es privado · Etapa 5)
    public int personaje;   // índice del personaje elegido en el lobby (C3)
    public ulong clientId;  // de quién es este jugador: cada cliente reconoce SU panel (vista egocéntrica · B2)
    public FixedString32Bytes nombre;   // nombre escrito en el lobby (se muestra en el panel · B2)

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref monedas);
        serializer.SerializeValue(ref cartas);
        serializer.SerializeValue(ref personaje);
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref nombre);
    }

    public bool Equals(EstadoJugador otro) =>
        hp == otro.hp && monedas == otro.monedas && cartas == otro.cartas
        && personaje == otro.personaje && clientId == otro.clientId && nombre.Equals(otro.nombre);
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
    public TMP_Text textoResultado;   // resultado de MI última acción (lo manda el server · B3.2)

    [Header("Animaciones (B2.3) · opcionales")]
    public AnimacionDados animacionDados;   // animación tipo casino de los dados (reusada de HotSeat)
    [Tooltip("Si NO hay animación de dados asignada, cuánto espera el server antes de aplicar el efecto.")]
    public float duracionResolucionFallback = 1.5f;   // respaldo del timing (B4)

    [Header("Fin de partida (B2.4) · opcionales")]
    public GameObject panelFin;           // panel que aparece al terminar (arranca APAGADO en la escena)
    public TMP_Text textoGanador;         // título del panel: "Ganó X"
    public TMP_Text textoEstadisticas;    // tabla con el estado final de todos los jugadores
    public string escenaMenu = "MENU";    // a dónde vuelve el botón "Volver al menú"

    [Header("Partida (la crea el server)")]
    public int cantidadJugadores = 2;
    public int hpInicial = 40;

    // --- Estado sincronizado (server escribe, todos leen) ---
    public NetworkVariable<int> turnoActual = new NetworkVariable<int>(0);
    public NetworkVariable<int> codigoSala = new NetworkVariable<int>(0);   // código de la sala (B3.1)
    public NetworkVariable<int> objetivoActual = new NetworkVariable<int>(0);  // a quién apunta el turno (B2)
    public NetworkVariable<int> hpMaximoSala = new NetworkVariable<int>(40);   // HP máx (para las barras · B2)
    // Nº de partida: sube con cada (re)inicio. La UI lo usa para resetear su estado visual en la revancha
    // (que el salto de HP al reiniciar NO dispare números flotantes). Declarado ANTES de 'jugadores'
    // para que, al llegar un cambio, ya esté actualizado cuando la UI reaccione a la lista.
    public NetworkVariable<int> generacionPartida = new NetworkVariable<int>(0);
    public NetworkList<EstadoJugador> jugadores;

    // La Partida REAL (de /Rules). Solo existe en el server; el cliente la deja en null.
    private IPartida partida;
    private Dado dado;   // el dado del server: las tiradas corren acá (nadie las puede falsear)
    private Mazo mazo;   // el mazo del server (de acá se reparten las cartas · Etapa 5)
    private PilaDescarte descarte;   // pila de descarte (para reciclar el mazo si se vacía · Etapa 6)
    private GestorCartas gestorCartas;   // resuelve el efecto de las cartas: bonus, dados, multiplicador
    private bool yaActuo = false;   // ¿el jugador en turno ya hizo su acción? (1 acción por turno · 7C)

    // --- Mi mano (B2.2): el cliente la guarda como NOMBRES de carta (para dibujar los sprites) ---
    public readonly List<string> MiMano = new List<string>();   // nombres de MIS cartas, en orden
    public event System.Action MiManoCambio;                    // avisa a la UI de cartas cuando cambia
    private readonly List<int> seleccion = new List<int>();     // índices elegidos por click

    // --- Flujo del turno en 2 PASOS (como el HotSeat): primero ELEGÍS la acción (en el cliente),
    //     y recién al tocar "Lanzar dados" se manda al server. Así podés cambiar el objetivo o
    //     elegir cartas ANTES de comprometer la jugada (sin el paso intermedio no tiene sentido
    //     "cambiar objetivo", porque el ataque se resolvería de un solo clic). ---
    private TipoAccion accionElegida;   // acción elegida en el cliente (todavía NO enviada al server)
    private bool haElegido = false;     // ¿ya elegí una acción este turno?

    // Server: hay una tirada "resolviéndose" (los dados están girando). Mientras tanto NO se acepta
    // otra acción, así el efecto se aplica RECIÉN cuando los dados paran (igual que el HotSeat).
    private bool resolviendo = false;

    private bool saliendo = false;   // true cuando ya estoy volviendo al menú (evita re-entrar / procesar en el shutdown)

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
            codigoSala.Value = (SesionRed.Instancia != null) ? SesionRed.Instancia.CodigoSala : 0;   // sala (B3.1)
            IniciarPartidaServidor();   // crea la Partida, reparte y espeja (se reusa en la revancha · B2.4)
        }

        MostrarTurno(turnoActual.Value);
        MostrarJugadores();
    }

    // Crea (o recrea) la Partida real en el server, reparte la mano inicial y la espeja.
    // Se usa al spawnear y también en la REVANCHA (B2.4). codigoSala NO se toca (misma sala).
    private void IniciarPartidaServidor()
    {
        if (!IsServer) return;

        // La cantidad sale de la SALA (cuántos se conectaron). Si no hay sesión, usa el default.
        int cant = (SesionRed.Instancia != null) ? SesionRed.Instancia.CantidadJugadores : cantidadJugadores;
        // Config elegida por el host en el lobby (B3). Defensivo: HP nunca menor a 1, rondas nunca negativas.
        int hp = Mathf.Max(1, (SesionRed.Instancia != null) ? SesionRed.Instancia.HpInicialConfig : hpInicial);
        int rondas = (SesionRed.Instancia != null) ? Mathf.Max(0, SesionRed.Instancia.RondasConfig) : 0;
        partida = new Partida(cant, hp, rondas);   // reusa /Rules (ahora HP y rondas son configurables)
        hpMaximoSala.Value = hp;                   // para calcular el llenado de las barras (B2)
        dado = new Dado(Reglas.CarasDado);                      // el dado vive en el server
        mazo = FabricaDeCartas.CrearMazo();                     // el mazo también (reusa la Fábrica)
        mazo.Mezclar();
        descarte = new PilaDescarte();                          // para reciclar si el mazo se vacía
        gestorCartas = new GestorCartas(descarte);              // resuelve las cartas (reusa /Rules)
        yaActuo = false;                                        // nadie actuó todavía en esta partida
        RepartirManoInicial(3);                                 // 3 cartas a cada jugador
        generacionPartida.Value++;                              // nueva partida → la UI resetea su estado visual
        EspejarPartida();
    }

    public override void OnNetworkDespawn()
    {
        turnoActual.OnValueChanged -= AlCambiarTurno;
        jugadores.OnListChanged -= AlCambiarJugadores;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= ManejarDesconexion;
    }

    // Personaje elegido por el jugador 'i' (viene del lobby, por SesionRed).
    private int PersonajeDe(int i) => (SesionRed.Instancia != null) ? SesionRed.Instancia.PersonajeDe(i) : 0;

    // clientId del jugador 'i' (del roster del server). Sirve para que cada cliente reconozca su panel.
    private ulong ClienteDe(int i) => (SesionRed.Instancia != null) ? SesionRed.Instancia.ClientIdDe(i) : 0;

    // Nombre del jugador 'i' (del lobby, por SesionRed). Vacío si no eligió uno.
    private FixedString32Bytes NombreDe(int i)
    {
        string n = (SesionRed.Instancia != null) ? SesionRed.Instancia.NombreDe(i) : "";
        return n;   // string → FixedString32Bytes (el nombre ya viene acotado del lobby)
    }

    // Nombre a mostrar del jugador 'i' leyendo la lista sincronizada (sirve en cualquier cliente). Fallback "Jugador i".
    private string NombreVisible(int i)
    {
        if (i >= 0 && i < jugadores.Count)
        {
            string n = jugadores[i].nombre.ToString();
            if (!string.IsNullOrEmpty(n)) return n;
        }
        return "Jugador " + i;
    }

    // Copia el estado de la Partida real a las variables de red (solo server).
    private void EspejarPartida()
    {
        if (!IsServer || partida == null) return;

        turnoActual.Value = partida.IndiceActual;       // de quién es el turno
        objetivoActual.Value = partida.IndiceObjetivo;  // a quién apunta (para resaltar · B2)

        // HP y monedas de cada jugador.
        if (jugadores.Count != partida.Jugadores.Count)
        {
            // Primera vez: armamos la lista de red con los jugadores reales.
            jugadores.Clear();
            for (int i = 0; i < partida.Jugadores.Count; i++)
            {
                Jugador j = partida.Jugadores[i];
                jugadores.Add(new EstadoJugador { hp = j.hp, monedas = j.monedas, cartas = j.mano.Count, personaje = PersonajeDe(i), clientId = ClienteDe(i), nombre = NombreDe(i) });
            }
        }
        else
        {
            // Ya existe: actualizamos solo lo que cambió (por eso el struct es IEquatable).
            for (int i = 0; i < partida.Jugadores.Count; i++)
            {
                Jugador j = partida.Jugadores[i];
                EstadoJugador nuevo = new EstadoJugador { hp = j.hp, monedas = j.monedas, cartas = j.mano.Count, personaje = PersonajeDe(i), clientId = ClienteDe(i), nombre = NombreDe(i) };
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
    private void AlCambiarTurno(int anterior, int nuevo)
    {
        haElegido = false;   // turno nuevo: hay que volver a elegir una acción (se limpia la elección local)
        MostrarTurno(nuevo);
    }

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
        ulong sender = rpcParams.Receive.SenderClientId;
        int numeroJugador = NumeroDelQuePide(rpcParams);

        // GATING (3B): solo el jugador EN TURNO puede avanzar. Si no, se rechaza el pedido.
        if (numeroJugador != partida.IndiceActual) { AvisarAlEmisor(sender, "Rechazado: no es tu turno."); return; }

        if (partida.Terminada) { AvisarAlEmisor(sender, "Rechazado: la partida terminó."); return; }

        // No pasar el turno mientras los dados giran (si no, el efecto se aplicaría sobre otro turno · B4).
        if (resolviendo) { AvisarAlEmisor(sender, "Esperá: los dados están girando."); return; }

        partida.PasarTurno();                 // lógica real de /Rules (saltea muertos, cuenta rondas...)
        partida.ElegirObjetivoPorDefecto();   // re-elige el rival para el nuevo turno (como IniciarTurno)
        yaActuo = false;                      // nuevo turno: el jugador puede volver a actuar
        AvisarAlEmisor(sender, "OK: pasaste el turno.");
        EspejarPartida();
        RevisarFin();                         // PasarTurno pudo terminar por límite de rondas
    }

    // ---------------- Acciones: flujo en 2 pasos (B4) ----------------
    // PASO 1 — los botones de acción AHORA solo ELIGEN (no ejecutan). El server se entera recién
    //           al "Lanzar dados". Así se puede cambiar el objetivo o elegir cartas antes de tirar.
    public void PedirAtacar()     { ElegirAccionLocal(TipoAccion.Atacar); }
    public void PedirCurarse()    { ElegirAccionLocal(TipoAccion.Curarse); }
    public void PedirRecolectar() { ElegirAccionLocal(TipoAccion.Recolectar); }
    public void PedirDescartar()  { ElegirAccionLocal(TipoAccion.Descartar); }

    // Guarda la acción elegida EN EL CLIENTE y avisa qué hacer después (no toca la red todavía).
    private void ElegirAccionLocal(TipoAccion tipo)
    {
        accionElegida = tipo;
        haElegido = true;
        if (textoResultado != null)
        {
            if (tipo == TipoAccion.Descartar)
                textoResultado.text = "Descartar: tocá la carta que querés tirar (perdés el turno).";
            else
                textoResultado.text = "Elegiste " + tipo + ". Cambiá el objetivo o elegí cartas, y lanzá los dados.";
        }
    }

    // PASO 3 — Botón "Lanzar dados": ahora SÍ manda la acción al server, con las cartas seleccionadas.
    public void PedirLanzarDados()
    {
        if (!haElegido) { if (textoResultado != null) textoResultado.text = "Primero elegí una acción."; return; }
        if (accionElegida == TipoAccion.Descartar) { if (textoResultado != null) textoResultado.text = "Para descartar, tocá la carta que querés tirar."; return; }
        AccionRpc(accionElegida, LeerSeleccion());
    }

    // Botón "Cambiar objetivo": rota a quién apunta el ataque (lo valida y aplica el server · B4).
    public void PedirCambiarObjetivo() { CambiarObjetivoRpc(); }

    // --- Selección de cartas por CLICK (B2.2): la maneja la UI de la mano (ManoOnline) ---
    // La mano llama a ClickCarta; según la acción elegida, descarta esa carta o la selecciona.
    public void ClickCarta(int indice)
    {
        if (haElegido && accionElegida == TipoAccion.Descartar) { PedirDescartarCarta(indice); return; }
        AlternarSeleccion(indice);
    }

    public void AlternarSeleccion(int indice)
    {
        if (indice < 0 || indice >= MiMano.Count) return;
        if (seleccion.Contains(indice)) seleccion.Remove(indice);
        else seleccion.Add(indice);
    }
    public bool EstaSeleccionada(int indice) => seleccion.Contains(indice);

    // Índices de cartas a usar. Prioridad: la selección por click; si no hay, el input de texto (fallback).
    private int[] LeerSeleccion()
    {
        if (seleccion.Count > 0) return seleccion.ToArray();   // B2.2: click

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
        ulong sender = rpcParams.Receive.SenderClientId;
        int numeroJugador = NumeroDelQuePide(rpcParams);

        // Mismo gating que el turno: solo el jugador en turno puede actuar.
        if (numeroJugador != partida.IndiceActual) { AvisarAlEmisor(sender, "Rechazado: no es tu turno."); return; }

        if (partida.Terminada) { AvisarAlEmisor(sender, "Rechazado: la partida terminó."); return; }

        if (yaActuo) { AvisarAlEmisor(sender, "Rechazado: ya actuaste este turno."); return; }

        // Mientras los dados giran (resolución en curso) no se acepta otra acción.
        if (resolviendo) { AvisarAlEmisor(sender, "Esperá: los dados están girando."); return; }

        // Descartar NO pasa por acá (se hace tocando una carta → DescartarRpc).
        if (tipo == TipoAccion.Descartar) { AvisarAlEmisor(sender, "Descartar se hace tocando una carta."); return; }

        // Ley Marcial (grupal): mientras está activa, solo se puede Atacar.
        if (partida.LeyMarcialActiva() && tipo != TipoAccion.Atacar)
        {
            AvisarAlEmisor(sender, "Rechazado: Ley Marcial, solo Atacar.");
            return;
        }

        Jugador jugador = partida.Jugadores[numeroJugador];
        gestorCartas.Mensajes.Clear();

        // Las cartas que el jugador eligió usar (por índice en SU mano).
        List<Carta> seleccionadas = new List<Carta>();
        foreach (int idx in cartasUsadas)
            if (idx >= 0 && idx < jugador.mano.Count) seleccionadas.Add(jugador.mano[idx]);

        // Igual que el GameManager, pero en el SERVER: calculamos TODO ahora (tirada + total con cartas)...
        Accion accion     = CrearAccion(tipo);                                // acción (polimorfismo)
        int dadosExtra    = gestorCartas.DadosExtra(seleccionadas, tipo);      // comodines de dado
        int multiplicador = gestorCartas.Multiplicador(seleccionadas, tipo);  // comodines de multiplicador
        int bonus         = gestorCartas.Bonus(seleccionadas, jugador, tipo); // bonus fijo de cartas

        int[] valores = TirarDados(accion.CantidadDados + dadosExtra);        // tira (base + extra)
        int total = Sumar(valores) * multiplicador + bonus;                   // total CON cartas (antes de defensas)

        // ...pero el EFECTO se aplica RECIÉN cuando los dados paran (como el callback del HotSeat).
        yaActuo = true;        // una acción por turno (7C)
        resolviendo = true;    // bloquea otra acción / pasar turno mientras gira la animación

        // 1) Difundimos la tirada a TODOS → arranca la animación de dados en cada cliente.
        MostrarTiradaRpc(numeroJugador, tipo, valores, total);

        // 2) El server espera lo que dura la animación y después aplica el efecto + espeja (HP baja al final).
        StartCoroutine(AplicarTrasAnimacion(sender, tipo, accion, jugador, total, valores.Length));
    }

    // Espera a que terminen de girar los dados y RECIÉN AHÍ aplica el efecto y espeja el estado público.
    // Así, en todas las pantallas, la barra de HP baja y los números flotantes salen cuando los dados paran.
    private System.Collections.IEnumerator AplicarTrasAnimacion(ulong sender, TipoAccion tipo, Accion accion, Jugador jugador, int total, int cantidadDados)
    {
        yield return new WaitForSeconds(DuracionAnimacion(cantidadDados));

        // Si mientras tanto cerramos / reiniciamos, no aplicamos nada (evita tocar una Partida vieja).
        if (saliendo || partida == null || partida.Terminada) { resolviendo = false; yield break; }

        int totalMostrar = total;   // para el aviso al emisor (el total ANTES de absorber)

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

        AvisarAlEmisor(sender, "OK: " + tipo + " (total " + totalMostrar + ").");
        EspejarPartida();        // estado público (HP, monedas, cantidad) → recién ahora cambian las barras
        EnviarManoATodos();      // varias manos pudieron cambiar (atacante y defensor)
        RevisarFin();            // ¿quedó un solo vivo? → fin
        resolviendo = false;     // listo: ya se puede actuar de nuevo (próximo turno)
    }

    // Cuánto dura la animación de dados (para esperar antes de aplicar). Si no hay animación, usa el fallback.
    // + un pequeño margen para que el HP cambie justo DESPUÉS de que los dados visualmente se asientan.
    private float DuracionAnimacion(int cantidadDados)
    {
        if (animacionDados != null)
            return animacionDados.duracionGiro + cantidadDados * animacionDados.delayEntreDados + 0.1f;
        return duracionResolucionFallback;
    }

    // ---------------- Cambiar objetivo (B4) ----------------
    // Solo el jugador en turno, antes de actuar, puede rotar a quién apunta. El resaltado se mueve en TODOS.
    [Rpc(SendTo.Server)]
    private void CambiarObjetivoRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numeroJugador = NumeroDelQuePide(rpcParams);

        if (numeroJugador != partida.IndiceActual) { AvisarAlEmisor(sender, "Rechazado: no es tu turno."); return; }
        if (partida.Terminada) { AvisarAlEmisor(sender, "Rechazado: la partida terminó."); return; }
        if (resolviendo) { AvisarAlEmisor(sender, "Esperá: los dados están girando."); return; }

        partida.CambiarObjetivo();   // regla de /Rules: rota al próximo rival vivo
        AvisarAlEmisor(sender, "Apuntás a " + NombreVisible(partida.IndiceObjetivo) + ".");
        EspejarPartida();            // objetivoActual cambia → el resaltado se mueve en todos los clientes
    }

    // ---------------- Descartar (B4) ----------------
    // La UI de la mano llama acá al tocar una carta cuando la acción elegida es Descartar.
    public void PedirDescartarCarta(int indice) { DescartarRpc(indice); }

    [Rpc(SendTo.Server)]
    private void DescartarRpc(int indice, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numero = NumeroDelQuePide(rpcParams);

        if (numero != partida.IndiceActual) { AvisarAlEmisor(sender, "Rechazado: no es tu turno."); return; }
        if (partida.Terminada) { AvisarAlEmisor(sender, "Rechazado: la partida terminó."); return; }
        if (yaActuo) { AvisarAlEmisor(sender, "Rechazado: ya actuaste este turno."); return; }
        if (resolviendo) { AvisarAlEmisor(sender, "Esperá: los dados están girando."); return; }

        Jugador j = partida.Jugadores[numero];
        if (indice < 0 || indice >= j.mano.Count) { AvisarAlEmisor(sender, "Rechazado: ahí no tenés carta."); return; }

        Carta carta = j.mano[indice];
        j.mano.RemoveAt(indice);      // la carta sale de la mano
        descarte.Agregar(carta);      // va a la pila de descarte (para reciclar · como en el HotSeat)
        yaActuo = true;               // descartar consume la acción del turno
        AvisarAlEmisor(sender, "OK: descartaste " + carta.nombre + ". Pasá el turno.");
        EspejarPartida();             // cambió la cantidad de cartas (público)
        EnviarManoATodos();           // y mi mano privada
    }

    // Etapa 4: el server DIFUNDE la tirada a TODOS (patrón inverso al ServerRpc: server → todos).
    [Rpc(SendTo.Everyone)]
    private void MostrarTiradaRpc(int numeroJugador, TipoAccion tipo, int[] valores, int totalFinal)
    {
        // Dados animados (casino): los ve TODO el mundo con los mismos valores del server.
        if (animacionDados != null && valores != null && valores.Length > 0)
            animacionDados.IniciarAnimacion(valores, null);

        if (textoTirada == null) return;
        string detalle = "";
        int sumaDados = 0;
        for (int i = 0; i < valores.Length; i++)
        {
            detalle += valores[i];
            sumaDados += valores[i];
            if (i < valores.Length - 1) detalle += " + ";
        }
        string txt = NombreVisible(numeroJugador) + " — " + tipo + ":  " + detalle + " = " + sumaDados;
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
        return (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(sender) : -1;
    }

    // ---------------- Avisar el resultado al emisor (B3.2) ----------------
    // El server le manda el resultado (válido/inválido) SOLO al cliente que pidió la acción.
    private void AvisarAlEmisor(ulong clientId, string mensaje)
    {
        Debug.Log("[EstadoOnline] → cliente " + clientId + ": " + mensaje);
        FixedString128Bytes msg = mensaje;
        ResultadoRpc(msg, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    // Llega SOLO al cliente que mandó la acción (ClientRpc dirigido).
    [Rpc(SendTo.SpecifiedInParams)]
    private void ResultadoRpc(FixedString128Bytes mensaje, RpcParams rpcParams)
    {
        if (textoResultado != null) textoResultado.text = mensaje.ToString();
    }

    // ---------------- Mano privada (5B) ----------------
    // Lo llama el botón "Ver mi mano": le pide al server MI mano.
    public void PedirMiMano() { PedirMiManoRpc(); }

    // Corre en el server: arma la mano del que pidió y se la manda SOLO a él.
    [Rpc(SendTo.Server)]
    private void PedirMiManoRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numero = (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(sender) : -1;
        EnviarManoA(sender, numero);
    }

    // Arma la mano del jugador 'numero' y se la manda SOLO a 'clientId' (ClientRpc dirigido).
    // Se usa al pedir la mano (5B) y después de comprar (6A).
    private void EnviarManoA(ulong clientId, int numero)
    {
        if (numero < 0 || numero >= partida.Jugadores.Count) return;

        List<Carta> mano = partida.Jugadores[numero].mano;
        // Nombres de las cartas separados por '|' (los nombres no usan '|'): el cliente los parte y busca el sprite.
        string nombres = "";
        for (int i = 0; i < mano.Count; i++)
        {
            nombres += mano[i].nombre;
            if (i < mano.Count - 1) nombres += "|";
        }

        FixedString512Bytes payload = nombres;   // los string no se serializan solos en RPCs
        ManoPrivadaRpc(payload, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    // Le reenvía a CADA jugador conectado su mano privada (cuando varias manos pudieron cambiar · 6C).
    private void EnviarManoATodos()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int numero = (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(clientId) : -1;
            EnviarManoA(clientId, numero);
        }
    }

    // Llega SOLO al dueño (por SendTo.SpecifiedInParams + RpcTarget.Single).
    [Rpc(SendTo.SpecifiedInParams)]
    private void ManoPrivadaRpc(FixedString512Bytes payload, RpcParams rpcParams)
    {
        // Reconstruye mi mano (nombres separados por '|').
        List<string> nueva = new List<string>();
        string s = payload.ToString();
        if (!string.IsNullOrEmpty(s))
            foreach (string n in s.Split('|'))
                if (!string.IsNullOrEmpty(n)) nueva.Add(n);

        // Si la mano CAMBIÓ (usé/compré cartas), la selección anterior ya no vale.
        if (ManoCambio(nueva)) seleccion.Clear();

        MiMano.Clear();
        MiMano.AddRange(nueva);

        // Texto opcional: si quedó asignado el textoMiMano viejo, lo seguimos llenando como ayuda.
        if (textoMiMano != null)
        {
            string txt = "";
            for (int i = 0; i < MiMano.Count; i++) txt += i + ") " + MiMano[i] + "\n";
            textoMiMano.text = "Tu mano:\n" + txt;
        }

        MiManoCambio?.Invoke();   // la UI de cartas (ManoOnline) se redibuja
    }

    // ¿La mano nueva es distinta a la que ya tenía? (para saber si invalidar la selección)
    private bool ManoCambio(List<string> nueva)
    {
        if (nueva.Count != MiMano.Count) return true;
        for (int i = 0; i < nueva.Count; i++)
            if (nueva[i] != MiMano[i]) return true;
        return false;
    }

    // ---------------- Comprar carta (6A) ----------------
    // Lo llama el botón "Comprar": le pide al server comprar una carta.
    public void PedirComprar() { ComprarRpc(); }

    [Rpc(SendTo.Server)]
    private void ComprarRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        int numero = (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(sender) : -1;
        if (numero != partida.IndiceActual) { AvisarAlEmisor(sender, "Rechazado: no es tu turno."); return; }

        if (partida.Terminada) { AvisarAlEmisor(sender, "Rechazado: la partida terminó."); return; }

        // No comprar mientras los dados giran (las monedas de Recolectar se aplican recién al final · B4).
        if (resolviendo) { AvisarAlEmisor(sender, "Esperá: los dados están girando."); return; }

        Jugador j = partida.Jugadores[numero];
        if (j.monedas < Reglas.CostoCarta)     { AvisarAlEmisor(sender, "Rechazado: te faltan monedas (cuesta " + Reglas.CostoCarta + ")."); return; }
        if (j.mano.Count >= Reglas.ManoMaxima) { AvisarAlEmisor(sender, "Rechazado: tu mano está llena."); return; }

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

        AvisarAlEmisor(sender, "OK: compraste una carta.");
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
            txt += "Jugador " + i + " (P" + jugadores[i].personaje + "): HP " + jugadores[i].hp
                 + " · Monedas " + jugadores[i].monedas + " · Cartas " + jugadores[i].cartas + "\n";
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
        string titulo = (numeroGanador >= 0) ? "Ganó " + NombreVisible(numeroGanador) : "Sin ganador";

        if (textoFin != null) textoFin.text = "FIN DE LA PARTIDA · " + titulo;   // texto inline viejo (compat)
        if (textoGanador != null) textoGanador.text = titulo;

        // Estadísticas finales de TODOS (desde la lista sincronizada): solo nombre + HP.
        if (textoEstadisticas != null)
        {
            string stats = "";
            for (int i = 0; i < jugadores.Count; i++)
            {
                EstadoJugador j = jugadores[i];
                string estado = (j.hp > 0) ? (j.hp + " HP") : "eliminado";
                stats += NombreVisible(i) + " · " + estado + "\n";
            }
            textoEstadisticas.text = stats;
        }

        if (panelFin != null) panelFin.SetActive(true);
    }

    // ---------------- Botones del panel de fin (B2.4) ----------------
    // "Volver al menú": me desconecto de la red y cargo el menú. Local a cada quien
    // (si se va el host, la red se cae y los clientes vuelven también).
    public void VolverAlMenu()
    {
        if (saliendo) return;   // ya estoy saliendo (evita doble carga / re-entrada)
        saliendo = true;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();   // corta la red (si soy host, se cae para todos)
        SceneManager.LoadScene(escenaMenu);
    }

    // "Revancha": nueva partida SIN desconectar a nadie. Lo puede apretar cualquiera; el server decide.
    public void PedirReiniciar() { ReiniciarRpc(); }

    // Corre en el server: recrea la partida y avisa a todos que oculten el panel de fin.
    [Rpc(SendTo.Server)]
    private void ReiniciarRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || partida == null || !partida.Terminada) return;   // solo desde una partida terminada
        IniciarPartidaServidor();   // nueva Partida + reparte + nueva "generación" + espeja
        EnviarManoATodos();         // empuja la mano nueva a cada cliente
        OcultarFinRpc();            // todos ocultan el panel de fin y limpian textos
    }

    // Todos ocultan el panel de fin y limpian los textos de la partida anterior.
    [Rpc(SendTo.Everyone)]
    private void OcultarFinRpc()
    {
        if (panelFin != null) panelFin.SetActive(false);
        if (textoFin != null) textoFin.text = "";
        if (textoTirada != null) textoTirada.text = "";
        if (textoResultado != null) textoResultado.text = "";
    }

    // ---------------- Desconexiones (7B · B2.4) ----------------
    // Lo llama NetworkManager cuando se cae una conexión.
    //  · En un CLIENTE: si pierdo al host, no hay server para seguir → vuelvo al menú solo.
    //  · En el SERVER: el cliente que se va queda ELIMINADO (HP a 0) → reusa la lógica de "muerto" de /Rules.
    private void ManejarDesconexion(ulong clientId)
    {
        // Cliente: cualquier desconexión que me llega es la MÍA (perdí al host o me cortaron) → al menú.
        if (!IsServer)
        {
            VolverAlMenu();
            return;
        }

        // Server: si ya estoy saliendo, no hay partida, o ya terminó → nada que hacer.
        if (saliendo || partida == null || partida.Terminada) return;

        int numero = (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(clientId) : -1;
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
