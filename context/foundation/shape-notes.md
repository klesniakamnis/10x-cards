---
project: "10xCards"
context_type: greenfield
created: 2026-09-11
updated: 2026-09-11
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  product_type: web-app
  target_scale:
    users: small
    qps: low
    data_volume: small
  timeline_budget:
    mvp_weeks: 3
    hard_deadline: null
    after_hours_only: true
  gray_areas_resolved:
    - topic: "primary persona"
      decision: "student uczelni wyższej (primary); profesjonalista uczący się zawodu (secondary)"
    - topic: "pain category"
      decision: "workflow friction — tworzenie fiszki 2–5 min × setki fiszek"
    - topic: "insight"
      decision: "Anki jest świetne, ale bariera tworzenia talii zabija adopcję SR — AI zdejmuje tę barierę"
    - topic: "auth mechanism"
      decision: "passwordless (magic link na email)"
    - topic: "user roles"
      decision: "płaski model — jedna rola, dane per-user"
    - topic: "guest access"
      decision: "wszystko za ścianą logowania — brak trybu demo"
    - topic: "mvp timeline"
      decision: "3 tygodnie after-hours (10xWorkflow default; brak Timeline acknowledgment)"
    - topic: "secondary success criterion"
      decision: "user wraca ≥ 3x/tydzień (retention proxy dla nawyku SR)"
    - topic: "guardrails"
      decision: "wklejony tekst nie służy do treningu modeli AI (privacy)"
    - topic: "domain rule"
      decision: "dwuetapowa reguła: (1) ekstrakcja LLM z tekstu → propozycje Q/A + human-in-the-loop akceptacja; (2) scheduling powtórek wg gotowego algorytmu SR"
    - topic: "sr algorithm family"
      decision: "SM-2 (skala 0–5) — klasyk Anki, dojrzały, dobrze udokumentowany"
    - topic: "latency floor"
      decision: "bez sztywnego limitu; wyłącznie widoczny progress > 2s podczas generowania"
    - topic: "language support"
      decision: "polski i angielski w MVP"
    - topic: "browser support"
      decision: "Chrome + Firefox desktop (2 najnowsze major)"
    - topic: "product type"
      decision: "web-app"
    - topic: "target scale"
      decision: "small (single-digit users) — hobbystyczny start"
    - topic: "hard deadline"
      decision: "brak; after-hours only"
  frs_drafted: 15
  quality_check_status: accepted
---

# 10xCards — shape notes

## Seed (user-provided, verbatim)

> Główny problem: Manualne tworzenie wysokiej jakości fiszek edukacyjnych jest czasochłonne, co zniechęca do korzystania z efektywnej metody nauki jaką jest spaced repetition.
>
> MVP: generowanie fiszek przez AI z wklejonego tekstu; manualne tworzenie; przegląd/edycja/usuwanie; proste konta użytkowników; integracja z gotowym algorytmem powtórek.
>
> Poza MVP: własny algorytm SR, import PDF/DOCX, współdzielenie zestawów, integracje z platformami edukacyjnymi, aplikacje mobilne.
>
> Kryteria sukcesu: 75% fiszek AI zaakceptowanych, 75% fiszek tworzonych z AI.

## Vision & Problem Statement

Studenci uczelni wyższych, którzy chcą wykorzystać spaced repetition do skutecznej nauki na sesję, rezygnują z tej metody, bo tworzenie wysokiej jakości fiszek zabiera 2–5 minut za sztukę × setki jednostek materiału do przerobienia. W typowym momencie — niedzielny wieczór, 40+ stron skryptu, egzamin w środę — wybór między „przeczytać skrypt jeszcze raz" a „stworzyć talię fiszek" wygrywa czytanie, mimo że jest wielokrotnie mniej skuteczne. Koszt dzisiaj: rezygnacja z SR albo używanie gorszych metod nauki, przy pełnej świadomości, że fiszki byłyby lepsze.

Insight: Anki, Quizlet i RemNote są funkcjonalnie dojrzałe, ale wszystkie zakładają, że użytkownik zainwestuje czas w utworzenie talii. Ta bariera onboardingowa jest głównym filtrem odsiewającym uczących się od SR. AI potrafi wygenerować szkic fiszek z wklejonego tekstu w sekundy — pod warunkiem, że human-in-the-loop (szybka akceptacja / edycja / odrzucenie) odbywa się w tym samym produkcie, a nie jako kolejny 4-etapowy workflow (ChatGPT → sformatuj → zaimportuj do Anki). 10xCards zdejmuje barierę tworzenia bez rezygnacji z kontroli jakości.

