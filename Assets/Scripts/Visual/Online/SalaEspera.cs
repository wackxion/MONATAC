// ============================================================
//  SalaEspera.cs  —  Capa: VISUAL (red · lobby)
//  Sala de espera SINCRONIZADA con look de "selección de personajes":
//  un slot por jugador (avatar + nombre del personaje + etiqueta),
//  y cada uno elige el suyo con flechas. El server arma la lista
//  (NetworkList); todos la ven. Reusa los mismos sprites/nombres que
//  la selección local (SeleccionPersonajesManager).
// ============================================================

using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;   // Image
using TMPro;

// Una fila de la sala: qué jugador es, su clientId (para reconocer su slot),
// qué personaje eligió y su nombre.
public struct EntradaLobby : INetworkSerializable, System.IEquatable<EntradaLobby>
{
    public int numero;              // número de jugador (0,1,2,3)
    public ulong clientId;          // para que cada cliente reconozca SU slot
    public int personaje;           // índice de personaje elegido
    public FixedString32Bytes nombre;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref numero);
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref personaje);
        serializer.SerializeValue(ref nombre);
    }

    public bool Equals(EntradaLobby o) =>
        numero == o.numero && clientId == o.clientId && personaje == o.personaje && nombre.Equals(o.nombre);
}

public class SalaEspera : NetworkBehaviour
{
    [Header("Personajes (los mismos de la selección local)")]
    public Sprite[] spritesPersonajes;      // los avatares
    public string[] nombresPersonajes;      // los nombres (mismo orden)

    [Header("Slots (uno por jugador · el índice = número de jugador)")]
    public GameObject[] slots;              // el panel de cada slot (se muestra/oculta según ocupado)
    public Image[] avatares;                // imagen del personaje de cada slot
    public TMP_Text[] nombresPersonajeUI;   // nombre del personaje de cada slot
    public TMP_Text[] etiquetasJugador;     // "Jugador N" de cada slot
    public GameObject[] flechasPorSlot;     // las flechas ◀▶ de cada slot (solo se muestran en MI slot)
    public TMP_InputField[] inputsNombre;   // input del nombre de cada slot (editable solo en MI slot)

    [Header("Config de la partida (la elige el HOST) · B3")]
    public TMP_InputField inputHp;        // HP inicial que escribe el host
    public TMP_InputField inputRondas;    // cantidad de rondas (0 = sin límite)
    public TMP_Text textoError;           // mensajes de validación (ej: "HP fuera de rango")
    public GameObject panelConfigHost;    // (opcional) panel con la config · se muestra SOLO al host

    // Límites de la config (mismos que el HotSeat). 0 rondas = sin límite.
    private const int HpMin = 20, HpMax = 200;
    private const int RondasMax = 200;
    private const int HpPorDefecto = 40, RondasPorDefecto = 0;

    public NetworkList<EntradaLobby> entradas;

    private int inputConectado = -1;   // a qué slot (mi número) le enganchamos el envío del nombre por código

    void Awake()
    {
        entradas = new NetworkList<EntradaLobby>();
    }

    public override void OnNetworkSpawn()
    {
        entradas.OnListChanged += AlCambiar;
        if (IsServer)
        {
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
                AgregarSiFalta(id);
            NetworkManager.Singleton.OnClientConnectedCallback += AgregarSiFalta;
        }

        // Config (B3): el panel solo lo ve/usa el HOST. Prefill con los valores por defecto.
        if (panelConfigHost != null) panelConfigHost.SetActive(IsServer);
        if (IsServer)
        {
            if (inputHp != null && string.IsNullOrWhiteSpace(inputHp.text)) inputHp.text = HpPorDefecto.ToString();
            if (inputRondas != null && string.IsNullOrWhiteSpace(inputRondas.text)) inputRondas.text = RondasPorDefecto.ToString();
        }
        if (textoError != null) textoError.text = "";

        Mostrar();
    }

    public override void OnNetworkDespawn()
    {
        entradas.OnListChanged -= AlCambiar;
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= AgregarSiFalta;

        // Saco el listener que enganché por código a mi input (limpieza al cambiar de escena).
        if (inputConectado >= 0 && inputsNombre != null && inputConectado < inputsNombre.Length
            && inputsNombre[inputConectado] != null)
            inputsNombre[inputConectado].onValueChanged.RemoveListener(CambiarNombre);
    }

