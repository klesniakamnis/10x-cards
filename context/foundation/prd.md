---
project: "10xCards"
version: 1
status: draft
created: 2026-09-11
context_type: greenfield
product_type: web-app
target_scale:
  users: small
  qps: low
  data_volume: small
timeline_budget:
  mvp_weeks: 3
  hard_deadline: null
  after_hours_only: true
---

# 10xCards — Product Requirements Document

## Vision & Problem Statement

Studenci uczelni wyższych, którzy chcą wykorzystać spaced repetition do skutecznej nauki na sesję, rezygnują z tej metody, bo tworzenie wysokiej jakości fiszek zabiera 2–5 minut za sztukę × setki jednostek materiału do przerobienia. W typowym momencie — niedzielny wieczór, 40+ stron skryptu, egzamin w środę — wybór między „przeczytać skrypt jeszcze raz" a „stworzyć talię fiszek" wygrywa czytanie, mimo że jest wielokrotnie mniej skuteczne. Koszt dzisiaj: rezygnacja z SR albo używanie gorszych metod nauki, przy pełnej świadomości, że fiszki byłyby lepsze.

Insight: dojrzałe narzędzia do fiszek zakładają, że użytkownik zainwestuje czas w utworzenie talii. Ta bariera onboardingowa jest głównym filtrem odsiewającym uczących się od SR. Możliwe jest wygenerowanie szkicu fiszek z wklejonego tekstu w sekundy — pod warunkiem, że kontrola jakości (szybka akceptacja / edycja / odrzucenie) odbywa się w tym samym produkcie, a nie jako kolejny wieloetapowy workflow. 10xCards zdejmuje barierę tworzenia bez rezygnacji z kontroli jakości.

## User & Persona

**Primary — Student uczelni wyższej.**  
Kontekst: uczy się do kolokwiów i egzaminów z konkretnych skryptów, podręczników i notatek z wykładu. Ma duży wolumen materiału do przerobienia w krótkim czasie (sesja). Zna spaced repetition z opowiadania lub próbował istniejących narzędzi i odbił się od tworzenia talii. Moment sięgnięcia po produkt: sesja + krótki termin + duży tekst — potrzebuje w jeden wieczór przekształcić 40-stronicowy skrypt w działającą talię i zacząć powtarzać.

### Secondary persona

**Profesjonalista uczący się zawodu** (programista, lekarz, prawnik). Uczy się z dokumentacji, artykułów, aktualizacji standardów. Rytm długoterminowy (miesiące), nie sprint. MVP obsługuje ten segment „przy okazji" — nie optymalizujemy pod niego workflow, ale ten sam mechanizm generowania z wklejonego tekstu również dla niego działa.

## Success Criteria

### Primary
- 75% fiszek wygenerowanych przez produkt jest akceptowanych przez użytkownika (bez edycji lub z drobną edycją traktowaną jako akceptacja) — mierzone jako `zaakceptowane / (zaakceptowane + odrzucone + porzucone)` w cyklu życia sesji generowania.
- 75% wszystkich fiszek w bazie zostało utworzonych ścieżką generowania (nie manualną) — potwierdza, że użytkownicy faktycznie używają głównej wartości produktu.

### Secondary
- Użytkownik wraca do produktu co najmniej 3 razy w tygodniu — proxy dla wykształcenia się nawyku powtórek (nie samego stworzenia talii i odejścia).

### Guardrails
- Tekst wklejony przez użytkownika do generowania fiszek nie jest wykorzystywany do trenowania modeli po stronie dostawcy generowania ani utrwalany poza kontekstem pojedynczego zapytania generującego. Naruszenie tego jest regresją nawet przy spełnionych kryteriach Primary.

## User Stories

### US-01: Student generuje fiszki z wklejonego skryptu

- **Given** zalogowany student z pustą talią
- **When** wkleja fragment skryptu (np. 5 stron) i uruchamia generowanie fiszek
- **Then** widzi listę propozycji Q/A, może każdą zaakceptować, edytować lub odrzucić, a zaakceptowane fiszki lądują w jego talii

#### Acceptance Criteria
- Lista propozycji pojawia się w rozsądnym czasie (nieblokujący UX; guardrails ustalane w NFR).
- Akceptacja fiszki nie wymaga dodatkowych akcji poza pojedynczą decyzją „akceptuj".
- Edycja Q i/lub A przed akceptacją odbywa się w tym samym widoku, bez przechodzenia na osobny ekran.
- Odrzucona propozycja znika z listy i nie trafia do talii.
- Po zamknięciu widoku propozycji użytkownik widzi liczbę zaakceptowanych fiszek dodanych do talii.

### US-02: Student tworzy fiszkę ręcznie

