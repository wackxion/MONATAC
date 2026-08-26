using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EfectoHoverCarta : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // --- Configuración desde el Inspector ---

    [Header("Referencia al gráfico de la carta")]
    public Transform graficoCarta;

    [Header("Animación de levantamiento")]
    public float cantidadLevantar = 1.5f;
    public float velocidadAnimacion = 10f;

    [Header("Animación de escala")]
    public float escalaHover = 1.15f;
    public float velocidadEscala = 8f;

    // [AGREGADO POR JULIAN] Frente y reverso de la carta
    [Header("Voltear carta (frente / reverso)")]
    [Tooltip("Image del frente de la carta (se oculta al hacer hover)")]
    public Image imagenFrente;
    [Tooltip("Image del reverso de la carta (se muestra al hacer hover)")]
    public Image imagenReverso;

    // --- Variables internas ---
    private Vector3 posicionOriginal;
    private Vector3 posicionLevantada;
    private Vector3 escalaOriginal;
    private Vector3 escalaAgrandada;
    private bool estaEncima = false;
    // [AGREGADO POR JULIAN] Bloquea el hover cuando no hay carta en el slot
    private bool puedeHover = true;

    private void Start()
    {
        if (graficoCarta == null)
            graficoCarta = transform;

        posicionOriginal = graficoCarta.localPosition;
        posicionLevantada = posicionOriginal + new Vector3(0, cantidadLevantar, 0);

        escalaOriginal = graficoCarta.localScale;
        escalaAgrandada = escalaOriginal * escalaHover;

        // [AGREGADO POR JULIAN] Al inicio, mostrar frente y ocultar reverso
        MostrarFrente();
    }

    private void Update()
    {
        // Movimiento suave
        Vector3 objetivoPos = estaEncima ? posicionLevantada : posicionOriginal;
        graficoCarta.localPosition = Vector3.Lerp(
            graficoCarta.localPosition,
            objetivoPos,
            Time.deltaTime * velocidadAnimacion
        );

        // Escala suave
        Vector3 objetivoEscala = estaEncima ? escalaAgrandada : escalaOriginal;
        graficoCarta.localScale = Vector3.Lerp(
            graficoCarta.localScale,
            objetivoEscala,
            Time.deltaTime * velocidadEscala
        );
    }

    // [AGREGADO POR JULIAN] Muestra el frente y oculta el reverso
    public void MostrarFrente()
    {
        if (imagenFrente != null) imagenFrente.gameObject.SetActive(true);
        if (imagenReverso != null) imagenReverso.gameObject.SetActive(false);
    }

    // [AGREGADO POR JULIAN] Muestra el reverso y oculta el frente
    public void MostrarReverso()
    {
        if (imagenFrente != null) imagenFrente.gameObject.SetActive(false);
        if (imagenReverso != null) imagenReverso.gameObject.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!puedeHover) return;
        estaEncima = true;
        MostrarReverso();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!puedeHover) return;
        estaEncima = false;
        MostrarFrente();
    }

    // [AGREGADO POR JULIAN] Activa o desactiva el hover desde el GameManager
    public void SetHoverActivo(bool activo)
    {
        puedeHover = activo;
        if (!activo)
        {
            estaEncima = false;
            MostrarFrente();
        }
    }
}