    private void AlCambiar(NetworkListEvent<EntradaLobby> e) => Mostrar();

    // Agrega al jugador de 'clientId' si todavía no está (solo server).
    private void AgregarSiFalta(ulong clientId)
    {
        if (!IsServer) return;
        int numero = (SesionRed.Instancia != null) ? SesionRed.Instancia.NumeroDe(clientId) : -1;
        if (numero < 0) return;

        foreach (EntradaLobby e in entradas)
            if (e.numero == numero) return;   // ya está

        entradas.Add(new EntradaLobby { numero = numero, clientId = clientId, personaje = 0, nombre = "Jugador " + numero });
    }

    // --- Elegir personaje (C2) ---
    public void PersonajeAnterior()  { CambiarPersonajeRpc(-1); }
    public void PersonajeSiguiente() { CambiarPersonajeRpc(+1); }

    [Rpc(SendTo.Server)]
    private void CambiarPersonajeRpc(int delta, RpcParams rpcParams = default)
    {
        int numero = (SesionRed.Instancia != null)
            ? SesionRed.Instancia.NumeroDe(rpcParams.Receive.SenderClientId) : -1;
        if (numero < 0) return;

        int cant = (spritesPersonajes != null && spritesPersonajes.Length > 0) ? spritesPersonajes.Length : 4;
        for (int i = 0; i < entradas.Count; i++)
        {
            if (entradas[i].numero == numero)
            {
                EntradaLobby e = entradas[i];
                e.personaje = (e.personaje + delta + cant) % cant;   // cicla 0..N-1
                entradas[i] = e;   // reasignar en el server sincroniza a todos
                return;
            }
        }
    }

    // --- Cambiar nombre (lo llama el input "On End Edit" del jugador local) ---
    public void CambiarNombre(string nombre) { CambiarNombreRpc(nombre); }

    [Rpc(SendTo.Server)]
    private void CambiarNombreRpc(FixedString32Bytes nombre, RpcParams rpcParams = default)
    {
        int numero = (SesionRed.Instancia != null)
            ? SesionRed.Instancia.NumeroDe(rpcParams.Receive.SenderClientId) : -1;
        if (numero < 0) return;

        for (int i = 0; i < entradas.Count; i++)
        {
            if (entradas[i].numero == numero)
            {
                EntradaLobby e = entradas[i];
                e.nombre = nombre;
                entradas[i] = e;   // reasignar sincroniza a todos
                return;
            }
        }
    }

    // --- Iniciar (C3 + B3): VALIDA la config, la copia a SesionRed y carga el juego ---
    public void Iniciar()
    {
        if (!IsServer || SesionRed.Instancia == null) return;

        // VALIDACIÓN de la config del host (el TP la pide: mensajes claros, sin errores sin capturar).
        // Leemos con TryParse: si no es un número, NO explota → avisamos y no arrancamos.
        int hp = HpPorDefecto;
        int rondas = RondasPorDefecto;

        if (inputHp != null && !string.IsNullOrWhiteSpace(inputHp.text))
        {
            if (!int.TryParse(inputHp.text.Trim(), out hp))
            { MostrarError("HP inválido: escribí un número entre " + HpMin + " y " + HpMax + "."); return; }
            if (hp < HpMin || hp > HpMax)
            { MostrarError("HP fuera de rango: tiene que estar entre " + HpMin + " y " + HpMax + "."); return; }
        }

        if (inputRondas != null && !string.IsNullOrWhiteSpace(inputRondas.text))
        {
            if (!int.TryParse(inputRondas.text.Trim(), out rondas))
            { MostrarError("Rondas inválidas: escribí un número (0 = sin límite, hasta " + RondasMax + ")."); return; }
            if (rondas < 0 || rondas > RondasMax)
            { MostrarError("Rondas fuera de rango: entre 0 (sin límite) y " + RondasMax + "."); return; }
        }

        // Config VÁLIDA: la guardamos para que el server cree la Partida con estos valores.
        if (textoError != null) textoError.text = "";
        SesionRed.Instancia.GuardarConfig(hp, rondas);

        foreach (EntradaLobby e in entradas)
        {
            SesionRed.Instancia.GuardarPersonaje(e.numero, e.personaje);
            SesionRed.Instancia.GuardarNombre(e.numero, e.nombre.ToString());   // el nombre viaja al juego
        }
        SesionRed.Instancia.Iniciar();
    }

