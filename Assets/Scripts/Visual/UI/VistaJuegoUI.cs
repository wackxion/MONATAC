// ============================================================
//  VistaJuegoUI.cs  —  Capa: VISUAL (presentación / Unity)
//  Se encarga SOLO de MOSTRAR: dibuja las barras, los nombres,
//  las monedas, el turno, la ronda, los dados y la mano de cartas.
//  NO conoce las reglas ni cambia el estado: recibe los datos
//  (la Partida y las cartas elegidas) y los dibuja.
//
//  Es la "Vista" del patrón MVP. El GameManager le delega el dibujado.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VistaJuegoUI : MonoBehaviour
{
    // Las 4 barras y los 4 nombres, EN ORDEN: [0]=Jugador 1, [1]=Jugador 2, etc.
    [Header("Barras de HP (en orden J1, J2, J3, J4)")]
    public Image[] barras;
    public TextMeshProUGUI[] nombres;

    [Header("Animación de barras de HP (mismos indices que barras)")]
    public AnimacionBarraHP[] animacionesBarras;

    [Header("Números flotantes de daño/cura")]
    public SpawnerNumerosFlotantes spawnerNumeros;

    [Header("Imágenes de los jugadores (SpriteRenderer, en orden J1, J2, J3, J4)")]
    public SpriteRenderer[] imagenesJugadores;

    [Header("Del jugador en turno")]
    public TextMeshProUGUI textoMonedas;

    [Header("Turno")]
    public TextMeshProUGUI textoTurno;
    public TextMeshProUGUI textoEstado;

    [Header("Fin de partida")]
    public GameObject panelFin;            // se ACTIVA cuando hay ganador
    public TextMeshProUGUI textoGanador;   // muestra "¡Ganó Jugador X!"

    [Header("Ronda")]
    public TextMeshProUGUI textoRonda;

    [Header("Dados")]
    public TextMeshProUGUI dado1Texto;
    public TextMeshProUGUI dado2Texto;
    public TextMeshProUGUI dado3Texto;

    [Header("Mano de cartas (textos de las cartas de abajo)")]
    public TextMeshProUGUI[] cartasTexto;

    [Header("Imágenes de cartas (slots de la mano)")]
    public Image[] cartasImagen;
    public Sprite[] spritesCartas;

    [Header("Reversos de cartas (slots de la mano)")]
    public Image[] cartasReverso;

    [Header("Marcos 'en uso' (uno por slot, arrancan apagados)")]
    public GameObject[] cartasMarcoUso;   // se prende el del slot cuando esa carta está seleccionada

    [Header("Colores del turno")]
    public Color colorBarra = Color.green;
    public Color colorObjetivo = new Color(1f, 0.3f, 0.3f);
    private float opacidadRival = 0.5f;
    private int[] hpAnterior = new int[4];
    private bool primeraActualizacion = true;

    [Header("Botones de acción (para iluminar/apagar según el turno)")]
    public Button botonAtacar;
    public Button botonCurarse;
    public Button botonRecolectar;
    public Button botonDescartar;
    public Button botonCambiarObjetivo;
    public Button botonLanzarDados;
    public Button botonComprar;
    public Button botonPasarTurno;

    [Header("Resaltado de cartas usables")]
    public Color colorCartaApagada = new Color(0.4f, 0.4f, 0.4f, 1f);   // gris para las que no aplican

    // Oculta las barras/nombres de los jugadores que no juegan (ej: si son 2, esconde J3 y J4).
    public void OcultarBarrasSobrantes(int cantidadJugadores)
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

    // Refresca toda la pantalla a partir del estado de la Partida y las cartas elegidas.
    // Recibe también el estado del turno para iluminar/apagar botones y cartas.
    public void Actualizar(IPartida partida, List<Carta> cartasSeleccionadas,
                           TipoAccion accionElegida, bool haElegido, bool yaTiro, bool puedeComprar)
    {
        List<Jugador> jugadores = partida.Jugadores;
        int indiceActual = partida.IndiceActual;
        int indiceObjetivo = partida.IndiceObjetivo;
        bool juegoTerminado = partida.Terminada;
        Jugador enTurno = partida.Actual();

        for (int i = 0; i < jugadores.Count; i++)
        {
            Jugador j = jugadores[i];

            // La barra: llena según su HP (0 a 1) y coloreada según turno/objetivo/rival.
            if (barras != null && i < barras.Length && barras[i] != null)
            {
                // Animación suave de la barra de HP
                if (animacionesBarras != null && i < animacionesBarras.Length && animacionesBarras[i] != null)
                    animacionesBarras[i].SetHP(j.hp, j.hpMaximo);
                else
                    barras[i].fillAmount = (float)j.hp / j.hpMaximo;

                // Número flotante si cambió el HP (no en la primera actualización)
                if (spawnerNumeros != null && !primeraActualizacion && j.hp != hpAnterior[i])
                {
                    int diferencia = hpAnterior[i] - j.hp;
                    spawnerNumeros.Spawn(i, diferencia);
                }
                hpAnterior[i] = j.hp;

                Color cTurno = colorBarra;
                float gris = cTurno.r * 0.3f + cTurno.g * 0.59f + cTurno.b * 0.11f;
                Color cRival = new Color(gris, gris, gris, 1f);
                Color cBarra;
                if (i == indiceActual) cBarra = cTurno;
                else if (i == indiceObjetivo) cBarra = colorObjetivo;
                else cBarra = cRival;
                barras[i].color = cBarra;
            }

            // El nombre con su HP y la marca de turno/objetivo/eliminado.
            if (nombres != null && i < nombres.Length && nombres[i] != null)
            {
                string marca = "";
                if (!j.EstaVivo())            marca = " (eliminado)";
                else if (i == indiceActual)   marca = " (turno)";
                else if (i == indiceObjetivo) marca = " (objetivo)";
                nombres[i].text = j.nombre + ": " + j.hp + " HP" + marca;

                if (i == indiceActual) nombres[i].color = Color.white;
                else if (i == indiceObjetivo) nombres[i].color = colorObjetivo;
                else nombres[i].color = new Color(opacidadRival, opacidadRival, opacidadRival, 1f);
            }

            // La imagen/avatar del jugador, coloreada igual que su nombre.
            if (imagenesJugadores != null && i < imagenesJugadores.Length && imagenesJugadores[i] != null)
            {
                if (i == indiceActual) imagenesJugadores[i].color = Color.white;
                else if (i == indiceObjetivo) imagenesJugadores[i].color = colorObjetivo;
                else imagenesJugadores[i].color = new Color(opacidadRival, opacidadRival, opacidadRival, 1f);
            }
        }

        // Monedas del jugador en turno.
        if (textoMonedas != null) textoMonedas.text = "" + enTurno.monedas;

        if (textoTurno != null && !juegoTerminado) textoTurno.text = "Turno de " + enTurno.nombre;

        // Ronda actual.
        if (textoRonda != null)
        {
            if (partida.TotalRondas > 0)
                textoRonda.text = "Ronda " + partida.RondaActual + "/" + partida.TotalRondas;
            else
                textoRonda.text = "Ronda " + partida.RondaActual;
        }

        // Mano de cartas del jugador en turno.
        if (cartasTexto != null)
        {
            for (int i = 0; i < cartasTexto.Length; i++)
            {
                if (cartasTexto[i] == null) continue;

                if (i < enTurno.mano.Count)
                {
                    Carta carta = enTurno.mano[i];
                    bool seleccionada = cartasSeleccionadas.Contains(carta);
                    string usar = seleccionada ? " [USAR]" : "";

                    // ¿Se ilumina esta carta? Sin acción elegida, todas normales.
                    // Con acción elegida: se iluminan las usables (Descartar ilumina todas).
                    bool resaltar = !haElegido
                        || accionElegida == TipoAccion.Descartar
                        || carta.AplicaA(accionElegida);
                    Color colorCarta = resaltar ? Color.white : colorCartaApagada;

                    Sprite spriteEncontrado = BuscarSprite(carta.nombre);
                    if (cartasImagen != null && i < cartasImagen.Length && cartasImagen[i] != null)
                    {
                        cartasImagen[i].sprite = spriteEncontrado;
                        cartasImagen[i].color = colorCarta;
                    }

                    Sprite reversoEncontrado = BuscarSprite(carta.nombre + " Reverso");
                    if (cartasReverso != null && i < cartasReverso.Length && cartasReverso[i] != null)
                    {
                        cartasReverso[i].sprite = reversoEncontrado;
                        cartasReverso[i].color = colorCarta;
                    }

                    ActivarHoverSlot(i, true);
                    MostrarMarcoUso(i, seleccionada);   // marco de "en uso" si está seleccionada

                    // Si hay imagen, no mostramos el texto. Si no hay, mostramos el nombre.
                    if (spriteEncontrado != null)
                        cartasTexto[i].text = "";
                    else
                        cartasTexto[i].text = carta.nombre + usar;
                }
                else
                {
                    cartasTexto[i].text = "";

                    if (cartasImagen != null && i < cartasImagen.Length && cartasImagen[i] != null)
                    {
                        cartasImagen[i].sprite = null;
                        cartasImagen[i].color = new Color(0, 0, 0, 0.5f);
                    }

                    if (cartasReverso != null && i < cartasReverso.Length && cartasReverso[i] != null)
                    {
                        cartasReverso[i].sprite = null;
                        cartasReverso[i].color = new Color(0, 0, 0, 0);
                    }

                    ActivarHoverSlot(i, false);
                    MostrarMarcoUso(i, false);   // slot vacío: marco apagado
                }
            }
        }

        // Iluminar/apagar los botones según lo que se puede hacer en este momento.
        ActualizarBotones(partida, accionElegida, haElegido, yaTiro, puedeComprar);

        primeraActualizacion = false;
    }

    // Enciende/apaga (interactable) cada botón según el estado del turno.
    // interactable = false en Unity oscurece el botón (Disabled Color) y bloquea el clic.
    private void ActualizarBotones(IPartida partida, TipoAccion accionElegida,
                                   bool haElegido, bool yaTiro, bool puedeComprar)
    {
        bool terminado = partida.Terminada;
        bool leyMarcial = partida.LeyMarcialActiva();
        bool hayCartas = partida.Actual().mano.Count > 0;

        // Antes de tirar podés elegir (o re-elegir) una acción.
        bool puedeElegir = !terminado && !yaTiro;

        // Acciones: con Ley Marcial activa, solo Atacar.
        SetInteractable(botonAtacar,     puedeElegir);
        SetInteractable(botonCurarse,    puedeElegir && !leyMarcial);
        SetInteractable(botonRecolectar, puedeElegir && !leyMarcial);
        SetInteractable(botonDescartar,  puedeElegir && !leyMarcial && hayCartas);

        // Cambiar objetivo: solo tiene sentido si vas a atacar y todavía no tiraste.
        SetInteractable(botonCambiarObjetivo, !terminado && haElegido && !yaTiro && accionElegida == TipoAccion.Atacar);
        // Lanzar: elegiste una acción (que no sea Descartar) y todavía no tiraste.
        SetInteractable(botonLanzarDados,     !terminado && haElegido && !yaTiro && accionElegida != TipoAccion.Descartar);
        // Comprar: solo el turno que recolectaste (puedeComprar ya implica que tiraste).
        SetInteractable(botonComprar,         !terminado && puedeComprar);
        // Pasar turno: recién cuando ya tiraste.
        SetInteractable(botonPasarTurno,      !terminado && yaTiro);
    }

    // Ayuda: setea interactable si el botón está conectado (guarda null).
    private void SetInteractable(Button boton, bool valor)
    {
        if (boton != null) boton.interactable = valor;
    }

    // Muestra los valores finales en los textos de los dados (versión sin animación).
    public void MostrarDados(int[] valores)
    {
        if (valores.Length > 0 && dado1Texto != null) dado1Texto.text = valores[0].ToString();
        if (valores.Length > 1 && dado2Texto != null) dado2Texto.text = valores[1].ToString();
        if (valores.Length > 2 && dado3Texto != null) dado3Texto.text = valores[2].ToString();
    }

    // Muestra la pantalla de fin con el ganador y activa el panel (con los botones Reiniciar/Menú).
    public void MostrarFin(string nombreGanador)
    {
        if (textoTurno != null) textoTurno.text = "Ganó " + nombreGanador;
        if (textoGanador != null) textoGanador.text = "¡Ganó " + nombreGanador + "!";
        if (panelFin != null) panelFin.SetActive(true);
    }

    // Muestra un mensaje de estado en pantalla (y también en la consola).
    public void MostrarMensaje(string txt)
    {
        Debug.Log(txt);
        if (textoEstado != null) textoEstado.text = txt;
    }

    // Activa o desactiva el hover de un slot de carta.
    private void ActivarHoverSlot(int indice, bool activo)
    {
        if (cartasImagen == null || indice >= cartasImagen.Length || cartasImagen[indice] == null) return;
        EfectoHoverCarta hover = cartasImagen[indice].GetComponentInParent<EfectoHoverCarta>();
        if (hover != null) hover.SetHoverActivo(activo);
    }

    // Muestra u oculta el marco de "en uso" de un slot de carta (guarda null por si no está conectado).
    private void MostrarMarcoUso(int indice, bool activo)
    {
        if (cartasMarcoUso == null || indice >= cartasMarcoUso.Length || cartasMarcoUso[indice] == null) return;
        cartasMarcoUso[indice].SetActive(activo);
    }

    // Busca un sprite por nombre, ignorando mayúsculas/minúsculas y sufijos "_0".
    public Sprite BuscarSprite(string nombreCarta)
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
}
