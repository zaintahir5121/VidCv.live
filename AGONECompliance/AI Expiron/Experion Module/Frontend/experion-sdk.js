/* Experion Standalone SDK
 * Circle gesture popup + suggestions + history + new chat.
 * Works with Experion Module backend.
 */
(function (window, document) {
    "use strict";

    if (window.ExperionSdk) {
        return;
    }

    const styleId = "experion-module-sdk-style";
    const state = {
        initialized: false,
        isOpen: false,
        authRequired: false,
        config: {
            apiBaseUrl: "/api/module/experion",
            productCode: "work",
            workspaceId: "default",
            sourceToken: "",
            userId: "",
            authTokenProvider: null,
            autoOpen: false
        },
        session: null,
        conversationId: null,
        triggerCooldownUntil: 0
    };

    const ui = {
        launcher: null,
        panel: null,
        status: null,
        sidebar: null,
        messages: null,
        suggestions: null,
        input: null,
        sendButton: null,
        newChatButton: null,
        authNotice: null,
        highlight: null
    };

    function ensureStyles() {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement("style");
        style.id = styleId;
        style.textContent = `
.experion-launcher{position:fixed;right:20px;bottom:20px;z-index:2147483000;border:0;border-radius:999px;width:74px;height:74px;cursor:pointer;background:transparent}
.experion-orb{display:flex;align-items:center;justify-content:center;width:74px;height:74px;border-radius:999px;background:radial-gradient(circle at 25% 20%,#6ce7ff,#4f7dff,#7a44ff);box-shadow:0 12px 36px rgba(79,125,255,.45)}
.experion-eye{width:9px;height:9px;border-radius:999px;background:#fff;margin:0 4px}
.experion-panel{position:fixed;right:20px;bottom:100px;width:760px;max-width:calc(100vw - 24px);height:560px;max-height:calc(100vh - 110px);display:none;flex-direction:column;z-index:2147483000;border-radius:24px;border:1px solid #dbe4fb;background:linear-gradient(180deg,#fff,#f8faff);overflow:hidden;box-shadow:0 24px 74px rgba(17,33,79,.22)}
.experion-header{display:flex;justify-content:space-between;align-items:flex-start;padding:12px 14px 8px}
.experion-brand{font-weight:800;color:#2c3f68}
.experion-close{border:0;background:#edf2ff;border-radius:10px;width:30px;height:30px;cursor:pointer}
.experion-status{font-size:11px;color:#7383a4;padding:0 14px 6px}
.experion-content{display:flex;min-height:0;flex:1 1 auto}
.experion-sidebar{width:240px;border-right:1px solid #e4eaf8;background:#f6f9ff;padding:10px}
.experion-new-chat{width:100%;text-align:left;padding:8px 10px;border-radius:10px;border:1px solid #cddaf8;background:#fff;color:#355084;font-size:12px;font-weight:700;cursor:pointer}
.experion-sidebar-label{font-size:11px;color:#7c8caf;margin:10px 4px 6px}
.experion-thread-list{overflow:auto;max-height:460px}
.experion-thread{width:100%;text-align:left;border:1px solid transparent;border-radius:10px;background:transparent;padding:8px;margin-bottom:4px;cursor:pointer}
.experion-thread:hover{background:#ebf2ff}
.experion-thread.active{border-color:#cad8ff;background:#eaf1ff}
.experion-thread-title{font-size:12px;color:#32476b;font-weight:700}
.experion-thread-time{font-size:10px;color:#7f8fb3}
.experion-main{flex:1 1 auto;display:flex;flex-direction:column;min-width:0;min-height:0}
.experion-hero{margin:4px 12px 10px;border:1px solid #e4ebfb;border-radius:14px;background:linear-gradient(135deg,#f8fbff,#eef3ff,#f4edff);padding:10px 12px;color:#4a5b7d;font-size:12px}
.experion-suggestions{display:flex;flex-wrap:wrap;gap:7px;padding:0 12px 10px}
.experion-chip{border:1px solid #d8e2fb;background:#f3f7ff;color:#4b5f8a;border-radius:999px;padding:7px 11px;font-size:12px;cursor:pointer}
.experion-messages{padding:4px 12px 10px;overflow:auto;flex:1 1 auto}
.experion-msg{font-size:12px;line-height:1.4;padding:9px 10px;border-radius:12px;margin-bottom:8px;white-space:pre-wrap}
.experion-msg.user{background:linear-gradient(135deg,#4f7fff,#7545ff);color:#fff}
.experion-msg.bot{background:#fff;border:1px solid #e1e8f8;color:#2f3b57}
.experion-auth{display:none;margin:0 12px 10px;padding:8px 10px;border:1px solid #f1c8c8;background:#fff4f4;color:#932a2a;border-radius:9px;font-size:12px}
.experion-input-wrap{display:flex;gap:8px;padding:8px 12px 12px;border-top:1px solid #e6ecfb}
.experion-input{flex:1;border:1px solid #d8e2f8;border-radius:12px;padding:10px 12px;font-size:13px}
.experion-send{border:0;border-radius:12px;padding:10px 12px;cursor:pointer;color:#fff;background:linear-gradient(135deg,#4a89ff,#6c3aed);font-weight:700}
.experion-highlight{position:fixed;z-index:2147482990;border:2px solid rgba(97,124,255,.9);border-radius:16px;box-shadow:0 0 0 9999px rgba(102,128,255,.1),0 0 0 4px rgba(115,176,255,.18);pointer-events:none}
`;
        document.head.appendChild(style);
    }

    function element(tag, className, text) {
        const el = document.createElement(tag);
        if (className) {
            el.className = className;
        }
        if (typeof text === "string") {
            el.textContent = text;
        }
        return el;
    }

    function setStatus(text) {
        if (ui.status) {
            ui.status.textContent = text;
        }
    }

    function normalizeProduct(productCode) {
        const normalized = String(productCode || "").trim().toLowerCase();
        return normalized || "work";
    }

    function clearMessages() {
        if (ui.messages) {
            ui.messages.innerHTML = "";
        }
    }

    function addMessage(role, content) {
        if (!ui.messages) {
            return;
        }
        const msg = element("div", "experion-msg " + (role === "user" ? "user" : "bot"), content || "");
        ui.messages.appendChild(msg);
        ui.messages.scrollTop = ui.messages.scrollHeight;
    }

    function formatDate(utcValue) {
        if (!utcValue) {
            return "";
        }
        const parsed = new Date(utcValue);
        if (isNaN(parsed.getTime())) {
            return "";
        }
        return parsed.toLocaleString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
    }

    function renderThreads(threads, activeId) {
        if (!ui.sidebar) {
            return;
        }
        ui.sidebar.innerHTML = "";
        const list = Array.isArray(threads) ? threads : [];
        if (list.length === 0) {
            ui.sidebar.appendChild(element("div", "experion-sidebar-label", "No previous chats"));
            return;
        }

        for (let i = 0; i < list.length; i++) {
            const thread = list[i];
            const button = element("button", "experion-thread");
            button.setAttribute("type", "button");
            if (String(thread.conversationId || "") === String(activeId || "")) {
                button.classList.add("active");
            }
            button.appendChild(element("div", "experion-thread-title", thread.title || "Conversation"));
            button.appendChild(element("div", "experion-thread-time", formatDate(thread.lastOccurredAtUtc)));
            button.addEventListener("click", function () {
                loadHistory(thread.conversationId);
            });
            ui.sidebar.appendChild(button);
        }
    }

    function renderSuggestions(suggestions) {
        if (!ui.suggestions) {
            return;
        }
        ui.suggestions.innerHTML = "";
        const items = Array.isArray(suggestions) ? suggestions.slice(0, 4) : [];
        if (items.length === 0) {
            ui.suggestions.style.display = "none";
            return;
        }
        ui.suggestions.style.display = "flex";
        for (let i = 0; i < items.length; i++) {
            const chip = element("button", "experion-chip", String(items[i]));
            chip.setAttribute("type", "button");
            chip.addEventListener("click", function () {
                ui.input.value = String(items[i]);
                sendMessage();
            });
            ui.suggestions.appendChild(chip);
        }
    }

    async function resolveAuthToken() {
        if (typeof state.config.authTokenProvider !== "function") {
            return null;
        }
        try {
            return await state.config.authTokenProvider();
        } catch {
            return null;
        }
    }

    async function apiCall(method, path, payload, query) {
        const token = await resolveAuthToken();
        const params = query
            ? "?" + Object.keys(query)
                .filter(function (key) { return query[key] !== undefined && query[key] !== null && String(query[key]).length > 0; })
                .map(function (key) { return encodeURIComponent(key) + "=" + encodeURIComponent(String(query[key])); })
                .join("&")
            : "";
        const url = state.config.apiBaseUrl.replace(/\/+$/, "") + path + params;
        const headers = {
            "X-Product-Code": normalizeProduct(state.config.productCode),
            "X-Workspace-Id": state.config.workspaceId || "default",
            "X-Source-Token": state.config.sourceToken || "",
            "X-User-Id": state.config.userId || ""
        };
        if (method !== "GET") {
            headers["Content-Type"] = "application/json";
        }
        if (token) {
            headers.Authorization = "Bearer " + token;
        }

        const response = await fetch(url, {
            method: method,
            headers: headers,
            body: method === "GET" ? undefined : JSON.stringify(payload || {})
        });

        if (!response.ok) {
            let message = "Experion request failed.";
            try {
                const parsed = await response.json();
                message = parsed.message || message;
            } catch {
                const text = await response.text();
                if (text) {
                    message = text;
                }
            }
            const error = new Error(message);
            error.status = response.status;
            throw error;
        }

        return await response.json();
    }

    function setAuthRequired(message) {
        state.authRequired = true;
        if (ui.authNotice) {
            ui.authNotice.style.display = "block";
            ui.authNotice.textContent = message || "Experion is unavailable without login/context.";
        }
        if (ui.input) {
            ui.input.disabled = true;
        }
        if (ui.sendButton) {
            ui.sendButton.disabled = true;
        }
        if (ui.newChatButton) {
            ui.newChatButton.disabled = true;
        }
        setStatus("Login required");
    }

    function clearAuthRequired() {
        state.authRequired = false;
        if (ui.authNotice) {
            ui.authNotice.style.display = "none";
            ui.authNotice.textContent = "";
        }
        if (ui.input) {
            ui.input.disabled = false;
        }
        if (ui.sendButton) {
            ui.sendButton.disabled = false;
        }
        if (ui.newChatButton) {
            ui.newChatButton.disabled = false;
        }
    }

    function randomGuid() {
        if (window.crypto && typeof window.crypto.randomUUID === "function") {
            return window.crypto.randomUUID();
        }
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (ch) {
            const rnd = Math.random() * 16 | 0;
            const val = ch === "x" ? rnd : (rnd & 0x3 | 0x8);
            return val.toString(16);
        });
    }

    async function loadHistory(conversationId) {
        if (state.authRequired) {
            return;
        }
        const history = await apiCall("GET", "/history", null, {
            conversationId: conversationId || "",
            conversationTake: 30,
            messageTake: 80
        });
        state.conversationId = history.activeConversationId || state.conversationId;
        renderThreads(history.threads || [], state.conversationId);
        clearMessages();
        const messages = Array.isArray(history.messages) ? history.messages : [];
        for (let i = 0; i < messages.length; i++) {
            addMessage(messages[i].role || "assistant", messages[i].content || "");
        }
        if (messages.length === 0) {
            addMessage("assistant", "I am ready to help. Start a new chat or select old chat.");
        }
    }

    async function bootstrap() {
        clearAuthRequired();
        const payload = {
            conversationId: state.conversationId,
            pageUrl: window.location.href,
            pageTitle: document.title || "",
            locale: navigator.language || "en",
            userTimezone: Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC"
        };
        const session = await apiCall("POST", "/session/bootstrap", payload);
        state.session = session;
        state.conversationId = session.conversationId || state.conversationId || randomGuid();
        setStatus("Connected");
        await loadHistory(state.conversationId);
        return session;
    }

    function getDomSnapshot(anchor) {
        const bodyText = (document.body && document.body.innerText ? document.body.innerText : "")
            .replace(/\s+/g, " ")
            .trim()
            .slice(0, 4000);
        const centerX = anchor ? Math.round(anchor.x + anchor.width / 2) : Math.round(window.innerWidth / 2);
        const centerY = anchor ? Math.round(anchor.y + anchor.height / 2) : Math.round(window.innerHeight / 2);
        const el = document.elementFromPoint(centerX, centerY);
        const elementText = el && el.textContent ? el.textContent.replace(/\s+/g, " ").trim().slice(0, 500) : "";
        const node = el ? el.outerHTML?.slice(0, 1000) || "" : "";
        return {
            sourceHtml: document.documentElement ? document.documentElement.outerHTML.slice(0, 12000) : "",
            sourceText: bodyText,
            focalElementText: elementText,
            focalElementHtml: node
        };
    }

    async function triggerContext(anchor, selectionText, triggerType) {
        if (!state.session || state.authRequired) {
            return;
        }
        const dom = getDomSnapshot(anchor);
        const payload = {
            sessionId: state.session.sessionId,
            conversationId: state.conversationId,
            triggerType: triggerType || "manual",
            selectionText: selectionText || "",
            pageUrl: window.location.href,
            domSnapshot: dom
        };
        const response = await apiCall("POST", "/context/resolve", payload);
        renderSuggestions(response.suggestedPrompts || []);
    }

    async function sendMessage() {
        if (state.authRequired) {
            return;
        }
        const text = String(ui.input && ui.input.value ? ui.input.value : "").trim();
        if (!text) {
            return;
        }
        ui.input.value = "";
        addMessage("user", text);

        try {
            if (!state.session) {
                await bootstrap();
            }
            const payload = {
                sessionId: state.session.sessionId,
                conversationId: state.conversationId,
                userPrompt: text,
                domSnapshot: getDomSnapshot(null)
            };
            const response = await apiCall("POST", "/conversation/message", payload);
            state.conversationId = response.conversationId || state.conversationId;
            addMessage("assistant", response.assistantMessage || "I can help with that.");
            await loadHistory(state.conversationId);
        } catch (error) {
            if (error && error.status === 401) {
                setAuthRequired(error.message);
                return;
            }
            addMessage("assistant", "Unable to process the request right now.");
            setStatus("Error");
        }
    }

    function showHighlight(rect) {
        if (!rect) {
            return;
        }
        if (!ui.highlight) {
            ui.highlight = element("div", "experion-highlight");
            document.body.appendChild(ui.highlight);
        }
        const pad = 8;
        ui.highlight.style.display = "block";
        ui.highlight.style.left = Math.max(0, rect.x - pad) + "px";
        ui.highlight.style.top = Math.max(0, rect.y - pad) + "px";
        ui.highlight.style.width = Math.max(24, rect.width + pad * 2) + "px";
        ui.highlight.style.height = Math.max(24, rect.height + pad * 2) + "px";
    }

    function hideHighlight() {
        if (ui.highlight) {
            ui.highlight.style.display = "none";
        }
    }

    function positionPanel(rect) {
        if (!ui.panel) {
            return;
        }
        if (!rect) {
            ui.panel.style.right = "20px";
            ui.panel.style.bottom = "100px";
            ui.panel.style.left = "auto";
            ui.panel.style.top = "auto";
            return;
        }
        const margin = 12;
        const gap = 14;
        const width = ui.panel.offsetWidth || 760;
        const height = ui.panel.offsetHeight || 560;
        const vw = window.innerWidth || document.documentElement.clientWidth;
        const vh = window.innerHeight || document.documentElement.clientHeight;
        let left = rect.x + rect.width + gap;
        if (left + width > vw - margin) {
            left = rect.x - width - gap;
        }
        if (left < margin) {
            left = margin;
        }
        let top = rect.y;
        if (top + height > vh - margin) {
            top = vh - height - margin;
        }
        if (top < margin) {
            top = margin;
        }
        ui.panel.style.left = Math.round(left) + "px";
        ui.panel.style.top = Math.round(top) + "px";
        ui.panel.style.right = "auto";
        ui.panel.style.bottom = "auto";
    }

    function openPanel(rect, selectionText, triggerType) {
        ui.panel.style.display = "flex";
        state.isOpen = true;
        positionPanel(rect || null);
        if (rect) {
            showHighlight(rect);
        } else {
            hideHighlight();
        }
        setStatus("Connecting...");
        bootstrap()
            .then(function () {
                return triggerContext(rect || null, selectionText || "", triggerType || "manual");
            })
            .then(function () {
                setStatus(state.authRequired ? "Login required" : "Connected");
            })
            .catch(function (error) {
                if (error && error.status === 401) {
                    setAuthRequired(error.message);
                } else {
                    setStatus("Connection failed");
                }
            });
    }

    function closePanel() {
        ui.panel.style.display = "none";
        state.isOpen = false;
        hideHighlight();
    }

    function togglePanel() {
        if (ui.panel.style.display === "none") {
            openPanel(null, "", "manual");
            return;
        }
        closePanel();
    }

    function startNewChat() {
        if (state.authRequired) {
            return;
        }
        state.conversationId = randomGuid();
        clearMessages();
        addMessage("assistant", "New conversation started.");
        renderThreads([], state.conversationId);
    }

    function isCircle(points) {
        if (!points || points.length < 12) {
            return false;
        }
        const first = points[0];
        const last = points[points.length - 1];
        const closeDistance = Math.hypot(last.x - first.x, last.y - first.y);
        if (closeDistance > 32) {
            return false;
        }
        let minX = Number.POSITIVE_INFINITY;
        let minY = Number.POSITIVE_INFINITY;
        let maxX = Number.NEGATIVE_INFINITY;
        let maxY = Number.NEGATIVE_INFINITY;
        for (let i = 0; i < points.length; i++) {
            const p = points[i];
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }
        const width = maxX - minX;
        const height = maxY - minY;
        if (width < 40 || height < 40) {
            return false;
        }
        const ratio = width / height;
        return ratio > 0.45 && ratio < 2.2;
    }

    function box(points) {
        let minX = Number.POSITIVE_INFINITY;
        let minY = Number.POSITIVE_INFINITY;
        let maxX = Number.NEGATIVE_INFINITY;
        let maxY = Number.NEGATIVE_INFINITY;
        for (let i = 0; i < points.length; i++) {
            const p = points[i];
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }
        return {
            x: Math.round(minX),
            y: Math.round(minY),
            width: Math.round(maxX - minX),
            height: Math.round(maxY - minY)
        };
    }

    function attachCircleTrigger() {
        let drawing = false;
        let points = [];

        document.addEventListener("pointerdown", function (event) {
            if (event.button !== 0) {
                return;
            }
            if (ui.panel.contains(event.target) || ui.launcher.contains(event.target)) {
                return;
            }
            drawing = true;
            points = [{ x: event.clientX, y: event.clientY }];
        }, true);

        document.addEventListener("pointermove", function (event) {
            if (!drawing) {
                return;
            }
            points.push({ x: event.clientX, y: event.clientY });
        }, true);

        document.addEventListener("pointerup", function (event) {
            if (!drawing) {
                return;
            }
            drawing = false;
            points.push({ x: event.clientX, y: event.clientY });
            if (Date.now() < state.triggerCooldownUntil) {
                return;
            }
            if (!isCircle(points)) {
                return;
            }
            state.triggerCooldownUntil = Date.now() + 5000;
            const rect = box(points);
            const selectedText = window.getSelection ? String(window.getSelection()).trim() : "";
            openPanel(rect, selectedText, "circle");
        }, true);
    }

    function buildUi() {
        ensureStyles();
        ui.launcher = element("button", "experion-launcher");
        ui.launcher.setAttribute("type", "button");
        ui.launcher.setAttribute("aria-label", "Open Experion");
        ui.launcher.addEventListener("click", togglePanel);
        const orb = element("div", "experion-orb");
        orb.appendChild(element("span", "experion-eye"));
        orb.appendChild(element("span", "experion-eye"));
        ui.launcher.appendChild(orb);

        ui.panel = element("section", "experion-panel");
        const header = element("div", "experion-header");
        header.appendChild(element("div", "experion-brand", "Experion"));
        const close = element("button", "experion-close", "×");
        close.setAttribute("type", "button");
        close.addEventListener("click", closePanel);
        header.appendChild(close);
        ui.panel.appendChild(header);

        ui.status = element("div", "experion-status", "Ready");
        ui.panel.appendChild(ui.status);

        const content = element("div", "experion-content");
        const sidebarWrap = element("div", "experion-sidebar");
        ui.newChatButton = element("button", "experion-new-chat", "+ New chat");
        ui.newChatButton.setAttribute("type", "button");
        ui.newChatButton.addEventListener("click", startNewChat);
        sidebarWrap.appendChild(ui.newChatButton);
        sidebarWrap.appendChild(element("div", "experion-sidebar-label", "Old chats"));
        ui.sidebar = element("div", "experion-thread-list");
        sidebarWrap.appendChild(ui.sidebar);

        const main = element("div", "experion-main");
        main.appendChild(element("div", "experion-hero", "Circle any section to open contextual assistance."));
        ui.suggestions = element("div", "experion-suggestions");
        main.appendChild(ui.suggestions);
        ui.messages = element("div", "experion-messages");
        main.appendChild(ui.messages);
        ui.authNotice = element("div", "experion-auth");
        main.appendChild(ui.authNotice);
        const inputWrap = element("div", "experion-input-wrap");
        ui.input = element("input", "experion-input");
        ui.input.setAttribute("placeholder", "Tell Experion what to do...");
        ui.input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                sendMessage();
            }
        });
        ui.sendButton = element("button", "experion-send", "Send");
        ui.sendButton.setAttribute("type", "button");
        ui.sendButton.addEventListener("click", sendMessage);
        inputWrap.appendChild(ui.input);
        inputWrap.appendChild(ui.sendButton);
        main.appendChild(inputWrap);

        content.appendChild(sidebarWrap);
        content.appendChild(main);
        ui.panel.appendChild(content);

        document.body.appendChild(ui.panel);
        document.body.appendChild(ui.launcher);
    }

    async function init(config) {
        if (state.initialized) {
            return;
        }
        state.config = Object.assign({}, state.config, config || {});
        buildUi();
        attachCircleTrigger();
        state.initialized = true;
        if (state.config.autoOpen) {
            openPanel(null, "", "auto");
        }
    }

    window.ExperionSdk = {
        init: init,
        open: function () { openPanel(null, "", "manual"); },
        close: closePanel,
        toggle: togglePanel,
        newChat: startNewChat,
        send: function (text) {
            if (!ui.input) {
                return;
            }
            ui.input.value = text || "";
            sendMessage();
        },
        resetSession: function () {
            state.session = null;
            state.conversationId = null;
            state.authRequired = false;
            clearAuthRequired();
            clearMessages();
            renderThreads([], null);
            setStatus("Ready");
        }
    };
})(window, document);
