(() => {
    const input = document.getElementById('Search');
    const rows = document.querySelectorAll('#TableBody tr');

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