document.addEventListener('DOMContentLoaded', function () {
    const questionEl = document.getElementById('question');
    const answerEl = document.getElementById('answer');
    const createBtn = document.getElementById('create-btn');
    const errorEl = document.getElementById('create-error');
    const successEl = document.getElementById('create-success');

    createBtn.addEventListener('click', async function () {
        const question = questionEl.value.trim();
        const answer = answerEl.value.trim();

        hideError();
        hideSuccess();

        if (!question || !answer) {
            showError('Wypełnij oba pola — pytanie i odpowiedź.');
            return;
        }
        if (question.length > 5000) {
            showError('Pytanie nie może przekraczać 5000 znaków.');
            return;
        }
        if (answer.length > 5000) {
            showError('Odpowiedź nie może przekraczać 5000 znaków.');
            return;
        }

        setFormDisabled(true);

        try {
            const response = await fetch('/api/flashcards', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ question, answer, source: 'Manual' })
            });

            if (response.status === 201) {
                showSuccess('Fiszka dodana!');
                questionEl.value = '';
                answerEl.value = '';
                questionEl.focus();
            } else {
                const data = await response.json().catch(() => null);
                showError(data?.error || 'Wystąpił błąd podczas tworzenia fiszki.');
            }
        } catch {
            showError('Nie udało się połączyć z serwerem.');
        } finally {
            setFormDisabled(false);
        }
    });

    function setFormDisabled(disabled) {
        createBtn.disabled = disabled;
        questionEl.disabled = disabled;
        answerEl.disabled = disabled;
    }

    function showError(msg) {
        errorEl.textContent = msg;
        errorEl.className = 'error-message';
        errorEl.hidden = false;
    }

    function hideError() {
        errorEl.hidden = true;
        errorEl.textContent = '';
        errorEl.className = '';
    }

    function showSuccess(msg) {
        successEl.textContent = msg;
        successEl.className = 'create-success';
        successEl.hidden = false;
    }

    function hideSuccess() {
        successEl.hidden = true;
        successEl.textContent = '';
        successEl.className = '';
    }
});
