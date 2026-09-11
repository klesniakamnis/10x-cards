---
project: "10xCards"
version: 1
status: draft
created: 2026-09-11
updated: 2026-09-12
prd_version: 1
main_goal: market-feedback
top_blocker: time
milestone_id: first-usable-deck
milestone_seq: 1
milestone_status: open
---

# Roadmap: 10xCards

> Derived from `context/foundation/prd.md` (v1) + auto-researched codebase baseline.
> Edit-in-place; archive when superseded.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Milestone

**M-1: First usable deck** — Status: open

- **Intent:** Dostarczyć kompletny cykl: wklejenie tekstu → generowanie fiszek AI → akceptacja/edycja/odrzucenie → sesja powtórek SM-2. Walidacja hipotezy produktowej: czy użytkownicy akceptują ≥75% fiszek wygenerowanych przez AI.
- **Source materials:** `context/foundation/prd.md` (v1)
- **Done when:** every F-NN and S-NN below is `done`.

## Vision recap

Studenci rezygnują ze spaced repetition, bo tworzenie fiszek zabiera 2–5 minut za sztukę. 10xCards zdejmuje tę barierę: AI generuje szkic fiszek z wklejonego tekstu w sekundy, a użytkownik kontroluje jakość przez szybką akceptację, edycję lub odrzucenie w tym samym produkcie. Wedge produktowy — cecha, bez której produkt jest nie do odróżnienia od generycznego narzędzia do fiszek — to fakt, że fiszki muszą być jednocześnie generowane przez AI z tekstu użytkownika i gatowane przez człowieka zanim trafią do talii.

## North star

**S-01: first-gated-generation** — Użytkownik wkleja tekst, widzi propozycje fiszek AI, akceptuje/edytuje/odrzuca, a zatwierdzone fiszki lądują w talii.

