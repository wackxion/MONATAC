// ============================================================
//  GestorAudio.cs  —  Capa: VISUAL (Unity)
//  Gestor central de sonido (patrón Singleton, igual que GameManager).
//  Reproduce la MÚSICA de fondo (en loop) y los EFECTOS (SFX).
//  Cualquier script llama:  GestorAudio.Instance.SonidoDados();
//
//  CÓMO CONECTARLO EN UNITY (resumen):
//    1) Crear un GameObject vacío llamado "GestorAudio".
//    2) Agregarle este script y DOS componentes AudioSource.
//    3) Arrastrar un AudioSource a "fuenteMusica" y otro a "fuenteSFX".
//    4) Arrastrar los clips (.mp3/.wav) a los campos de Clips.
// ============================================================

using UnityEngine;
using UnityEngine.UI;                 // para enganchar los Button
using UnityEngine.SceneManagement;    // para detectar cuando carga una escena

public class GestorAudio : MonoBehaviour
{
    // --- PATRÓN SINGLETON (una sola instancia global) ---
    public static GestorAudio Instance { get; private set; }

    [Header("Fuentes de audio (2 AudioSource)")]
    public AudioSource fuenteMusica;   // parlante de la música (loop)
    public AudioSource fuenteSFX;      // parlante de los efectos (PlayOneShot)

    [Header("Clips de música")]
    public AudioClip musicaFondo;      // música que suena todo el tiempo

    [Header("Clips de efectos (SFX)")]
    public AudioClip clic;             // clic de botones (UI)
    public AudioClip danio;            // al recibir daño
    public AudioClip curacion;         // al curarse
    public AudioClip dados;            // al tirar los dados
    public AudioClip comprar;          // al comprar una carta
    public AudioClip cambiarObjetivo;  // al cambiar de objetivo
    public AudioClip ganador;          // en la pantalla de fin (victoria)
    public AudioClip monedasMuchas;    // al recolectar mucho (>= Reglas.RecoleccionAlta)
    public AudioClip monedasPocas;     // al recolectar poco

    // ¿El sonido está silenciado? (solo lectura desde afuera)
    public bool silenciado { get; private set; }

    // Volumen de música y efectos (0 a 1). Solo lectura desde afuera.
    public float volumenMusica { get; private set; } = 1f;
    public float volumenSFX    { get; private set; } = 1f;

    void Awake()
    {
        // Si ya existe otro GestorAudio, este se destruye (única instancia).
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // sobrevive al cambio de escena → la música no se corta
    }

    void Start()
    {
        // Recupera lo que el jugador dejó guardado (silencio y volúmenes) con PlayerPrefs.
        silenciado    = PlayerPrefs.GetInt("silenciado", 0) == 1;
        volumenMusica = PlayerPrefs.GetFloat("volMusica", 1f);
        volumenSFX    = PlayerPrefs.GetFloat("volSFX", 1f);
        AplicarVolumenes();
        AplicarSilencio();

        // Arranca la música de fondo al iniciar.
        if (musicaFondo != null)
            ReproducirMusica(musicaFondo);

        // Engancha los botones de la escena actual (la primera, ej: MENU).
        EngancharBotones();
        // Y se suscribe para enganchar los botones de cada escena que se cargue después.
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        // Buena práctica: desuscribirse para no dejar referencias colgadas.
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // Se ejecuta cada vez que se carga una escena nueva (juego, personalización, etc.).
    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        EngancharBotones();
    }

    // Hace que TODOS los botones de la escena reproduzcan el clic al presionarse.
    private void EngancharBotones()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in botones)
        {
            // Le sumamos el sonido de clic a lo que el botón ya hacía (no lo reemplaza).
            b.onClick.AddListener(SonidoClic);
        }
    }

    // --- MÚSICA: reproduce un clip en loop ---
    public void ReproducirMusica(AudioClip clip)
    {
        if (fuenteMusica == null || clip == null) return;
        fuenteMusica.clip = clip;
        fuenteMusica.loop = true;   // se repite sin cortar
        fuenteMusica.Play();
    }

    // --- SFX: reproduce un efecto corto (pueden solaparse varios) ---
    public void ReproducirSFX(AudioClip clip)
    {
        if (fuenteSFX == null || clip == null) return;
        fuenteSFX.PlayOneShot(clip);
    }

    // --- Atajos para los sonidos concretos del juego ---
    // (así otros scripts llaman GestorAudio.Instance.SonidoDados() sin pasar el clip)
    public void SonidoClic()            { ReproducirSFX(clic); }
    public void SonidoDanio()           { ReproducirSFX(danio); }
    public void SonidoCuracion()        { ReproducirSFX(curacion); }
    public void SonidoDados()           { ReproducirSFX(dados); }
    public void SonidoComprar()         { ReproducirSFX(comprar); }
    public void SonidoCambiarObjetivo() { ReproducirSFX(cambiarObjetivo); }
    public void SonidoGanador()         { ReproducirSFX(ganador); }
    public void SonidoMonedasMuchas()   { ReproducirSFX(monedasMuchas); }
    public void SonidoMonedasPocas()    { ReproducirSFX(monedasPocas); }

    // ========================================================
    //  OPCIONES DE SONIDO (las usa el botón/panel de sonido)
    // ========================================================

    // --- MUTE: silenciar / reactivar TODO el sonido de un toque ---
    // Lo llama el BotonSonido. Da vuelta la bandera 'silenciado', aplica el
    // cambio a las dos fuentes y lo GUARDA para que se recuerde la próxima vez.
    public void AlternarSonido()
    {
        silenciado = !silenciado;                                // invierte: true<->false
        AplicarSilencio();                                       // aplica el mute a las AudioSource
        PlayerPrefs.SetInt("silenciado", silenciado ? 1 : 0);    // guarda (1 = silenciado, 0 = con sonido)
    }

    // Pone el 'mute' de las dos fuentes según la bandera 'silenciado'.
    // 'mute = true' apaga el sonido de esa fuente sin cambiar su volumen.
    private void AplicarSilencio()
    {
        if (fuenteMusica != null) fuenteMusica.mute = silenciado;
        if (fuenteSFX != null)    fuenteSFX.mute = silenciado;
    }

    // --- VOLUMEN: lo regulan los sliders del PanelSonido ---
    // Ajusta el volumen de la MÚSICA. 'v' va de 0 (mudo) a 1 (máximo).
    public void SetVolumenMusica(float v)
    {
        volumenMusica = Mathf.Clamp01(v);                        // Clamp01 = fuerza el valor a quedar entre 0 y 1
        if (fuenteMusica != null) fuenteMusica.volume = volumenMusica;
        PlayerPrefs.SetFloat("volMusica", volumenMusica);        // recuerda el valor
    }

    // Ajusta el volumen de los EFECTOS (SFX). 'v' va de 0 a 1.
    public void SetVolumenSFX(float v)
    {
        volumenSFX = Mathf.Clamp01(v);
        // Nota: PlayOneShot (que usamos para los SFX) toma el volumen de esta
        // fuente como multiplicador, así que cambiar 'volume' regula los efectos.
        if (fuenteSFX != null) fuenteSFX.volume = volumenSFX;
        PlayerPrefs.SetFloat("volSFX", volumenSFX);
    }

    // Aplica de una los volúmenes guardados a las dos fuentes.
    // Se llama en Start() al recuperar los valores de PlayerPrefs.
    private void AplicarVolumenes()
    {
        if (fuenteMusica != null) fuenteMusica.volume = volumenMusica;
        if (fuenteSFX != null)    fuenteSFX.volume = volumenSFX;
    }
}
