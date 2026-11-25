// ─── Toast Notifications ───────────────────────────────────────
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const id = 'toast_' + Date.now();
    const icon = type === 'success' ? '✓' : '✕';
    const colorClass = type === 'success' ? 'toast-success' : 'toast-error';
    const textColor = type === 'success' ? '#166534' : '#991B1B';

    const toastHtml = `
        <div id="${id}" class="toast toast-hotel ${colorClass} show align-items-center" role="alert" aria-atomic="true">
            <div class="d-flex align-items-center p-3 gap-3">
                <span style="font-size:1.1rem;color:${textColor};font-weight:700;">${icon}</span>
                <div class="flex-grow-1" style="font-size:0.875rem;color:#1F2937;">${message}</div>
                <button type="button" class="btn-close btn-close-sm" onclick="document.getElementById('${id}').remove()"></button>
            </div>
        </div>`;

    container.insertAdjacentHTML('beforeend', toastHtml);
    setTimeout(() => { const el = document.getElementById(id); if (el) el.remove(); }, 5000);
}

// Auto-dismiss toasts on page load
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.auto-toast').forEach(el => {
        setTimeout(() => el.remove(), 5000);
    });
});

// ─── Date Validation ───────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    const checkIn  = document.getElementById('CheckInDate');
    const checkOut = document.getElementById('CheckOutDate');

    if (checkIn && checkOut) {
        // Set min dates
        const today = new Date().toISOString().split('T')[0];
        checkIn.min = today;
        checkOut.min = today;

        checkIn.addEventListener('change', () => {
            const cin = new Date(checkIn.value);
            cin.setDate(cin.getDate() + 1);
            checkOut.min = cin.toISOString().split('T')[0];
            if (checkOut.value && new Date(checkOut.value) <= new Date(checkIn.value)) {
                checkOut.value = cin.toISOString().split('T')[0];
            }
            updateNightsDisplay();
        });

        checkOut.addEventListener('change', updateNightsDisplay);
        updateNightsDisplay();
    }

    function updateNightsDisplay() {
        const display = document.getElementById('nightsDisplay');
        if (!display || !checkIn?.value || !checkOut?.value) return;
        const diff = Math.round((new Date(checkOut.value) - new Date(checkIn.value)) / 86400000);
        if (diff > 0) {
            display.textContent = diff + ' Night' + (diff > 1 ? 's' : '');
            display.className = 'badge bg-success';
        } else {
            display.textContent = 'Invalid';
            display.className = 'badge bg-danger';
        }
    }
});

// ─── AJAX: Add Service to Active Stay ─────────────────────────
function addService(bookingId) {
    const serviceId = document.getElementById('serviceSelect').value;
    const quantity  = parseInt(document.getElementById('serviceQty').value) || 1;

    if (!serviceId) { showToast('Please select a service.', 'error'); return; }

    fetch('/FrontDesk/AddService', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
        body: JSON.stringify({ bookingId, serviceId: parseInt(serviceId), quantity })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            updateServicesTable(data.services);
            updateRunningTotal(data.roomTotal, data.serviceTotal, data.grandEstimate);
            showToast('Service added successfully!', 'success');
        } else {
            showToast(data.message || 'Failed to add service.', 'error');
        }
    })
    .catch(() => showToast('Network error. Please try again.', 'error'));
}

// ─── AJAX: Remove Service ─────────────────────────────────────
function removeService(bookingServiceId, bookingId) {
    if (!confirm('Remove this service?')) return;

    fetch('/FrontDesk/RemoveService', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
        body: JSON.stringify({ bookingServiceId, bookingId })
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            updateServicesTable(data.services);
            updateRunningTotal(data.roomTotal, data.serviceTotal, data.grandEstimate);
            showToast('Service removed.', 'success');
        } else {
            showToast(data.message || 'Failed to remove service.', 'error');
        }
    })
    .catch(() => showToast('Network error.', 'error'));
}

function updateServicesTable(services) {
    const tbody = document.getElementById('servicesTableBody');
    if (!tbody) return;

    if (!services || services.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">No services added yet.</td></tr>';
        return;
    }

    tbody.innerHTML = services.map(s => `
        <tr>
            <td>${s.name}</td>
            <td class="text-center">${s.qty}</td>
            <td class="text-end">₹${s.unitPrice.toFixed(2)}</td>
            <td class="text-end fw-semibold">₹${s.total.toFixed(2)}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-outline-danger py-0 px-2"
                        onclick="removeService(${s.id}, ${document.getElementById('activeBookingId').value})">
                    &times;
                </button>
            </td>
        </tr>
    `).join('');
}

function updateRunningTotal(roomTotal, serviceTotal, grandTotal) {
    const fmt = v => '₹' + parseFloat(v).toLocaleString('en-IN', { minimumFractionDigits: 2 });
    const setEl = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
    setEl('rtRoomTotal', fmt(roomTotal));
    setEl('rtServiceTotal', fmt(serviceTotal));
    setEl('rtGrandTotal', fmt(grandTotal));
}

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

// ─── Checkout Preview: Live discount recalculation ────────────
document.addEventListener('DOMContentLoaded', () => {
    const discountInput = document.getElementById('Discount');
    if (discountInput) {
        discountInput.addEventListener('input', recalcCheckout);
    }
});

function recalcCheckout() {
    const roomCharges    = parseFloat(document.getElementById('roomChargesRaw')?.value || 0);
    const serviceCharges = parseFloat(document.getElementById('serviceChargesRaw')?.value || 0);
    const gstRate        = parseFloat(document.getElementById('gstRateRaw')?.value || 0);
    let discount         = parseFloat(document.getElementById('Discount')?.value || 0);
    if (discount < 0) discount = 0;

    const taxable    = Math.max(0, roomCharges - discount);
    const tax        = Math.round(taxable * gstRate / 100 * 100) / 100;
    const cgst       = Math.round(tax / 2 * 100) / 100;
    const sgst       = Math.round((tax - cgst) * 100) / 100;
    const grandTotal = roomCharges + serviceCharges - discount + tax;

    const fmt = v => v.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const setEl = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };

    setEl('previewCgst', fmt(cgst));
    setEl('previewSgst', fmt(sgst));
    setEl('previewTax', fmt(tax));
    setEl('previewGrandTotal', fmt(Math.max(0, grandTotal)));
}

// ─── Confirm Dialogs ───────────────────────────────────────────
document.querySelectorAll('[data-confirm]').forEach(btn => {
    btn.addEventListener('click', e => {
        if (!confirm(btn.dataset.confirm)) e.preventDefault();
    });
});
