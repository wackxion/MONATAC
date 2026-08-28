// ============================================================
//  GameManager.cs  —  Capa: VISUAL (presentación / Unity)
//  Coordina la partida y conecta la lógica (Jugador, Dado) con
//  la interfaz de Unity. Ahora muestra las 4 barras a la vez:
//  cada jugador tiene su barra fija, y se MARCA quién está en
//  turno "(turno)" y a quién apunta el ataque "(objetivo)".
//
//  FLUJO DE UN TURNO:
//    1) Elegir acción            -> OnAtacar / OnCurarse / OnRecolectar
//    2) (si atacás) elegir a quién -> OnCambiarObjetivo
//    3) Lanzar los dados          -> OnLanzarDados
//    4) Pasar el turno            -> OnPasarTurno
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Las 4 barras y los 4 nombres, EN ORDEN: [0]=Jugador 1, [1]=Jugador 2, etc.
    // En el Inspector se ponen como arreglos de tamaño 4.
    [Header("Barras de HP (en orden J1, J2, J3, J4)")]
    public Image[] barras;
    public TextMeshProUGUI[] nombres;

    [Header("Del jugador en turno")]
    public TextMeshProUGUI textoMonedas;

    [Header("Turno")]
    public TextMeshProUGUI textoTurno;
    public TextMeshProUGUI textoEstado;

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    [Header("Mano de cartas (textos de las cartas de abajo)")]
    public TextMeshProUGUI[] cartasTexto;

    // [MODIFICADO POR JULIAN] Imágenes de las cartas en los slots de la mano
    [Header("Imágenes de cartas (slots de la mano)")]
    public Image[] cartasImagen;
    public Sprite[] spritesCartas;

    // [AGREGADO POR JULIAN] Reversos de las cartas
    [Header("Reversos de cartas (slots de la mano)")]
    public Image[] cartasReverso;

    // [AGREGADO POR JULIAN] Referencia al componente de animación de dados
    [Header("Animación de dados")]
    public AnimacionDados animacionDados;

    private int hpInicial = 40;          // 40 para probar rápido; el real es 100
    private int cantidadJugadores;       // lo define el menú (Config.cantidadJugadores)

    // --- Datos de la partida ---
    private Partida partida;          // REGLAS: jugadores, orden de turno y victoria (capa Rules)
    private Dado dado;
    private Mazo mazo;                // el mazo de cartas
    private PilaDescarte descarte;    // la pila de descarte
    private GestorCartas gestorCartas; // REGLAS: resuelve el efecto de las cartas elegidas
    private List<Carta> cartasSeleccionadas = new List<Carta>();  // cartas que el jugador eligió usar este turno

    // Accesos de solo lectura que delegan en la Partida (así el resto del código no cambia).
    private List<Jugador> jugadores => partida.Jugadores;
    private int indiceActual        => partida.IndiceActual;
    private int indiceObjetivo      => partida.IndiceObjetivo;

    // --- Estado del turno ---
    private TipoAccion accionElegida;
    private bool haElegido = false;
    private bool yaTiro = false;
    private bool puedeComprar = false;    // ¿puede comprar cartas este turno? (solo si Recolectó)
    private bool juegoTerminado => partida.Terminada;

    // [AGREGADO POR JULIAN] Variables para la animación de dados
    private int totalDados;               // resultado final de los dados (sin bonus/mult)
    private int totalFinal;               // resultado final con bonus y multiplicador
    private int dadosExtra;               // dados extra por comodines
    private int multiplicador;            // multiplicador por comodines
    private int bonusCartas;              // bonus fijo por cartas

    // --- PATRÓN SINGLETON ---
    // Garantiza que exista UNA sola instancia del GameManager, accesible
    // globalmente con GameManager.Instance desde cualquier otro script.
    public static GameManager Instance { get; private set; }

    // Awake() lo llama Unity ANTES que Start(). Acá aseguramos la única instancia.
    void Awake()
    {
        // Si ya existía otro GameManager, este sobra y se destruye.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;   // este pasa a ser LA instancia única
    }

    void Start()
    {
        // La cantidad la eligió el menú. Si se juega esta escena directo,
        // usa el valor por defecto de Config. Clamp la mantiene entre 2 y 4.
        cantidadJugadores = Mathf.Clamp(Config.cantidadJugadores, 2, 4);
        partida = new Partida(cantidadJugadores, hpInicial);   // crea la partida (capa Reglas)

        OcultarBarrasSobrantes();   // esconde las barras de los jugadores que no juegan

        dado = new Dado();
        descarte = new PilaDescarte();
        gestorCartas = new GestorCartas(descarte);
        mazo = FabricaDeCartas.CrearMazo();   // la Fábrica arma el mazo
        mazo.Mezclar();

        IniciarTurno();
    }

    // Muestra solo las barras/nombres de los jugadores que hay en la partida
    // y oculta las que sobran (ej: si son 2, esconde las de J3 y J4).
    private void OcultarBarrasSobrantes()
    {
        if (barras == null) return;
        for (int i = 0; i < barras.Length; i++)
        {
            bool existe = i < cantidadJugadores;
            // La barra vive dentro de BarraHP_Fondo (su "padre"): ocultamos todo el conjunto.
            if (barras[i] != null) barras[i].transform.parent.gameObject.SetActive(existe);
            if (nombres != null && i < nombres.Length && nombres[i] != null)
                nombres[i].gameObject.SetActive(existe);
        }
    }

    private Jugador Actual()   { return jugadores[indiceActual]; }
    private Jugador Objetivo() { return jugadores[indiceObjetivo]; }

    // --- Botones de acción: solo eligen ---
    public void OnAtacar()     { ElegirAccion(TipoAccion.Atacar); }
    public void OnCurarse()    { ElegirAccion(TipoAccion.Curarse); }
    public void OnRecolectar() { ElegirAccion(TipoAccion.Recolectar); }

    private void ElegirAccion(TipoAccion accion)
    {
        if (juegoTerminado || yaTiro) return;
        // Ley Marcial: mientras esté activa, solo se puede Atacar.
        if (partida.LeyMarcialActiva() && accion != TipoAccion.Atacar)
        {
            Mensaje("Ley Marcial activa: este round solo se puede Atacar.");
            return;
        }
        accionElegida = accion;
        haElegido = true;
        Mensaje("Elegiste " + accion + ". Ahora lanzá los dados.");
    }

    // --- Botón CAMBIAR OBJETIVO: rota entre rivales vivos ---
    public void OnCambiarObjetivo()
    {
        if (juegoTerminado) return;
        partida.CambiarObjetivo();
        ActualizarUI();
        Mensaje("Objetivo: " + Objetivo().nombre);
    }

    // --- Botón LANZAR DADOS: aplica la acción ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; }

        // [AGREGADO POR JULIAN] Bloquear si la animación de dados está corriendo
        if (animacionDados != null && animacionDados.EstaAnimando()) return;

        Jugador jugador = Actual();

        // Orden fijo de las cartas elegidas: (1) dados extra, (2) multiplicador, (3) bonus fijo.
        int dadosBase = (accionElegida == TipoAccion.Curarse) ? 2 : 3;   // curarse: 2d4; el resto: 3d4
        dadosExtra = gestorCartas.DadosExtra(cartasSeleccionadas, accionElegida);   // comodín +1d4
        multiplicador = gestorCartas.Multiplicador(cartasSeleccionadas, accionElegida); // comodín x2
        bonusCartas = gestorCartas.Bonus(cartasSeleccionadas, jugador, accionElegida); // pasiva/un uso/vencimiento
        foreach (string m in gestorCartas.Mensajes) Mensaje(m);   // avisos de pago/descarte de vencimiento

        // Tiramos los dados y obtenemos los valores individuales
        int cantidadTotal = dadosBase + dadosExtra;
        int[] valoresDados = new int[cantidadTotal];
        totalDados = 0;
        for (int i = 0; i < cantidadTotal; i++)
        {
            valoresDados[i] = dado.Tirar();
            totalDados += valoresDados[i];
        }
        totalFinal = totalDados * multiplicador + bonusCartas;

        // [AGREGADO POR JULIAN] Si hay animación, arrancamos la animación
        if (animacionDados != null)
        {
            Mensaje("Tirando los dados...");
            animacionDados.IniciarAnimacion(valoresDados, AplicarAccionPostAnimacion);
        }
        else
        {
            // Sin animación: aplicar directo (fallback por si no se asignó)
            MostrarDadosFinales(valoresDados);
            AplicarAccionPostAnimacion();
        }
    }

    // [AGREGADO POR JULIAN] Muestra los valores finales en los textos de los dados
    private void MostrarDadosFinales(int[] valores)
    {
        if (valores.Length > 0 && dado1Texto != null) dado1Texto.text = valores[0].ToString();
        if (valores.Length > 1 && dado2Texto != null) dado2Texto.text = valores[1].ToString();
        if (valores.Length > 2 && dado3Texto != null) dado3Texto.text = valores[2].ToString();
    }

    // [AGREGADO POR JULIAN] Se llama cuando la animación de dados termina.
    // Aplica la acción (ataque/curarse/recolectar) con el resultado ya calculado.
    private void AplicarAccionPostAnimacion()
    {
        Jugador jugador = Actual();

        if (accionElegida == TipoAccion.Atacar)
        {
            Jugador defensor = Objetivo();
            int total = totalFinal;
            // 1) REACCIÓN (auto): el defensor absorbe daño (Escudo de Monedas).
            gestorCartas.Mensajes.Clear();
            total = gestorCartas.Absorber(defensor, total);
            foreach (string m in gestorCartas.Mensajes) Mensaje(m);
            // 2) Se aplica el daño que queda.
            defensor.RecibirDanio(total);
            Mensaje(jugador.nombre + " ataca a " + defensor.nombre + " por " + total + " (x" + multiplicador + ", bonus +" + bonusCartas + ").");
            // 3) REFLECTANTE (auto): devuelve daño / roba monedas / cura (Espejo, Bolsillo Roto, Vampirismo).
            gestorCartas.Mensajes.Clear();
            gestorCartas.Reflejar(defensor, jugador);
            foreach (string m in gestorCartas.Mensajes) Mensaje(m);
            // Las cartas defensivas usadas van al descarte (un solo uso).
            gestorCartas.DescartarUsadas(defensor);
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            jugador.Curar(totalFinal);
            Mensaje(jugador.nombre + " se cura " + totalFinal + " HP.");
        }
        else // Recolectar
        {
            jugador.GanarMonedas(totalFinal);
            puedeComprar = true;   // habilita el botón Comprar carta este turno
            Mensaje(jugador.nombre + " recolecta " + totalFinal + " monedas. Podés comprar cartas (6 c/u) o acumular.");
        }

        gestorCartas.DescartarUsadas(jugador);   // saca de la mano las cartas de un solo uso ya gastadas
        cartasSeleccionadas.Clear();    // limpia la selección para el próximo turno
        yaTiro = true;
        ActualizarUI();
        VerificarVictoria();
    }

    // --- Botón de cada carta de la mano: elegir/deselegir para usarla este turno ---
    public void OnUsarCarta(int indice)
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción (Atacar/Curarse/Recolectar)."); return; }
        if (yaTiro)     { Mensaje("Ya lanzaste los dados este turno."); return; }

        Jugador j = Actual();
        if (indice < 0 || indice >= j.mano.Count) { Mensaje("Ahí no tenés carta."); return; }

        Carta carta = j.mano[indice];

        // Las defensivas y grupales NO se eligen: se activan solas.
        if (carta is CartaReflectante || carta is CartaReaccion || carta is CartaGrupal)
        {
            Mensaje(carta.nombre + " se activa sola, no se elige.");
            return;
        }

        // Toggle: si ya estaba elegida la saco; si no, la agrego.
        if (cartasSeleccionadas.Contains(carta))
        {
            cartasSeleccionadas.Remove(carta);
            Mensaje("Sacaste " + carta.nombre + " de la jugada.");
        }
        else
        {
            cartasSeleccionadas.Add(carta);
            Mensaje("Vas a usar " + carta.nombre + " en esta acción.");
        }
        ActualizarUI();
    }

    // --- Botón COMPRAR CARTA ---
    // Comprás si querés (una o varias veces), pero SOLO en el turno en que
    // Recolectaste. Si no comprás, las monedas quedan acumuladas para después.
    public void OnComprarCarta()
    {
        if (juegoTerminado) return;
        if (!puedeComprar) { Mensaje("Solo podés comprar cartas el turno que Recolectás."); return; }

        Jugador j = Actual();
        if (j.monedas < 6)     { Mensaje("Te faltan monedas (cada carta cuesta 6)."); return; }
        if (j.mano.Count >= 5) { Mensaje("Tu mano está llena (máximo 5 cartas)."); return; }

        if (mazo.EstaVacio()) mazo.Reciclar(descarte);   // mazo circular
        Carta comprada = mazo.Robar();
        j.GastarMonedas(6);

        CartaGrupal grupal = comprada as CartaGrupal;
        if (grupal != null)
        {
            // Las grupales se activan APENAS se compran (polimorfismo) y NO van a la mano.
            ContextoGrupal ctx = new ContextoGrupal {
                jugadores = jugadores, mazo = mazo, descarte = descarte, comprador = j, partida = partida
            };
            Mensaje(j.nombre + " compró la carta grupal: " + grupal.nombre + ".");
            Mensaje(grupal.AplicarATodos(ctx));   // cada grupal aplica su propio efecto
            descarte.Agregar(comprada);
        }
        else
        {
            j.mano.Add(comprada);
            Mensaje(j.nombre + " compró: " + comprada.nombre + " (te quedan " + j.monedas + " monedas).");
        }
        ActualizarUI();
    }

    // Aplica el efecto de una carta grupal a TODOS los jugadores.
    // --- Botón PASAR TURNO ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }
        partida.PasarTurno();   // (adentro descuenta Ley Marcial si está activa)
        IniciarTurno();
    }

    private void IniciarTurno()
    {
        haElegido = false;
        yaTiro = false;
        puedeComprar = false;   // cada turno arranca sin poder comprar (hasta que Recolectes)
        cartasSeleccionadas.Clear();   // arranca sin cartas elegidas
        partida.ElegirObjetivoPorDefecto();
        ActualizarUI();
        Mensaje("Turno de " + Actual().nombre + ": elegí una acción.");
    }

    // Delega en la Partida (capa Reglas) y muestra el resultado si terminó.
    private void VerificarVictoria()
    {
        partida.VerificarVictoria();
        if (partida.Terminada)
        {
            Mensaje("FIN DEL JUEGO. Ganó " + partida.Ganador.nombre + "!");
            if (textoTurno != null) textoTurno.text = "Ganó " + partida.Ganador.nombre;
        }
    }

    private int TirarDados(int cantidad)
    {
        int total = 0;
        string d1 = "", d2 = "", d3 = "";
        for (int i = 0; i < cantidad; i++)
        {
            int valor = dado.Tirar();
            total += valor;
            if (i == 0) d1 = valor.ToString();
            else if (i == 1) d2 = valor.ToString();
            else if (i == 2) d3 = valor.ToString();
            // los dados más allá del 3ro (por el comodín +1d4) suman al total pero no se muestran
        }
        if (dado1Texto != null) dado1Texto.text = d1;
        if (dado2Texto != null) dado2Texto.text = d2;
        if (dado3Texto != null) dado3Texto.text = d3;
        return total;
    }

    // Refresca las 4 barras. Cada barra muestra SIEMPRE a su jugador,
    // y el nombre lleva la marca "(turno)" o "(objetivo)" según corresponda.
    void ActualizarUI()
    {
        for (int i = 0; i < jugadores.Count; i++)
        {
            Jugador j = jugadores[i];

            // La barra: llena según su HP (0 a 1).
            if (barras != null && i < barras.Length && barras[i] != null)
                barras[i].fillAmount = (float)j.hp / j.hpMaximo;

            // El nombre con su HP y la marca de turno/objetivo.
            if (nombres != null && i < nombres.Length && nombres[i] != null)
            {
                string marca = "";
                if (!j.EstaVivo())            marca = " (eliminado)";
                else if (i == indiceActual)   marca = " (turno)";
                else if (i == indiceObjetivo) marca = " (objetivo)";
                nombres[i].text = j.nombre + ": " + j.hp + " HP" + marca;
            }
        }

        // Monedas del jugador en turno.
        if (textoMonedas != null) textoMonedas.text = "Monedas: " + Actual().monedas;

        if (textoTurno != null && !juegoTerminado) textoTurno.text = "Turno de " + Actual().nombre;

        // Mano de cartas del jugador en turno: muestra el nombre de cada carta,
        // o "-" si ese slot está vacío.
        if (cartasTexto != null)
        {
            Jugador enTurno = Actual();
            for (int i = 0; i < cartasTexto.Length; i++)
            {
                if (cartasTexto[i] == null) continue;

                if (i < enTurno.mano.Count)
                {
                    Carta carta = enTurno.mano[i];
                    string usar = cartasSeleccionadas.Contains(carta) ? " [USAR]" : "";

                    // [MODIFICADO POR JULIAN] Asignar imagen de la carta si hay sprite disponible
                    Sprite spriteEncontrado = BuscarSprite(carta.nombre);
                    if (cartasImagen != null && i < cartasImagen.Length && cartasImagen[i] != null)
                    {
                        cartasImagen[i].sprite = spriteEncontrado;
                        cartasImagen[i].color = Color.white;
                    }

                    // [AGREGADO POR JULIAN] Asignar sprite de reverso
                    Sprite reversoEncontrado = BuscarSprite(carta.nombre + " Reverso");
                    if (cartasReverso != null && i < cartasReverso.Length && cartasReverso[i] != null)
                    {
                        cartasReverso[i].sprite = reversoEncontrado;
                        cartasReverso[i].color = Color.white;
                    }

                    // [AGREGADO POR JULIAN] Activar hover si hay carta
                    ActivarHoverSlot(i, true);

                    // Si hay imagen, no mostramos el texto. Si no hay, mostramos el nombre.
                    if (spriteEncontrado != null)
                        cartasTexto[i].text = "";
                    else
                        cartasTexto[i].text = carta.nombre + usar;
                }
                else
                {
                    cartasTexto[i].text = "";

                    // [MODIFICADO POR JULIAN] Sin carta: mostrar slot vacío semitransparente
                    if (cartasImagen != null && i < cartasImagen.Length && cartasImagen[i] != null)
                    {
                        cartasImagen[i].sprite = null;
                        cartasImagen[i].color = new Color(0, 0, 0, 0.5f);
                    }

                    // [AGREGADO POR JULIAN] Sin carta: ocultar reverso
                    if (cartasReverso != null && i < cartasReverso.Length && cartasReverso[i] != null)
                    {
                        cartasReverso[i].sprite = null;
                        cartasReverso[i].color = new Color(0, 0, 0, 0);
                    }

                    // [AGREGADO POR JULIAN] Desactivar hover si no hay carta
                    ActivarHoverSlot(i, false);
                }
            }
        }
    }

    // [AGREGADO POR JULIAN] Activa o desactiva el hover de un slot de carta
    private void ActivarHoverSlot(int indice, bool activo)
    {
        if (cartasImagen == null || indice >= cartasImagen.Length || cartasImagen[indice] == null) return;
        EfectoHoverCarta hover = cartasImagen[indice].GetComponentInParent<EfectoHoverCarta>();
        if (hover != null) hover.SetHoverActivo(activo);
    }

    // [MODIFICADO POR JULIAN] Busca un sprite por nombre, ignorando mayúsculas/minúsculas y sufijos "_0".
    private Sprite BuscarSprite(string nombreCarta)
    {
        if (spritesCartas == null) return null;
        string nombreBuscado = nombreCarta.ToLower();
        for (int i = 0; i < spritesCartas.Length; i++)
        {
            if (spritesCartas[i] != null)
            {
                string nombreSprite = spritesCartas[i].name.ToLower();
                if (nombreSprite == nombreBuscado || nombreSprite == nombreBuscado + "_0")
                    return spritesCartas[i];
            }
        }
        return null;
    }

    private void Mensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }
}
