// ═══════════════════════════════════════════════════
// RoomHub Admin — Premium Interactions
// ═══════════════════════════════════════════════════

// Sidebar Toggle
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    sidebar.classList.toggle('-translate-x-full');
    overlay.classList.toggle('hidden');
    document.body.classList.toggle('overflow-hidden');
}

// Auto-dismiss toast with smooth slide-out
document.addEventListener('DOMContentLoaded', () => {
    // Toast auto-dismiss
    document.querySelectorAll('.toast-notification').forEach(toast => {
        // Progress bar animation
        const bar = toast.querySelector('.toast-progress');
        if (bar) {
            bar.style.transition = 'width 4s linear';
            requestAnimationFrame(() => bar.style.width = '0%');
        }
        setTimeout(() => {
            toast.style.transition = 'all 0.5s cubic-bezier(0.4, 0, 0.2, 1)';
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-12px) scale(0.98)';
            setTimeout(() => toast.remove(), 500);
        }, 4500);
    });

    // Animated counter for stat values
    document.querySelectorAll('[data-count]').forEach(el => {
        const target = parseFloat(el.dataset.count);
        const suffix = el.dataset.suffix || '';
        const prefix = el.dataset.prefix || '';
        const isDecimal = el.dataset.decimal === 'true';
        const duration = 1200;
        const start = performance.now();

        function animate(now) {
            const progress = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 4); // ease-out-quart
            const current = eased * target;
            el.textContent = prefix + (isDecimal ? current.toFixed(1) : Math.floor(current).toLocaleString('vi-VN')) + suffix;
            if (progress < 1) requestAnimationFrame(animate);
        }
        requestAnimationFrame(animate);
    });

    // Stagger table rows animation
    document.querySelectorAll('.admin-table tbody tr').forEach((row, i) => {
        row.style.opacity = '0';
        row.style.transform = 'translateY(8px)';
        row.style.transition = `all 0.4s cubic-bezier(0.16, 1, 0.3, 1) ${i * 0.04}s`;
        requestAnimationFrame(() => {
            setTimeout(() => {
                row.style.opacity = '1';
                row.style.transform = 'translateY(0)';
            }, 50);
        });
    });
});

// Confirm action with styled dialog
function confirmAction(message, formId) {
    if (confirm(message)) {
        document.getElementById(formId).submit();
    }
    return false;
}
