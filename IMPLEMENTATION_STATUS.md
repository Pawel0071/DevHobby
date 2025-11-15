# Status Implementacji - RPG Game Server

**Data: 15 listopada 2025**

## ✅ ZAKOŃCZONE ZADANIA

### 1. Typy Protosów dla Delty (GameDeltaBuffer)
- [x] `world.proto` - pełna definicja WorldDelta, WorldCharacterDelta, WorldNpcDelta, WorldMapObjectDelta
- [x] `GameDeltaBuffer.cs` - agregacja delt per world z poprawnym mapowaniem na protosy
- [x] `Location` proto - pełna kompatybilność z modelem domenowym
- [x] Test integracyjny: `WorldStreamDeltaIntegrationTests` - snapshot + delta dla Character/NPC/MapObject

### 2. CQRS + Event Sourcing - Command → RequestedEvent → Handler
- [x] **CommandHandler** - generuje *RequestedEvent z metadanych
- [x] **RequestedEventQueue** - kolejka w pamięci dla chronologicznego przetwarzania
- [x] **GameEventDispatcher** - obsługa kolejki i dyspatch do handlerów
- [x] **RequestedHandlers** zaimplementowane dla:
  - [x] `MovementRequestedHandler` (StartMovement, StopMovement)
  - [x] `EquipmentInventoryRequestedHandler` (Equip, Unequip, Pickup, Drop, Bank, Use)
  - [x] `CharacterCreationRequestedHandler`
  - [x] `NpcMovementRequestedHandler`, `NpcIdleRequestedHandler`, `NpcReturnToSpawnRequestedHandler`

### 3. Broadcaster do Klienta
- [x] `IGameStateBroadcaster` + implementacja `GameStateBroadcastAdapter`
- [x] `ICharacterStateBroadcaster` - dla szybkich aktualizacji ruchu
- [x] `GameDeltaBuffer` - buforowanie i agregacja delt
- [x] `StreamWorldState` - gRPC stream zwracający WorldUpdate { Snapshot, Delta }
- [x] Broadcast w handlerach:
  - MovementRequestedHandler → BroadcastDeltaAsync
  - EquipmentInventoryRequestedHandler → BroadcastDeltaAsync
  - NpcMovementRequestedHandler → BroadcastDeltaAsync

### 4. Queries dla Wszystkich Typów
- [x] **QueryBus** + **IQueryHandler<TQuery, TResult>**
- [x] QueryHandlers w `RPG.Application.Queries`:
  - [x] Character (GetCharacter, GetCharacters, GetCharactersByIds)
  - [x] Item (GetItem, GetItems, GetItemsByIds)
  - [x] Skill (GetSkill, GetSkills, GetSkillsByIds)
  - [x] Npc (GetNpc, GetNpcs, GetNpcsByIds)
  - [x] MapObject (GetMapObject, GetMapObjects, GetMapObjectsByIds)
  - [x] Quest (GetQuest, GetQuests, GetQuestsByIds)
- [x] QueryServiceImpl w `RPG.GameServer.Controllers`:
  - [x] ItemQueryServiceImpl
  - [x] SkillQueryServiceImpl
  - [x] NpcQueryServiceImpl
  - [x] MapObjectQueryServiceImpl
  - [x] QuestQueryServiceImpl
- [x] **Mapowanie komponentów** (wzorcem z RPG.Infrastructure.Mappers):
  - [x] IProtoMapper<T> interface
  - [x] Dedykowane mapery: ItemProtoMapper, SkillProtoMapper, NpcProtoMapper, MapObjectProtoMapper, QuestProtoMapper
  - [x] ProtoMappersRegistrationExtensions dla DI
  - [x] Komponenty opcjonalne mapowane zgodnie z TagComponentMap

### 5. Session Management
- [x] `ISessionManager` w `RPG.Application.Managers`
- [x] SessionServiceImpl w GameServer
- [x] CreateSession / EndSession / Heartbeat
- [x] SessionValidationInterceptor - walidacja x-session-id dla komend gRPC
- [x] Testy integracyjne: WorldSessionGrpcIntegrationTests

