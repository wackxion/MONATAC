// ============================================================
//  SesionRed.cs  —  Capa: VISUAL (red · persistente)
//  El "cerebro de red" que vive del MENÚ al JUEGO (DontDestroyOnLoad).
//  Absorbe lo que antes hacían RedMonatac (crear/unirse) e
//  IdentidadJugadores (quién es quién), pero AHORA la conexión pasa
//  en el lobby, antes de cargar la escena del juego. Por eso tiene
//  que ser persistente: si viviera en la escena Online, se perdería
//  todo al cambiar de escena y llegaría tarde a las conexiones.
//
//  Singleton accesible con SesionRed.Instancia desde cualquier lado.
// ============================================================

using System.Collections;            // IEnumerator (corrutinas para cerrar/reabrir la red)
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;   // LoadSceneMode (para cargar escenas en red)

public class SesionRed : MonoBehaviour
{
    public static SesionRed Instancia { get; private set; }

    // Roster: clientId → número de Jugador (por orden de conexión). Lo mantiene el server.
    private readonly Dictionary<ulong, int> jugadorDeCliente = new Dictionary<ulong, int>();
    private int proximo = 0;

    // Código de la sala (lo genera el host al crear). Los clientes lo reciben sincronizado.
    public int CodigoSala { get; private set; }

    // Avisos de red para la UI del menú (ej: "no se pudo conectar"). El PanelOnline se suscribe.
    public event System.Action<string> OnMensajeRed;
    private bool intentandoConectar = false;   // true mientras un cliente trata de unirse (para detectar el fallo)

    // Personaje elegido por cada jugador en el lobby (numero → índice). La sala lo copia antes de iniciar.
    private readonly Dictionary<int, int> personajeDeJugador = new Dictionary<int, int>();
    public void GuardarPersonaje(int numero, int personaje) { personajeDeJugador[numero] = personaje; }
    public int PersonajeDe(int numero) => personajeDeJugador.TryGetValue(numero, out int p) ? p : 0;

    // Nombre escrito por cada jugador en el lobby (numero → texto). La sala también lo copia al iniciar.
    private readonly Dictionary<int, string> nombreDeJugador = new Dictionary<int, string>();
    public void GuardarNombre(int numero, string nombre) { nombreDeJugador[numero] = nombre; }
    public string NombreDe(int numero) => nombreDeJugador.TryGetValue(numero, out string n) ? n : "";

    // Config que eligió el HOST en el lobby (HP inicial, rondas y tiempo de turno). La sala la copia al iniciar (B3).
    public int HpInicialConfig { get; private set; } = 40;
    public int RondasConfig { get; private set; } = 0;   // 0 = sin límite
    public int TiempoTurnoConfig { get; private set; } = 30;   // segundos por turno (timer de turno)
    public void GuardarConfig(int hp, int rondas, int tiempoTurno) { HpInicialConfig = hp; RondasConfig = rondas; TiempoTurnoConfig = tiempoTurno; }

    [Header("Reconexión")]
    [Tooltip("Si está activo, usa Connection Approval + un playerId estable para que un jugador que se cae pueda volver a su lugar. Desactivalo si diera problemas de conexión.")]
    public bool reconexionActivada = true;

    // (server) playerId estable de cada jugador → su número. Sobrevive a la ausencia → reconoce al que vuelve.
    private readonly Dictionary<string, int> numeroDePlayerId = new Dictionary<string, int>();
    // (server) clientId → playerId que llegó en la aprobación; se lee al conectarse y se descarta.
    private readonly Dictionary<ulong, string> playerIdPendiente = new Dictionary<ulong, string>();
    public bool PartidaIniciada { get; private set; }