## User & Persona

**Primary — Student uczelni wyższej.**  
Kontekst: uczy się do kolokwiów i egzaminów z konkretnych skryptów / podręczników / notatek z wykładu. Ma duży wolumen materiału do przerobienia w krótkim czasie (sesja). Zna spaced repetition z opowiadania lub próbował Anki i odbił się od tworzenia talii. Moment sięgnięcia po produkt: sesja + krótki termin + duży tekst — potrzebuje w jeden wieczór przekształcić 40-stronicowy skrypt w działającą talię i zacząć powtarzać.

### Secondary persona
**Profesjonalista uczący się zawodu** (programista, lekarz, prawnik). Uczy się z dokumentacji, artykułów, aktualizacji standardów. Rytm długoterminowy (miesiące), nie sprint. MVP obsługuje ten segment „przy okazji" — nie optymalizujemy pod niego workflow, ale ten sam mechanizm generowania z wklejonego tekstu również dla niego działa.

## Access Control

Model jednorolowy: każdy zarejestrowany użytkownik ma taki sam zestaw uprawnień — może tworzyć, edytować, usuwać wyłącznie własne fiszki. Brak roli admina, brak współdzielenia talii, brak trybu gościa. Wszystkie widoki produktowe są za ścianą logowania; niezalogowany użytkownik widzi wyłącznie landing / ekran logowania / ekran rejestracji.

Mechanizm logowania: passwordless — użytkownik podaje email, dostaje magic link, klika i jest zalogowany. Rejestracja i logowanie to ta sama ścieżka (pierwsze kliknięcie linka na nieznany email = utworzenie konta). Brak hasła do resetowania, brak sekretów po stronie użytkownika.

## Success Criteria

### Primary
- 75% fiszek wygenerowanych przez AI jest akceptowanych przez użytkownika (bez edycji lub z drobną edycją traktowaną jako akceptacja) — mierzone jako `zaakceptowane / (zaakceptowane + odrzucone + porzucone)` w cyklu życia sesji generowania.
- 75% wszystkich fiszek w bazie zostało utworzonych ścieżką AI (nie manualną) — potwierdza, że użytkownicy faktycznie używają głównej wartości produktu.

### Secondary
- Użytkownik wraca do produktu co najmniej 3 razy w tygodniu — proxy dla wykształcenia się nawyku powtórek (nie samego stworzenia talii i odejścia).

### Guardrails
- Tekst wklejony przez użytkownika do generowania fiszek nie jest używany do trenowania modeli AI po stronie dostawcy ani utrwalany poza kontekstem pojedynczego zapytania generującego. Naruszenie tego jest regresją nawet przy spełnionych Primary.

## MVP flow (draft — feeds FRs w Phase 4)