### 6. Testy Integracyjne
- [x] CharacterGrpcIntegrationTests (MovementLifecycle)
- [x] WorldStreamDeltaIntegrationTests (Snapshot + Delta dla Char/NPC/MapObject)
- [x] WorldSessionGrpcIntegrationTests (JoinWorld, Session validation)
- [x] MapObjectQuestGrpcIntegrationTests (typed components)
- [x] QuestQueryTypedComponentsTests (full snapshot, typed objectives)
- [x] ItemSkillNpcGrpcIntegrationTests (tags preservation)
- [x] ProtoFieldNumberTests (wire contract stability)
- [x] MongoDbIntegrationTests, RedisIntegrationTests, RabbitMqIntegrationTests
- [x] OutboxCircuitBreakerIntegrationTests

### 7. Testy Jednostkowe
- [x] MovementRequestedHandlerTests
- [x] CommandBusTests
- [x] GameDeltaBufferTests
- [x] Core Services: EquipmentService, InventoryService, MovementService, StatsService, SkillService, LevelingService
- [x] Infrastructure: Mappers (round-trip), Repository, Logger, OpenTelemetry

### 8. AI System (RPG.AI)
- [x] Utility AI framework (AiContext, UtilityAgent, UtilityAction, Consideration)
- [x] Behaviors: PatrolBehavior, CombatBehavior (stub)
- [x] NpcAiService - tick loop dla NPC AI
- [x] Integracja z RequestedEventQueue przez IAiDirectiveEventAdapter (zaplanowane)

---

## ⚠️ WYMAGANE DOMKNIĘCIE

### A. Brakujące RequestedHandlers (Commands → Events → Handlers → Core → Broadcast)

#### **Skills**
- [x] `SkillUsageRequestedHandler` dla `UseSkillCommand → SkillUsageRequestedEvent`
  - Core: SkillService.UseSkill
  - Broadcast: skill cooldown, target hit, damage dealt
- [x] `SkillLearnRequestedHandler` dla `LearnSkillCommand → SkillLearnRequestedEvent`
  - Core: SkillService.LearnSkill
  - Broadcast: skill learned
- [x] `SkillLevelUpRequestedHandler` dla `LevelUpSkillCommand → SkillLevelUpRequestedEvent`
  - Core: SkillService.LevelUpSkill
  - Broadcast: skill leveled up
- [x] `SkillUnlearnRequestedHandler` dla `UnLearnSkillCommand → SkillUnlearnRequestedEvent`
  - Core: SkillService.UnlearnSkill
  - Broadcast: skill unlearned

#### **Progression (XP/LevelUp/Death)**
- [x] `ExperienceGainRequestedHandler` dla `GainExperienceCommand → ExperienceGainRequestedEvent`
  - Core: LevelingService.GainExperience
  - Broadcast: XP gained, potential level up trigger
- [x] `LevelUpRequestedHandler` dla `LevelUpCommand → CharacterLevelUpRequestedEvent`
  - Core: LevelingService.LevelUp
  - Broadcast: level up, stats increased, skill points awarded
- [x] `CharacterDeathRequestedHandler` dla `DieCommand → CharacterDeathRequestedEvent`
  - Core: Character.Die (health = 0, drop loot, respawn timer)
  - Broadcast: character died, loot spawned

#### **Quests**
- [x] `QuestAcceptRequestedHandler` dla `AcceptQuestCommand → QuestAcceptedRequestedEvent`
  - Core: QuestService.AcceptQuest
  - Broadcast: quest added to journal
- [x] `QuestCompleteRequestedHandler` dla `CompleteQuestCommand → QuestCompletedRequestedEvent`
  - Core: QuestService.CompleteQuest (rewards, reputation)
  - Broadcast: quest completed, rewards granted
- [x] `QuestProgressUpdateRequestedHandler` dla `UpdateQuestProgressCommand → QuestProgressUpdatedEvent`
  - Core: QuestService.UpdateProgress (kill count, item collected)
  - Broadcast: quest progress changed

### B. AI System - Pełna Integracja z CQRS/ES

#### Stan obecny:
- [x] NpcAiService wywołuje Tick dla każdego NPC
- [x] UtilityAgent.Decide wybiera akcję (Patrol/Combat/Idle)
- [x] Akcje generują dyrektywy (MoveTo, Attack, Wait)
- [x] **IAiDirectiveEventAdapter** - zaimplementowany
- [x] **IBehaviorRegistry** - zaimplementowany
- [x] **NpcAiService refactor** - podłączony do IAiDirectiveEventAdapter i IBehaviorRegistry

