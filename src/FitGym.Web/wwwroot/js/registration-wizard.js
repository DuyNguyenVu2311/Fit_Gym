(function () {
    'use strict';
    const form = document.querySelector('[data-registration-wizard]');
    if (!form) return;
    // Steps 1–4 collect input. Step 5 is rendered only from the saved server result.
    const rules = window.FitGymRegistrationRules;
    const member = form.querySelector('#MaHoiVien');
    const packageInput = form.querySelector('#MaGoiTap');
    const dateInput = form.querySelector('#NgayBatDau');
    const consent = form.querySelector('#DongYSinhTracHoc');
    const method = form.querySelector('#PhuongThucThanhToan');
    const cash = form.querySelector('#CashReceived');
    const quoteToken = form.querySelector('#QuoteToken');
    const quoteState = form.querySelector('[data-quote-state]');
    const next = form.querySelector('[data-wizard-next]');
    const previous = form.querySelector('[data-wizard-previous]');
    const submit = form.querySelector('[data-wizard-submit]');
    const error = form.querySelector('[data-wizard-error]');
    let step = 1;
    let quote = null;
    let quoteSequence = 0;
    let loading = false;
    let submitting = false;
    const money = { format: amount => `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(amount)} VND` };
    const formatDate = value => value.split('-').reverse().join('/');
    const write = (selector, value) => { form.querySelector(selector).textContent = value; };
    const state = () => ({ memberId: member.value, packageId: packageInput.value, startDate: dateInput.value, quote, consent: consent.checked, method: method.value, cashReceived: cash.checked });
    function showError(message) { error.textContent = message; error.hidden = false; }
    function hideError() { error.hidden = true; error.textContent = ''; }
    function render() {
        form.querySelectorAll('[data-wizard-panel]').forEach(panel => { panel.hidden = Number(panel.dataset.wizardPanel) !== step; });
        document.querySelectorAll('[data-step-indicator]').forEach(indicator => {
            const number = Number(indicator.dataset.stepIndicator);
            indicator.classList.toggle('current', number === step);
            indicator.classList.toggle('completed', number < step);
            if (number === step) indicator.setAttribute('aria-current', 'step'); else indicator.removeAttribute('aria-current');
        });
        previous.hidden = step === 1;
        next.hidden = step === 4;
        submit.hidden = step !== 4;
        next.disabled = loading;
        next.textContent = 'Tiếp tục';
        form.querySelector('[data-wizard-counter]').textContent = `Bước ${step} / 5`;
        form.querySelector('[data-cash-confirmation]').hidden = method.value !== 'TienMat';
        form.querySelector('[data-qr-information]').hidden = method.value !== 'QR';
        submit.textContent = method.value === 'QR' ? 'Tạo yêu cầu QR' : 'Xác nhận đã thu tiền & hoàn tất';
        write('[data-review-member]', member.selectedOptions[0]?.textContent || '—');
        write('[data-review-package]', quote?.packageName || '—');
        write('[data-review-dates]', quote ? `${formatDate(quote.startDate)} → ${formatDate(quote.endDate)}` : '—');
        write('[data-review-total]', quote ? money.format(quote.total) : '—');
    }
    async function loadQuote() {
        const sequence = ++quoteSequence;
        quote = null;
        quoteToken.value = '';
        cash.checked = false;
        hideError();
        write('[data-quote-name]', 'Chưa xác nhận'); write('[data-quote-end]', '—'); write('[data-quote-total]', '—');
        if (!packageInput.value || !dateInput.value) { loading = false; quoteState.hidden = true; render(); return; }
        loading = true; render();
        quoteState.hidden = false;
        write('[data-quote-state]', 'Đang tính thời hạn và số tiền…');
        try {
            const url = new URL(form.dataset.quoteUrl, window.location.origin);
            url.searchParams.set('memberId', member.value); url.searchParams.set('packageId', packageInput.value); url.searchParams.set('startDate', dateInput.value);
            const response = await fetch(url, { headers: { Accept: 'application/json' }, cache: 'no-store' });
            if (sequence !== quoteSequence) return;
            if (response.redirected || response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng tải lại trang.');
            const data = await response.json();
            if (sequence !== quoteSequence) return;
            if (!response.ok) throw new Error(data.message || 'Không thể tính gói tập. Vui lòng chọn lại.');
            quote = data;
            quoteToken.value = data.quoteToken;
            if (!rules.canProceed(2, state())) { quote = null; throw new Error('Thông tin gói đã thay đổi. Vui lòng chọn lại.'); }
            write('[data-quote-name]', data.packageName); write('[data-quote-end]', formatDate(data.endDate)); write('[data-quote-total]', money.format(data.total));
            quoteState.hidden = true; hideError();
        } catch (exception) {
            if (sequence === quoteSequence) { quote = null; quoteToken.value = ''; quoteState.hidden = true; showError(exception.message || 'Không thể kết nối. Vui lòng thử lại.'); }
        } finally { if (sequence === quoteSequence) { loading = false; render(); } }
    }
    function validateStep() {
        const current = state();
        if (step === 1 && !rules.canProceed(1, current)) { member.reportValidity(); showError('Vui lòng chọn hội viên.'); return false; }
        if (step === 2) {
            if (!dateInput.reportValidity() || !packageInput.reportValidity()) return false;
            if (loading || !rules.canProceed(2, current)) { showError('Cần có thời hạn và số tiền hợp lệ trước khi tiếp tục.'); return false; }
        }
        if (step === 3 && !rules.canProceed(3, current)) { showError('Cần ghi nhận sự đồng ý của hội viên trước khi tiếp tục quy trình này.'); return false; }
        return true;
    }
    next.addEventListener('click', () => { hideError(); if (validateStep()) { step++; render(); form.querySelector(`[data-wizard-panel="${step}"] h2`).scrollIntoView({ block: 'nearest' }); } });
    previous.addEventListener('click', () => { if (!submitting) { hideError(); step = Math.max(1, step - 1); render(); } });
    member.addEventListener('change', () => { cash.checked = false; consent.checked = false; hideError(); loadQuote(); });
    packageInput.addEventListener('change', loadQuote); dateInput.addEventListener('change', loadQuote);
    method.addEventListener('change', () => { cash.checked = false; hideError(); render(); });
    form.addEventListener('submit', event => {
        if (submitting) { event.preventDefault(); return; }
        hideError();
        if (step !== 4 || loading || !rules.canSubmit(state())) {
            event.preventDefault();
            if (!rules.canProceed(1, state())) step = 1;
            else if (!rules.canProceed(2, state())) step = 2;
            else if (!rules.canProceed(3, state())) step = 3;
            render(); showError(step === 4 ? 'Vui lòng xác nhận đã nhận đủ tiền mặt.' : 'Vui lòng hoàn tất thông tin trước khi ghi nhận giao dịch.');
            return;
        }
        submitting = true; submit.disabled = true; previous.disabled = true; submit.textContent = 'Đang ghi nhận…';
    });
    render();
    if (packageInput.value && dateInput.value) loadQuote();
})();
