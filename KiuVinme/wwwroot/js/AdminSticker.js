window.showStatus = function(message, type = 'info') {
    const statusArea = document.getElementById('statusArea');
    statusArea.className = `alert alert-${type}`;
    statusArea.textContent = message;
};

window.populateStickers = async function() {
    try {
        showStatus('Populating stickers from folders...', 'warning');

        const response = await fetch('/sticker/PopulateStickers', {
            method: 'POST'
        });

        if (response.ok) {
            const message = await response.text();
            showStatus(message, 'success');
            setTimeout(() => loadStickerPacks(), 1000);
        } else {
            const error = await response.text();
            showStatus(`Error: ${error}`, 'danger');
        }
    } catch (err) {
        showStatus(`Network error: ${err.message}`, 'danger');
    }
};

window.loadStickerPacks = async function() {
    try {
        showStatus('Loading sticker packs...', 'info');

        const response = await fetch('/sticker/GetStickerPacks');
        if (response.ok) {
            const packs = await response.json();
            displayStickerPacks(packs);
            showStatus(`Loaded ${packs.length} sticker packs`, 'success');
        } else {
            showStatus('Failed to load sticker packs', 'danger');
        }
    } catch (err) {
        showStatus(`Error loading packs: ${err.message}`, 'danger');
    }
};

window.displayStickerPacks = function(packs) {
    const container = document.getElementById('stickerPacksContainer');

    if (packs.length === 0) {
        container.innerHTML = '<p class="text-muted">No sticker packs found</p>';
        return;
    }

    let html = '';
    for (const pack of packs) {
        html += `
                    <div class="pack-preview">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h5>${pack.name}</h5>
                                <p><strong>Stickers:</strong> ${pack.stickerCount} | <strong>Active:</strong> ${pack.isActive}</p>
                            </div>
                            <button class="btn btn-sm btn-outline-danger" data-delete-pack-id="${pack.uid}" title="Delete this pack">
                                🗑️
                            </button>
                        </div>
                        <div id="pack-${pack.uid}" class="stickers-preview">
                            <button class="btn btn-sm btn-outline-primary" data-pack-id="${pack.uid}">
                                View Stickers
                            </button>
                        </div>
                    </div>
                `;
    }
    container.innerHTML = html;

    // Add event listeners for pack buttons
    container.querySelectorAll('[data-pack-id]').forEach(btn => {
        btn.addEventListener('click', function() {
            loadPackStickers(this.getAttribute('data-pack-id'));
        });
    });

    // Add event listeners for delete buttons
    container.querySelectorAll('[data-delete-pack-id]').forEach(btn => {
        btn.addEventListener('click', function() {
            const packId = this.getAttribute('data-delete-pack-id');
            const packName = this.closest('.pack-preview').querySelector('h5').textContent;
            deleteStickerPack(packId, packName);
        });
    });
};

window.loadPackStickers = async function(packId) {
    try {
        const response = await fetch(`/sticker/GetStickersFromPack?packId=${packId}`);
        if (response.ok) {
            const stickers = await response.json();
            displayPackStickers(packId, stickers);
        }
    } catch (err) {
        console.error('Error loading stickers:', err);
    }
};

window.displayPackStickers = function(packId, stickers) {
    const container = document.getElementById(`pack-${packId}`);
    let html = '<div class="mt-2">';

    stickers.forEach(sticker => {
        html += `
                    <img src="${sticker.imageUrl}" 
                         alt="${sticker.displayName}" 
                         title="${sticker.displayName}"
                         class="sticker-preview"
                         onerror="this.style.display='none'">
                `;
    });

    html += '</div>';
    container.innerHTML = html;
};

window.showStickerInfo = async function() {
    try {
        showStatus('Printing sticker info to console...', 'info');

        const response = await fetch('/sticker/StickerInfo');
        if (response.ok) {
            showStatus('Check the application console for detailed sticker information', 'success');
        } else {
            showStatus('Failed to print sticker info', 'danger');
        }
    } catch (err) {
        showStatus(`Error: ${err.message}`, 'danger');
    }
};

window.deleteStickerPack = async function(packId, packName) {
    if (!confirm(`Are you sure you want to delete the sticker pack "${packName}"? This will delete all stickers in this pack.`)) {
        return;
    }

    try {
        showStatus(`Deleting sticker pack "${packName}"...`, 'warning');

        const response = await fetch(`/sticker/DeleteStickerPack/${packId}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            const message = await response.text();
            showStatus(message, 'success');
            setTimeout(() => loadStickerPacks(), 1000);
        } else {
            const error = await response.text();
            showStatus(`Error: ${error}`, 'danger');
        }
    } catch (err) {
        showStatus(`Network error: ${err.message}`, 'danger');
    }
};

window.deleteAllStickerPacks = async function() {
    if (!confirm('Are you sure you want to delete ALL sticker packs? This cannot be undone!')) {
        return;
    }

    try {
        showStatus('Deleting all sticker packs...', 'warning');

        const response = await fetch('/sticker/DeleteAllStickerPacks', {
            method: 'DELETE'
        });

        if (response.ok) {
            const message = await response.text();
            showStatus(message, 'success');
            setTimeout(() => loadStickerPacks(), 1000);
        } else {
            const error = await response.text();
            showStatus(`Error: ${error}`, 'danger');
        }
    } catch (err) {
        showStatus(`Network error: ${err.message}`, 'danger');
    }
};

// Initialize page when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('Admin page loaded');

    // Add event listeners to buttons
    document.getElementById('populateBtn').addEventListener('click', populateStickers);
    document.getElementById('refreshBtn').addEventListener('click', loadStickerPacks);
    document.getElementById('infoBtn').addEventListener('click', showStickerInfo);
    document.getElementById('deleteAllBtn').addEventListener('click', deleteAllStickerPacks);

    // Load sticker packs on page load
    loadStickerPacks();
});