// ============================================================
//  VistaOnline.cs  —  Capa: VISUAL (UI del juego online)
//  B2.1: dibuja los PANELES DE JUGADOR (avatar + barra de HP +
//  nombre + monedas + turno/objetivo resaltado) leyendo el estado
//  SINCRONIZADO de EstadoOnline (la NetworkList + NetworkVariables).
//  Reusa los sprites de personajes y las barras (Image fill).
//  NO toca reglas: solo muestra lo que llega por red.
// ============================================================

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;   // Dictionary (colores originales de los botones)

public class VistaOnline : MonoBehaviour
{
    [Header("Estado sincronizado")]
    public EstadoOnline estado;

    [Header("Personajes (mismos sprites de la selección)")]
    public Sprite[] spritesPersonajes;

    [Header("Paneles de jugador (orden = número de jugador 0..3)")]
    public GameObject[] paneles;     // panel de cada jugador (se muestra/oculta)
    public Image[] avatares;         // imagen del personaje
    public Image[] barrasHP;         // barra de HP (Image tipo Filled)
    public TMP_Text[] nombres;       // "Jugador N: HP"

    [Header("Del jugador en turno")]
    public TMP_Text textoTurno;
    public TMP_Text textoMonedas;

    [Header("Contador de rondas (B4) · opcional")]
    public TMP_Text textoRonda;   // muestra las rondas que quedan (o la ronda actual si no hay límite)

    [Header("Colores de resaltado")]
    public Color colorTurno = Color.green;
    public Color colorObjetivo = new Color(1f, 0.3f, 0.3f);
    public Color colorRival = new Color(0.5f, 0.5f, 0.5f);

    [Header("Números flotantes · opcional")]
    public NumerosFlotantesOnline numerosFlotantes;   // popea -N/+N sobre el panel que cambió de HP

    [Header("Botones de acción (habilitar/apagar + resaltar el elegido · B4) · opcionales")]
    public Button botonAtacar;
    public Button botonCurarse;
    public Button botonRecolectar;
    public Button botonDescartar;
    public Button botonCambiarObjetivo;
    public Button botonLanzarDados;
    public Button botonComprar;
    public Button botonPasarTurno;
    [Tooltip("Color 'glow' del botón de la acción que elegiste (el que vas a lanzar).")]
    public Color colorAccionElegida = new Color(1f, 0.92f, 0.45f);
    private Dictionary<Button, ColorBlock> coloresOriginales;   // para restaurar el color de cada botón

    // Animación de cada barra (si la barra tiene el componente AnimacionBarraHP). Opcional.
    private AnimacionBarraHP[] barrasAnim;
    private int[] hpPrevio;           // HP anterior por número de jugador (-1 = aún no visto); detecta el delta
    private int ultimaGeneracion = -1; // Nº de partida visto; si cambia (revancha) reseteo el estado visual

    void Start()
    {
        if (estado == null) return;

        // Cacheo la animación de cada barra (si cada barra tiene AnimacionBarraHP en el mismo objeto).
        if (barrasHP != null)
        {
            barrasAnim = new AnimacionBarraHP[barrasHP.Length];
            for (int i = 0; i < barrasHP.Length; i++)
                if (barrasHP[i] != null) barrasAnim[i] = barrasHP[i].GetComponent<AnimacionBarraHP>();
        }

        // HP previo por jugador (para los números flotantes). -1 = todavía no vi a ese jugador.
        if (paneles != null)
        {
            hpPrevio = new int[paneles.Length];
            for (int i = 0; i < hpPrevio.Length; i++) hpPrevio[i] = -1;
        }

        estado.jugadores.OnListChanged += AlCambiarLista;
        estado.turnoActual.OnValueChanged += AlCambiarInt;
        estado.objetivoActual.OnValueChanged += AlCambiarInt;
        estado.hpMaximoSala.OnValueChanged += AlCambiarInt;
        estado.rondaActual.OnValueChanged += AlCambiarInt;
        estado.rondasTotales.OnValueChanged += AlCambiarInt;
        // Fase del turno + elección local → refrescan los botones (habilitar/apagar/resaltar · B4).
        estado.turnoYaActuo.OnValueChanged += AlCambiarBool;
        estado.turnoResolviendo.OnValueChanged += AlCambiarBool;
        estado.leyMarcialRed.OnValueChanged += AlCambiarBool;
        estado.partidaTerminada.OnValueChanged += AlCambiarBool;
        estado.UICambio += Refrescar;
        Refrescar();
    }

