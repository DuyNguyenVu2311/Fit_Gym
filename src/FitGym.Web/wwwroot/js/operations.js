document.querySelectorAll('[data-sidebar-toggle]').forEach(button => {
    button.addEventListener('click', () => {
        const open = document.body.classList.toggle('sidebar-open');
        document.querySelectorAll('[data-sidebar-toggle]').forEach(control => control.setAttribute('aria-expanded', String(open)));
    });
});
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
        document.body.classList.remove('sidebar-open');
        document.querySelectorAll('[data-sidebar-toggle]').forEach(control => control.setAttribute('aria-expanded', 'false'));
    }
});
