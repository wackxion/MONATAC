// ============================================================
//  LoadingScreen.cs  —  Capa: VISUAL (Unity)
//  Pantalla de carga que muestra una barra de progreso mientras
//  se carga la escena del juego en background.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI")]
    public Image barraProgreso;
    public TextMeshProUGUI textoCargando;

    [Header("Configuración")]
    public string nombreEscena = "juego";
    public float duracionMinima = 1.0f;

    private void Start()
    {
        StartCoroutine(CargarEscena());
    }

    private IEnumerator CargarEscena()
    {
        AsyncOperation carga = SceneManager.LoadSceneAsync(nombreEscena);
        carga.allowSceneActivation = false;

        float tiempoInicio = Time.time;
        float progresoVisual = 0f;

        while (carga.progress < 0.9f || Time.time - tiempoInicio < duracionMinima)
        {
            float progresoReal = Mathf.Clamp01(carga.progress / 0.9f);
            progresoVisual = Mathf.MoveTowards(progresoVisual, progresoReal, Time.deltaTime / duracionMinima);

            if (barraProgreso != null)
                barraProgreso.fillAmount = progresoVisual;

            if (textoCargando != null)
                textoCargando.text = "Cargando... " + Mathf.RoundToInt(progresoVisual * 100) + "%";

            yield return null;
        }

        if (barraProgreso != null)
            barraProgreso.fillAmount = 1f;

        carga.allowSceneActivation = true;
    }
}