    void OnDestroy()
    {
        if (estado == null) return;
        estado.jugadores.OnListChanged -= AlCambiarLista;
        estado.turnoActual.OnValueChanged -= AlCambiarInt;
        estado.objetivoActual.OnValueChanged -= AlCambiarInt;
        estado.hpMaximoSala.OnValueChanged -= AlCambiarInt;
        estado.rondaActual.OnValueChanged -= AlCambiarInt;
        estado.rondasTotales.OnValueChanged -= AlCambiarInt;
        estado.turnoYaActuo.OnValueChanged -= AlCambiarBool;
        estado.turnoResolviendo.OnValueChanged -= AlCambiarBool;
        estado.leyMarcialRed.OnValueChanged -= AlCambiarBool;
        estado.partidaTerminada.OnValueChanged -= AlCambiarBool;
        estado.UICambio -= Refrescar;
    }

    private void AlCambiarLista(NetworkListEvent<EstadoJugador> e) => Refrescar();
    private void AlCambiarInt(int anterior, int nuevo) => Refrescar();
    private void AlCambiarBool(bool anterior, bool nuevo) => Refrescar();

    private void Refrescar()
    {
        if (estado == null || paneles == null) return;

        int turno = estado.turnoActual.Value;
        int objetivo = estado.objetivoActual.Value;
        int hpMax = estado.hpMaximoSala.Value;
        var lista = estado.jugadores;
        int cant = lista.Count;

        // REVANCHA: si arrancó una partida nueva, olvido el HP previo. Así el salto de vida
        // (ej. 8 → 40) NO dispara números flotantes y las barras vuelven a llenas de una.
        int gen = estado.generacionPartida.Value;
        if (gen != ultimaGeneracion)
        {
            ultimaGeneracion = gen;
            if (hpPrevio != null) for (int i = 0; i < hpPrevio.Length; i++) hpPrevio[i] = -1;
        }

        // VISTA EGOCÉNTRICA: YO voy siempre en el primer panel y el resto rota.
        // En la pantalla de cada uno, el panel 0 es el suyo (como en los juegos de cartas).
        int miNumero = MiNumero(lista);

        for (int s = 0; s < paneles.Length; s++)
        {
            bool ocupado = s < cant;
            if (paneles[s] != null) paneles[s].SetActive(ocupado);
            if (!ocupado) continue;

            // El panel (slot) 's' muestra al jugador relativo: yo en el 0, y los demás girando.
            int p = (cant > 0) ? ((miNumero + s) % cant) : s;

            EstadoJugador j = lista[p];
            // El resaltado compara contra el número REAL del jugador (turno/objetivo son absolutos).
            Color c = (p == turno) ? colorTurno : (p == objetivo) ? colorObjetivo : colorRival;

            // ¿Primera vez que veo a este jugador? (-1 = sí). Sirve para bar inmediata y para no popear al inicio.
            int hpAntes = (hpPrevio != null && p < hpPrevio.Length) ? hpPrevio[p] : -1;
            bool primeraVezJug = hpAntes < 0;

            // Avatar.
            if (avatares != null && s < avatares.Length && avatares[s] != null
                && spritesPersonajes != null && j.personaje < spritesPersonajes.Length)
                avatares[s].sprite = spritesPersonajes[j.personaje];

            // Barra de HP (llenado 0..1) + color según turno/objetivo/rival.
            if (barrasHP != null && s < barrasHP.Length && barrasHP[s] != null)
            {
                barrasHP[s].color = c;   // el color (turno/objetivo/rival) se setea siempre

                AnimacionBarraHP anim = (barrasAnim != null && s < barrasAnim.Length) ? barrasAnim[s] : null;
                if (anim != null)
                {
                    // Con componente: anima el llenado (la 1ra vez de ese jugador, directo, para no bajar desde "lleno").
                    if (primeraVezJug) anim.SetHPInmediato(j.hp, hpMax);
                    else               anim.SetHP(j.hp, hpMax);
                }
                else
                {
                    // Sin componente: llenado directo (respaldo, como antes).
                    barrasHP[s].fillAmount = (hpMax > 0) ? (float)j.hp / hpMax : 0f;
                }
            }

            // Números flotantes: si cambió el HP de este jugador, popea -N/+N sobre SU panel (slot s).
            // La 1ra vez de cada jugador solo registro el HP (sin popear). Indexado por número REAL.
            if (hpPrevio != null && p < hpPrevio.Length)
            {
                if (!primeraVezJug && hpAntes != j.hp && numerosFlotantes != null)
                    numerosFlotantes.Spawn(s, j.hp - hpAntes);
                hpPrevio[p] = j.hp;
            }

            // Nombre + HP + marca de turno/objetivo ("(vos)" en tu propio panel).
            if (nombres != null && s < nombres.Length && nombres[s] != null)
            {
                string nombreJug = j.nombre.ToString();
                if (string.IsNullOrEmpty(nombreJug)) nombreJug = "Jugador " + p;   // fallback si no eligió nombre
                string quien = nombreJug + (p == miNumero ? "" : "");
                string marca = (p == turno) ? "" : (p == objetivo) ? "" : "";
                nombres[s].text = quien + ": " + j.hp + " HP" + marca;
            }
        }

        // Turno y monedas del jugador en turno (referidos al número real).
        if (textoTurno != null)
        {
            if (turno == miNumero) textoTurno.text = "¡Tu turno!";
            else
            {
                string nt = (turno >= 0 && turno < cant) ? lista[turno].nombre.ToString() : "";
                textoTurno.text = "Turno de " + (string.IsNullOrEmpty(nt) ? ("Jugador " + turno) : nt);
            }
        }
        // Monedas: SIEMPRE las MÍAS (el ícono está al lado de mi propio panel), no las del que está en turno.
        if (textoMonedas != null && miNumero >= 0 && miNumero < cant)
            textoMonedas.text = lista[miNumero].monedas + "";

        // Contador de rondas: si hay límite, cuenta cuántas QUEDAN (10, 9, 8...); si no, muestra la actual.
        if (textoRonda != null)
        {
            int total = estado.rondasTotales.Value;
            if (total > 0)
            {
                int restantes = Mathf.Max(0, total - estado.rondaActual.Value + 1);
                textoRonda.text = "Rondas restantes: " + restantes;
            }
            else
            {
                textoRonda.text = "Ronda " + estado.rondaActual.Value;   // sin límite: va subiendo
            }
        }

        // Botones: habilitar/apagar según lo que YO puedo hacer ahora, y resaltar la acción elegida.
        ActualizarBotones(miNumero, turno, cant);
    }