- **Given** zalogowany student przeglądający swoją talię
- **When** wybiera „Dodaj fiszkę" i wpisuje pytanie oraz odpowiedź
- **Then** fiszka jest zapisana w talii i wchodzi do rotacji powtórek na tych samych zasadach co fiszki wygenerowane przez produkt

#### Acceptance Criteria
- Formularz wymaga wypełnienia obu pól (pytania i odpowiedzi).
- Zapisana fiszka jest natychmiast widoczna na liście talii.
- Ręcznie utworzona fiszka jest traktowana identycznie w planowaniu powtórek jak fiszka wygenerowana.

### US-03: Student przeprowadza sesję powtórek

- **Given** zalogowany student z talią zawierającą fiszki, których terminy powtórek wypadają dziś lub wcześniej
- **When** rozpoczyna sesję „Ucz się"
- **Then** produkt prezentuje mu fiszki jedna po drugiej; student odpowiada i ocenia jakość swojego przypomnienia, a produkt wyznacza kolejny termin powtórki

#### Acceptance Criteria
- Sesja pokazuje wyłącznie fiszki z bieżącym lub przekroczonym terminem powtórki.
- Po każdej fiszce użytkownik ocenia jakość swojego przypomnienia w skali oczekiwanej przez wybrane planowanie powtórek.
- Sesja kończy się, gdy wszystkie należne na dzisiaj fiszki zostały przećwiczone.
- Wyniki są trwale zapisane; kolejna sesja tego samego dnia nie pokazuje ponownie tych samych fiszek.

## Functional Requirements

### Authentication
- FR-001: Odwiedzający może podać email i zażądać jednorazowego linka logującego dostarczanego na ten email. Priority: must-have
  > Socrates: Rozważone kontrargumenty (wymóg dostępu do maila, ryzyko trafienia do spamu). Decyzja: logowanie bezhasłowe jest świadomą decyzją; ryzyka nie są blockerami MVP.
- FR-002: Użytkownik z ważnym linkiem logującym może otworzyć aplikację jako zalogowany. Priority: must-have
  > Socrates: Rozważone kontrargumenty (okres ważności linka, jednorazowość, sesja per-urządzenie). Decyzja: detale bezpieczeństwa → NFR, nie zmiana FR.
- FR-003: Zalogowany użytkownik może się wylogować. Priority: must-have
  > Socrates: Rozważone kontrargumenty (friction ponownego logowania, wylogowanie „wszystkie sesje"). Decyzja: standardowa funkcja; ryzyka realne, ale nie zmieniają FR w MVP.

### Generowanie fiszek
- FR-004: Zalogowany użytkownik może wkleić fragment tekstu (do ustalonego limitu długości) i zażądać wygenerowania propozycji fiszek. Priority: must-have
  > Socrates: Rozważony kontrargument: brak limitu długości → nieograniczone zużycie zasobów po stronie generowania (koszt, latencja). Rozwiązanie: FR-004 nakłada limit długości; konkretna wartość liczbowa trafia do Open Questions do rozstrzygnięcia przy wyborze stacku.
- FR-005: Zalogowany użytkownik widzi listę wygenerowanych par pytanie/odpowiedź jako propozycje (nie zapisane fiszki). Priority: must-have
  > Socrates: Rozważone kontrargumenty (rozjeżdżający się UX listy przy dużej liczbie propozycji, deduplikacja). Decyzja: MVP — prosta lista; optymalizacje później.
- FR-006: Zalogowany użytkownik może zaakceptować propozycję fiszki, co dołącza ją do jego talii. Priority: must-have
  > Socrates: Rozważone kontrargumenty (bulk accept obchodzi metrykę 75%, brak historii akceptacji / undo). Decyzja: pojedyncza akceptacja per fiszka; bulk explicite wykluczony w Non-Goals.
- FR-007: Zalogowany użytkownik może edytować treść propozycji (pytanie i/lub odpowiedź) przed akceptacją. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak walidacji treści, metryka „acceptance" zawyżana przez ciężko edytowane propozycje). Decyzja: edycja inline jest standardem; walidacja → NFR; kwestia „ciężkiej edycji" → Open Questions.
- FR-008: Zalogowany użytkownik może odrzucić propozycję fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak reason capture do iteracji, brak undo dla odrzucenia). Decyzja: odrzucenie binarne w MVP; reason capture jako nice-to-have poza MVP.

