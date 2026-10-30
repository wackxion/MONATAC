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

    [Header("Colores de resaltado")]
    public Color colorTurno = Color.green;
    public Color colorObjetivo = new Color(1f, 0.3f, 0.3f);
    public Color colorRival = new Color(0.5f, 0.5f, 0.5f);

    [Header("Números flotantes · opcional")]
    public NumerosFlotantesOnline numerosFlotantes;   // popea -N/+N sobre el panel que cambió de HP

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
        Refrescar();
    }

    void OnDestroy()
    {
        if (estado == null) return;
        estado.jugadores.OnListChanged -= AlCambiarLista;
        estado.turnoActual.OnValueChanged -= AlCambiarInt;
        estado.objetivoActual.OnValueChanged -= AlCambiarInt;
        estado.hpMaximoSala.OnValueChanged -= AlCambiarInt;
    }

    private void AlCambiarLista(NetworkListEvent<EstadoJugador> e) => Refrescar();
    private void AlCambiarInt(int anterior, int nuevo) => Refrescar();

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
