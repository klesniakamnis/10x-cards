document.addEventListener('DOMContentLoaded', async function () {
    const container = document.getElementById('deck-container');
    const emptyEl = document.getElementById('deck-empty');
    const errorEl = document.getElementById('deck-error');

    let cards = [];

    try {
        const response = await fetch('/api/flashcards');
        if (!response.ok) throw new Error('Nie udało się pobrać fiszek.');
        cards = await response.json();
    } catch {
        showError('Nie udało się pobrać fiszek. Spróbuj odświeżyć stronę.');
        return;
    }

    renderDeck();

    function renderDeck() {
        container.innerHTML = '';
        if (cards.length === 0) {
            emptyEl.hidden = false;
            return;
        }
        emptyEl.hidden = true;
        cards.forEach(card => container.appendChild(createCardEl(card)));
    }

    function createCardEl(card) {
        const el = document.createElement('div');
        el.className = 'deck-card';
        el.dataset.id = card.id;
        renderCardDisplay(el, card);
        return el;
    }

    function renderCardDisplay(el, card) {
        const date = new Date(card.createdAt).toLocaleDateString('pl-PL');
        const sourceLabel = card.source === 'AiGenerated' ? 'AI' : 'Ręczna';
        el.innerHTML = '';

        const questionSection = document.createElement('div');
        questionSection.className = 'card-section';
        questionSection.innerHTML = '<div class="card-label">Pytanie</div>';
        const questionText = document.createElement('div');
        questionText.className = 'card-text';
        questionText.textContent = card.question;
        questionSection.appendChild(questionText);

        const answerSection = document.createElement('div');
        answerSection.className = 'card-section';
        answerSection.innerHTML = '<div class="card-label">Odpowiedź</div>';
        const answerText = document.createElement('div');
        answerText.className = 'card-text';
        answerText.textContent = card.answer;
        answerSection.appendChild(answerText);

        const meta = document.createElement('div');
        meta.className = 'card-meta';
        meta.innerHTML = '<span class="source-badge source-badge--' + (card.source === 'AiGenerated' ? 'ai' : 'manual') + '">' + sourceLabel + '</span>';
        const dateSpan = document.createElement('span');
        dateSpan.className = 'card-date';
        dateSpan.textContent = date;
        meta.appendChild(dateSpan);

        const actions = document.createElement('div');
        actions.className = 'card-actions';

        const editBtn = document.createElement('button');
        editBtn.className = 'btn-edit';
        editBtn.textContent = 'Edytuj';
        editBtn.addEventListener('click', () => renderCardEdit(el, card));

        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn-reject';
        deleteBtn.textContent = 'Usuń';
        deleteBtn.addEventListener('click', () => handleDelete(el, card));

        actions.appendChild(editBtn);
        actions.appendChild(deleteBtn);

        el.appendChild(questionSection);
        el.appendChild(answerSection);
        el.appendChild(meta);
        el.appendChild(actions);
    }

    function renderCardEdit(el, card) {
        el.innerHTML = '';

        const questionSection = document.createElement('div');
        questionSection.className = 'card-section';
        questionSection.innerHTML = '<div class="card-label">Pytanie</div>';
        const questionInput = document.createElement('textarea');
        questionInput.className = 'edit-textarea';
        questionInput.value = card.question;
        questionInput.maxLength = 5000;
        questionSection.appendChild(questionInput);

        const answerSection = document.createElement('div');
        answerSection.className = 'card-section';
        answerSection.innerHTML = '<div class="card-label">Odpowiedź</div>';
        const answerInput = document.createElement('textarea');
        answerInput.className = 'edit-textarea';
        answerInput.value = card.answer;
        answerInput.maxLength = 5000;
        answerSection.appendChild(answerInput);

        const cardError = document.createElement('div');
        cardError.className = 'card-error';
        cardError.hidden = true;

        const actions = document.createElement('div');
        actions.className = 'card-actions';

        const saveBtn = document.createElement('button');
        saveBtn.className = 'btn-accept';
        saveBtn.textContent = 'Zapisz';
        saveBtn.addEventListener('click', async () => {
            const newQuestion = questionInput.value.trim();
            const newAnswer = answerInput.value.trim();

            if (!newQuestion || !newAnswer) {
                cardError.textContent = 'Oba pola muszą być wypełnione.';
                cardError.className = 'card-error error-message';
                cardError.hidden = false;
                return;
            }
            if (newQuestion.length > 5000 || newAnswer.length > 5000) {
                cardError.textContent = 'Tekst nie może przekraczać 5000 znaków.';
                cardError.className = 'card-error error-message';
                cardError.hidden = false;
                return;
            }

            saveBtn.disabled = true;
            cancelBtn.disabled = true;

            try {
                const response = await fetch('/api/flashcards/' + card.id, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ question: newQuestion, answer: newAnswer })
                });

                if (!response.ok) {
                    const data = await response.json().catch(() => null);
                    throw new Error(data?.error || 'Błąd podczas zapisywania.');
                }

                const updated = await response.json();
                card.question = updated.question;
                card.answer = updated.answer;
                card.updatedAt = updated.updatedAt;
                renderCardDisplay(el, card);
            } catch (err) {
                cardError.textContent = err.message;
                cardError.className = 'card-error error-message';
                cardError.hidden = false;
                saveBtn.disabled = false;
                cancelBtn.disabled = false;
            }
        });

        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn-edit';
        cancelBtn.textContent = 'Anuluj';
        cancelBtn.addEventListener('click', () => renderCardDisplay(el, card));

        actions.appendChild(saveBtn);
        actions.appendChild(cancelBtn);

        el.appendChild(questionSection);
        el.appendChild(answerSection);
        el.appendChild(cardError);
        el.appendChild(actions);

        questionInput.focus();
    }

    async function handleDelete(el, card) {
        if (!confirm('Czy na pewno chcesz usunąć tę fiszkę?')) return;

        try {
            const response = await fetch('/api/flashcards/' + card.id, { method: 'DELETE' });
            if (!response.ok) throw new Error('Nie udało się usunąć fiszki.');

            el.classList.add('fade-out');
            setTimeout(() => {
                el.remove();
                cards = cards.filter(c => c.id !== card.id);
                if (cards.length === 0) emptyEl.hidden = false;
            }, 300);
        } catch {
            showError('Nie udało się usunąć fiszki. Spróbuj ponownie.');
        }
    }

    function showError(msg) {
        errorEl.textContent = msg;
        errorEl.className = 'error-message';
        errorEl.hidden = false;
    }
});
