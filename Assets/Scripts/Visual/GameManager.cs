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

    private int hpInicial = 40;          // 40 para probar rápido; el real es 100
    private int cantidadJugadores;       // lo define el menú (Config.cantidadJugadores)

    // --- Datos de la partida ---
    private List<Jugador> jugadores = new List<Jugador>();
    private int indiceActual = 0;     // quién juega ahora
    private int indiceObjetivo = 0;   // a quién apunta el ataque
    private Dado dado;
    private Mazo mazo;                // el mazo de cartas
    private PilaDescarte descarte;    // la pila de descarte
    private List<Carta> cartasSeleccionadas = new List<Carta>();  // cartas que el jugador eligió usar este turno

    // --- Estado del turno ---
    private TipoAccion accionElegida;
    private bool haElegido = false;
    private bool yaTiro = false;
    private bool puedeComprar = false;    // ¿puede comprar cartas este turno? (solo si Recolectó)
    private int turnosLeyMarcial = 0;     // turnos que quedan de Ley Marcial (todos deben Atacar)
    private bool juegoTerminado = false;

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

        for (int i = 0; i < cantidadJugadores; i++)
        {
            jugadores.Add(new Jugador("Jugador " + (i + 1), hpInicial));
        }

        OcultarBarrasSobrantes();   // esconde las barras de los jugadores que no juegan

        dado = new Dado();
        descarte = new PilaDescarte();
        mazo = FabricaDeCartas.CrearMazo();   // la Fábrica arma el mazo
        mazo.Mezclar();

        indiceActual = 0;
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
        if (turnosLeyMarcial > 0 && accion != TipoAccion.Atacar)
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
        indiceObjetivo = SiguienteRivalVivo(indiceObjetivo);
        ActualizarUI();
        Mensaje("Objetivo: " + Objetivo().nombre);
    }

    // --- Botón LANZAR DADOS: aplica la acción ---
    public void OnLanzarDados()
    {
        if (juegoTerminado) return;
        if (!haElegido) { Mensaje("Primero elegí una acción."); return; }
        if (yaTiro)     { Mensaje("Ya tiraste. Pasá el turno."); return; }

        Jugador jugador = Actual();

        // Orden fijo de las cartas elegidas: (1) dados extra, (2) multiplicador, (3) bonus fijo.
        int dadosBase = (accionElegida == TipoAccion.Curarse) ? 2 : 3;   // curarse: 2d4; el resto: 3d4
        int extra = DadosExtraSeleccionados(accionElegida);   // comodín +1d4
        int mult  = MultiplicadorSeleccionado(accionElegida); // comodín x2
        int bonus = BonusSeleccionadas(jugador, accionElegida); // pasiva / un uso / vencimiento (paga por uso)

        int total = TirarDados(dadosBase + extra) * mult + bonus;

        if (accionElegida == TipoAccion.Atacar)
        {
            Jugador defensor = Objetivo();
            // 1) REACCIÓN (auto): el defensor absorbe daño gastando monedas (Escudo de Monedas).
            total = AplicarReaccion(defensor, total);
            // 2) Se aplica el daño que queda.
            defensor.RecibirDanio(total);
            Mensaje(jugador.nombre + " ataca a " + defensor.nombre + " por " + total + " (x" + mult + ", bonus +" + bonus + ").");
            // 3) REFLECTANTE (auto): el defensor devuelve daño al atacante (Espejo de Sangre).
            AplicarReflejo(defensor, jugador);
            // Las cartas defensivas usadas por el defensor van al descarte (son de un solo uso).
            DescartarUsadas(defensor);
        }
        else if (accionElegida == TipoAccion.Curarse)
        {
            jugador.Curar(total);
            Mensaje(jugador.nombre + " se cura " + total + " HP.");
        }
        else // Recolectar
        {
            jugador.GanarMonedas(total);
            puedeComprar = true;   // habilita el botón Comprar carta este turno
            Mensaje(jugador.nombre + " recolecta " + total + " monedas. Podés comprar cartas (6 c/u) o acumular.");
        }

        DescartarUsadas(jugador);       // saca de la mano las cartas de un solo uso ya gastadas
        cartasSeleccionadas.Clear();    // limpia la selección para el próximo turno
        yaTiro = true;
        ActualizarUI();
        VerificarVictoria();
    }

    // Suma los bonus que dan las cartas de la mano para una acción.
    // Cada carta responde con su propio BonusPara() (polimorfismo).
    // Suma el bonus SOLO de las cartas que el jugador eligió usar este turno.
    // Aplica las cartas elegidas y devuelve el bonus total.
    // Las de vencimiento pagan por uso: 1er uso gratis, los siguientes 2 monedas;
    // si no puede pagar o se queda sin usos, la carta se descarta.
    private int BonusSeleccionadas(Jugador j, TipoAccion accion)
    {
        int total = 0;
        // Copia de la selección porque podemos descartar cartas mientras recorremos.
        List<Carta> seleccion = new List<Carta>(cartasSeleccionadas);

        foreach (Carta c in seleccion)
        {
            CartaVencimiento v = c as CartaVencimiento;
            if (v != null)
            {
                if (!v.SirvePara(accion)) continue;   // no aplica a esta acción

                int costo = v.CostoDelProximoUso();    // 0 el primer uso, 2 los siguientes
                if (costo > 0 && !j.GastarMonedas(costo))
                {
                    Mensaje(j.nombre + ": no pudo pagar " + v.nombre + ", se descarta.");
                    DescartarCarta(j, v);
                    continue;
                }
                if (costo > 0) Mensaje(j.nombre + " paga " + costo + " por usar " + v.nombre + ".");

                total += v.Usar();                     // aplica bonus y gasta un uso
                if (v.SinUsos())
                {
                    Mensaje(v.nombre + " se quedó sin usos y se descarta.");
                    DescartarCarta(j, v);
                }
            }
            else
            {
                total += c.BonusPara(accion);          // pasiva / un uso
            }
        }
        return total;
    }

    // Saca una carta de la mano (y de la selección) y la manda al descarte.
    private void DescartarCarta(Jugador j, Carta c)
    {
        cartasSeleccionadas.Remove(c);
        j.mano.Remove(c);
        descarte.Agregar(c);
    }

    // Dados extra que suman las cartas elegidas (comodín +1d4).
    private int DadosExtraSeleccionados(TipoAccion accion)
    {
        int extra = 0;
        foreach (Carta c in cartasSeleccionadas) extra += c.DadosExtra(accion);
        return extra;
    }

    // Multiplicador de las cartas elegidas (comodín x2). Empieza en 1 (no cambia nada).
    private int MultiplicadorSeleccionado(TipoAccion accion)
    {
        int mult = 1;
        foreach (Carta c in cartasSeleccionadas) mult *= c.Multiplicador(accion);
        return mult;
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

    // REACCIÓN automática: recorre la mano del defensor buscando Escudo de Monedas.
    // Cada escudo absorbe daño gastando monedas (2 monedas = 1 HP). Devuelve el daño restante.
    private int AplicarReaccion(Jugador defensor, int dano)
    {
        foreach (Carta c in defensor.mano)
        {
            CartaReaccion escudo = c as CartaReaccion;   // ¿es una carta de reacción?
            if (escudo != null)
            {
                int antes = dano;
                dano = escudo.Absorber(dano, defensor);
                if (dano < antes)
                    Mensaje(defensor.nombre + " absorbe " + (antes - dano) + " con " + escudo.nombre + ".");
            }
        }
        return dano;
    }

    // REFLECTANTE automática: si el defensor tiene Espejo de Sangre, devuelve daño al atacante.
    private void AplicarReflejo(Jugador defensor, Jugador atacante)
    {
        foreach (Carta c in defensor.mano)
        {
            // Espejo de Sangre: devuelve daño fijo.
            CartaReflectante espejo = c as CartaReflectante;
            if (espejo != null)
            {
                int reflejo = espejo.Reflejar();
                atacante.RecibirDanio(reflejo);
                Mensaje(defensor.nombre + " refleja " + reflejo + " a " + atacante.nombre + " con " + espejo.nombre + ".");
                continue;
            }

            // Bolsillo Roto: roba monedas al atacante; el faltante entra como daño directo.
            CartaBolsilloRoto bolsillo = c as CartaBolsilloRoto;
            if (bolsillo != null)
            {
                int robado = System.Math.Min(bolsillo.monedasARobar, atacante.monedas);
                atacante.GastarMonedas(robado);
                defensor.GanarMonedas(robado);
                int faltante = bolsillo.monedasARobar - robado;
                if (faltante > 0) atacante.RecibirDanio(faltante);
                bolsillo.usada = true;
                Mensaje(defensor.nombre + " le roba " + robado + " monedas a " + atacante.nombre +
                        (faltante > 0 ? " (+" + faltante + " de daño)" : "") + " con " + bolsillo.nombre + ".");
                continue;
            }

            // Vampirismo Defensivo: el defensor se cura al ser atacado.
            CartaVampirismoDefensivo vamp = c as CartaVampirismoDefensivo;
            if (vamp != null)
            {
                defensor.Curar(vamp.curacion);
                vamp.usada = true;
                Mensaje(defensor.nombre + " se cura " + vamp.curacion + " con " + vamp.nombre + ".");
                continue;
            }
        }
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
            // Las grupales se activan APENAS se compran y NO van a la mano.
            Mensaje(j.nombre + " compró la carta grupal: " + grupal.nombre + ".");
            AplicarGrupal(grupal, j);
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
    private void AplicarGrupal(CartaGrupal g, Jugador comprador)
    {
        if (g is CartaColecta)
        {
            // Suma todas las monedas y las reparte parejo; el resto va al comprador.
            int suma = 0;
            foreach (Jugador j in jugadores) suma += j.monedas;
            int cada = suma / jugadores.Count;
            int resto = suma % jugadores.Count;
            foreach (Jugador j in jugadores) j.EstablecerMonedas(cada);
            comprador.GanarMonedas(resto);
            Mensaje("Colecta: se repartieron " + suma + " monedas (" + cada + " a cada uno, resto " + resto + " para " + comprador.nombre + ").");
        }
        else if (g is CartaExceso)
        {
            // Cada jugador recibe 1 carta gratis (si tiene lugar).
            foreach (Jugador j in jugadores)
            {
                if (j.mano.Count >= 5) continue;
                if (mazo.EstaVacio()) mazo.Reciclar(descarte);
                Carta nueva = mazo.Robar();
                if (nueva == null) continue;
                if (nueva is CartaGrupal) descarte.Agregar(nueva);   // no encadenar grupales
                else j.mano.Add(nueva);
            }
            Mensaje("Exceso: cada jugador recibió una carta gratis.");
        }
        else if (g is CartaPenitencia)
        {
            // Destruye todas las cartas de vencimiento de todas las manos.
            foreach (Jugador j in jugadores)
                for (int i = j.mano.Count - 1; i >= 0; i--)
                    if (j.mano[i] is CartaVencimiento) { descarte.Agregar(j.mano[i]); j.mano.RemoveAt(i); }
            Mensaje("Penitencia: se destruyeron todas las cartas de vencimiento.");
        }
        else if (g is CartaDescarteGrupal)
        {
            // Cada jugador descarta 1 carta (la primera, por simplicidad).
            foreach (Jugador j in jugadores)
                if (j.mano.Count > 0) { descarte.Agregar(j.mano[0]); j.mano.RemoveAt(0); }
            Mensaje("Descarte Grupal: cada jugador descartó una carta.");
        }
        else if (g is CartaLeyMarcial)
        {
            // El próximo round todos están obligados a Atacar.
            turnosLeyMarcial = jugadores.Count;
            Mensaje("Ley Marcial: este round todos están obligados a Atacar.");
        }
    }

    // Manda al descarte las cartas de un solo uso que ya se usaron.
    private void DescartarUsadas(Jugador j)
    {
        for (int i = j.mano.Count - 1; i >= 0; i--)
        {
            if (j.mano[i].DebeDescartarse())   // un uso o comodín ya gastado
            {
                descarte.Agregar(j.mano[i]);
                j.mano.RemoveAt(i);
            }
        }
    }

    // --- Botón PASAR TURNO ---
    public void OnPasarTurno()
    {
        if (juegoTerminado) return;
        if (!yaTiro) { Mensaje("Elegí una acción y lanzá los dados antes de pasar."); return; }
        if (turnosLeyMarcial > 0) turnosLeyMarcial--;   // se va gastando Ley Marcial
        indiceActual = SiguienteJugadorVivo(indiceActual);
        IniciarTurno();
    }

    private void IniciarTurno()
    {
        haElegido = false;
        yaTiro = false;
        puedeComprar = false;   // cada turno arranca sin poder comprar (hasta que Recolectes)
        cartasSeleccionadas.Clear();   // arranca sin cartas elegidas
        indiceObjetivo = SiguienteRivalVivo(indiceActual);
        ActualizarUI();
        Mensaje("Turno de " + Actual().nombre + ": elegí una acción.");
    }

    private int SiguienteJugadorVivo(int desde)
    {
        int i = desde;
        for (int intento = 0; intento < jugadores.Count; intento++)
        {
            i = (i + 1) % jugadores.Count;
            if (jugadores[i].EstaVivo()) return i;
        }
        return desde;
    }

    private int SiguienteRivalVivo(int desde)
    {
        int i = desde;
        for (int intento = 0; intento < jugadores.Count; intento++)
        {
            i = (i + 1) % jugadores.Count;
            if (i != indiceActual && jugadores[i].EstaVivo()) return i;
        }
        return indiceActual;
    }

    private void VerificarVictoria()
    {
        int vivos = 0;
        Jugador ultimo = null;
        foreach (Jugador j in jugadores)
        {
            if (j.EstaVivo()) { vivos++; ultimo = j; }
        }
        if (vivos <= 1)
        {
            juegoTerminado = true;
            Mensaje("FIN DEL JUEGO. Ganó " + ultimo.nombre + "!");
            if (textoTurno != null) textoTurno.text = "Ganó " + ultimo.nombre;
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
                }
            }
        }
    }

    // [MODIFICADO POR JULIAN] Busca un sprite por nombre exacto.
    // Los sprites se llaman igual que las cartas (ej: "Filo Eterno").
    private Sprite BuscarSprite(string nombreCarta)
    {
        if (spritesCartas == null) return null;
        for (int i = 0; i < spritesCartas.Length; i++)
        {
            if (spritesCartas[i] != null && spritesCartas[i].name == nombreCarta)
                return spritesCartas[i];
        }
        return null;
    }

    private void Mensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }
}