    // Habilita/apaga cada botón según el estado del turno (mismo criterio que el HotSeat, pero del lado
    // del cliente: lee las banderas de fase que el server espeja + mi estado de la lista sincronizada).
    private void ActualizarBotones(int miNumero, int turno, int cant)
    {
        bool esMiTurno   = (turno == miNumero);
        bool terminado   = estado.partidaTerminada.Value;
        bool yaActuo     = estado.turnoYaActuo.Value;
        bool resolviendo = estado.turnoResolviendo.Value;
        bool leyMarcial  = estado.leyMarcialRed.Value;
        bool haElegido   = estado.HaElegido;
        TipoAccion accion = estado.AccionElegida;

        bool hayCartas = estado.MiMano.Count > 0;

        // ¿Me alcanza para comprar? Leo MIS monedas y cartas de la lista sincronizada.
        bool puedoComprar = false;
        if (miNumero >= 0 && miNumero < cant)
        {
            EstadoJugador yo = estado.jugadores[miNumero];
            puedoComprar = yo.monedas >= Reglas.CostoCarta && yo.cartas < Reglas.ManoMaxima;
        }

        // Antes de tirar, en MI turno (sin dados girando ni haber actuado), puedo elegir/re-elegir acción.
        bool puedeElegir = esMiTurno && !terminado && !resolviendo && !yaActuo;

        SetInteractable(botonAtacar,     puedeElegir);
        SetInteractable(botonCurarse,    puedeElegir && !leyMarcial);
        SetInteractable(botonRecolectar, puedeElegir && !leyMarcial);
        SetInteractable(botonDescartar,  puedeElegir && !leyMarcial && hayCartas);

        // Cambiar objetivo: solo si elegiste Atacar y todavía no tiraste.
        SetInteractable(botonCambiarObjetivo, puedeElegir && haElegido && accion == TipoAccion.Atacar);
        // Lanzar: elegiste una acción (que no sea Descartar) y todavía no tiraste.
        SetInteractable(botonLanzarDados,     puedeElegir && haElegido && accion != TipoAccion.Descartar);
        // Comprar: en tu turno, sin dados girando, si te alcanza.
        SetInteractable(botonComprar,         esMiTurno && !terminado && !resolviendo && puedoComprar);
        // Pasar turno: recién cuando ya actuaste (y los dados pararon).
        SetInteractable(botonPasarTurno,      esMiTurno && !terminado && !resolviendo && yaActuo);

        // Resaltar ("glow") el botón de la acción que vas a lanzar.
        bool resaltable = esMiTurno && haElegido && !yaActuo && !resolviendo;
        ResaltarAccion(botonAtacar,     resaltable && accion == TipoAccion.Atacar);
        ResaltarAccion(botonCurarse,    resaltable && accion == TipoAccion.Curarse);
        ResaltarAccion(botonRecolectar, resaltable && accion == TipoAccion.Recolectar);
        ResaltarAccion(botonDescartar,  resaltable && accion == TipoAccion.Descartar);
    }