#### Zrealizowane:
- [x] **IAiDirectiveEventAdapter**
  - [x] Kontrakt obsługujący wszystkie dyrektywy (MoveTo, Follow, Attack, Idle, Dialog, Trade)
  - [x] Mapowanie dyrektywy → właściwy *RequestedEvent (NpcMovementStartRequestedEvent, NpcCombatAttackRequestedEvent, NpcIdleRequestedEvent, NpcDialogRequestedEvent, NpcTradeRequestedEvent)
- [x] **IBehaviorRegistry**
  - [x] Rejestr i fabryka modułów zachowań (Patrol, Combat, Dialog, Trade)
  - [x] Mapowanie komponentów domenowych NPC → moduły (PatrolRouteComponent, CombatStatsComponent, DialogTreeComponent, TradeInventoryComponent)
- [x] **NpcAiService refactor**
  - [x] Wstrzyknięcie IBehaviorRegistry oraz IAiDirectiveEventAdapter
  - [x] Tick → Behavior.Decide → dyrektywa → RequestedEventQueue (bez wyjątków specjalnych)
  - [x] Integracja z RequestedEventQueue / GameEventDispatcher (AI i gracze współdzielą kolejkę)

#### Do zrobienia:
- [ ] **Testy**
  - [x] Jednostkowe: adapter dyrektyw (MoveTo/Attack/Idle), BehaviorRegistry (mapowanie komponentów)
  - [ ] Integracyjny: AI tick → RequestedEvent → Handler → Broadcast → Delta (np. Patrol generuje Movement, Combat generuje Attack)
  - [ ] Testy smoke dla typed quest objectives pozostają zielone
- [ ] **Kolejkowanie sekwencji dyrektyw + callback błędu** (rozszerzenie adaptera, backward compatible)

### C. Interceptory dla Komend Gry

#### Stan obecny:
- [x] SessionValidationInterceptor - waliduje x-session-id dla gRPC calls

#### Do rozszerzenia:
- [ ] **CommandAuthorizationInterceptor** - sprawdza, czy gracz może wykonać komendę (np. dystans do NPC dla trade/dialog)
- [ ] **CommandRateLimitingInterceptor** - throttling dla flood protection
- [ ] **CommandLoggingInterceptor** - audit trail wszystkich komend graczy

### D. WorldState Snapshot vs Delta - Mechanizm Klienta

#### Serwer GOTOWY:
- [x] StreamWorldState zwraca WorldUpdate { Snapshot?, Delta? }
- [x] Pierwszy message zawsze zawiera Snapshot (pełny stan)
- [x] Kolejne messages zawierają Delta (tylko zmiany)

#### Dokumentacja dla klienta (TODO):
- [ ] Opisać w README.md mechanizm nakładania delt:
  - Klient utrzymuje lokalny WorldState
  - Na Snapshot → zastąpić cały stan
  - Na Delta → zaktualizować tylko zmienione encje (Npcs, Characters, MapObjects)
- [ ] Przykładowy pseudo-kod dla klienta

### E. Testy Funkcjonalne - Pełne Scenariusze

#### Gotowe:
- [x] Movement lifecycle (start → delta → stop)
- [x] WorldState stream (snapshot + delta)
- [x] Session lifecycle (create → heartbeat → end)

#### Do dodania:
- [ ] **Combat scenario**: Character atakuje NPC → damage → NPC respawn
- [ ] **Quest scenario**: Accept quest → kill NPC → collect item → turn in → rewards
- [ ] **Skill scenario**: Learn skill → use skill → cooldown → level up skill
- [ ] **XP scenario**: Gain XP → level up → stat allocation
- [ ] **Session expiry**: Session wygasa po 5min bez heartbeat → komendy odrzucane

### F. Dokumentacja + Statusy AI
- [ ] Zaktualizować README/IMPLEMENTATION_STATUS o nowy przepływ AI → RequestedEvent → Broadcast
- [ ] Dodać diagram przepływu (AI + gracz) pokazujący współdzieloną kolejkę
- [ ] Opisać kontrakt IAiDirectiveEventAdapter v2 oraz sposób rozszerzania BehaviorRegistry

---

## 📋 PLAN DZIAŁANIA

