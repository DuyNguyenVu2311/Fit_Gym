document.querySelector('[data-password-toggle]')?.addEventListener('click', function () {
    const input = document.getElementById('Password');
    const visible = input.type === 'password';
    input.type = visible ? 'text' : 'password';
    this.textContent = visible ? 'Ẩn' : 'Hiện';
    this.setAttribute('aria-pressed', String(visible));
});
