// ============================================================
//  ManoOnline.cs  —  Capa: VISUAL (UI del juego online · B2.2)
//  Dibuja MI mano con los SPRITES reales de las cartas y deja
//  SELECCIONARLAS con click (marco de selección). La selección
//  la guarda EstadoOnline; los botones de acción la usan.
//  Reusa el criterio de BuscarSprite de la VistaJuegoUI de HotSeat.
//
//  La mano es PRIVADA: llega por el RPC dirigido de EstadoOnline y
//  este presenter se redibuja cuando dispara 'MiManoCambio'.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManoOnline : MonoBehaviour
{
    [Header("Estado sincronizado")]
    public EstadoOnline estado;

    [Header("Sprites de carta (mismos que la UI de HotSeat)")]
    public Sprite[] spritesCartas;

    [Header("Slots de la mano (en orden)")]
    public Image[] cartas;          // FRENTE de cada slot (se le agrega Button si no tiene)
    public Image[] reversos;        // (opcional) REVERSO de cada slot · se ve al hacer hover (como en HotSeat)
    public GameObject[] marcos;     // (opcional) marco de selección por slot · arranca apagado
    public TMP_Text[] textos;       // (opcional) nombre de la carta por slot, por si falta el sprite

    [Header("Colores")]
    public Color colorSeleccion = new Color(1f, 0.9f, 0.3f);   // tinte si NO hay marco asignado
    public Color colorSinSprite = new Color(0.2f, 0.2f, 0.2f); // slot con carta pero sin sprite encontrado
    public Color colorApagada   = new Color(0.4f, 0.4f, 0.4f, 1f);   // gris: carta que NO sirve para la acción elegida

    private GameObject[] raices;   // raíz (el 'carta') de cada slot: se apaga entero si el slot está vacío

    void Start()
    {
        // Cada slot se hace clickeable REUSANDO el Button del 'carta' padre (en HotSeat el slot YA es un Button).
        // Así, clickear en cualquier parte de la carta (frente, marco, etc.) selecciona esa carta.
        if (cartas != null)
        {
            raices = new GameObject[cartas.Length];
            for (int i = 0; i < cartas.Length; i++)
            {
                if (cartas[i] == null) continue;

                Button b = cartas[i].GetComponentInParent<Button>();   // el 'carta' padre es el botón
                if (b == null)
                {
                    // Si no hubiera botón en el padre, lo ponemos en el propio FRENTE.
                    b = cartas[i].gameObject.AddComponent<Button>();
                    cartas[i].raycastTarget = true;
                }
                raices[i] = b.gameObject;   // el 'carta': prender/apagar el slot entero

                b.transition = Selectable.Transition.None;   // que no pelee con el color/hover propio
                b.interactable = true;
                int idx = i;                                 // captura por valor
                b.onClick.RemoveAllListeners();              // saca el listener viejo (GameManager del copy/paste)
                b.onClick.AddListener(() => AlClickearCarta(idx));
            }
        }

        if (estado != null)
        {
            estado.MiManoCambio += Refrescar;
            estado.UICambio += Refrescar;   // re-tiñe las cartas cuando elegís/cambiás de acción
        }
        Refrescar();
    }

    void OnDestroy()
    {
        if (estado != null)
        {
            estado.MiManoCambio -= Refrescar;
            estado.UICambio -= Refrescar;
        }
    }

    // Lo dispara el click en un slot. EstadoOnline decide según la acción elegida:
    //  · en modo Descartar → descarta esa carta (pierde el turno);
    //  · si no → la selecciona/deselecciona para usarla en la jugada.
    public void AlClickearCarta(int indice)
    {
        if (estado == null) return;
        estado.ClickCarta(indice);
        Refrescar();
    }

    private void Refrescar()
    {
        if (cartas == null || estado == null) return;

        for (int i = 0; i < cartas.Length; i++)
        {
            bool hay = i < estado.MiMano.Count;

            // Slot vacío: apago el 'carta' entero (nada de hover ni reverso en blanco flotando).
            if (raices != null && i < raices.Length && raices[i] != null)
                raices[i].SetActive(hay);

            if (!hay) continue;

            string nombre = estado.MiMano[i];
            Sprite sp = BuscarSprite(nombre);

            // ¿Esta carta se resalta? Sin acción elegida, todas normales. Con acción elegida, solo las que
            // aplican (Descartar deja todas normales: podés tirar cualquiera). Mismo criterio que el HotSeat.
            bool resaltar = !estado.HaElegido
                || estado.AccionElegida == TipoAccion.Descartar
                || estado.CartaAplica(nombre, estado.AccionElegida);

            // FRENTE.
            if (cartas[i] != null)
            {
                cartas[i].gameObject.SetActive(true);
                cartas[i].sprite = sp;

                bool sel = estado.EstaSeleccionada(i);
                bool hayMarco = marcos != null && i < marcos.Length && marcos[i] != null;
                Color baseColor = (sp != null) ? Color.white : colorSinSprite;

                // Si la carta NO sirve para la acción elegida, va en gris (y tampoco se puede seleccionar).
                if (!resaltar)     cartas[i].color = colorApagada;
                else if (hayMarco) cartas[i].color = baseColor;                        // con marco: el marco marca la selección
                else               cartas[i].color = sel ? colorSeleccion : baseColor; // sin marco: tinte de respaldo
            }

            // REVERSO (se muestra al hacer hover): mismo criterio que HotSeat → "<nombre> Reverso".
            if (reversos != null && i < reversos.Length && reversos[i] != null)
            {
                Sprite rev = BuscarSprite(nombre + " Reverso");
                reversos[i].sprite = rev;
                reversos[i].color = !resaltar ? colorApagada : ((rev != null) ? Color.white : colorSinSprite);
            }

            // TEXTO (opcional).
            if (textos != null && i < textos.Length && textos[i] != null)
                textos[i].text = nombre;

            // MARCO de selección.
            if (marcos != null && i < marcos.Length && marcos[i] != null)
                marcos[i].SetActive(estado.EstaSeleccionada(i));
        }
    }

    // Mismo criterio que VistaJuegoUI: ignora mayúsculas y acepta el sufijo "_0".
    private Sprite BuscarSprite(string nombreCarta)
    {
        if (spritesCartas == null) return null;
        string buscado = nombreCarta.ToLower();
        foreach (Sprite s in spritesCartas)
            if (s != null && (s.name.ToLower() == buscado || s.name.ToLower() == buscado + "_0"))
                return s;
        return null;
    }
}
