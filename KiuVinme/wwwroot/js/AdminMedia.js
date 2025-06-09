    window.showStatus = function(message, type = 'info') {
    const statusArea = document.getElementById('statusArea');
    statusArea.className = `alert alert-${type}`;
    statusArea.innerHTML = message;
};

    window.populateFromFolder = async function() {
    try {
    showStatus('Scanning /media folder for videos...', 'warning');

    // Fixed the URL - removed the duplicate /media
    const response = await fetch('/media/videos');
    if (!response.ok) {
    throw new Error(`Failed to fetch videos from folder: ${response.status} ${response.statusText}`);
}

    const data = await response.json();
    const videos = data.videos || [];

    if (videos.length === 0) {
    showStatus('No videos found in /media folder', 'warning');
    return;
}

    showStatus(`Found ${videos.length} videos. Adding to database...`, 'info');

    let addedCount = 0;
    let skippedCount = 0;

    for (const video of videos) {
    try {
    const addResponse = await fetch('/media', {
    method: 'POST',
    headers: {
    'Content-Type': 'application/json'
},
    body: JSON.stringify({
    imageUrl: video.url,
    isUserCreated: false,
    isActive: true
})
});

    if (addResponse.ok) {
    addedCount++;
    console.log(`Added video: ${video.name}`);
} else {
    const errorText = await addResponse.text();
    if (errorText.includes('already exists')) {
    skippedCount++;
    console.log(`Skipped existing video: ${video.name}`);
} else {
    console.error(`Error adding ${video.name}:`, errorText);
}
}
} catch (err) {
    console.error(`Error adding ${video.name}:`, err);
}
}

    showStatus(`✅ Added ${addedCount} new ads, skipped ${skippedCount} existing ones`, 'success');
    setTimeout(() => loadAds(), 1000);

} catch (err) {
    console.error('Error in populateFromFolder:', err);
    showStatus(`❌ Error: ${err.message}`, 'danger');
}
};

    window.addVideoAd = async function(adData) {
    try {
    const response = await fetch('/media', {
    method: 'POST',
    headers: {
    'Content-Type': 'application/json'
},
    body: JSON.stringify(adData)
});

    if (response.ok) {
    showStatus('✅ Video ad added successfully!', 'success');
    document.getElementById('addAdForm').reset();
    setTimeout(() => loadAds(), 1000);
} else {
    const error = await response.text();
    showStatus(`❌ Error: ${error}`, 'danger');
}
} catch (err) {
    showStatus(`❌ Network error: ${err.message}`, 'danger');
}
};

    window.loadAds = async function() {
    try {
    showStatus('Loading video ads...', 'info');

    const response = await fetch('/media/GetAllAds');
    if (response.ok) {
    const ads = await response.json();
    displayAds(ads);
    showStatus(`📊 Loaded ${ads.length} video ads`, 'success');
} else {
    showStatus('❌ Failed to load ads', 'danger');
}
} catch (err) {
    showStatus(`❌ Error loading ads: ${err.message}`, 'danger');
}
};

    window.displayAds = function(ads) {
    const container = document.getElementById('adsContainer');

    if (ads.length === 0) {
    container.innerHTML = '<p class="text-muted">No video ads found</p>';
    return;
}

    let html = '';
    for (const ad of ads) {
    html += `
                <div class="ad-item">
                    <div class="row">
                        <div class="col-md-3">
                            <video class="video-preview" controls muted>
                                <source src="${ad.imageUrl}" type="video/mp4">
                                Video preview not available
                            </video>
                        </div>
                        <div class="col-md-7">
                            <h6>Video Ad #${ad.uid.substring(0, 8)}</h6>
                            <p><strong>URL:</strong> <code>${ad.imageUrl || 'No URL'}</code></p>
                            <p><strong>Impressions:</strong> ${ad.impressionCount}</p>
                            <p><strong>Status:</strong> 
                                <span class="badge ${ad.isActive ? 'bg-success' : 'bg-danger'}">
                                    ${ad.isActive ? 'Active' : 'Inactive'}
                                </span>
                                ${ad.isUserCreated ? '<span class="badge bg-info ms-1">User Created</span>' : ''}
                            </p>
                        </div>
                        <div class="col-md-2 text-end">
                            <button class="btn btn-sm btn-outline-primary mb-2 w-100" onclick="toggleAdStatus('${ad.uid}', ${ad.isActive})">
                                ${ad.isActive ? '⏸️ Deactivate' : '▶️ Activate'}
                            </button>
                            <button class="btn btn-sm btn-outline-danger w-100" onclick="deleteAd('${ad.uid}')">
                                🗑️ Delete
                            </button>
                        </div>
                    </div>
                </div>
            `;
}
    container.innerHTML = html;
};

    window.toggleAdStatus = async function(adId, currentStatus) {
    try {
    const response = await fetch(`/media/${adId}/toggle`, {
    method: 'PATCH'
});

    if (response.ok) {
    showStatus(`✅ Ad ${currentStatus ? 'deactivated' : 'activated'} successfully`, 'success');
    setTimeout(() => loadAds(), 500);
} else {
    showStatus('❌ Failed to toggle ad status', 'danger');
}
} catch (err) {
    showStatus(`❌ Error: ${err.message}`, 'danger');
}
};

    window.deleteAd = async function(adId) {
    if (!confirm('Are you sure you want to delete this video ad?')) {
    return;
}

    try {
    const response = await fetch(`/media/DeleteAd/${adId}`, {
    method: 'DELETE'
});

    if (response.ok) {
    showStatus('✅ Video ad deleted successfully', 'success');
    setTimeout(() => loadAds(), 500);
} else {
    showStatus('❌ Failed to delete ad', 'danger');
}
} catch (err) {
    showStatus(`❌ Error: ${err.message}`, 'danger');
}
};

    window.deleteAllAds = async function() {
    if (!confirm('Are you sure you want to delete ALL video ads? This cannot be undone!')) {
    return;
}

    try {
    showStatus('Deleting all video ads...', 'warning');

    const response = await fetch('/media', {
    method: 'DELETE'
});

    if (response.ok) {
    const result = await response.json();
    showStatus(`✅ ${result.message}`, 'success');
    setTimeout(() => loadAds(), 1000);
} else {
    const error = await response.text();
    showStatus(`❌ Error: ${error}`, 'danger');
}
} catch (err) {
    showStatus(`❌ Network error: ${err.message}`, 'danger');
}
};

    window.testRandomAd = async function() {
    try {
    const response = await fetch('/media/GetAllAds');
    if (response.ok) {
    const ads = await response.json();
    if (ads.length > 0) {
    const randomAd = ads[Math.floor(Math.random() * ads.length)];
    showStatus(`🎲 Random ad: ${randomAd.imageUrl} (Impressions: ${randomAd.impressionCount})`, 'info');
} else {
    showStatus('❌ No active ads available', 'warning');
}
} else {
    showStatus('❌ Failed to get ads', 'warning');
}
} catch (err) {
    showStatus(`❌ Error: ${err.message}`, 'danger');
}
};

    // Initialize page
    document.addEventListener('DOMContentLoaded', function() {
    console.log('Video ads admin page loaded');

    // Event listeners
    document.getElementById('populateFromFolderBtn').addEventListener('click', populateFromFolder);
    document.getElementById('refreshAdsBtn').addEventListener('click', loadAds);
    document.getElementById('testRandomAdBtn').addEventListener('click', testRandomAd);
    document.getElementById('deleteAllAdsBtn').addEventListener('click', deleteAllAds);

    // Form submission
    document.getElementById('addAdForm').addEventListener('submit', function(e) {
    e.preventDefault();

    const adData = {
    imageUrl: document.getElementById('videoUrl').value.trim(),
    isUserCreated: document.getElementById('isUserCreated').checked,
    isActive: document.getElementById('isActive').checked
};

    if (!adData.imageUrl) {
    showStatus('❌ Please enter a video URL', 'danger');
    return;
}

    addVideoAd(adData);
});

    // Load ads on page load
    loadAds();
});