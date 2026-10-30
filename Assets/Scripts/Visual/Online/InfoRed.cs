// ============================================================
//  InfoRed.cs  —  Capa: VISUAL (UI del juego online)
//  Muestra el ID de este cliente y el CÓDIGO de la sala (lo pide el
//  TP: "cada cliente muestra su ID y la sala a la que está conectado").
//  El código lo genera el host y se sincroniza por 'codigoSala'
//  (NetworkVariable en EstadoOnline).
// ============================================================

using Unity.Netcode;
using UnityEngine;
using TMPro;

public class InfoRed : MonoBehaviour
{
    public TMP_Text textoInfo;    // dónde mostrar "ID + sala"
    public EstadoOnline estado;   // de acá se lee el código de sala sincronizado

    void Start()
    {
        if (estado != null) estado.codigoSala.OnValueChanged += AlCambiarCodigo;
        Refrescar();
    }

    void OnDestroy()
    {
        if (estado != null) estado.codigoSala.OnValueChanged -= AlCambiarCodigo;
    }

    // Cuando llega/actualiza el código desde el server, refrescamos el texto.
    private void AlCambiarCodigo(int anterior, int nuevo) => Refrescar();

    private void Refrescar()
    {
        if (textoInfo == null || NetworkManager.Singleton == null) return;

        ulong miId = NetworkManager.Singleton.LocalClientId;
        string rol = NetworkManager.Singleton.IsHost ? "Host" : "Cliente";
        int codigo = (estado != null) ? estado.codigoSala.Value : 0;

        textoInfo.text = rol + "  ·  ID " + miId + "  ·  Sala #" + codigo;
    }
}
