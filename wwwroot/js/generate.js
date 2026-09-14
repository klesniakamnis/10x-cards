document.addEventListener('DOMContentLoaded', function () {
    const sourceText = document.getElementById('source-text');
    const charCounter = document.getElementById('char-counter');
    const generateBtn = document.getElementById('generate-btn');
    const proposalsContainer = document.getElementById('proposals-container');
    const summaryContainer = document.getElementById('summary-container');
    const errorContainer = document.getElementById('error-container');

    let acceptedCount = 0;
    let totalProposals = 0;

    sourceText.addEventListener('input', function () {
        charCounter.textContent = this.value.length.toLocaleString('pl-PL') + ' / 10 000';
    });

    generateBtn.addEventListener('click', async function () {
        const text = sourceText.value.trim();

        if (!text) {
            showError('Wklej tekst przed wygenerowaniem fiszek.');
            return;
        }
        if (text.length > 10000) {
            showError('Tekst nie może przekraczać 10 000 znaków.');
            return;
        }

        hideError();
        hideSummary();
        proposalsContainer.innerHTML = '';
        acceptedCount = 0;
        totalProposals = 0;

        setLoading(true);

        try {
            const response = await fetch('/api/generation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ sourceText: text })
            });

            if (!response.ok) {
                const data = await response.json().catch(() => ({}));
                throw new Error(data.error || 'Wystąpił błąd podczas generowania.');
            }

            const data = await response.json();

            if (!data.proposals || data.proposals.length === 0) {
                showError('Nie udało się wygenerować fiszek z tego tekstu. Spróbuj wkleić inny fragment z większą ilością treści merytorycznej.');
                setLoading(false);
                return;
            }

            totalProposals = data.proposals.length;
            data.proposals.forEach(function (proposal) {
                renderProposalCard(proposal);
            });
        } catch (err) {
            showError(err.message);
        } finally {
            setLoading(false);
        }
    });

    function setLoading(loading) {
        generateBtn.disabled = loading;
        sourceText.disabled = loading;
        if (loading) {
            proposalsContainer.innerHTML = '<div class="loading-spinner">Generowanie fiszek...</div>';
        } else {
            var spinner = proposalsContainer.querySelector('.loading-spinner');
            if (spinner) spinner.remove();
        }
    }

    function showError(message) {
        errorContainer.innerHTML = '<div class="error-message">' + escapeHtml(message) + '</div>';
    }

    function hideError() {
        errorContainer.innerHTML = '';
    }

    function hideSummary() {
        summaryContainer.innerHTML = '';
    }

    function renderProposalCard(proposal) {
        const card = document.createElement('div');
        card.className = 'proposal-card';
        card.dataset.question = proposal.question;
        card.dataset.answer = proposal.answer;

        card.innerHTML =
            '<div class="card-section">' +
                '<div class="card-label">Pytanie</div>' +
                '<div class="card-text question-text">' + escapeHtml(proposal.question) + '</div>' +
            '</div>' +
            '<div class="card-section">' +
                '<div class="card-label">Odpowiedź</div>' +
                '<div class="card-text answer-text">' + escapeHtml(proposal.answer) + '</div>' +
            '</div>' +
            '<div class="card-actions">' +
                '<button class="btn-accept">Akceptuj</button>' +
                '<button class="btn-edit">Edytuj</button>' +
                '<button class="btn-reject">Odrzuć</button>' +
            '</div>';

        card.querySelector('.btn-accept').addEventListener('click', function () { acceptCard(card); });
        card.querySelector('.btn-edit').addEventListener('click', function () { editCard(card); });
        card.querySelector('.btn-reject').addEventListener('click', function () { rejectCard(card); });

        proposalsContainer.appendChild(card);
    }

    async function acceptCard(card) {
        const question = card.dataset.question;
        const answer = card.dataset.answer;
        const acceptBtn = card.querySelector('.btn-accept');

        acceptBtn.disabled = true;
        acceptBtn.textContent = '...';

        try {
            const response = await fetch('/api/flashcards', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ question: question, answer: answer, source: 'AiGenerated' })
            });

            if (!response.ok) {
                const data = await response.json().catch(() => ({}));
                throw new Error(data.error || 'Nie udało się zapisać fiszki.');
            }

            acceptedCount++;
            removeCard(card);
        } catch (err) {
            acceptBtn.disabled = false;
            acceptBtn.textContent = 'Akceptuj';
            const existing = card.querySelector('.card-error');
            if (existing) existing.remove();
            const errorDiv = document.createElement('div');
            errorDiv.className = 'card-error error-message';
            errorDiv.textContent = err.message;
            card.appendChild(errorDiv);
        }
    }

    function rejectCard(card) {
        removeCard(card);
    }

    function editCard(card) {
        const questionText = card.querySelector('.question-text');
        const answerText = card.querySelector('.answer-text');
        const actions = card.querySelector('.card-actions');

        const qTextarea = document.createElement('textarea');
        qTextarea.className = 'edit-textarea';
        qTextarea.value = card.dataset.question;

        const aTextarea = document.createElement('textarea');
        aTextarea.className = 'edit-textarea';
        aTextarea.value = card.dataset.answer;

        questionText.replaceWith(qTextarea);
        answerText.replaceWith(aTextarea);

        actions.innerHTML =
            '<button class="btn-accept">Zapisz</button>' +
            '<button class="btn-edit">Anuluj</button>';

        actions.querySelector('.btn-accept').addEventListener('click', function () {
            card.dataset.question = qTextarea.value;
            card.dataset.answer = aTextarea.value;

            const newQText = document.createElement('div');
            newQText.className = 'card-text question-text';
            newQText.textContent = qTextarea.value;

            const newAText = document.createElement('div');
            newAText.className = 'card-text answer-text';
            newAText.textContent = aTextarea.value;

            qTextarea.replaceWith(newQText);
            aTextarea.replaceWith(newAText);

            actions.innerHTML =
                '<button class="btn-accept">Akceptuj</button>' +
                '<button class="btn-edit">Edytuj</button>' +
                '<button class="btn-reject">Odrzuć</button>';

            actions.querySelector('.btn-accept').addEventListener('click', function () { acceptCard(card); });
            actions.querySelector('.btn-edit').addEventListener('click', function () { editCard(card); });
            actions.querySelector('.btn-reject').addEventListener('click', function () { rejectCard(card); });
        });

        actions.querySelector('.btn-edit').addEventListener('click', function () {
            const newQText = document.createElement('div');
            newQText.className = 'card-text question-text';
            newQText.textContent = card.dataset.question;

            const newAText = document.createElement('div');
            newAText.className = 'card-text answer-text';
            newAText.textContent = card.dataset.answer;

            qTextarea.replaceWith(newQText);
            aTextarea.replaceWith(newAText);

            actions.innerHTML =
                '<button class="btn-accept">Akceptuj</button>' +
                '<button class="btn-edit">Edytuj</button>' +
                '<button class="btn-reject">Odrzuć</button>';

            actions.querySelector('.btn-accept').addEventListener('click', function () { acceptCard(card); });
            actions.querySelector('.btn-edit').addEventListener('click', function () { editCard(card); });
            actions.querySelector('.btn-reject').addEventListener('click', function () { rejectCard(card); });
        });
    }

    function removeCard(card) {
        card.classList.add('fade-out');
        setTimeout(function () {
            card.remove();
            checkAllHandled();
        }, 300);
    }

    function checkAllHandled() {
        if (proposalsContainer.children.length === 0 && totalProposals > 0) {
            showSummary();
        }
    }

    function showSummary() {
        const msg = acceptedCount === 0
            ? 'Nie dodano żadnych fiszek. Wklej nowy tekst, aby wygenerować kolejne propozycje.'
            : 'Dodano ' + acceptedCount + ' ' + pluralize(acceptedCount) + ' do Twojej talii. Wklej kolejny tekst, aby wygenerować więcej.';
        summaryContainer.innerHTML = '<div class="summary-message">' + escapeHtml(msg) + '</div>';

        sourceText.value = '';
        sourceText.disabled = false;
        generateBtn.disabled = false;
        charCounter.textContent = '0 / 10 000';
    }

    function pluralize(n) {
        if (n === 1) return 'fiszkę';
        if (n >= 2 && n <= 4) return 'fiszki';
        return 'fiszek';
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
});