### Manualne tworzenie
- FR-009: Zalogowany użytkownik może ręcznie utworzyć fiszkę (pytanie i odpowiedź) bez korzystania ze ścieżki generowania. Priority: must-have
  > Socrates: Rozważone kontrargumenty (osłabienie insightu „zdejmujemy barierę", trudność liczenia metryki 75% z generowania). Decyzja: manualne pozostaje jako safety net; kryterium Primary #2 (75% ze ścieżki generowania) obsługuje ryzyko metryczne.

### Zarządzanie fiszkami
- FR-010: Zalogowany użytkownik może przeglądać wszystkie swoje zapisane fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak talii/kategoryzacji, brak wyszukiwania). Decyzja: MVP — podstawowa lista; kategorie i wyszukiwanie poza zakresem MVP.
- FR-011: Zalogowany użytkownik może edytować istniejącą fiszkę. Priority: must-have
  > Socrates: Rozważone kontrargumenty (edycja niszczy historię powtórek, brak historii wersji). Decyzja: edycja pozostaje; kwestie stanu powtórek → NFR/Business Logic.
- FR-012: Zalogowany użytkownik może usunąć istniejącą fiszkę. Priority: must-have
  > Socrates: Rozważone kontrargumenty (brak potwierdzenia, przywracalność). Decyzja: hard delete + wymóg potwierdzenia jako standardowy UX guardrail (NFR).

### Powtórki
- FR-013: Zalogowany użytkownik może rozpocząć sesję powtórek — produkt wybiera fiszki należne do przećwiczenia dzisiaj. Priority: must-have
  > Socrates: Rozważone kontrargumenty (niedookreślona rodzina planowania, empty state dla nowej talii). Decyzja: rodzina planowania → Open Questions; empty state → Open Questions.
- FR-014: Zalogowany użytkownik może odpowiedzieć na fiszkę i ocenić jakość swojego przypomnienia w skali oczekiwanej przez planowanie powtórek. Priority: must-have
  > Socrates: Rozważone kontrargumenty (skala oceny to funkcja algorytmu, self-assessment nie do skontrolowania). Decyzja: skala razem z wyborem planowania → Open Questions; self-assessment akceptowany jako aksjomat.
- FR-015: System zapisuje wynik powtórki i wyznacza kolejny termin powtórki dla fiszki. Priority: must-have
  > Socrates: Rozważone kontrargumenty (zachowanie stanu przy przerwanej sesji, strefy czasowe). Decyzja: zapisywanie i wyznaczanie terminu to core produktu; szczegóły → Open Questions.

## Non-Functional Requirements

- Podczas generowania propozycji fiszek użytkownik widzi ciągły, widoczny sygnał postępu, jeśli operacja trwa dłużej niż dwie sekundy; brak sztywnego górnego limitu czasu w MVP, ale operacja nie może zostawiać użytkownika bez feedbacku.
- Produkt obsługuje wejściowy tekst w języku polskim i angielskim; propozycje fiszek generowane są w tym samym języku, w którym napisany jest wklejony tekst.
- Produkt pozostaje w pełni użyteczny na dwóch najnowszych stabilnych wersjach dwóch głównych przeglądarek desktopowych (Chrome, Firefox); użyteczność na innych przeglądarkach i urządzeniach mobilnych nie jest gwarantowana.
- Tekst wklejony przez użytkownika nie opuszcza kontekstu pojedynczego zapytania generującego w sposób obserwowalny — nie pojawia się w wynikach wyszukiwania, nie trafia do innych użytkowników, nie jest wykorzystywany do trenowania modeli.
- Usunięcie fiszki wymaga jawnego potwierdzenia użytkownika (jedna dodatkowa akcja) — chroni przed przypadkową utratą treści.
- Widoki produktowe są dostępne wyłącznie dla zalogowanego użytkownika; próba dostania się do widoku wewnętrznego bez ważnej sesji kończy się przekierowaniem na ekran logowania bez wycieku informacji o istnieniu zasobu.

## Business Logic

10xCards zamienia wklejony przez użytkownika fragment tekstu w propozycje fiszek pytanie/odpowiedź, które użytkownik akceptuje, edytuje lub odrzuca — a następnie planuje kolejne powtórki tych fiszek na podstawie samooceny jakości przypomnienia po każdej odpowiedzi.

Reguła składa się z dwóch etapów, oba widocznych dla użytkownika:

1. **Propozycja fiszek + walidacja przez człowieka.** Na wejściu: fragment tekstu w języku polskim lub angielskim, wklejony przez użytkownika. Na wyjściu: lista propozycji par pytanie/odpowiedź. Użytkownik napotyka je jako listę do przejrzenia — akceptuje po jednej, edytuje inline i zapisuje jako akceptację, albo odrzuca. Metryki 75% acceptance i 75% udziału ścieżki propozycji odnoszą się do tego etapu.

2. **Planowanie powtórek.** Na wejściu: zbiór zaakceptowanych fiszek użytkownika + historia jego dotychczasowych ocen jakości przypomnienia. Na wyjściu: kolejny termin powtórki per fiszka. Użytkownik napotyka to jako sesję „Ucz się" — produkt prezentuje mu fiszki, których termin nadszedł; użytkownik po każdej odpowiedzi ocenia jakość swojego przypomnienia w gradowanej skali; produkt wyznacza kolejny termin tej fiszki na podstawie tej oceny i historii poprzednich ocen.

