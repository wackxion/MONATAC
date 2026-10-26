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

using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class SesionRed : MonoBehaviour
{
    public static SesionRed Instancia { get; private set; }

    // Roster: clientId → número de Jugador (por orden de conexión). Lo mantiene el server.
    private readonly Dictionary<ulong, int> jugadorDeCliente = new Dictionary<ulong, int>();
    private int proximo = 0;

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

    // --- Crear / Unirse (lo llaman los botones del panel Online) ---
    public void Crear()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("[SesionRed] Sala creada (soy Host).");
    }

    public void Unirse(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";   // por defecto, misma PC
        UnityTransport t = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (t != null) t.SetConnectionData(ip.Trim(), 7777);   // 7777 = puerto por defecto de UTP
        NetworkManager.Singleton.StartClient();
        Debug.Log("[SesionRed] Uniéndome a " + ip + "...");
    }

    // --- Roster (lo arma el server a medida que se conectan) ---
    private void AlConectarse(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int numero = proximo++;
        jugadorDeCliente[clientId] = numero;
        bool esHost = clientId == NetworkManager.ServerClientId;
        Debug.Log("[SesionRed] Jugador " + numero + " conectado" + (esHost ? " (host)" : "")
                  + "  [clientId " + clientId + "]");
    }

    private void AlDesconectarse(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (jugadorDeCliente.TryGetValue(clientId, out int numero))
            Debug.Log("[SesionRed] Jugador " + numero + " se desconectó  [clientId " + clientId + "].");
    }

    // Consultas que va a usar el juego (EstadoOnline) más adelante.
    public int NumeroDe(ulong clientId) => jugadorDeCliente.TryGetValue(clientId, out int n) ? n : -1;
    public int CantidadJugadores => jugadorDeCliente.Count;
}
