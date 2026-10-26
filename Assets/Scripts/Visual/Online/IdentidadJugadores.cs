// ============================================================
//  IdentidadJugadores.cs  —  Capa: VISUAL (red · usa Netcode)
//  El CORAZÓN de la Etapa 1: cuando un cliente se conecta, el
//  SERVIDOR le asigna un número de Jugador por ORDEN de conexión
//  (host = Jugador 0, 2º = Jugador 1, etc.) y lo guarda en un mapa.
//
//  Solo el servidor arma el mapa (es la autoridad). Por ahora solo
//  lo loguea: en la Etapa 2 ese número se usará para sincronizar el
//  estado, y en la 3 para saber "¿es tu turno?".
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class IdentidadJugadores : MonoBehaviour
{
    // Mapa clientId -> número de Jugador (0,1,2,3). Lo mantiene SOLO el servidor.
    private readonly Dictionary<ulong, int> jugadorDeCliente = new Dictionary<ulong, int>();

    // El próximo número a repartir (sube con cada conexión).
    private int proximoJugador = 000;

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[Identidad] No hay NetworkManager en la escena.");
            return;
        }
        // Nos enganchamos a los eventos de conexión/desconexión del NetworkManager.
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarse;
        NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarse;
    }

    void OnDestroy()
    {
        // Buena práctica: desenganchar para no dejar el evento colgado al cerrar la escena.
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarse;
        NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarse;
    }

    // Se dispara por CADA cliente que entra (incluido el propio host).
    private void AlConectarse(ulong clientId)
    {
        // Solo el SERVIDOR decide las identidades. En un cliente puro, esto no corre.
        if (!NetworkManager.Singleton.IsServer) return;

        int numero = proximoJugador;
        proximoJugador++;
        jugadorDeCliente[clientId] = numero;

        bool esHost = clientId == NetworkManager.ServerClientId;
        Debug.Log("[Identidad] Jugador " + numero + " conectado" + (esHost ? " (host)" : "")
                  + "  [clientId " + clientId + "]");
    }

    // Etapa 7: avisa la desconexión. NO borra el mapeo, para que EstadoOnline pueda
    // resolver qué Jugador era y eliminarlo (el efecto en el juego lo maneja EstadoOnline).
    private void AlDesconectarse(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (jugadorDeCliente.TryGetValue(clientId, out int numero))
            Debug.Log("[Identidad] Jugador " + numero + " se desconectó  [clientId " + clientId + "].");
    }

    // Devuelve el número de Jugador de un clientId (o -1 si no está registrado).
    // Lo usa EstadoOnline para validar "¿es tu turno?" en los RPCs (Etapa 3).
    public int NumeroDe(ulong clientId)
    {
        return jugadorDeCliente.TryGetValue(clientId, out int numero) ? numero : -1;
    }
}
