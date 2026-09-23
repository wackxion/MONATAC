// ============================================================
//  GameManager.cs  —  Capa: VISUAL (orquestación / Unity)
//  ORQUESTA la partida: recibe los clics de los botones, coordina
//  las reglas (Partida, GestorCartas, Dado, Mazo) y le pide a la
//  VistaJuegoUI que DIBUJE. Ya NO dibuja él mismo (eso es la Vista).
//
//  FLUJO DE UN TURNO:
//    1) Elegir acción -> OnAtacar / OnCurarse / OnRecolectar / OnDescartar
//    2) (si atacás) elegir a quién -> OnCambiarObjetivo
//    3) Lanzar los dados -> OnLanzarDados
//    4) Pasar el turno -> OnPasarTurno
// ============================================================

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;   // para reiniciar la partida / volver al menú

public class GameManager : MonoBehaviour, IVistaJuego   // implementa la interface (patrón MVP: es la VISTA)
{
    // La VISTA que dibuja la pantalla (se asigna en el Inspector).
    [Header("Vista (dibujado)")]
    public VistaJuegoUI vista;

    // [AGREGADO POR JULIAN] Componente de animación de dados (lo dispara la orquestación).
    [Header("Animación de dados")]
    public AnimacionDados animacionDados;

    // [AGREGADO POR JULIAN] Componente de animación de carta grupal.
    [Header("Animación de carta grupal")]
    public AnimacionCartaGrupal animacionCartaGrupal;

    //hecho/modificado por Julian
    private int hpInicial;               // se carga de Config
    private int cantidadJugadores;       // lo define el menú (Config.cantidadJugadores)

    // --- Datos de la partida ---
    private IPartida partida;         // REGLAS (abstracción · DIP): jugadores, turnos y victoria
    private Dado dado;
    private Mazo mazo;                // el mazo de cartas
    private PilaDescarte descarte;    // la pila de descarte
    private IGestorCartas gestorCartas; // REGLAS (abstracción · DIP): resuelve el efecto de las cartas
    private PresentadorJuego presentador; // MVP: coordina la acción "Cambiar Objetivo" (capa Rules)
    private List<Carta> cartasSeleccionadas = new List<Carta>();  // cartas que el jugador eligió usar este turno

    // Accesos de solo lectura que delegan en la Partida (así el resto del código no cambia).
    private List<Jugador> jugadores => partida.Jugadores;
    private int indiceActual        => partida.IndiceActual;
    private int indiceObjetivo      => partida.IndiceObjetivo;

