(function () {
    const form = document.querySelector('[data-renewal-form]');
    if (!form) return;
    const packageInput = form.querySelector('#MaGoiTap');
    const method = form.querySelector('#PhuongThucThanhToan');
    const cash = form.querySelector('#CashReceived');
    const token = form.querySelector('#QuoteToken');
    const submit = form.querySelector('[data-renewal-submit]');
    const message = form.querySelector('[data-renewal-state]');
    let sequence = 0;
    let submitting = false;
    const formatDate = date => date.split('-').reverse().join('/');
    const money = { format: amount => `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(amount)} VND` };
    function updateButton() { submit.disabled = submitting || !token.value || (method.value === 'TienMat' && !cash.checked); }
    async function refresh() {
        const request = ++sequence;
        cash.checked = false; token.value = ''; updateButton();
        ['start', 'end', 'total'].forEach(field => { form.querySelector(`[data-renewal-${field}]`).textContent = '—'; });
        message.hidden = false;
        message.textContent = 'Đang tính thời hạn và giá gia hạn…';
        if (!packageInput.value) { message.textContent = 'Chưa có gói tập đang cung cấp.'; return; }
        try {
            const url = new URL(form.dataset.quoteUrl, location.origin);
            url.searchParams.set('registrationId', form.querySelector('#MaDangKyGoc').value); url.searchParams.set('packageId', packageInput.value);
            const response = await fetch(url, { headers: { Accept: 'application/json' }, cache: 'no-store' });
            if (response.redirected || response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng tải lại trang.');
            const data = await response.json();
            if (request !== sequence) return;
            if (!response.ok) throw new Error(data.message || 'Không thể tính thời hạn gia hạn.');
            token.value = data.quoteToken;
            form.querySelector('[data-renewal-start]').textContent = formatDate(data.startDate);
            form.querySelector('[data-renewal-end]').textContent = formatDate(data.endDate);
            form.querySelector('[data-renewal-total]').textContent = money.format(data.total);
            message.hidden = true;
        } catch (error) { if (request === sequence) { token.value = ''; message.textContent = error.message || 'Không thể kết nối.'; } }
        finally { if (request === sequence) updateButton(); }
    }
    packageInput.addEventListener('change', refresh);
    method.addEventListener('change', () => { cash.checked = false; updateButton(); });
    cash.addEventListener('change', updateButton);
    form.addEventListener('submit', event => {
        if (submitting || !token.value || (method.value === 'TienMat' && !cash.checked)) { event.preventDefault(); return; }
        submitting = true; updateButton(); submit.textContent = 'Đang ghi nhận…';
    });
    refresh();
})();
