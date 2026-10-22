// ============================================================
//  RedMonatac.cs  —  Capa: VISUAL (red · usa Unity + Netcode)
//  Conecta los botones de la escena "Online" con Netcode.
//    - Host   → arranca como SERVIDOR + CLIENTE a la vez (juega y hostea).
//    - Cliente→ se conecta a un host que ya esté esperando.
//
//  Es la base de la Etapa 1 (reusa la idea de 'RedBasica' del ejercicio 6).
//  Los botones se enganchan desde el Inspector (OnClick), NO por código,
//  para no caer en el bug de "doble conexión" del ejercicio.
// ============================================================

using UnityEngine;
using Unity.Netcode;

public class RedMonatac : MonoBehaviour
{
    // Lo llama el botón "Host". Es servidor y cliente al mismo tiempo.
    public void IniciarHost()
    {
        if (NetworkManager.Singleton.IsListening) return;   // ya conectado: evita doble arranque
        NetworkManager.Singleton.StartHost();
        Debug.Log("[RedMonatac] Host iniciado (soy servidor + cliente).");
    }

    // Lo llama el botón "Cliente". Se conecta al host (por defecto 127.0.0.1, local).
    public void IniciarCliente()
    {
        if (NetworkManager.Singleton.IsListening) return;   // ya conectado: evita doble arranque
        NetworkManager.Singleton.StartClient();
        Debug.Log("[RedMonatac] Cliente conectándose al host...");
    }
}