    // --- Estado del turno ---
    private TipoAccion accionElegida;
    private Accion accionActual;   // la acción polimórfica (AccionAtacar/Curarse/Recolectar)
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;   // este pasa a ser LA instancia única
    }

    // Unity lo llama cuando el objeto se destruye (cambio/reinicio de escena).
    // Ciclo de vida limpio: desconectamos el Presentador para no dejar el evento colgado.
    void OnDestroy()
    {
        if (presentador != null) presentador.Desconectar();
    }

    // --- Implementación de IVistaJuego (patrón MVP): delega en la Vista ---
    // La Vista AVISA por este evento; el PresentadorJuego (Rules) lo escucha.
    public event System.Action AlPedirCambiarObjetivo;
    // El Presentador nos PIDE estas dos cosas (a través de la interface):
    public void RefrescarPantalla()        { ActualizarUI(); }
    public void MostrarMensaje(string txt) { Mensaje(txt); }

    void Start()
    {
        //hecho por pilar
        // Cargar HP máximo y rondas de Config
        hpInicial = Config.hpMaximo;

        // La cantidad la eligió el menú. Clamp la mantiene entre 2 y 4.
        cantidadJugadores = Mathf.Clamp(Config.cantidadJugadores, 2, 4);
        partida = new Partida(cantidadJugadores, hpInicial, Config.cantidadRondas, Config.nombresJugadores);   // crea la partida (capa Reglas)

        if (vista != null) vista.OcultarBarrasSobrantes(cantidadJugadores);   // esconde barras de los que no juegan
        if (vista != null) vista.AplicarPersonajes(cantidadJugadores);        // pone el personaje elegido a cada avatar

        dado = new Dado();
        descarte = new PilaDescarte();
        gestorCartas = new GestorCartas(descarte);
        mazo = FabricaDeCartas.CrearMazo();   // la Fábrica arma el mazo
        mazo.Mezclar();

        // MVP: creamos el Presentador y le pasamos esta Vista (this) y el Modelo (partida).
        presentador = new PresentadorJuego(this, partida);

        IniciarTurno();
    }

    private Jugador Actual()   { return jugadores[indiceActual]; }
    private Jugador Objetivo() { return jugadores[indiceObjetivo]; }

    // --- Botones de acción: solo eligen ---
    public void OnAtacar()     { ElegirAccion(TipoAccion.Atacar); }
    public void OnCurarse()    { ElegirAccion(TipoAccion.Curarse); }
    public void OnRecolectar() { ElegirAccion(TipoAccion.Recolectar); }

    // Acción DESCARTAR: solo se puede si tenés al menos una carta en la mano.
    public void OnDescartar()
    {
        if (juegoTerminado || yaTiro) return;
        if (Actual().mano.Count == 0) { Mensaje("No tenés cartas para descartar."); return; }
        ElegirAccion(TipoAccion.Descartar);
    }

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
        accionActual = CrearAccion(accion);   // crea el objeto polimórfico de esta acción
        haElegido = true;
        if (accion == TipoAccion.Descartar)
            Mensaje("Descartar: tocá la carta que querés tirar (perdés el turno).");
        else
            Mensaje("Elegiste " + accionActual.Nombre + ". Ahora lanzá los dados.");
        ActualizarUI();   // refresca botones (habilita Lanzar) y resalta las cartas usables
    }

    // Crea el objeto Accion que corresponde al tipo elegido (herencia + polimorfismo).
    private Accion CrearAccion(TipoAccion tipo)
    {
        switch (tipo)
        {
            case TipoAccion.Atacar:    return new AccionAtacar();
            case TipoAccion.Curarse:   return new AccionCurarse();
            case TipoAccion.Descartar: return new AccionDescartar();
            default:                   return new AccionRecolectar();   // Recolectar
        }
    }

    // --- Botón CAMBIAR OBJETIVO: rota entre rivales vivos (MVP: dispara el evento) ---
    public void OnCambiarObjetivo()
    {
        if (juegoTerminado) return;
        AlPedirCambiarObjetivo?.Invoke();
        if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoCambiarObjetivo();   // SFX
    }

    // --- Botón LANZAR DADOS: aplica la acción ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; }
        if (accionElegida == TipoAccion.Descartar) { Mensaje("Para descartar, tocá la carta que querés tirar."); return; }

        // [AGREGADO POR JULIAN] Bloquear si la animación de dados está corriendo
        if (animacionDados != null && animacionDados.EstaAnimando()) return;

        Jugador jugador = Actual();

        // Orden fijo de las cartas elegidas: (1) dados extra, (2) multiplicador, (3) bonus fijo.
        int dadosBase = accionActual.CantidadDados;   // POLIMORFISMO: cada acción sabe cuántos dados tira
        dadosExtra = gestorCartas.DadosExtra(cartasSeleccionadas, accionElegida);
        multiplicador = gestorCartas.Multiplicador(cartasSeleccionadas, accionElegida);
        bonusCartas = gestorCartas.Bonus(cartasSeleccionadas, jugador, accionElegida);
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

        // SFX: sonido de tirar los dados.
        if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoDados();

        // [AGREGADO POR JULIAN] Si hay animación, arrancamos la animación
        if (animacionDados != null)
        {
            Mensaje("Tirando los dados...");
            animacionDados.IniciarAnimacion(valoresDados, AplicarAccionPostAnimacion);
        }
        else
        {
            // Sin animación: mostrar directo (la Vista dibuja) y aplicar.
            if (vista != null) vista.MostrarDados(valoresDados);
            AplicarAccionPostAnimacion();
        }
    }

    // [AGREGADO POR JULIAN] Se llama cuando la animación de dados termina.
    // Aplica la acción con el resultado ya calculado.
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
            // 2) Se aplica el daño que queda (POLIMORFISMO: AccionAtacar daña al objetivo).
            accionActual.Aplicar(partida, total);
            if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoDanio();   // SFX: daño
            Mensaje(jugador.nombre + " ataca a " + defensor.nombre + " por " + total + " (x" + multiplicador + ", bonus +" + bonusCartas + ").");
            // 3) REFLECTANTE (auto): devuelve daño / roba monedas / cura.
            gestorCartas.Mensajes.Clear();
            gestorCartas.Reflejar(defensor, jugador);
            foreach (string m in gestorCartas.Mensajes) Mensaje(m);
            gestorCartas.DescartarUsadas(defensor);
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            accionActual.Aplicar(partida, totalFinal);   // POLIMORFISMO: AccionCurarse cura al jugador
            if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoCuracion();   // SFX: curación
            Mensaje(jugador.nombre + " se cura " + totalFinal + " HP.");
        }
        else // Recolectar
        {
            accionActual.Aplicar(partida, totalFinal);   // POLIMORFISMO: AccionRecolectar suma monedas
            puedeComprar = true;   // habilita el botón Comprar carta este turno
            // SFX: distinto sonido según si recolectó mucho o poco.
            if (GestorAudio.Instance != null)
            {
                if (totalFinal >= Reglas.RecoleccionAlta) GestorAudio.Instance.SonidoMonedasMuchas();
                else                                       GestorAudio.Instance.SonidoMonedasPocas();
            }
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

        // Acción DESCARTAR: al tocar una carta, se descarta al instante y se pierde el turno.
        if (accionElegida == TipoAccion.Descartar)
        {
            j.mano.RemoveAt(indice);    // la carta sale de la mano
            descarte.Agregar(carta);    // va a la pila de descarte (para reciclar)
            Mensaje(j.nombre + " descartó " + carta.nombre + ". Pasá el turno.");
            yaTiro = true;              // descartar consume el turno
            ActualizarUI();
            return;
        }

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

    // --- Botón COMPRAR CARTA (solo el turno que Recolectás; si no, acumulás monedas) ---
    public void OnComprarCarta()
    {
        if (juegoTerminado) return;
        if (!puedeComprar) { Mensaje("Solo podés comprar cartas el turno que Recolectás."); return; }

        Jugador j = Actual();
        if (j.monedas < Reglas.CostoCarta)     { Mensaje("Te faltan monedas (cada carta cuesta " + Reglas.CostoCarta + ")."); return; }
        if (j.mano.Count >= Reglas.ManoMaxima) { Mensaje("Tu mano está llena (máximo " + Reglas.ManoMaxima + " cartas)."); return; }

        if (mazo.EstaVacio()) mazo.Reciclar(descarte);   // mazo circular
        Carta comprada = mazo.Robar();
        j.GastarMonedas(Reglas.CostoCarta);
        if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoComprar();   // SFX: compra exitosa

        CartaGrupal grupal = comprada as CartaGrupal;
        if (grupal != null)
        {
            // Las grupales se activan APENAS se compran (polimorfismo) y NO van a la mano.
            ContextoGrupal ctx = new ContextoGrupal {
                jugadores = jugadores, mazo = mazo, descarte = descarte, comprador = j, partida = partida
            };
            Mensaje(j.nombre + " compró la carta grupal: " + grupal.nombre + ".");

            Sprite reverso = vista != null ? vista.BuscarSprite(grupal.nombre + " Reverso") : null;

            if (animacionCartaGrupal != null && reverso != null)
            {
                animacionCartaGrupal.IniciarAnimacion(reverso, () =>
                {
                    Mensaje(grupal.AplicarATodos(ctx));
                    descarte.Agregar(comprada);
                    ActualizarUI();
                });
            }
            else
            {
                // Fallback sin animación
                Mensaje(grupal.AplicarATodos(ctx));
                descarte.Agregar(comprada);
            }
        }
        else
        {
            j.mano.Add(comprada);
            Mensaje(j.nombre + " compró: " + comprada.nombre + " (te quedan " + j.monedas + " monedas).");
        }
        ActualizarUI();
    }

    // --- Botón PASAR TURNO ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }
        partida.PasarTurno();   // (adentro descuenta Ley Marcial y puede terminar por LÍMITE DE RONDAS)

        // Si se alcanzó el límite de rondas, la partida ya terminó dentro de PasarTurno.
        if (juegoTerminado) { MostrarPantallaFin(); return; }

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
        if (partida.Terminada) MostrarPantallaFin();
    }

    // Muestra la pantalla de fin (se usa tanto al ganar por eliminación como por límite de rondas).
    private void MostrarPantallaFin()
    {
        Mensaje("FIN DEL JUEGO. Ganó " + partida.Ganador.nombre + "!");
        if (GestorAudio.Instance != null) GestorAudio.Instance.SonidoGanador();   // SFX: victoria
        if (vista != null) vista.MostrarFin(partida);   // pasa la partida para mostrar las estadísticas
    }

    // --- Botones de la pantalla de FIN de partida (cierran el game loop) ---
    public void OnReiniciar()  { SceneManager.LoadScene("juego"); }   // nueva partida (misma config de Config)
    public void OnVolverMenu() { SceneManager.LoadScene("MENU"); }    // vuelve al menú principal

    // ===== "Puentes" a la VISTA: el GameManager ya no dibuja, le pide a VistaJuegoUI que lo haga =====

    // Refresca toda la pantalla. Le pasa a la Vista el estado del turno para que
    // pueda iluminar/apagar los botones y resaltar las cartas usables.
    void ActualizarUI()
    {
        if (vista != null)
            vista.Actualizar(partida, cartasSeleccionadas, accionElegida, haElegido, yaTiro, puedeComprar);
    }

    // Muestra un mensaje de estado (y en consola como respaldo).
    private void Mensaje(string txt)
    {
        if (vista != null) vista.MostrarMensaje(txt);
        else Debug.Log(txt);
    }
}