    // Muestra un mensaje de validación (y lo deja en consola por si no hay textoError asignado).
    private void MostrarError(string msg)
    {
        if (textoError != null) textoError.text = msg;
        Debug.LogWarning("[SalaEspera] Config inválida: " + msg);
    }

    // --- Dibujo de los slots (lo corre TODOS cuando cambia la lista) ---
    private void Mostrar()
    {
        // Mi número: busco mi clientId en la lista (así sé cuál slot es el mío).
        ulong miId = (NetworkManager.Singleton != null) ? NetworkManager.Singleton.LocalClientId : 999;
        int miNumero = -1;
        foreach (EntradaLobby e in entradas)
            if (e.clientId == miId) { miNumero = e.numero; break; }

        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            // Buscar la entrada de este slot.
            bool ocupado = false;
            EntradaLobby entrada = default;
            foreach (EntradaLobby e in entradas)
                if (e.numero == i) { entrada = e; ocupado = true; break; }

            if (slots[i] != null) slots[i].SetActive(ocupado);
            if (!ocupado) continue;

            int personaje = entrada.personaje;

            if (avatares != null && i < avatares.Length && avatares[i] != null
                && spritesPersonajes != null && personaje < spritesPersonajes.Length)
                avatares[i].sprite = spritesPersonajes[personaje];

            if (nombresPersonajeUI != null && i < nombresPersonajeUI.Length && nombresPersonajeUI[i] != null
                && nombresPersonajes != null && personaje < nombresPersonajes.Length)
                nombresPersonajeUI[i].text = nombresPersonajes[personaje];

            if (etiquetasJugador != null && i < etiquetasJugador.Length && etiquetasJugador[i] != null)
                etiquetasJugador[i].text = "Jugador " + i;

            // Nombre: el input muestra el nombre; solo es editable en MI slot.
            if (inputsNombre != null && i < inputsNombre.Length && inputsNombre[i] != null)
            {
                string actual = entrada.nombre.ToString();
                // MI propio input NO se pisa con el eco del server (si llega con retraso, me pelearía la
                // escritura). Los slots de los DEMÁS sí muestran el nombre sincronizado.
                if (i != miNumero && inputsNombre[i].text != actual) inputsNombre[i].text = actual;
                inputsNombre[i].interactable = (i == miNumero);
            }

            // Las flechas ◀▶ solo se muestran en MI slot.
            if (flechasPorSlot != null && i < flechasPorSlot.Length && flechasPorSlot[i] != null)
                flechasPorSlot[i].SetActive(i == miNumero);
        }

        // --- Robustez del nombre (B3): enganchamos EN CÓDIGO el envío de MI nombre en CADA tecla. ---
        // El bug era que el nombre del cliente no quedaba guardado: el input solo mandaba al server en el
        // "On End Edit" (Enter / perder foco), y al iniciar el host el input del cliente nunca perdía el foco.
        // Mandando onValueChanged, el server SIEMPRE tiene mi nombre actual. No depende de cablear el Inspector.
        if (miNumero >= 0 && inputsNombre != null && miNumero < inputsNombre.Length
            && inputsNombre[miNumero] != null && inputConectado != miNumero)
        {
            // Si mi número cambió, saco el listener del input anterior.
            if (inputConectado >= 0 && inputConectado < inputsNombre.Length && inputsNombre[inputConectado] != null)
                inputsNombre[inputConectado].onValueChanged.RemoveListener(CambiarNombre);

            // Inicializo mi input UNA vez con el nombre actual (ANTES de enganchar, para no auto-enviarlo).
            string mio = "";
            foreach (EntradaLobby e in entradas) if (e.numero == miNumero) { mio = e.nombre.ToString(); break; }
            inputsNombre[miNumero].text = mio;

            // Engancho el envío al server en cada cambio de texto.
            inputsNombre[miNumero].onValueChanged.AddListener(CambiarNombre);
            inputConectado = miNumero;
        }
    }
}