Primary path:
1. Landing → sign-in
2. Email → magic link → zalogowany w apce
3. Wklej fragment tekstu (skrypt, notatki, artykuł)
4. „Wygeneruj fiszki" → lista propozycji Q/A
5. Dla każdej propozycji: akceptuj / edytuj / odrzuć
6. Zaakceptowane fiszki dołączają do talii użytkownika
7. „Ucz się" → algorytm SR wybiera fiszki do sesji
8. Odpowiada na fiszki (skala „wiem / nie wiem" lub podobna)
9. Sesja się kończy, algorytm zapisuje kolejne terminy powtórek

Secondary paths:
- Ręczne dodanie fiszki (bez AI) — pole „nowa fiszka" → wpisuje Q i A → zapisuje
- Przegląd/edycja/usunięcie istniejących fiszek w talii
- Wylogowanie / logowanie na innym urządzeniu

Timeline: 3 tygodnie after-hours. Brak `## Timeline acknowledgment` — mieści się w 10xWorkflow default (≤ 3 tygodnie).

## Functional Requirements

### Authentication
- FR-001: Odwiedzający może podać email i zażądać magic linka do logowania. Priority: must-have
  > Socrates: Rozważone kontrargumenty (wymóg dostępu do maila, ryzyko trafienia do spamu). Decyzja: passwordless jest świadomym wyborem z Phase 2; ryzyka nie są blockerami MVP.
- FR-002: Użytkownik z ważnym magic linkiem może otworzyć aplikację jako zalogowany. Priority: must-have
  > Socrates: Rozważone kontrargumenty (limit czasowy linka, jednorazowość, sesja per-urządzenie). Decyzja: to detale bezpieczeństwa magic linka → NFR, nie zmiana FR.
- FR-003: Zalogowany użytkownik może się wylogować. Priority: must-have
  > Socrates: Rozważone kontrargumenty (friction re-loginu, wylogowanie „wszystkie sesje"). Decyzja: standardowa funkcja; ryzyka realne, ale nie zmieniają FR w MVP.

### AI generation
- FR-004: Zalogowany użytkownik może wkleić fragment tekstu (do ustalonego limitu długości) i zażądać wygenerowania fiszek. Priority: must-have
  > Socrates: Rozważony kontrargument: brak limitu długości = user wklei 200 stron i zapchamy LLM (koszt, latencja). Rozwiązanie: FR-004 nakłada limit długości; konkretna wartość (np. 10k znaków) trafia do NFR / Open Questions do decyzji przy stack-selection.
- FR-005: Zalogowany użytkownik widzi listę wygenerowanych par Q/A jako propozycje (nie zapisane fiszki). Priority: must-have
  > Socrates: Rozważone kontrargumenty (rozjeżdżający się UX listy przy 100+ propozycjach, deduplikacja). Decyzja: MVP — prosta lista; optymalizacje później.
- FR-006: Zalogowany użytkownik może zaakceptować propozycję fiszki — dołącza do jego talii. Priority: must-have
  > Socrates: Rozważone kontrargumenty (bulk accept obchodzi metrykę 75%, brak historii akceptacji/undo). Decyzja: pojedyncza akceptacja per fiszka; bulk explicite wykluczony w Non-Goals.
- FR-007: Zalogowany użytkownik może edytować treść propozycji przed akceptacją. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak walidacji Q/A, metryka „acceptance" zawyżana przez ciężko edytowane fiszki). Decyzja: inline edycja standardowa; walidacja → NFR.
- FR-008: Zalogowany użytkownik może odrzucić propozycję fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak reason capture do iteracji promptu, brak undo dla odrzucenia). Decyzja: MVP — odrzucenie binarne; reason capture jako nice-to-have poza MVP.

### Manual creation
- FR-009: Zalogowany użytkownik może ręcznie utworzyć fiszkę (Q i A) bez użycia AI. Priority: must-have
  > Socrates: Rozważone kontrargumenty (osłabienie insightu „AI zdejmuje barierę", trudność liczenia metryki 75% z AI). Decyzja: manualne pozostaje jako safety net; Primary #2 (75% z AI) obsługuje ryzyko metryczne.

### Deck management
- FR-010: Zalogowany użytkownik może przeglądać wszystkie swoje zapisane fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak talii/kategoryzacji, brak wyszukiwania). Decyzja: MVP — podstawowa lista; kategorie/search poza zakresem MVP.
- FR-011: Zalogowany użytkownik może edytować istniejącą fiszkę. Priority: must-have
  > Socrates: Rozważone kontrargumenty (edycja niszczy historię SR, brak historii wersji). Decyzja: edycja pozostaje; kwestie SR-state → NFR/Business Logic.
- FR-012: Zalogowany użytkownik może usunąć istniejącą fiszkę. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak potwierdzenia, soft-delete vs hard-delete). Decyzja: hard delete + confirmacja jako standardowy UX guardrail (do NFR).

### Spaced repetition
- FR-013: Zalogowany użytkownik może rozpocząć sesję powtórek — algorytm wybiera fiszki do przećwiczenia. Priority: must-have
  > Socrates: Rozważone kontrargumenty (niedookreślony algorytm SM-2 vs FSRS vs Leitner, empty state dla nowej talii). Decyzja: wybór rodziny algorytmu → Business Logic; empty state → NFR.
- FR-014: Zalogowany użytkownik może odpowiedzieć na fiszkę i oznaczyć jakość odpowiedzi (skala algorytmu SR). Priority: must-have
  > Socrates: Rozważone kontrargumenty (skala oceny to funkcja algorytmu, self-assessment nie do skontrolowania). Decyzja: skala razem z wyborem algorytmu (Business Logic); self-assessment akceptowany jako aksjomat.
- FR-015: System zapisuje wynik powtórki i wyznacza kolejny termin powtórki dla fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (zachowanie stanu przy przerwanej sesji, strefy czasowe). Decyzja: zapisywanie i scheduling to core algorytmu; detale → NFR / Open Questions.

## Business Logic

10xCards zamienia wklejony przez użytkownika fragment tekstu w propozycje fiszek pytanie/odpowiedź, które użytkownik akceptuje, edytuje lub odrzuca; zaakceptowane fiszki są następnie prezentowane do powtórki w terminach wyznaczanych przez gotowy algorytm spaced repetition (rodzina SM-2, skala oceny 0–5).

