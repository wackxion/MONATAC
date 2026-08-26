using UnityEngine;
using UnityEngine.EventSystems;

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

    // --- Variables internas ---
    private Vector3 posicionOriginal;    // dónde estaba la carta antes del hover
    private Vector3 posicionLevantada;   // posición a la que sube
    private Vector3 escalaOriginal;      // tamaño original de la carta
    private Vector3 escalaAgrandada;     // tamaño cuando está encima del mouse
    private bool estaEncima = false;     // ¿el mouse está sobre esta carta?

    private void Start()
    {
        // Si no se asignó un gráfico, animamos el mismo objeto
        if (graficoCarta == null)
            graficoCarta = transform;

        // Guardamos la posición original para poder volver después
        posicionOriginal = graficoCarta.localPosition;
        posicionLevantada = posicionOriginal + new Vector3(0, cantidadLevantar, 0);

        // Lo mismo con la escala
        escalaOriginal = graficoCarta.localScale;
        escalaAgrandada = escalaOriginal * escalaHover;
    }

    private void Update()
    {
        // Movimiento suave: va hacia la posición objetivo (levanta o baja)
        Vector3 objetivoPos = estaEncima ? posicionLevantada : posicionOriginal;
        graficoCarta.localPosition = Vector3.Lerp(
            graficoCarta.localPosition,
            objetivoPos,
            Time.deltaTime * velocidadAnimacion
        );

        // Escala suave: se agranda o vuelve al tamaño original
        Vector3 objetivoEscala = estaEncima ? escalaAgrandada : escalaOriginal;
        graficoCarta.localScale = Vector3.Lerp(
            graficoCarta.localScale,
            objetivoEscala,
            Time.deltaTime * velocidadEscala
        );
    }

    // El EventSystem de Unity llama esto cuando el mouse entra al área de la carta.
    // Funciona con UI de Canvas
    public void OnPointerEnter(PointerEventData eventData)
    {
        estaEncima = true;
    }

    // El EventSystem llama esto cuando el mouse sale del área de la carta.
    public void OnPointerExit(PointerEventData eventData)
    {
        estaEncima = false;
    }
}