> Gwiazda przewodnia to najmniejszy przebieg end-to-end, którego dostarczenie udowadnia hipotezę produktową — umieszczony tak wcześnie, jak pozwalają zależności, bo wszystko inne ma sens tylko jeśli ten przepływ działa. Kryterium sukcesu PRD („75% fiszek AI zaakceptowanych") wprost mierzy ten slice.

## At a glance

| ID   | Change ID                | Outcome (user can …)                                                  | Prerequisites | PRD refs                                  | Status   |
| ---- | ------------------------ | --------------------------------------------------------------------- | ------------- | ----------------------------------------- | -------- |
| F-01 | data-schema-setup        | (foundation) EF Core + baza danych + schemat bazowy                   | —             | all FRs (persistence)                     | in-progress |
| F-02 | auth-magic-link          | (foundation) Passwordless auth (magic link) + middleware autoryzacji   | F-01          | FR-001, FR-002, FR-003, Access Control    | proposed |
| S-01 | first-gated-generation   | Generuje fiszki AI z wklejonego tekstu, akceptuje/edytuje/odrzuca     | F-01, F-02    | US-01, FR-004–FR-008                      | proposed |
| S-02 | manual-flashcard-creation| Tworzy fiszkę ręcznie (pytanie i odpowiedź)                           | F-01, F-02    | US-02, FR-009                             | proposed |
| S-03 | deck-management          | Przegląda, edytuje i usuwa fiszki z talii                             | F-01, F-02    | FR-010, FR-011, FR-012, NFR (confirmacja) | proposed |
| S-04 | srs-review-session       | Przeprowadza sesję powtórek SM-2 (skala 0–5)                          | S-01          | US-03, FR-013, FR-014, FR-015             | proposed |

## Streams

Pomoc nawigacyjna — grupuje elementy współdzielące łańcuch zależności. Kanoniczny porządek żyje w grafie zależności poniżej; ta tabela to proponowana kolejność czytania wzdłuż równoległych ścieżek.

| Stream | Theme                        | Chain                          | Note                                                                |
| ------ | ---------------------------- | ------------------------------ | ------------------------------------------------------------------- |
| A      | Generowanie i powtórki       | `F-01` → `F-02` → `S-01` → `S-04` | Główna ścieżka walidacyjna: od danych przez auth do gwiazdy przewodniej i sesji SR. |
| B      | Operacje na talii            | `S-02` → `S-03`               | Możliwe równolegle z S-01 po ukończeniu F-01 + F-02. Kolejność w strumieniu to porządek czytania, nie zależność. |

## Baseline

Stan codebase'u na 2026-09-11 (zbadany automatycznie + potwierdzony przez użytkownika). Foundations poniżej zakładają, że te warstwy są obecne i NIE budują ich od nowa.

- **Frontend:** absent — brak UI; projekt to czysty webapi scaffold.
- **Backend / API:** present — `Program.cs:22-37`, dwa endpointy (`/health`, `/weatherforecast`), minimal API ASP.NET Core 9.0.
- **Data:** absent — brak Entity Framework, brak migracji, brak schematów.
- **Auth:** absent — brak pakietów auth, brak middleware, magic link zaplanowany ale nie wdrożony.
- **Deploy / infra:** partial — Azure App Service F1 wdrożony ręcznie (swedencentral), brak CI/CD (GitHub Actions).
- **Observability:** absent — brak Serilog/AppInsights/OpenTelemetry, tylko domyślny ILogger.

## Foundations

### F-01: Data schema setup

- **Outcome:** (foundation) Entity Framework Core skonfigurowany z bazą danych; schemat bazowy (User, Flashcard) gotowy, migracje działają.
- **Change ID:** data-schema-setup
- **PRD refs:** Wszystkie FR wymagają trwałego zapisu danych (fiszki, użytkownicy, wyniki powtórek).
- **Unlocks:** F-02, S-01, S-02, S-03, S-04
- **Prerequisites:** —
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Schemat może wymagać rozszerzenia przy S-04 (pola SM-2: interval, easiness, repetitions, next_review). Minimalny schemat teraz; rozszerzenie w slajsie, który tego potrzebuje.
- **Status:** in-progress

### F-02: Auth scaffold (magic link)

- **Outcome:** (foundation) Passwordless auth via magic link działa end-to-end; endpointy chronione middleware'em autoryzacji; niezalogowani przekierowani na ekran logowania.
- **Change ID:** auth-magic-link
- **PRD refs:** FR-001, FR-002, FR-003, NFR (widoki za ścianą logowania), Access Control
- **Unlocks:** S-01, S-02, S-03, S-04
- **Prerequisites:** F-01 (potrzebna tabela User)
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Wybór dostawcy email do magic linków (SendGrid, Resend, Mailgun) — Owner: /10x-plan. Block: no (na dev można logować linki do konsoli).
- **Risk:** Magic link wymaga dostawcy email; bez niego auth działa tylko w dev. Ale to nie blokuje planowania ani implementacji — dostawca to konfiguracja, nie architektura.
- **Status:** proposed

## Slices

### S-01: First gated generation ★

- **Outcome:** Użytkownik może wkleić tekst, zobaczyć propozycje fiszek wygenerowane przez AI, zaakceptować/edytować/odrzucić każdą propozycję, a zaakceptowane fiszki lądują w talii.
- **Change ID:** first-gated-generation
- **PRD refs:** US-01, FR-004, FR-005, FR-006, FR-007, FR-008
- **Prerequisites:** F-01, F-02
- **Parallel with:** S-02, S-03
- **Blockers:** —
- **Unknowns:**
  - Wybór dostawcy LLM i konfiguracja promptu generującego — Owner: /10x-plan. Block: no. Guardrail: dostawca musi spełniać politykę „brak trenowania na danych klientów" (Success Criteria PRD).
  - Konkretny limit długości wklejanego tekstu (Open Question 1 z PRD; punkt startowy: 10 000 znaków) — Owner: /10x-plan. Block: no.
- **Risk:** Jakość generowanych fiszek zależy od promptu i modelu; walidacja wymaga prawdziwych użytkowników. Największy slice pod względem zakresu (5 FR), ale nierozerwalny — nie da się walidować generowania bez UI akceptacji. To jest gwiazda przewodnia: jeśli ten slice nie działa, reszta produktu nie ma sensu.
- **Status:** proposed

### S-02: Manual flashcard creation

- **Outcome:** Użytkownik może ręcznie utworzyć fiszkę (pytanie i odpowiedź) bez korzystania z AI.
- **Change ID:** manual-flashcard-creation
- **PRD refs:** US-02, FR-009
- **Prerequisites:** F-01, F-02
- **Parallel with:** S-01, S-03
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Mały, izolowany slice; ryzyko niskie. Może służyć jako fallback do testowania powtórek zanim generowanie AI jest gotowe.
- **Status:** proposed

### S-03: Deck management

- **Outcome:** Użytkownik może przeglądać wszystkie swoje fiszki, edytować istniejącą fiszkę i usunąć fiszkę z potwierdzeniem.
- **Change ID:** deck-management
- **PRD refs:** FR-010, FR-011, FR-012, NFR (potwierdzenie przy usuwaniu)
- **Prerequisites:** F-01, F-02
- **Parallel with:** S-01, S-02
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Standardowy CRUD; ryzyko niskie. Edycja fiszki po powtórkach może wpływać na stan SM-2 — decyzja w /10x-plan S-04.
- **Status:** proposed

### S-04: Spaced repetition session

- **Outcome:** Użytkownik może rozpocząć sesję powtórek, odpowiadać na fiszki, oceniać jakość przypomnienia w skali 0–5, a system wyznacza kolejne terminy powtórek algorytmem SM-2.
- **Change ID:** srs-review-session
- **PRD refs:** US-03, FR-013, FR-014, FR-015, Business Logic (etap 2: scheduling powtórek)
- **Prerequisites:** S-01 (potrzebne fiszki w talii do przećwiczenia)
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Wybór konkretnej biblioteki SM-2 dla .NET (lub własna implementacja algorytmu) — Owner: /10x-plan. Block: no. Rodzina rozstrzygnięta: SM-2, skala 0–5 (shape-notes).
  - Empty state dla talii bez fiszek z nadchodzącym terminem (Open Question 3 z PRD) — Owner: /10x-plan. Block: no.
  - Strefa czasowa dla terminów powtórek: UTC z konwersją client-side czy user-locale server-side (Open Question 4 z PRD) — Owner: /10x-plan. Block: no.
  - Zachowanie stanu przy przerwanej sesji: wznowienie vs restart (Open Question 5 z PRD) — Owner: /10x-plan. Block: no.
- **Risk:** Wymaga rozszerzenia schematu bazy o pola SM-2 (interval, easiness_factor, repetitions, next_review_date). Algorytm SM-2 jest dobrze udokumentowany, ale integracja ze stanem fiszek wymaga staranności.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID                | Suggested issue title                                | Ready for `/10x-plan` | Notes                           |
| ---------- | ------------------------ | ---------------------------------------------------- | --------------------- | ------------------------------- |
| F-01       | data-schema-setup        | Setup EF Core + database schema (User, Flashcard)    | yes                   | Run `/10x-plan data-schema-setup` |
| F-02       | auth-magic-link          | Implement passwordless auth (magic link)             | no                    | Depends on F-01                 |
| S-01       | first-gated-generation   | AI flashcard generation with human gate (north star) | no                    | Depends on F-01, F-02           |
| S-02       | manual-flashcard-creation| Manual flashcard creation                            | no                    | Depends on F-01, F-02           |
| S-03       | deck-management          | Deck management (browse, edit, delete)               | no                    | Depends on F-01, F-02           |
| S-04       | srs-review-session       | Spaced repetition review session (SM-2)              | no                    | Depends on S-01                 |

## Open Roadmap Questions

1. **Konkretny limit długości wklejanego tekstu (FR-004).** Punkt startowy: 10 000 znaków. Ostateczna wartość zależy od wybranego dostawcy LLM (context window, koszt). — Owner: /10x-plan S-01. Block: S-01.
2. **Konkretna biblioteka SM-2 dla .NET (lub własna implementacja).** Rodzina rozstrzygnięta (SM-2, skala 0–5), ale wybór implementacji otwarty. — Owner: /10x-plan S-04. Block: S-04.
3. **Empty state dla nowej talii przy „Ucz się" (FR-013).** Co widzi użytkownik bez fiszek z nadchodzącym terminem? Sugerowana odpowiedź: komunikat + wezwanie do akcji. — Owner: /10x-plan S-04. Block: —.
4. **Strefa czasowa dla terminów powtórek (FR-015).** UTC z konwersją client-side czy user-locale server-side? — Owner: /10x-plan S-04. Block: —.
5. **Zachowanie stanu przy przerwanej sesji powtórek (FR-013–015).** Wznowienie od miejsca przerwania czy restart? — Owner: /10x-plan S-04. Block: —.
6. **Metryka „acceptance" a ciężko edytowana propozycja (FR-007).** Czy propozycja z >90% przepisaną treścią liczy się jako zaakceptowana? — Owner: analiza danych z produkcji. Block: —.

## Parked

- **Własny algorytm SR** — Why parked: PRD §Non-Goals. MVP używa gotowej implementacji SM-2.
- **Import PDF/DOCX/EPUB** — Why parked: PRD §Non-Goals. MVP obsługuje wyłącznie tekst wklejony.
- **Współdzielenie talii** — Why parked: PRD §Non-Goals. Każdy użytkownik widzi wyłącznie swoje fiszki.
- **Integracja z platformami edukacyjnymi** — Why parked: PRD §Non-Goals. MVP jest samodzielną aplikacją.
- **Aplikacja mobilna** — Why parked: PRD §Non-Goals. Web-only.
- **Tryb offline / PWA / sync multi-device** — Why parked: PRD §Non-Goals.
- **Monetyzacja / paywall** — Why parked: PRD §Non-Goals.
- **Tagi / kategorie / wiele talii** — Why parked: PRD §Non-Goals.
- **CI/CD pipeline (GitHub Actions)** — Why parked: Przydatne, ale nie blokuje żadnego slice'a. Manual deploy via `az webapp up` wystarcza na czas MVP. Może być dodane w dowolnym momencie.
- **Observability (Serilog / AppInsights / OpenTelemetry)** — Why parked: Cel to market-feedback, nie jakość operacyjna. Domyślny ILogger wystarcza na czas MVP.

## Milestone History

(Append-only. Empty on the first milestone.)

## Done

(Empty on first generation. `/10x-archive` appends entries here when a change is archived.)