Reguła składa się z dwóch etapów, oba widocznych dla użytkownika:

1. **Ekstrakcja + walidacja przez człowieka.** Na wejściu: fragment tekstu w języku polskim lub angielskim. Na wyjściu: lista propozycji par Q/A. Użytkownik encounter je jako listę do przejrzenia — akceptuje po jednej, edytuje inline (Q i/lub A) i zapisuje jako akceptację, albo odrzuca. Metryki 75% acceptance i 75% udziału AI odnoszą się do tego etapu.

2. **Scheduling powtórek.** Na wejściu: zbiór fiszek użytkownika + historia jego odpowiedzi. Na wyjściu: kolejne terminy powtórek per fiszka. Użytkownik encounter to jako sesję „Ucz się" — algorytm wybiera fiszki, których termin nadszedł; użytkownik oznacza jakość każdej odpowiedzi w skali 0–5; algorytm zapisuje kolejny termin dla tej fiszki.

Manualnie utworzone fiszki (FR-009) wchodzą w etap #2 na identycznych zasadach jak fiszki AI — z pominięciem etapu #1.

## Non-Functional Requirements

- Podczas generowania fiszek użytkownik widzi ciągły, widoczny sygnał postępu, jeśli operacja trwa dłużej niż dwie sekundy; brak sztywnego górnego limitu czasu w MVP (ale sesja nie może zostawiać użytkownika bez feedbacku).
- Produkt obsługuje wejściowy tekst w języku polskim i angielskim; fiszki generowane są w tym samym języku, w którym napisany jest wklejony tekst.
- Produkt działa na dwóch najnowszych stabilnych wersjach Chrome i Firefox na desktopie; inne przeglądarki i urządzenia mobilne nie są objęte gwarancją.
- Tekst wklejony przez użytkownika do generowania fiszek nie jest utrwalany poza kontekstem pojedynczego zapytania generującego, ani udostępniany dostawcy modelu AI do celów treningowych (mirror guardrail z Success Criteria — tu jako właściwość obserwowalna).
- Usunięcie fiszki wymaga potwierdzenia użytkownika (jedna dodatkowa akcja) — chroni przed przypadkową utratą danych.
- Wszystkie widoki produktowe wymagają zalogowanego użytkownika; nieautoryzowane żądanie do widoku wewnętrznego kończy się przekierowaniem na ekran logowania bez wycieku informacji o istnieniu zasobu.

## Non-Goals

- **Brak własnego algorytmu spaced repetition.** Używamy gotowej implementacji rodziny SM-2; nie inwestujemy w rozwój algorytmu ani w tuning parametrów (np. FSRS-owe).
- **Brak importu z formatów PDF / DOCX / EPUB / notatek Notion.** MVP obsługuje wyłącznie tekst wklejony do pola input.
- **Brak współdzielenia talii między użytkownikami.** Każdy użytkownik widzi wyłącznie swoje fiszki; nie ma publikacji, followowania, remix'owania talii innych.
- **Brak integracji z platformami edukacyjnymi (Moodle, Google Classroom, Canvas).** MVP jest samodzielną aplikacją.
- **Brak aplikacji mobilnej.** Web-only w MVP; responsywność mobile nie jest gwarantowana (patrz NFR o przeglądarkach).
- **Brak trybu offline / PWA / synchronizacji multi-device.** Aplikacja działa online-only; sesja per-przeglądarka.
- **Brak monetyzacji, paywalla ani kont premium w MVP.** Wszystkie funkcje darmowe; monetyzacja to zakres poza MVP.
- **Brak systemu tagów, kategorii ani wielu talii per użytkownik.** Każdy użytkownik ma jeden liniowy zbiór fiszek; podział / organizacja to zakres poza MVP.

## Open Questions

1. **Konkretny limit długości wklejanego tekstu w FR-004.** Sugerowany punkt startowy: 10 000 znaków. Ostateczna decyzja zależy od wybranego dostawcy LLM (context window, cost per generation). — Owner: rozstrzygnięcie przy stack-selection.
2. **Empty state dla nowej talii przy pierwszym „Ucz się" (FR-013).** Co widzi user, który jeszcze nie ma żadnych fiszek z nadchodzącym terminem powtórki? Sugerowana odpowiedź: komunikat + CTA „Dodaj pierwsze fiszki". — Owner: uszczegółowić w /10x-plan.
3. **Strefa czasowa dla wyznaczania kolejnych terminów powtórek (FR-015).** UTC z konwersją client-side czy user-locale server-side? — Owner: rozstrzygnięcie przy stack-selection.
4. **Zachowanie stanu przy przerwanej sesji powtórek (FR-013–FR-015).** Czy sesję można wznowić od miejsca, w którym została przerwana, czy zaczyna się od nowa? — Owner: rozstrzygnięcie w /10x-plan (UX decyzja).
5. **Metryka „acceptance" a ciężko edytowana fiszka (FR-007).** Czy fiszka, w której user przepisał 90% treści, liczy się jako „zaakceptowana" (poziom edycji nie ma znaczenia), czy potrzebujemy odrębnej kategorii „zaakceptowana po ciężkiej edycji"? — Owner: rozstrzygnięcie przy pierwszej analizie danych z produkcji.

