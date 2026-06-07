(() => {
    const input = document.getElementById('categorySearch');
    const rows = document.querySelectorAll('#categoryTableBody tr');

    if (!input || rows.length === 0) {
        return;
    }

    input.addEventListener('input', () => {
        const term = input.value.toLowerCase().trim();

        rows.forEach(row => {
            const name = (row.dataset.categoryName || '').toLowerCase();
            row.classList.toggle('is-hidden', !name.includes(term));
        });
    });
})();

