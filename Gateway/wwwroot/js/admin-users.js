let currentUserId = null;

// Initialize Page Data on DOM Load
document.addEventListener('DOMContentLoaded', () => {
    // 1. First preference: Get ID from the HTML element attribute (from @Model.Id)
    const container = document.getElementById('userDetailsContainer');
    if (container && container.dataset.userId) {
        currentUserId = container.dataset.userId;
    }

    // 2. Fallback: Parse the GUID directly from the browser URL (handles /Details/{id} or /{id}/Groups)
    if (!currentUserId) {
        const pathSegments = window.location.pathname.split('/');
        const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
        currentUserId = pathSegments.find(segment => guidRegex.test(segment));
    }

    if (currentUserId) {
        loadAssignedGroups();
        loadAvailableGroups();
    }
});

// [BUENO] Search / Filter Table by Email
function filterUsersByEmail() {
    let input = document.getElementById('emailSearchInput').value.toLowerCase();
    let rows = document.querySelectorAll('#usersTable tbody tr');
    rows.forEach(row => {
        let emailCell = row.querySelector('.user-email');
        if (emailCell) {
            let emailText = emailCell.textContent.toLowerCase();
            row.style.display = emailText.includes(input) ? '' : 'none';
        }
    });
}

// Toggle Active Status
async function toggleUserStatus(userId, checkbox) {
    const row = checkbox.closest('tr');
    const badge = row ? row.querySelector('.status-badge') : null;
    const toggleLabel = row ? row.querySelector('.toggle-label') : null;
    const isActive = checkbox.checked;

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    if (badge) {
        badge.innerText = isActive ? 'Active' : 'Suspended';
        badge.className = isActive ? 'badge badge-active-green status-badge' : 'badge badge-suspended-gray status-badge';
    }

    if (toggleLabel) {
        toggleLabel.innerText = isActive ? 'ON' : 'OFF';
        toggleLabel.className = `me-2 fw-bold small toggle-label ${isActive ? 'text-light' : 'text-secondary'}`;
    }

    fetch(`/Admin/Users/ToggleActive/${userId}`, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token
        }
    })
        .then(response => {
            if (!response.ok) throw new Error('Network response error');
            return response.json();
        })
        .catch(err => {
            console.error('Error toggling status:', err);
            checkbox.checked = !isActive;
            if (badge) {
                badge.innerText = !isActive ? 'Active' : 'Suspended';
                badge.className = !isActive ? 'badge badge-active-green status-badge' : 'badge badge-suspended-gray status-badge';
            }
            if (toggleLabel) {
                toggleLabel.innerText = !isActive ? 'ON' : 'OFF';
                toggleLabel.className = `me-2 fw-bold small toggle-label ${!isActive ? 'text-light' : 'text-secondary'}`;
            }
        });
}

// -------------------------------------------------------------
// BACKEND GROUP ENDPOINTS
// -------------------------------------------------------------

// GET /Admin/Users/{userId}/Groups
function loadAssignedGroups() {
    if (!currentUserId) return;

    fetch(`/Admin/Users/${currentUserId}/Groups`)
        .then(response => response.json())
        .then(groups => {
            const listElem = document.getElementById('assigned-groups-list');
            if (!listElem) return;

            listElem.innerHTML = '';

            if (!groups || groups.length === 0) {
                listElem.innerHTML = '<li class="list-group-item bg-dark text-muted border-secondary">No groups assigned</li>';
                return;
            }

            groups.forEach(g => {
                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center bg-dark text-white border-secondary';
                li.innerHTML = `
                    <span>${g.appName ? g.appName + ' - ' : ''}${g.name || g.groupName}</span>
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeGroup('${g.groupId || g.id}')">Remove</button>
                `;
                listElem.appendChild(li);
            });
        })
        .catch(err => console.error("Error loading assigned groups:", err));
}

// GET /Admin/Users/{userId}/Groups/Available
function loadAvailableGroups() {
    if (!currentUserId) return;

    fetch(`/Admin/Users/${currentUserId}/Groups/Available`)
        .then(response => response.json())
        .then(groups => {
            const dropdown = document.getElementById('available-groups-dropdown');
            if (!dropdown) return;

            dropdown.innerHTML = '';

            if (!groups || groups.length === 0) {
                dropdown.innerHTML = '<option value="">No available groups</option>';
                return;
            }

            groups.forEach(g => {
                const option = document.createElement('option');
                option.value = g.groupId || g.id;
                option.textContent = g.appName ? `${g.appName} - ${g.name}` : (g.name || g.groupName);
                dropdown.appendChild(option);
            });
        })
        .catch(err => console.error("Error loading available groups:", err));
}

// DELETE /Admin/Users/{userId}/Groups/{groupId}
function removeGroup(groupId) {
    if (!currentUserId || !groupId) return;

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    fetch(`/Admin/Users/${currentUserId}/Groups/${groupId}`, {
        method: 'DELETE',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token
        }
    })
        .then(response => {
            if (response.ok) {
                loadAssignedGroups();
                loadAvailableGroups();
            } else {
                response.json().then(data => alert(data.message || 'Failed to remove group.')).catch(() => alert('Failed to remove group.'));
            }
        })
        .catch(err => console.error("Error removing group:", err));
}

// POST /Admin/Users/{userId}/Groups
function assignGroup() {
    const dropdown = document.getElementById('available-groups-dropdown');
    const groupId = dropdown ? dropdown.value : null;

    if (!currentUserId || !groupId) {
        alert('Please select a valid group.');
        return;
    }

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';

    fetch(`/Admin/Users/${currentUserId}/Groups`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ groupId: parseInt(groupId, 10) })
    })
        .then(response => {
            if (response.ok) {
                loadAssignedGroups();
                loadAvailableGroups();
            } else {
                response.json().then(data => alert(data.message || 'Failed to assign group.')).catch(() => alert('Failed to assign group.'));
            }
        })
        .catch(err => console.error("Error assigning group:", err));
}

// Password Generator & Reset Flow
function generateTempPassword() {
    const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    let password = "Tmp#";
    for (let i = 0; i < 8; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}

function copyTempPasswordToClipboard() {
    const tempPassText = document.getElementById('tempPasswordDisplay')?.innerText;
    if (tempPassText) {
        navigator.clipboard.writeText(tempPassText).then(() => {
            alert('Temporary password copied to clipboard!');
        }).catch(err => {
            console.error('Failed to copy: ', err);
        });
    }
}

async function triggerPasswordReset(userId) {
    try {
        const targetId = userId || currentUserId;
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        await fetch(`/Admin/Users/ResetPassword/${targetId}`, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            }
        }).catch(() => { });

        const newTempPassword = generateTempPassword();

        const tempDisplay = document.getElementById('tempPasswordDisplay');
        if (tempDisplay) tempDisplay.innerText = newTempPassword;

        const tempSection = document.getElementById('tempPasswordSection');
        if (tempSection) tempSection.classList.remove('d-none');

        let successModalEl = document.getElementById('resetSuccessModal');
        if (successModalEl) {
            let modalInstance = new bootstrap.Modal(successModalEl);
            modalInstance.show();
        }
    } catch (err) {
        console.error("Error triggering password reset:", err);
    }
}