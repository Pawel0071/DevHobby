# RPG.AI

Ten moduł zawiera Utility AI wykorzystywane przez serwer gry do sterowania NPC. Repozytorium implementuje kontekst `AiContext`, definicje akcji, katalog akcji, oceny użyteczności i dyrektywy przekazywane dalej do pola bitwy.

## Cykl życia `AiContext`

- `NpcAiService` przechowuje konteksty w pamięci (`ConcurrentDictionary<Guid, AiContext>`) i **re-używa** ich pomiędzy kolejnymi tickami.
- W każdym kroku ticku metoda `PrepareContext` aktualizuje bieżące pola (`Self`, `NearbyPlayers`, `Target`, statystyki, tablice zagrożeń).
- Po zakończonym ticku kontekst może zostać wyczyszczony przez `AiContext.Reset`. Metoda przyjmuje flagi (`AiContextResetOptions`), które pozwalają zachować np. tablice cooldownów lub blackboard, co minimalizuje alokacje.
- Nie twórz nowego `AiContext` w każdej akcji – koszt GC byłby wysoki. Używaj istniejącego egzemplarza i wywołuj `Reset` tylko gdy NPC został usunięty z pamięci (np. despawn).

## Helpery odległości

`AiContext.CalculateDistanceTo` i `UpdateDistanceToTarget` obsługują brak lokalizacji oraz zwracają `float.PositiveInfinity`, dzięki czemu wszystkie rozważania dystansu zachowują się przewidywalnie (brak „skoków” do `(0,0,0)`).

## Testy

Minimalne testy dla Utility AI znajdują się w `RPG.UnitTest/Core/NPCAiTests` i obejmują:
- `CooldownConsiderationTests`
- `UtilityAgentTests`
- `UtilityActionCatalogTests`

Przy dodawaniu nowych rozważań/akcji rozbuduj ten katalog, aby zachować kontrolę nad kontraktami.

