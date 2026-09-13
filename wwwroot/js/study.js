document.addEventListener('DOMContentLoaded', async function () {
    const emptyEl = document.getElementById('study-empty');
    const cardEl = document.getElementById('study-card');
    const summaryEl = document.getElementById('study-summary');
    const errorEl = document.getElementById('study-error');

    const progressEl = cardEl.querySelector('.study-progress');
    const questionEl = cardEl.querySelector('.study-question');
    const answerEl = cardEl.querySelector('.study-answer');
    const revealBtn = cardEl.querySelector('.study-reveal-btn');
    const gradeButtons = cardEl.querySelector('.grade-buttons');
    const gradeError = cardEl.querySelector('.grade-error');
    const summaryCount = summaryEl.querySelector('.summary-count');

    let cards = [];
    let currentIndex = 0;

    try {
        const response = await fetch('/api/flashcards/due');
        if (!response.ok) throw new Error('Nie udało się pobrać fiszek.');
        cards = await response.json();
    } catch {
        showError('Nie udało się pobrać fiszek do powtórki. Spróbuj odświeżyć stronę.');
        return;
    }

    if (cards.length === 0) {
        emptyEl.hidden = false;
        return;
    }

    showCard();

    revealBtn.addEventListener('click', function () {
        answerEl.hidden = false;
        revealBtn.hidden = true;
        gradeButtons.hidden = false;
    });

    gradeButtons.addEventListener('click', async function (e) {
        const btn = e.target.closest('.grade-btn');
        if (!btn) return;

        const grade = parseInt(btn.dataset.grade, 10);
        const card = cards[currentIndex];

        setGradeButtonsDisabled(true);
        gradeError.hidden = true;

        try {
            const response = await fetch('/api/flashcards/' + card.id + '/review', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ grade: grade })
            });

            if (!response.ok) {
                const data = await response.json().catch(() => null);
                throw new Error(data?.error || 'Błąd podczas zapisywania oceny.');
            }

            currentIndex++;
            if (currentIndex >= cards.length) {
                showSummary();
            } else {
                showCard();
            }
        } catch (err) {
            gradeError.textContent = err.message;
            gradeError.hidden = false;
            setGradeButtonsDisabled(false);
        }
    });

    function showCard() {
        const card = cards[currentIndex];
        cardEl.hidden = false;
        progressEl.textContent = 'Fiszka ' + (currentIndex + 1) + ' z ' + cards.length;
        questionEl.textContent = card.question;
        answerEl.textContent = card.answer;
        answerEl.hidden = true;
        revealBtn.hidden = false;
        gradeButtons.hidden = true;
        gradeError.hidden = true;
        setGradeButtonsDisabled(false);
    }

    function showSummary() {
        cardEl.hidden = true;
        summaryCount.textContent = 'Powtórzono ' + cards.length + ' ' + pluralize(cards.length) + '.';
        summaryEl.hidden = false;
    }

    function pluralize(n) {
        if (n === 1) return 'fiszkę';
        if (n >= 2 && n <= 4) return 'fiszki';
        return 'fiszek';
    }

    function setGradeButtonsDisabled(disabled) {
        gradeButtons.querySelectorAll('.grade-btn').forEach(function (btn) {
            btn.disabled = disabled;
        });
    }

    function showError(msg) {
        errorEl.textContent = msg;
        errorEl.className = 'error-message';
        errorEl.hidden = false;
    }
});
