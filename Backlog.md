# 📋 MONATAC — Backlog de Programación

Listado organizado de las tareas de programación del proyecto, agrupadas por área.
**Estado:** ✅ hecho · 🟡 en progreso · 🔜 pendiente.

---

## 1. Datos (capa `/Data`)
| Tarea | Estado |
|---|---|
| Enums `TipoAccion` y `TipoCarta` | ✅ |
| Clase `Jugador` (HP, monedas, mano) con encapsulamiento | ✅ |
| Clase abstracta `Carta` + 6 subtipos (herencia) | ✅ |
| Comodines (`Multiplicador`, `Dado`) y variantes (`BolsilloRoto`, `VampirismoDefensivo`) | ✅ |
| `Mazo` y `PilaDescarte` (con reciclado circular) | ✅ |
| `Config` (static, pasa datos entre escenas) | ✅ |

## 2. Reglas (capa `/Rules`)
| Tarea | Estado |
|---|---|
| Clase `Dado` (generación de azar) | ✅ |
| `FabricaDeCartas` — patrón Factory, arma las 54 cartas | ✅ |
| Control del flujo del turno (elegir → tirar → pasar) | ✅ |
| Condición de victoria (último jugador vivo) | ✅ |
| Clase `Partida` — flujo de ronda, turnos y victoria (SRP) | ✅ |
| Clase `GestorCartas` — resolución de cartas (bonus, comodines, descarte) y combate defensivo | ✅ |
| Grupales polimórficas (`AplicarATodos` + `ContextoGrupal`) | ✅ |

## 3. Acciones principales
| Tarea | Estado |
|---|---|
| Acción **Atacar** (3d4 + cartas) | ✅ |
| Acción **Curarse** (2d4 + cartas) | ✅ |
| Acción **Recolectar** (3d4 + comprar) | ✅ |
| Anunciar la acción antes de tirar los dados | ✅ |
| Elegir objetivo del ataque (rota rivales vivos) | ✅ |

## 4. Sistema de cartas
| Tarea | Estado |
|---|---|
| Comprar cartas al Recolectar (opcional, acumulable) | ✅ |
| Elegir qué cartas usar de la mano (`OnUsarCarta`) | ✅ |
| Bonus de cartas por polimorfismo (`BonusPara`) | ✅ |
| Orden fijo: dados extra → multiplicador → bonus | ✅ |
| Cartas defensivas automáticas (reflejo / absorción) — un solo uso | ✅ |
| Cartas de vencimiento con pago por uso | ✅ |
| Cartas grupales (Colecta, Exceso, Penitencia, Descarte, Ley Marcial) | ✅ |
| Descarte y mazo circular | ✅ |

## 5. Visual (capa `/Visual`)
| Tarea | Estado |
|---|---|
| 4 barras de HP con marca turno / objetivo / eliminado | ✅ |
| Dados en pantalla | ✅ |
| Monedas, cartel de turno y texto de estado | ✅ |
| Mano de cartas en pantalla (texto) | ✅ |
| Imágenes/sprites de cartas | ✅ |
| Animación hover de cartas | ✅ |
| 5º slot de mano (la mano máxima es 5) | ✅ |
| Animaciones de dados / daño / feedback | 🔜 |

## 6. Menú y escenas
| Tarea | Estado |
|---|---|
| Escena `MENU` con botones 2 / 3 / 4 jugadores | ✅ |
| Cargar la escena `juego` con la cantidad elegida | ✅ |
| Botón Multijugador Online (placeholder "Próximamente") | ✅ |

## 7. Arquitectura y patrones
| Tarea | Estado |
|---|---|
| Organización en carpetas `/Data`, `/Rules`, `/Visual` | ✅ |
| Patrón **Singleton** (`GameManager.Instance`) | ✅ |
| Patrón **Factory** (`FabricaDeCartas`) | ✅ |
| Herencia + polimorfismo (cartas) | ✅ |
| Encapsulamiento del estado (`Jugador`) | ✅ |
| Separación de reglas y UI: `Partida` + `GestorCartas` (SRP) | ✅ |
| Diagramas UML (clases, estados, secuencia) | ✅ |

## 8. Hito 2 — Arquitectura MVP + DIP
| Tarea | Estado |
|---|---|
| `/Data` y `/Rules` sin motor gráfico (usar `System.Random`, no `UnityEngine.Random`) | 🔜 |
| Persistencia de **3 datos** del menú → juego (jugadores + HP inicial + límite de rondas) | 🔜 |
| HP inicial elegible en el menú (**50 / 100 / 150 / 200**) | 🔜 |
| **Limitador de rondas** elegible en el menú | 🔜 |
| Clase `abstract Accion` (herencia + polimorfismo en las acciones) | 🔜 |
| **DIP**: `interface` de la vista + `event` (Rules y Visual no se conocen directo) | 🔜 |
| **MVP estricto**: Presenter en `/Rules` (Modelo en `/Data`, Vista en `/Visual`) | 🔜 |

---

## 🔜 Pendientes (backlog futuro, por prioridad)
1. **Multijugador online** (el botón ya está preparado).
2. **Balance y pruebas**: HP inicial configurable (hoy 40 para probar, real 100), testeo de partidas de 3–4 jugadores.
3. **Cartas grupales más fieles** (ej. aviso visual de Ley Marcial, elección de carta en Descarte Grupal).
4. **Acción secundaria nueva** del jugador (ej. *Defenderse*), usando la jerarquía `abstract Accion` ya creada.
