// ============================================================
//  JugadorTests.cs  —  Pruebas unitarias (EditMode / NUnit)
//  Verifica las reglas de la clase Jugador (capa Data).
//  Son EditMode porque Jugador es una clase C# pura: se crea
//  con "new" y se prueba sin abrir el juego.
//
//  Cada test sigue el patrón AAA: Arrange, Act, Assert.
// ============================================================

using NUnit.Framework;

[TestFixture]
public class JugadorTests
{
    // ------------------------------------------------------------
    //  REGLA: al recibir daño, el HP nunca queda negativo.
    //  Data-driven: varios casos en un solo test con [TestCase].
    //  Parámetros: (hpInicial, danio, hpEsperado)
    // ------------------------------------------------------------
    [TestCase(40, 10, 30, Description = "Daño normal resta bien")]
    [TestCase(40, 40,  0, Description = "Daño exacto deja en 0")]
    [TestCase(40, 50,  0, Description = "Daño mayor NO baja de 0")]
    public void RecibirDanio_NuncaBajaDeCero(int hpInicial, int danio, int hpEsperado)
    {
        // ARRANGE
        Jugador jugador = new Jugador("Test", hpInicial);

        // ACT
        jugador.RecibirDanio(danio);

        // ASSERT
        Assert.AreEqual(hpEsperado, jugador.hp, "El HP no puede ser negativo.");
    }

    // ------------------------------------------------------------
    //  REGLA: al curarse, el HP nunca supera el máximo.
    //  El jugador arranca con 40/40, recibe 30 (queda en 10),
    //  y luego se cura 'cura'. Parámetros: (cura, hpEsperado)
    // ------------------------------------------------------------
    [TestCase(5,  15, Description = "Cura parcial suma normal")]
    [TestCase(30, 40, Description = "Cura exacta llega al máximo")]
    [TestCase(99, 40, Description = "Cura excesiva NO supera el máximo")]
    public void Curar_NoSuperaElMaximo(int cura, int hpEsperado)
    {
        // ARRANGE
        Jugador jugador = new Jugador("Test", 40);
        jugador.RecibirDanio(30);   // queda en 10 HP

        // ACT
        jugador.Curar(cura);

        // ASSERT
        Assert.AreEqual(hpEsperado, jugador.hp, "El HP no puede superar el máximo.");
    }

    // ------------------------------------------------------------
    //  REGLA: GastarMonedas devuelve false y no gasta si no alcanza.
    //  Parámetros: (monedasIniciales, costo, exitoEsperado, monedasFinales)
    // ------------------------------------------------------------
    [TestCase(10, 6, true,  4, Description = "Alcanza: gasta y devuelve true")]
    [TestCase( 6, 6, true,  0, Description = "Justo: gasta todo")]
    [TestCase( 5, 6, false, 5, Description = "No alcanza: no gasta, devuelve false")]
    public void GastarMonedas_SoloSiAlcanza(int monedasIniciales, int costo, bool exitoEsperado, int monedasFinales)
    {
        // ARRANGE
        Jugador jugador = new Jugador("Test", 40);
        jugador.GanarMonedas(monedasIniciales);

        // ACT
        bool exito = jugador.GastarMonedas(costo);

        // ASSERT
        Assert.AreEqual(exitoEsperado, exito, "El resultado de gastar no coincide.");
        Assert.AreEqual(monedasFinales, jugador.monedas, "Las monedas restantes no coinciden.");
    }

    // ------------------------------------------------------------
    //  REGLA: EstaVivo depende de si el HP es mayor que 0.
    // ------------------------------------------------------------
    [Test]
    public void EstaVivo_EsFalsoConCeroHP()
    {
        // ARRANGE
        Jugador jugador = new Jugador("Test", 10);

        // ACT
        jugador.RecibirDanio(10);   // lo deja en 0

        // ASSERT
        Assert.IsFalse(jugador.EstaVivo(), "Con 0 HP el jugador debe estar muerto.");
    }
}