    // Identificador ESTABLE de este jugador (sobrevive a cerrar/abrir la app). Guardado en PlayerPrefs, con
    // prefijo editor/build para que, al probar en la MISMA PC, el editor (host) y la build (cliente) no choquen.
    private string MiPlayerId()
    {
        // 1) Override por línea de comandos: permite probar DOS builds en la MISMA PC sin que choquen.
        //    Ej:  MONATAC.exe -id host    /    MONATAC.exe -id cli
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-id" && !string.IsNullOrEmpty(args[i + 1]))
                return "ARG-" + args[i + 1];

        // 2) Si no, un GUID persistente (único por máquina). Prefijo editor/build para no chocar editor+build.
        string guid = PlayerPrefs.GetString("monatac_playerId", "");
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString("monatac_playerId", guid);
            PlayerPrefs.Save();
        }
        return (Application.isEditor ? "ED-" : "BU-") + guid;
    }

    void Awake()
    {
        // Singleton PERSISTENTE: vive del menú al juego (no se destruye al cambiar de escena).
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarse;
        NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarse;

        // Reconexión: el server aprueba las conexiones y lee el playerId para reconocer a los que vuelven.
        if (reconexionActivada)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback += AprobarConexion;
        }
    }

    // Limpia TODO el estado de la sesión anterior. Como el singleton es persistente
    // (DontDestroyOnLoad), si no se limpia, al crear/unirse de nuevo quedan datos viejos
    // (roster, numeración, nombres) → "la sala no se cierra". Se llama al crear o unirse.
    public void Limpiar()
    {
        jugadorDeCliente.Clear();
        personajeDeJugador.Clear();
        nombreDeJugador.Clear();
        proximo = 0;
        CodigoSala = 0;
        HpInicialConfig = 40;   // vuelve a los valores por defecto (B3)
        RondasConfig = 0;
        TiempoTurnoConfig = 30;
        numeroDePlayerId.Clear();
        playerIdPendiente.Clear();
        PartidaIniciada = false;
    }

    // --- Crear / Unirse (lo llaman los botones del panel Online) ---
    // Van por corrutina para poder CERRAR primero una conexión anterior que haya quedado a medias
    // (ej: intenté unirme a una sala que no existía). Si no, el NetworkManager queda activo y no arranca.
    public void Crear()  { StartCoroutine(CrearRutina()); }
    public void Unirse(string ip) { StartCoroutine(UnirseRutina(ip)); }

    private IEnumerator CrearRutina()
    {
        yield return CerrarRedSiHaceFalta();
        Limpiar();   // arranque limpio (por si venimos de una sala anterior)
        CodigoSala = Random.Range(1000, 10000);   // código de sala de 4 dígitos (1000-9999)
        // El host escucha en TODAS las interfaces (0.0.0.0), no solo en localhost, así una PC remota
        // de la misma red puede conectarse apuntando a la IP LAN del host (requisito para jugar entre 2 PCs).
        UnityTransport th = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (th != null) th.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
        if (reconexionActivada) NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(MiPlayerId());
        NetworkManager.Singleton.StartHost();
        // Carga sincronizada: lleva al host (y después a los que se unan) a la sala de espera.
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        Debug.Log("[SesionRed] Sala creada (Host). Código #" + CodigoSala + ". Cargando Lobby...");
    }

    private IEnumerator UnirseRutina(string ip)
    {
        yield return CerrarRedSiHaceFalta();
        Limpiar();   // arranque limpio (por si venimos de una sala anterior)
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";   // por defecto, misma PC
        UnityTransport t = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (t != null)
        {
            t.SetConnectionData(ip.Trim(), 7777);   // 7777 = puerto por defecto de UTP
            // Fallar RÁPIDO si no hay sala (en vez de reintentar ~60s y quedar "colgado").
            t.MaxConnectAttempts = 6;
            t.ConnectTimeoutMS   = 500;   // 6 × 500ms ≈ 3s y avisa
        }
        if (reconexionActivada) NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(MiPlayerId());
        intentandoConectar = true;   // para detectar si el join falla (no había sala)
        NetworkManager.Singleton.StartClient();
        Debug.Log("[SesionRed] Uniéndome a " + ip + "...");
    }

    // Cierra una conexión anterior que haya quedado a medias y espera a que termine, así el
    // siguiente StartHost/StartClient arranca limpio (si no, "no deja crear/unirse de nuevo").
    private IEnumerator CerrarRedSiHaceFalta()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsListening || nm.IsClient || nm.IsServer))
        {
            Debug.Log("[SesionRed] Cerrando la red anterior antes de reconectar...");
            intentandoConectar = false;
            nm.Shutdown();
            float tope = Time.time + 3f;                               // tope de seguridad
            while (nm.IsListening && Time.time < tope) yield return null;   // esperar a que termine de cerrar
            yield return null;                                         // un frame de gracia
        }
    }

    // Botón "Iniciar" del host: lleva a TODOS a la escena del juego (carga sincronizada).
    public void Iniciar()
    {
        if (!NetworkManager.Singleton.IsServer) return;   // solo el host puede iniciar
        PartidaIniciada = true;   // a partir de acá, los desconocidos ya no entran (solo reconexiones)
        NetworkManager.Singleton.SceneManager.LoadScene("online", LoadSceneMode.Single);
        Debug.Log("[SesionRed] Iniciando partida → cargando el juego...");
    }

    // --- Roster (lo arma el server a medida que se conectan) ---
    private void AlConectarse(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Cliente: me conecté bien → ya no estoy "intentando" (el join salió).
        if (!nm.IsServer)
        {
            if (clientId == nm.LocalClientId) intentandoConectar = false;
            return;
        }

        // Server: armo el roster. Con reconexión, si el playerId ya tenía número, se lo devuelvo.
        string playerId = playerIdPendiente.TryGetValue(clientId, out string pid) ? pid : "";
        playerIdPendiente.Remove(clientId);

        int numero;
        if (!string.IsNullOrEmpty(playerId) && numeroDePlayerId.TryGetValue(playerId, out int viejo))
        {
            numero = viejo;             // RECONEXIÓN: vuelve a su número de antes
            QuitarClientesDe(numero);   // saco el clientId viejo (ya muerto) para no dejar mapeos colgados
        }
        else
        {
            numero = proximo++;         // jugador NUEVO (por orden de conexión)
            if (!string.IsNullOrEmpty(playerId)) numeroDePlayerId[playerId] = numero;
        }
        jugadorDeCliente[clientId] = numero;
        bool esHost = clientId == NetworkManager.ServerClientId;
        Debug.Log("[SesionRed] Jugador " + numero + " conectado" + (esHost ? " (host)" : "")
                  + "  [clientId " + clientId + "]");
    }

    // Saca del roster cualquier clientId que apuntara a 'numero' (la conexión anterior que se cayó).
    private void QuitarClientesDe(int numero)
    {
        List<ulong> viejos = new List<ulong>();
        foreach (KeyValuePair<ulong, int> par in jugadorDeCliente)
            if (par.Value == numero) viejos.Add(par.Key);
        foreach (ulong c in viejos) jugadorDeCliente.Remove(c);
    }

    // Server: decide si acepta a quien entra. Lee el playerId del payload para reconocer reconexiones.
    private void AprobarConexion(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse resp)
    {
        resp.CreatePlayerObject = false;   // no usamos un objeto de jugador por conexión
        resp.Pending = false;

        string playerId = (req.Payload != null && req.Payload.Length > 0)
            ? System.Text.Encoding.UTF8.GetString(req.Payload) : "";
        playerIdPendiente[req.ClientNetworkId] = playerId;

        bool conocido = !string.IsNullOrEmpty(playerId) && numeroDePlayerId.ContainsKey(playerId);
        if (!PartidaIniciada || conocido)
            resp.Approved = true;          // en el lobby (jugador nuevo) o reconexión de un conocido
        else
        {
            resp.Approved = false;         // partida en curso y desconocido → no entra (espectador = futuro)
            resp.Reason = "La partida ya está en curso.";
        }
    }

    private void AlDesconectarse(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            if (jugadorDeCliente.TryGetValue(clientId, out int numero))
                Debug.Log("[SesionRed] Jugador " + numero + " se desconectó  [clientId " + clientId + "].");
            return;
        }

        // Cliente: si NUNCA llegué a conectarme, el join falló (no había sala) → cierro y aviso para reintentar.
        // Si ya estaba conectado, es una desconexión normal (salí / se cayó el host): lo maneja EstadoOnline.
        if (intentandoConectar)
        {
            intentandoConectar = false;
            Debug.Log("[SesionRed] No se pudo conectar (no hay sala en esa IP).");
            nm.Shutdown();
            if (OnMensajeRed != null) OnMensajeRed.Invoke("No se pudo conectar. ¿Hay una sala creada en esa IP?");
        }
    }

    // Consultas que va a usar el juego (EstadoOnline) más adelante.
    public int NumeroDe(ulong clientId) => jugadorDeCliente.TryGetValue(clientId, out int n) ? n : -1;
    public int CantidadJugadores => jugadorDeCliente.Count;

    // Inverso del roster: el clientId del jugador 'numero'. Lo usa EstadoOnline para que
    // cada cliente reconozca su propio panel (vista egocéntrica). ulong.MaxValue = no está.
    public ulong ClientIdDe(int numero)
    {
        foreach (KeyValuePair<ulong, int> par in jugadorDeCliente)
            if (par.Value == numero) return par.Key;
        return ulong.MaxValue;
    }
}
