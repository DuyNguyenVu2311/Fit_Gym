(function (root) {
    'use strict';
    function canProceed(step, state) {
        if (step === 1) return Number(state.memberId) > 0;
        if (step === 2) return Number(state.packageId) > 0 && !!state.quote &&
            Number(state.quote.memberId) === Number(state.memberId) &&
            Number(state.quote.packageId) === Number(state.packageId) &&
            state.quote.startDate === state.startDate;
        if (step === 3) return state.consent === true;
        if (step === 4) return state.method === 'QR' || (state.method === 'TienMat' && state.cashReceived === true);
        return false;
    }
    function canSubmit(state) { return [1, 2, 3, 4].every(step => canProceed(step, state)); }
    const rules = { canProceed, canSubmit };
    if (typeof module !== 'undefined' && module.exports) module.exports = rules;
    else root.FitGymRegistrationRules = rules;
})(typeof globalThis !== 'undefined' ? globalThis : this);
