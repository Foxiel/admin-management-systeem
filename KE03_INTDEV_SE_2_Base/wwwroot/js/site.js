// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//searchbar
let isSearching = false;
const searchInput = document.getElementById("customerSearch");
const tableBody = document.getElementById("customerTable");

let timeout = null;

searchInput.addEventListener("input", function () {

    clearTimeout(timeout);

    timeout = setTimeout(() => {

        if (isSearching) return;
        isSearching = true;

        fetch(`/Customers/Search?term=${encodeURIComponent(this.value)}`)
            .then(res => res.json())
            .then(data => {

                // BELANGRIJK: volledige reset
                tableBody.innerHTML = "";

                const rows = data.map(item => `
                    <tr>
                        <td>${item.name}</td>
                        <td>${item.email}</td>
                        <td>${item.telefoonnr}</td>
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

                                    <!-- trash can -->
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                          d="M3 6h18M8 6V4h8v2M6 6l1 14a2 2 0 002 2h6a2 2 0 002-2l1-14" />

                                    <!-- lid details -->
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                          d="M10 11v6M14 11v6" />

                                </svg>
                            </a>

                        </td>
                    </tr>
                `).join("");

                tableBody.innerHTML = rows;

            })
            .finally(() => {
                isSearching = false;
            });

    }, 200);
});
