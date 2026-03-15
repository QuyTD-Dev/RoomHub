document.addEventListener('DOMContentLoaded', () => {
    const favoriteButtons = document.querySelectorAll('.btn-toggle-favorite');

    favoriteButtons.forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.preventDefault();
            e.stopPropagation(); // Ngăn sự kiện click lan ra ngoài thẻ card

            const roomId = this.getAttribute('data-roomid');
            const icon = this.querySelector('.favorite-icon');

            // Lấy Token chống giả mạo từ Layout
            const tokenElement = document.querySelector('#antiforgery-form input[name="__RequestVerificationToken"]');
            const token = tokenElement ? tokenElement.value : '';

            try {
                // Gọi API POST
                const response = await fetch('/Favorites/ToggleFavorite', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(parseInt(roomId)) // Gửi ID dạng JSON thuần
                });

                const result = await response.json();

                if (result.success) {
                    // Cập nhật giao diện (Optimistic UI Update)
                    if (result.isFavorited) {
                        // Thả tim: Tô đỏ và chuyển Fill = 1
                        icon.classList.remove('text-slate-400');
                        icon.classList.add('text-red-500');
                        icon.style.fontVariationSettings = "'FILL' 1";
                    } else {
                        // Bỏ tim: Về màu xám và Fill = 0
                        icon.classList.remove('text-red-500');
                        icon.classList.add('text-slate-400');
                        icon.style.fontVariationSettings = "'FILL' 0";
                    }
                } else {
                    alert(result.message);
                }
            } catch (error) {
                console.error('Lỗi khi gọi API yêu thích:', error);
                alert("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }
        });
    });
});