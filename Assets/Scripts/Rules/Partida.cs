// ============================================================
//  Partida.cs  —  Capa: RULES (reglas)
//  Maneja el CONTROL DEL FLUJO DE LA RONDA y la CONDICIÓN DE
//  VICTORIA: los jugadores, de quién es el turno, a quién se
//  apunta y quién ganó. Es lógica pura (no conoce Unity ni la UI).
// ============================================================

using System.Collections.Generic;

public class Partida
{
    private List<Jugador> jugadores = new List<Jugador>();

    public int IndiceActual { get; private set; } = 0;    // jugador en turno
    public int IndiceObjetivo { get; private set; } = 0;  // rival apuntado
    public bool Terminada { get; private set; } = false;  // ¿ya hay ganador?
    public Jugador Ganador { get; private set; } = null;  // el ganador (si Terminada)
    public int TurnosLeyMarcial { get; private set; } = 0; // turnos que quedan de Ley Marcial
    
    //hecho por pilar
    // Variables para rondas
    public int RondaActual { get; private set; } = 1;
    public int TotalRondas { get; private set; } = 0; // 0 = sin límite

    // Crea la partida con 'cantidad' jugadores, cada uno con 'hpInicial'.
    public Partida(int cantidad, int hpInicial, int totalRondas = 0)
    {
        TotalRondas = totalRondas;
        for (int i = 0; i < cantidad; i++)
            jugadores.Add(new Jugador("Jugador " + (i + 1), hpInicial));
        ElegirObjetivoPorDefecto();
    }

    // --- Accesos de lectura ---
    public List<Jugador> Jugadores { get { return jugadores; } }
    public int Cantidad { get { return jugadores.Count; } }
    public Jugador Actual()   { return jugadores[IndiceActual]; }
    public Jugador Objetivo() { return jugadores[IndiceObjetivo]; }

    // Pasa el turno al siguiente jugador VIVO.
    public void PasarTurno()
    {
        if (TurnosLeyMarcial > 0) TurnosLeyMarcial--;   // se va gastando Ley Marcial
        IndiceActual = SiguienteVivo(IndiceActual, false);
        
        //hecho por pilar
        // Si el jugador actual es el primero, avanzamos de ronda
        if (IndiceActual == 0)
        {
            RondaActual++;
            VerificarRondas();
        }
    }

    // Ley Marcial: activa que el próximo round todos deban Atacar.
    public void ActivarLeyMarcial() { TurnosLeyMarcial = jugadores.Count; }
    public bool LeyMarcialActiva()  { return TurnosLeyMarcial > 0; }

    // Fija el objetivo por defecto: el primer rival vivo después del actual.
    public void ElegirObjetivoPorDefecto()
    {
        IndiceObjetivo = SiguienteVivo(IndiceActual, true);
    }

    // Rota el objetivo al siguiente rival vivo.
    public void CambiarObjetivo()
    {
        IndiceObjetivo = SiguienteVivo(IndiceObjetivo, true);
    }

    // Busca el siguiente índice VIVO. Si 'soloRivales', excluye al jugador actual.
    private int SiguienteVivo(int desde, bool soloRivales)
    {
        int i = desde;
        for (int intento = 0; intento < jugadores.Count; intento++)
        {
            i = (i + 1) % jugadores.Count;
            if (soloRivales && i == IndiceActual) continue;
            if (jugadores[i].EstaVivo()) return i;
        }
        return soloRivales ? IndiceActual : desde;
    }

    // CONDICIÓN DE VICTORIA: si queda un solo jugador vivo, hay ganador.
    public void VerificarVictoria()
    {
        int vivos = 0;
        Jugador ultimo = null;
        foreach (Jugador j in jugadores)
            if (j.EstaVivo()) { vivos++; ultimo = j; }

        if (vivos <= 1)
        {
            Terminada = true;
            Ganador = ultimo;
        }
    }
    
    //hecho por pilar
    // Verifica si se alcanzó el límite de rondas
    private void VerificarRondas()
    {
        if (TotalRondas > 0 && RondaActual > TotalRondas)
        {
            // Si se acabaron las rondas, gana el jugador con más HP
            Terminada = true;
            Jugador ganador = null;
            int mayorHP = -1;
            
            foreach (Jugador j in jugadores)
            {
                if (j.hp > mayorHP)
                {
                    mayorHP = j.hp;
                    ganador = j;
                }
            }
            
            Ganador = ganador;
        }
    }
}
