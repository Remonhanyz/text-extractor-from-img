let historyData = [];
let activeItem = null;

async function fetchHistory() {
  try {
    const res = await fetch('/api/history');
    historyData = await res.json();
    renderList();
  } catch (e) {
    console.error('Failed to load history:', e);
  }
}

function renderList() {
  const q = document.getElementById('searchInput').value.toLowerCase().trim();
  const listPane = document.getElementById('listPane');
  listPane.innerHTML = '';

  const filtered = historyData.filter(item => !q || (item.Text && item.Text.toLowerCase().includes(q)));
  filtered.forEach((item, idx) => {
    const div = document.createElement('div');
    div.className = 'history-item' + (activeItem && activeItem.Id === item.Id ? ' active' : '');
    div.onclick = () => selectItem(item);
    div.ondblclick = () => { selectItem(item); copyDetail(); };

    const timeDiv = document.createElement('div');
    timeDiv.className = 'history-time';
    timeDiv.textContent = item.Timestamp;

    const previewDiv = document.createElement('div');
    previewDiv.className = 'history-preview';
    previewDiv.textContent = item.Text ? item.Text.replace(/\n/g, ' ') : '';

    div.appendChild(timeDiv);
    div.appendChild(previewDiv);
    listPane.appendChild(div);

    if (idx === 0 && (!activeItem || !historyData.find(x => x.Id === activeItem.Id))) {
      selectItem(item);
    }
  });

  if (filtered.length === 0) {
    listPane.innerHTML = '<div style="padding: 20px; color: #64748b; text-align: center;">No snippets found</div>';
    document.getElementById('detailText').value = '';
    document.getElementById('detailTimestamp').textContent = 'No selection';
  }
}

function selectItem(item) {
  activeItem = item;
  document.querySelectorAll('.history-item').forEach(el => el.classList.remove('active'));
  document.getElementById('detailText').value = item.Text || '';
  document.getElementById('detailTimestamp').textContent = 'Captured: ' + item.Timestamp + ' | ' + (item.Text ? item.Text.length : 0) + ' characters';
}

function copyDetail() {
  const text = document.getElementById('detailText').value;
  if (!text) return;
  navigator.clipboard.writeText(text).then(() => showToast('Copied to clipboard!'));
}

async function deleteDetail() {
  if (!activeItem) return;
  await fetch('/api/history/delete?id=' + encodeURIComponent(activeItem.Id), { method: 'POST' });
  activeItem = null;
  fetchHistory();
}

async function clearHistory() {
  if (!confirm('Are you sure you want to clear all history?')) return;
  await fetch('/api/history/clear', { method: 'POST' });
  activeItem = null;
  fetchHistory();
}

function showToast(msg) {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.classList.add('show');
  setTimeout(() => t.classList.remove('show'), 2000);
}

document.getElementById('searchInput').addEventListener('input', renderList);
fetchHistory();
