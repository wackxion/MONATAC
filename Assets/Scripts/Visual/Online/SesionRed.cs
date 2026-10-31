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

    // Config que eligió el HOST en el lobby (HP inicial y cantidad de rondas). La sala la copia al iniciar (B3).
    public int HpInicialConfig { get; private set; } = 40;
    public int RondasConfig { get; private set; } = 0;   // 0 = sin límite
    public void GuardarConfig(int hp, int rondas) { HpInicialConfig = hp; RondasConfig = rondas; }

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

        // Server: armo el roster por orden de conexión.
        int numero = proximo++;
        jugadorDeCliente[clientId] = numero;
        bool esHost = clientId == NetworkManager.ServerClientId;
        Debug.Log("[SesionRed] Jugador " + numero + " conectado" + (esHost ? " (host)" : "")
                  + "  [clientId " + clientId + "]");
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
