// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Generic search function
function setupSearch(searchInputId, tableBodyId, apiEndpoint, rowTemplate) {
    const searchInput = document.getElementById(searchInputId);
    const tableBody = document.getElementById(tableBodyId);

    if (!searchInput || !tableBody) return;

    let isSearching = false;
    let timeout = null;

    searchInput.addEventListener("input", function () {
        clearTimeout(timeout);

        timeout = setTimeout(() => {
            if (isSearching) return;
            isSearching = true;

            fetch(`${apiEndpoint}?term=${encodeURIComponent(this.value)}`)
                .then(res => res.json())
                .then(data => {
                    tableBody.innerHTML = "";
                    const rows = data.map(item => rowTemplate(item)).join("");
                    tableBody.innerHTML = rows;
                })
                .finally(() => {
                    isSearching = false;
                });
        }, 200);
    });
}

// Customer search
setupSearch(
    "customerSearch",
    "customerTable",
    "/Customers/Search",
    (item) => `
        <tr>
            <td>${item.naam}</td>
            <td>${item.email}</td>
            <td>${item.aantalBestellingen}</td>
            <td class="action-buttons">
                <a href="/Customers/Details/${item.id}" class="details-link">Details</a>
                <a href="/Customers/Edit/${item.id}" class="crud-icon-btn icon-only edit-btn">
                    <svg class="icon-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                    </svg>
                </a>
                <a href="/Customers/Delete/${item.id}" class="crud-icon-btn icon-only delete-btn">
                    <svg class="icon-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M3 6h18M8 6V4h8v2M6 6l1 14a2 2 0 002 2h6a2 2 0 002-2l1-14" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M10 11v6M14 11v6" />
                    </svg>
                </a>
            </td>
        </tr>
    `
);

// Leverancier search
setupSearch(
    "leverancierSearch",
    "leverancierTable",
    "/Leverancier/Search",
    (item) => `
        <tr>
            <td>${item.naam}</td>
            <td class="action-buttons">
                <a href="/Leverancier/Edit/${item.id}" class="crud-icon-btn icon-only edit-btn">
                    <svg class="icon-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                    </svg>
                </a>
                <a href="/Leverancier/Delete/${item.id}" class="crud-icon-btn icon-only delete-btn">
                    <svg class="icon-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M3 6h18M8 6V4h8v2M6 6l1 14a2 2 0 002 2h6a2 2 0 002-2l1-14" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                              d="M10 11v6M14 11v6" />
                    </svg>
                </a>
            </td>
        </tr>
    `
);
//deleate popup
function openModal() {
    const modal = document.getElementById("deleteModal");
    if (modal) modal.style.display = "flex";
}

function closeModal() {
    const modal = document.getElementById("deleteModal");
    if (modal) modal.style.display = "none";
}

function confirmDelete() {
    const form = document.getElementById("deleteForm");
    if (form) form.submit();
}
//hamburgermenu
document.addEventListener("DOMContentLoaded", function () {
    const btn = document.getElementById("menuToggle");
    const sidebar = document.getElementById("sidebar");
    const content = document.querySelector(".main-content");

    if (!btn) {
        console.error("menuToggle niet gevonden");
        return;
    }

    if (!sidebar) {
        console.error("sidebar niet gevonden");
        return;
    }

    btn.addEventListener("click", function () {
        console.log("hamburger clicked");

        sidebar.classList.toggle("closed");

        if (content) {
            content.classList.toggle("expanded");
        }
    });
});

function onMedewerkerClick() {
    alert("Onder constructie");
}