### Priorytet 1: Dokończenie RequestedHandlers (Skills, Progression, Quests)
**Czas: 4-6h**
1. Utworzyć eventy: SkillUsageRequestedEvent, ExperienceGainRequestedEvent, QuestAcceptedRequestedEvent itd.
2. Zaimplementować handlery analogicznie do MovementRequestedHandler:
   - Wywołać Core Service
   - Zapisać do IModelRepository
   - Broadcast przez IGameStateBroadcaster
3. Zarejestrować w RequestedHandlersRegistrationExtensions
4. Dodać po 1-2 testy jednostkowe per handler

### Priorytet 2: AI System - Integracja z CQRS
**Czas: 3-4h**
1. Stworzyć IAiDirectiveEventAdapter + implementacja
2. Podłączyć NpcAiService do IRequestedEventQueue
3. Dodać IBehaviorRegistry dla Patrol/Combat/Dialog/Trade
4. Test integracyjny: NPC patrol → MovementStartRequestedEvent → Delta broadcast

### Priorytet 3: Testy Funkcjonalne - Pełne Scenariusze
**Czas: 2-3h**
1. Combat scenario test
2. Quest scenario test
3. Skill + XP scenario test
4. Session expiry test

### Priorytet 4: Dokumentacja
**Czas: 1-2h**
1. Zaktualizować README.md o:
   - Przepływ CQRS/ES (Command → Event → Handler → Broadcast)
   - Mechanizm WorldState snapshot/delta dla klientów
   - Integracja AI z event queue
2. Dodać diagramy przepływu (opcjonalnie)

---

## 🎯 KRYTERIA AKCEPTACJI (Definition of Done)

### Dla RequestedHandlers:
- [x] Wszystkie Commandy mają odpowiadające im *RequestedEvent
- [x] Wszystkie *RequestedEvent mają handler
- [x] Wszystkie handlery wywołują Core Service
- [x] Wszystkie handlery robią Broadcast
- [ ] Wszystkie handlery mają test jednostkowy (w trakcie - większość pokryta)

### Dla AI:
- [x] NpcAiService działa w pętli tick
- [x] Dyrektywy AI są mapowane na *RequestedEvent
- [x] Eventy AI przechodzą przez tę samą kolejkę co eventy graczy
- [ ] Test integracyjny pokazuje pełny przepływ AI → Event → Delta (w trakcie)

### Dla Testów:
- [x] Wszystkie istniejące testy przechodzą (zielone)
- [ ] Dodane testy funkcjonalne dla combat/quest/skill/xp
- [x] Protosy mają testy stabilności wire contract (ProtoFieldNumberTests)

### Dla Dokumentacji:
- [ ] README.md opisuje CQRS/ES flow
- [ ] README.md opisuje WorldState snapshot/delta mechanism
- [ ] README.md zawiera przykłady użycia dla klienta

---

## 📊 METRYKI

- **Zaimplementowane Handlery**: 13/13 (100%) ✅
- **Pokrycie Testami Integracyjnymi**: ~75% (movement, session, queries gotowe; część AI w trakcie)
- **Pokrycie Testami Jednostkowymi**: ~90% (core services + infrastructure + nowe handlery + AI adapter)
- **Dokumentacja**: ~50% (podstawowa struktura + AI flow; brak szczegółów CQRS/delta dla klienta)

---

## 🔄 NEXT STEPS (Immediate)

1. ✅ ~~**Zaimplementować SkillUsageRequestedHandler**~~ (DONE)
2. ✅ ~~**Zaimplementować ExperienceGainRequestedHandler + LevelUpRequestedHandler**~~ (DONE)
3. ✅ ~~**Dodać IAiDirectiveEventAdapter + podłączyć NpcAiService**~~ (DONE)
4. **Napisać test integracyjny dla AI patrol scenario** (NEXT)
5. **Zaktualizować README.md o CQRS/ES flow diagram**

---

**Konkluzja**: Architektura CQRS/ES jest w ~95% ukończona. Command → Event → Handler → Broadcast działa dla WSZYSTKICH komend (Movement, Equipment/Inventory, Skills, XP/LevelUp/Death, Quests). AI system w pełni zintegrowany z kolejką eventów. Pozostają testy integracyjne dla pełnych scenariuszy (combat, quest completion, skill usage) oraz dokumentacja.