Manualnie utworzone fiszki (FR-009) wchodzą w etap #2 na identycznych zasadach jak fiszki ze ścieżki propozycji — z pominięciem etapu #1.

## Access Control

Model jednorolowy: każdy zarejestrowany użytkownik ma taki sam zestaw uprawnień — może tworzyć, edytować i usuwać wyłącznie własne fiszki. Nie istnieje rola administratora, tryb gościa ani współdzielenie treści między użytkownikami. Wszystkie widoki produktowe są za ścianą logowania; niezalogowany użytkownik widzi wyłącznie landing, ekran logowania i ekran rejestracji.

Mechanizm logowania jest bezhasłowy: użytkownik podaje email, otrzymuje jednorazowy link logujący na ten email i po jego otwarciu jest zalogowany. Rejestracja i logowanie są tą samą ścieżką — pierwsze użycie linka na wcześniej nieznany adres oznacza utworzenie konta. Użytkownik nie zarządza hasłem; produkt nie przechowuje sekretu, który mógłby wyciec.

## Non-Goals

- **Brak własnego algorytmu spaced repetition.** MVP nie inwestuje w projektowanie ani tuning parametrów planowania powtórek — używamy gotowej, dojrzałej rodziny wybranej przy stack-selection.
- **Brak importu z formatów PDF, DOCX, EPUB ani z narzędzi do notatek.** MVP obsługuje wyłącznie tekst wklejony do pola input.
- **Brak współdzielenia talii między użytkownikami.** Każdy użytkownik widzi wyłącznie swoje fiszki; nie ma publikacji, followowania, remix'owania cudzych talii.
- **Brak integracji z platformami edukacyjnymi (Moodle, Google Classroom, Canvas i podobne).** MVP jest samodzielną aplikacją.
- **Brak aplikacji mobilnej.** MVP dostarczany wyłącznie jako aplikacja webowa; użyteczność na urządzeniach mobilnych nie jest gwarantowana.
- **Brak trybu offline, funkcjonalności PWA ani synchronizacji między urządzeniami.** Aplikacja działa online-only; sesja per-przeglądarka.
- **Brak monetyzacji, paywalla ani kont premium.** W MVP wszystkie funkcje są darmowe; monetyzacja to zakres poza MVP.
- **Brak systemu tagów, kategorii ani wielu talii per użytkownik.** Każdy użytkownik ma jeden liniowy zbiór fiszek; struktura organizacyjna to zakres poza MVP.

## Open Questions

1. **Konkretny limit długości wklejanego tekstu w FR-004.** Punkt startowy rozważany w discovery: rząd 10 000 znaków. Ostateczna wartość zależy od wybranego dostawcy generowania (dostępny kontekst wejściowy, koszt pojedynczego zapytania). — Owner: rozstrzygnięcie przy stack-selection.
2. **Konkretna rodzina algorytmu planowania powtórek i kształt skali oceny.** W discovery wstępnie skłoniono się ku rodzinie SM-2 ze skalą 0–5 (patrz shape-notes `## Forward: tech-stack`). Wybór konkretnej biblioteki i dokładny kształt skali (liczba stopni, etykiety) domyka się przy stack-selection. — Owner: rozstrzygnięcie przy stack-selection.
3. **Empty state dla nowej talii przy pierwszym „Ucz się" (FR-013).** Co widzi użytkownik, który nie ma jeszcze żadnych fiszek z nadchodzącym terminem powtórki? Sugerowana odpowiedź: komunikat + wezwanie do akcji „Dodaj pierwsze fiszki". — Owner: rozstrzygnięcie w /10x-plan.
4. **Strefa czasowa dla wyznaczania kolejnych terminów powtórek (FR-015).** Czas serwera z konwersją do lokalnej strefy po stronie klienta, czy strefa użytkownika utrwalona po stronie serwera? Wpływa na to, kiedy fiszki „przypadają na dziś". — Owner: rozstrzygnięcie przy stack-selection.
5. **Zachowanie stanu przy przerwanej sesji powtórek (FR-013–FR-015).** Czy sesję można wznowić od miejsca przerwania, czy zaczyna się od początku? Decyzja UX z konsekwencjami dla stanu po stronie klienta i serwera. — Owner: rozstrzygnięcie w /10x-plan.
6. **Metryka „acceptance" a ciężko edytowana propozycja (FR-007).** Czy propozycja, w której użytkownik przepisał większość treści (np. > 90%), liczy się jako „zaakceptowana" (poziom edycji nie ma znaczenia), czy potrzebna jest odrębna kategoria „zaakceptowana po ciężkiej edycji"? — Owner: rozstrzygnięcie przy pierwszej analizie danych z produkcji.