    // Setea interactable si el botón está conectado (interactable=false lo oscurece y bloquea el clic).
    private void SetInteractable(Button boton, bool valor)
    {
        if (boton != null) boton.interactable = valor;
    }

    // Resalta (o restaura) un botón cambiando su color NORMAL. Guardo el color original la 1ra vez
    // para poder volver a dejarlo como estaba. El fundido propio del Button hace la transición suave.
    private void ResaltarAccion(Button boton, bool resaltado)
    {
        if (boton == null) return;
        if (coloresOriginales == null) coloresOriginales = new Dictionary<Button, ColorBlock>();
        if (!coloresOriginales.ContainsKey(boton)) coloresOriginales[boton] = boton.colors;

        ColorBlock original = coloresOriginales[boton];
        if (resaltado)
        {
            ColorBlock cb = original;
            cb.normalColor = colorAccionElegida;
            cb.highlightedColor = colorAccionElegida;
            cb.selectedColor = colorAccionElegida;
            boton.colors = cb;
        }
        else
        {
            boton.colors = original;   // vuelve a su color de siempre
        }
    }

    // Mi número de jugador: busco MI clientId en la lista sincronizada (igual que el lobby busca su slot).
    // Si no me encuentro (fallback), devuelvo 0 → se ve sin rotar.
    private int MiNumero(NetworkList<EstadoJugador> lista)
    {
        ulong miId = (NetworkManager.Singleton != null) ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        for (int i = 0; i < lista.Count; i++)
            if (lista[i].clientId == miId) return i;
        return 0;
    }
}
