document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".btn-plus").forEach(btn => {
        btn.addEventListener("click", function () {
            const row = btn.closest("tr");
            const input = row.querySelector(".quantity-input");
            input.value = parseInt(input.value) + 1;
            submitUpdate(input);
        });
    });

    document.querySelectorAll(".btn-minus").forEach(btn => {
        btn.addEventListener("click", function () {
            const row = btn.closest("tr");
            const input = row.querySelector(".quantity-input");
            if (parseInt(input.value) > 1) {
                input.value = parseInt(input.value) - 1;
                submitUpdate(input);
            }
        });
    });

    function submitUpdate(input) {
        const id = input.dataset.id;
        const quantity = input.value;

        fetch("/Cart/Update", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: `id=${id}&quantity=${quantity}`
        })
            .then(() => location.reload());
    }
});

// ===== FLASH SALE COUNTDOWN =====
(function () {
    const countdownEl = document.getElementById("countdown");
    if (!countdownEl) return;

    const endTime = new Date().getTime() + 3 * 60 * 60 * 1000;

    setInterval(() => {
        const d = endTime - new Date().getTime();
        if (d <= 0) {
            countdownEl.innerText = "Đã kết thúc";
            return;
        }

        const h = String(Math.floor(d / 36e5)).padStart(2, "0");
        const m = String(Math.floor(d % 36e5 / 6e4)).padStart(2, "0");
        const s = String(Math.floor(d % 6e4 / 1000)).padStart(2, "0");

        countdownEl.innerText = `${h}:${m}:${s}`;
    }, 1000);
})();