## Forward: tech-stack

Notatki dla downstream'owego kroku wyboru stacku (nie są częścią PRD):

- MVP wymaga zewnętrznego dostawcy LLM do generowania fiszek. Guardrail „tekst nie służy do treningu modeli" ogranicza wybór dostawców do tych z policy „no training on customer data" (np. OpenAI enterprise, Anthropic Claude API, Azure OpenAI, self-hosted).
- Gotowy algorytm SM-2 dostępny w bibliotekach kilku języków (JS/TS: `ts-fsrs`, `supermemo.ts`; Python: `supermemo2`, `py-fsrs`). Wybór biblioteki wpłynie na dokładny kształt skali oceny.
- Passwordless auth (magic link) dostępny out-of-the-box w wielu BaaS/auth providers (Supabase, Auth0, Clerk, WorkOS). Wybór wpłynie na wektor „lock-in vs. self-hosted".
- Target scale = small: bez potrzeby multi-region, autoscaling, dedicated infra. Prosta hosting (Vercel/Netlify/Fly.io/Render) wystarcza.

## Forward: technical-roadmap

Notatki dla downstream'owego kroku planowania technicznego (nie są częścią PRD):

- Testy: minimum kontraktowe dla generacji AI (input tekstu → output listy propozycji z minimalnym schematem), reszta w /10x-plan.
- Deployment/CI: MVP nie wymaga zaawansowanego pipeline'u — jeden środowisko prod, deployment na push do main wystarcza.

## User Stories

### US-01: Student generuje fiszki z wklejonego skryptu

- **Given** zalogowany student z pustą talią
- **When** wkleja 5-stronicowy fragment skryptu i klika „Wygeneruj fiszki"
- **Then** widzi listę propozycji Q/A, może każdą zaakceptować, edytować lub odrzucić, a zaakceptowane fiszki lądują w jego talii

#### Acceptance Criteria
- Lista propozycji pojawia się w rozsądnym czasie (nieblokujący UX; guardrails ustalane w NFR)
- Akceptacja fiszki nie wymaga dodatkowych klików (jedno kliknięcie „akceptuj")
- Edycja Q i/lub A przed akceptacją jest inline'owa (bez przechodzenia na osobny ekran)
- Odrzucona propozycja znika z listy i nie trafia do talii
- Po zamknięciu widoku propozycji użytkownik widzi liczbę zaakceptowanych fiszek dodanych do talii

### US-02: Student tworzy fiszkę ręcznie

- **Given** zalogowany student przeglądający swoją talię
- **When** klika „Dodaj fiszkę" i wpisuje Q i A
- **Then** fiszka jest zapisana w talii i wchodzi do rotacji powtórek na tych samych zasadach co fiszki AI

#### Acceptance Criteria
- Formularz wymaga wypełnienia obu pól (Q i A)
- Zapisana fiszka jest natychmiast widoczna na liście talii
- Ręcznie utworzona fiszka jest traktowana identycznie przez algorytm SR jak fiszka AI

### US-03: Student przeprowadza sesję powtórek

- **Given** zalogowany student z talią zawierającą fiszki, których terminy powtórek wypadają dziś lub wcześniej
- **When** klika „Ucz się"
- **Then** algorytm SR prezentuje mu fiszki jedna po drugiej; student odpowiada i oznacza jakość swojej odpowiedzi, a algorytm zapisuje kolejny termin powtórki

#### Acceptance Criteria
- Sesja pokazuje wyłącznie fiszki z bieżącym lub przekroczonym terminem powtórki
- Po każdej fiszce użytkownik oznacza jakość odpowiedzi w skali oczekiwanej przez algorytm SR
- Sesja kończy się, gdy wszystkie należne fiszki zostały przećwiczone
- Wyniki są trwale zapisane; kolejna sesja tego samego dnia nie pokazuje ponownie tych samych fiszek
