(() => {
    function initTableSearch() {
        const input = document.querySelector('[data-table-search]');
        const tableBodySelector = input?.dataset.tableSearchTarget;
        const tableBody = tableBodySelector ? document.querySelector(tableBodySelector) : null;
        const rows = tableBody ? tableBody.querySelectorAll('tr') : [];

        if (!input || !tableBody || rows.length === 0) {
            return;
        }

        input.addEventListener('input', () => {
            const term = input.value.toLowerCase().trim();

            rows.forEach(row => {
                const rowText = row.textContent.toLowerCase();
                row.classList.toggle('is-hidden', !rowText.includes(term));
            });
        });
    }

    document.addEventListener('DOMContentLoaded', initTableSearch);
})();