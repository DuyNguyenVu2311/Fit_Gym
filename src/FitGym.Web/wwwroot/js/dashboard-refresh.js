(function () {
    'use strict';
    const host = document.querySelector('[data-dashboard-host]');
    if (!host) return;
    const status = document.querySelector('[data-dashboard-status]');
    const initial = host.querySelector('[data-dashboard-snapshot]');
    if (!initial) return;
    const selectedDate = initial.dataset.date;
    const live = initial.dataset.liveDay === 'true';
    let busy = false, stopped = false, controller;
    function vietnamDate() {
        const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
        const value = type => parts.find(part => part.type === type).value;
        return `${value('year')}-${value('month')}-${value('day')}`;
    }
    function message(text, failed) {
        status.textContent = text;
        status.classList.toggle('d-none', !failed);
        status.classList.toggle('text-danger', failed);
        status.classList.toggle('text-secondary', !failed);
    }
    async function refresh() {
        if (stopped || busy || document.hidden || !host.isConnected) return;
        busy = true;
        controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 10000);
        const date = live ? vietnamDate() : selectedDate;
        try {
            const url = new URL(host.dataset.url, window.location.origin);
            url.searchParams.set('date', date);
            const response = await fetch(url, { headers: { Accept: 'text/html' }, cache: 'no-store', signal: controller.signal });
            if (response.redirected || response.status === 401 || response.status === 403) {
                stopped = true; clearInterval(timer);
                message('Phiên đăng nhập hoặc quyền truy cập đã thay đổi. Vui lòng tải lại trang để đăng nhập.', true);
                return;
            }
            if (!response.ok) throw new Error('refresh-failed');
            const markup = await response.text();
            const snapshot = new DOMParser().parseFromString(markup, 'text/html').querySelector('[data-dashboard-snapshot]');
            if (!snapshot || snapshot.dataset.date !== date) throw new Error('invalid-snapshot');
            if (!stopped && host.isConnected) {
                host.replaceChildren(snapshot);
                message('', false);
            }
        } catch (_) {
            if (!stopped) message('Không thể cập nhật. Vui lòng thử lại.', true);
        } finally { clearTimeout(timeout); busy = false; }
    }
    let timer = setInterval(refresh, 60000);
    document.querySelector('[data-dashboard-refresh]')?.addEventListener('click', refresh);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.addEventListener('pagehide', () => { stopped = true; clearInterval(timer); controller?.abort(); });
    window.addEventListener('pageshow', event => {
        if (!event.persisted) return;
        stopped = false;
        clearInterval(timer);
        timer = setInterval(refresh, 60000);
        refresh();
    });
})();
