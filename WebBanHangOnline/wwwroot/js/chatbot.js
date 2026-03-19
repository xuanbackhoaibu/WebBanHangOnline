(() => {
  const el = (id) => document.getElementById(id);

  const btn = el("xb-chat-btn");
  const box = el("xb-chat-box");
  const body = el("xb-chat-body");
  const input = el("xb-chat-msg");
  const sendBtn = el("xb-chat-send");
  const closeBtn = el("xb-chat-close");
  const clearBtn = el("xb-chat-clear");
  const statusEl = el("xb-chat-status");

  const STORAGE_KEY = "xb-chat-history-v1";

  const nowTime = () => {
    const d = new Date();
    return d.getHours().toString().padStart(2, "0") + ":" + d.getMinutes().toString().padStart(2, "0");
  };

  const scrollToBottom = () => {
    body.scrollTop = body.scrollHeight;
  };

  const setStatus = (text, tone = "ok") => {
    statusEl.textContent = text;
    statusEl.style.opacity = "0.95";
    statusEl.dataset.tone = tone;
  };

  const saveHistory = (items) => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
    } catch { /* ignore */ }
  };

  const loadHistory = () => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  };

  const history = loadHistory();

  const appendRow = (role, content) => {
    const row = document.createElement("div");
    row.className = `xb-row ${role}`;

    const bubble = document.createElement("div");
    bubble.className = "xb-bubble";

    const msg = document.createElement("div");
    msg.className = `xb-msg ${role}`;
    msg.appendChild(content);

    const time = document.createElement("div");
    time.className = "xb-time";
    time.textContent = nowTime();

    bubble.appendChild(msg);
    bubble.appendChild(time);
    row.appendChild(bubble);
    body.appendChild(row);
    scrollToBottom();

    return row;
  };

  const textNode = (text) => document.createTextNode(text ?? "");

  const renderBotPayload = (payload) => {
    // payload: { text?: string, products?: [{name, price, image, link}] } OR plain string
    const wrap = document.createElement("div");

    const text = (payload && typeof payload === "object") ? (payload.text ?? "") : String(payload ?? "");
    if (text) {
      const p = document.createElement("div");
      p.appendChild(textNode(text));
      wrap.appendChild(p);
    }

    const products = (payload && typeof payload === "object" && Array.isArray(payload.products)) ? payload.products : null;
    if (products && products.length) {
      const list = document.createElement("div");
      list.className = "xb-products";

      for (const p of products.slice(0, 5)) {
        const item = document.createElement("div");
        item.className = "xb-product";

        const img = document.createElement("img");
        img.alt = p?.name ? String(p.name) : "Sản phẩm";
        if (p?.image) img.src = String(p.image);

        const meta = document.createElement("div");

        const name = document.createElement("div");
        name.style.fontWeight = "700";
        name.appendChild(textNode(p?.name ? String(p.name) : "Sản phẩm"));

        const price = document.createElement("div");
        price.className = "xb-price";
        if (typeof p?.price === "number") price.appendChild(textNode(`${p.price.toLocaleString()}đ`));

        const link = document.createElement("a");
        link.href = p?.link ? String(p.link) : "#";
        link.appendChild(textNode("Xem sản phẩm"));

        meta.appendChild(name);
        meta.appendChild(price);
        meta.appendChild(link);

        item.appendChild(img);
        item.appendChild(meta);
        list.appendChild(item);
      }

      wrap.appendChild(list);
    }

    return wrap;
  };

  let typingRow = null;
  const showTyping = () => {
    if (typingRow) return;
    const typing = document.createElement("div");
    typing.className = "xb-typing";
    const dots = document.createElement("span");
    dots.className = "xb-dots";
    dots.innerHTML = "<i></i><i></i><i></i>";
    typing.appendChild(dots);
    const label = document.createElement("span");
    label.appendChild(textNode("Đang trả lời..."));
    label.style.color = "#6b7280";
    label.style.fontSize = "13px";
    typing.appendChild(label);
    typingRow = appendRow("bot", typing);
  };
  const hideTyping = () => {
    if (!typingRow) return;
    typingRow.remove();
    typingRow = null;
  };

  const pushHistory = (item) => {
    history.push(item);
    if (history.length > 60) history.splice(0, history.length - 60);
    saveHistory(history);
  };

  const replayHistory = () => {
    body.innerHTML = "";
    for (const h of history) {
      if (h?.role === "user") appendRow("user", textNode(String(h.text ?? "")));
      if (h?.role === "bot") appendRow("bot", renderBotPayload(h.payload ?? h.text ?? ""));
    }
  };

  replayHistory();

  const welcome = el("xb-chat-welcome")?.value || "Xin chào, mình có thể tư vấn size, phối đồ hoặc gợi ý sản phẩm theo ngân sách.";
  if (!history.length) {
    appendRow("bot", textNode(welcome));
    pushHistory({ role: "bot", text: welcome, at: Date.now() });
  }

  const toggle = () => {
    const open = box.style.display === "flex";
    box.style.display = open ? "none" : "flex";
    if (!open) {
      setTimeout(() => input?.focus(), 80);
      scrollToBottom();
    }
  };

  btn?.addEventListener("click", toggle);
  closeBtn?.addEventListener("click", () => (box.style.display = "none"));
  clearBtn?.addEventListener("click", () => {
    history.splice(0, history.length);
    saveHistory(history);
    replayHistory();
    appendRow("bot", textNode(welcome));
    pushHistory({ role: "bot", text: welcome, at: Date.now() });
  });

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

  connection.onreconnecting(() => setStatus("Mất kết nối, đang thử lại…", "warn"));
  connection.onreconnected(() => setStatus("Đã kết nối", "ok"));
  connection.onclose(() => setStatus("Offline", "err"));

  connection.on("ReceiveMessage", (_user, data) => {
    hideTyping();
    sendBtn.disabled = false;
    appendRow("bot", renderBotPayload(data));
    pushHistory({ role: "bot", payload: data, at: Date.now() });
  });

  connection.start()
    .then(() => setStatus("Đã kết nối", "ok"))
    .catch(() => setStatus("Offline", "err"));

  const send = async () => {
    const msg = (input.value || "").trim();
    if (!msg) return;

    appendRow("user", textNode(msg));
    pushHistory({ role: "user", text: msg, at: Date.now() });
    input.value = "";
    sendBtn.disabled = true;

    showTyping();
    try {
      await connection.invoke("SendMessage", "user", msg);
    } catch {
      hideTyping();
      sendBtn.disabled = false;
      appendRow("bot", textNode("Hiện mình không gửi được. Bạn thử lại giúp mình nhé."));
      pushHistory({ role: "bot", text: "Hiện mình không gửi được. Bạn thử lại giúp mình nhé.", at: Date.now() });
    }
  };

  sendBtn?.addEventListener("click", send);
  input?.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  });
})();
