/* AG ONE Experion SDK
 * Framework-agnostic assistant launcher with contextual circle trigger.
 * Includes per-user conversation sidebar (new chat + old chats).
 */
(function (window, document) {
    "use strict";

    if (window.AGONEExperion) {
        return;
    }

    const styleId = "agone-experion-style";
    const state = {
        initialized: false,
        isOpen: false,
        authRequired: false,
        config: {
            apiBaseUrl: "/api/experion",
            sourceToken: "",
            productCode: "unknown",
            workspaceId: "",
            userId: "",
            authTokenProvider: null,
            autoOpen: false
        },
        session: null,
        currentConversationId: null,
        triggerCooldownUntil: 0,
        currentAnchor: null,
        lastTriggerResponse: null,
        history: null
    };

    const ui = {
        launcher: null,
        panel: null,
        headerTitle: null,
        headerSubtitle: null,
        closeButton: null,
        status: null,
        highlight: null,
        sidebarList: null,
        newChatButton: null,
        messages: null,
        suggestions: null,
        input: null,
        sendButton: null,
        authNotice: null
    };

    function ensureStyles() {
        if (document.getElementById(styleId)) {
            return;
        }

        const style = document.createElement("style");
        style.id = styleId;
        style.textContent = `
.agone-experion-launcher {
  position: fixed;
  right: 20px;
  bottom: 20px;
  z-index: 2147483000;
  border: 0;
  border-radius: 999px;
  width: 76px;
  height: 76px;
  padding: 0;
  cursor: pointer;
  background: transparent;
}
.agone-experion-launcher:focus-visible {
  outline: 2px solid #ffffff;
  outline-offset: 3px;
}
.agone-experion-orb {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 76px;
  height: 76px;
  border-radius: 999px;
  background: radial-gradient(circle at 28% 20%, #54d0ff 0%, #4779F7 36%, #6C3AED 70%, #8f42ff 100%);
  box-shadow: 0 12px 38px rgba(57, 111, 255, 0.45), 0 0 22px rgba(88, 176, 255, 0.55);
  animation: agoneOrbPulse 2.8s ease-in-out infinite;
}
@keyframes agoneOrbPulse {
  0%, 100% { transform: scale(1); box-shadow: 0 12px 38px rgba(57, 111, 255, 0.45), 0 0 22px rgba(88, 176, 255, 0.55); }
  50% { transform: scale(1.04); box-shadow: 0 18px 44px rgba(108, 58, 237, 0.48), 0 0 30px rgba(88, 176, 255, 0.65); }
}
.agone-experion-eye {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  background: #f8fdff;
  margin: 0 5px;
  box-shadow: 0 0 14px rgba(255,255,255,0.9);
}
.agone-experion-panel {
  position: fixed;
  right: 20px;
  bottom: 104px;
  width: 760px;
  max-width: calc(100vw - 24px);
  height: 560px;
  max-height: calc(100vh - 110px);
  border-radius: 28px;
  color: #253046;
  box-shadow: 0 28px 78px rgba(17, 33, 79, 0.24);
  z-index: 2147483000;
  display: none;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #dce5f7;
  background: linear-gradient(180deg, #ffffff 0%, #f7f9ff 100%);
}
.agone-experion-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 14px 8px;
}
.agone-experion-brand {
  display: flex;
  align-items: center;
  gap: 10px;
}
.agone-experion-brand-orb {
  width: 34px;
  height: 34px;
  border-radius: 999px;
  background: radial-gradient(circle at 28% 20%, #7de3ff 0%, #5a8bff 45%, #7a4aff 100%);
  box-shadow: 0 0 16px rgba(108, 58, 237, 0.6);
}
.agone-experion-title {
  font-size: 16px;
  font-weight: 800;
  letter-spacing: 0.2px;
  color: #253048;
}
.agone-experion-subtitle {
  font-size: 11px;
  color: #7b8aa8;
  margin-top: 2px;
}
.agone-experion-close {
  border: none;
  background: #edf2ff;
  color: #4f5f86;
  border-radius: 10px;
  width: 32px;
  height: 32px;
  cursor: pointer;
  font-size: 16px;
}
.agone-experion-status {
  font-size: 11px;
  color: #7383a4;
  padding: 0 14px 4px;
}
.agone-experion-content {
  display: flex;
  flex: 1 1 auto;
  min-height: 0;
}
.agone-experion-sidebar {
  width: 240px;
  border-right: 1px solid #e4eaf8;
  background: linear-gradient(180deg, #f5f8ff 0%, #f9fbff 100%);
  padding: 10px 10px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.agone-experion-new-chat {
  border: 1px solid #cad8f7;
  color: #335084;
  background: #ffffff;
  border-radius: 10px;
  padding: 9px 10px;
  cursor: pointer;
  font-size: 12px;
  font-weight: 700;
  text-align: left;
}
.agone-experion-history-label {
  font-size: 11px;
  color: #7082a8;
  padding: 2px 4px 0;
}
.agone-experion-thread-list {
  overflow: auto;
  min-height: 0;
  padding-right: 2px;
}
.agone-experion-thread-group {
  margin-bottom: 10px;
}
.agone-experion-thread-group-title {
  font-size: 10px;
  font-weight: 700;
  color: #8091b6;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin: 0 4px 6px;
}
.agone-experion-thread-item {
  width: 100%;
  border: 1px solid transparent;
  border-radius: 10px;
  background: transparent;
  padding: 8px;
  text-align: left;
  cursor: pointer;
  margin-bottom: 4px;
}
.agone-experion-thread-item:hover {
  background: #ecf2ff;
}
.agone-experion-thread-item.active {
  border-color: #c9d8ff;
  background: #eaf1ff;
}
.agone-experion-thread-title {
  color: #334769;
  font-size: 12px;
  font-weight: 700;
  margin-bottom: 2px;
}
.agone-experion-thread-meta {
  color: #7688af;
  font-size: 10px;
}
.agone-experion-main {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
}
.agone-experion-robot-hero {
  margin: 4px 12px 10px;
  border: 1px solid #e4ebfb;
  border-radius: 18px;
  background: linear-gradient(135deg, #f8fbff 0%, #eef3ff 52%, #f3edff 100%);
  padding: 12px 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}
.agone-experion-robot-meta strong {
  display: block;
  font-size: 13px;
  color: #25304a;
}
.agone-experion-robot-meta span {
  font-size: 11px;
  color: #6e7ea3;
}
.agone-experion-robot-avatar {
  position: relative;
  width: 82px;
  height: 68px;
}
.agone-experion-robot-head {
  position: absolute;
  left: 14px;
  top: 0;
  width: 52px;
  height: 38px;
  border-radius: 18px;
  background: #ffffff;
  border: 1px solid #dce6fb;
  box-shadow: 0 8px 16px rgba(39, 72, 167, 0.15);
}
.agone-experion-robot-visor {
  position: absolute;
  left: 20px;
  top: 10px;
  width: 40px;
  height: 16px;
  border-radius: 8px;
  background: linear-gradient(135deg, #3f4fff, #6f45f2);
}
.agone-experion-robot-dot {
  position: absolute;
  top: 14px;
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: #c8f0ff;
  box-shadow: 0 0 8px rgba(181, 241, 255, 0.9);
}
.agone-experion-robot-dot.left { left: 27px; }
.agone-experion-robot-dot.right { left: 43px; }
.agone-experion-robot-body {
  position: absolute;
  left: 24px;
  top: 39px;
  width: 31px;
  height: 25px;
  border-radius: 15px;
  background: #ffffff;
  border: 1px solid #dce6fb;
}
.agone-experion-suggestions {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  padding: 0 12px 10px;
}
.agone-experion-chip {
  border: 1px solid #d8e2fb;
  color: #4b5f8a;
  background: #f3f7ff;
  border-radius: 999px;
  padding: 7px 11px;
  font-size: 12px;
  line-height: 1;
  cursor: pointer;
}
.agone-experion-chip:hover {
  background: #e8f0ff;
}
.agone-experion-messages {
  padding: 4px 12px 10px;
  overflow: auto;
  flex: 1 1 auto;
  min-height: 100px;
}
.agone-experion-msg {
  font-size: 12px;
  line-height: 1.43;
  padding: 9px 10px;
  border-radius: 13px;
  margin-bottom: 8px;
  max-width: 100%;
  white-space: pre-wrap;
}
.agone-experion-msg.user {
  margin-left: 0;
  background: linear-gradient(135deg, rgba(80,127,255,0.85) 0%, rgba(122,74,255,0.92) 100%);
  border: 1px solid rgba(171, 191, 255, 0.28);
  color: #f3f7ff;
}
.agone-experion-msg.bot {
  margin-right: 0;
  background: #ffffff;
  border: 1px solid #e1e8f8;
  color: #2f3b57;
}
.agone-experion-input-wrap {
  display: flex;
  gap: 8px;
  padding: 8px 12px 12px;
  border-top: 1px solid #e6ecfb;
  margin-top: auto;
}
.agone-experion-input {
  flex: 1;
  min-width: 0;
  border: 1px solid #d8e2f8;
  border-radius: 12px;
  padding: 10px 12px;
  font-size: 13px;
  background: #ffffff;
  color: #22304a;
}
.agone-experion-input::placeholder {
  color: #8fa1c6;
}
.agone-experion-send {
  border: 0;
  border-radius: 12px;
  color: #fff;
  min-width: 52px;
  padding: 10px 12px;
  cursor: pointer;
  font-weight: 600;
  background: linear-gradient(135deg, #4a89ff 0%, #6C3AED 100%);
}
.agone-experion-auth-notice {
  display: none;
  margin: 0 12px 10px;
  border: 1px solid #f5d7d7;
  background: #fff6f6;
  color: #8f2a2a;
  font-size: 12px;
  line-height: 1.4;
  border-radius: 10px;
  padding: 9px 10px;
}
.agone-experion-highlight {
  position: fixed;
  z-index: 2147482990;
  border: 2px solid rgba(97, 124, 255, 0.9);
  border-radius: 16px;
  box-shadow: 0 0 0 9999px rgba(102, 128, 255, 0.1), 0 0 0 4px rgba(115, 176, 255, 0.18);
  pointer-events: none;
}
`;
        document.head.appendChild(style);
    }

    function createElement(tag, className, text) {
        const element = document.createElement(tag);
        if (className) {
            element.className = className;
        }
        if (typeof text === "string") {
            element.textContent = text;
        }
        return element;
    }

    function ensureUi() {
        if (ui.launcher && ui.panel) {
            return;
        }

        ensureStyles();

        ui.launcher = createElement("button", "agone-experion-launcher");
        ui.launcher.setAttribute("type", "button");
        ui.launcher.setAttribute("aria-label", "Open AG ONE Experion");
        ui.launcher.addEventListener("click", function () {
            togglePanel();
        });
        const launcherOrb = createElement("div", "agone-experion-orb");
        launcherOrb.appendChild(createElement("span", "agone-experion-eye"));
        launcherOrb.appendChild(createElement("span", "agone-experion-eye"));
        ui.launcher.appendChild(launcherOrb);

        ui.panel = createElement("section", "agone-experion-panel");
        ui.panel.setAttribute("role", "dialog");
        ui.panel.setAttribute("aria-label", "AG ONE Experion assistant");

        const header = createElement("div", "agone-experion-header");
        const brand = createElement("div", "agone-experion-brand");
        brand.appendChild(createElement("div", "agone-experion-brand-orb"));
        const brandText = createElement("div");
        ui.headerTitle = createElement("div", "agone-experion-title", "Experion");
        ui.headerSubtitle = createElement("div", "agone-experion-subtitle", "AI execution companion");
        brandText.appendChild(ui.headerTitle);
        brandText.appendChild(ui.headerSubtitle);
        brand.appendChild(brandText);
        ui.closeButton = createElement("button", "agone-experion-close", "×");
        ui.closeButton.setAttribute("type", "button");
        ui.closeButton.addEventListener("click", function () { closePanel(); });
        header.appendChild(brand);
        header.appendChild(ui.closeButton);

        ui.status = createElement("div", "agone-experion-status", "Disconnected");
        const content = createElement("div", "agone-experion-content");
        const sidebar = createElement("aside", "agone-experion-sidebar");
        ui.newChatButton = createElement("button", "agone-experion-new-chat", "+ New chat");
        ui.newChatButton.setAttribute("type", "button");
        ui.newChatButton.addEventListener("click", function () {
            startNewChat();
        });
        sidebar.appendChild(ui.newChatButton);
        sidebar.appendChild(createElement("div", "agone-experion-history-label", "Old chats"));
        ui.sidebarList = createElement("div", "agone-experion-thread-list");
        sidebar.appendChild(ui.sidebarList);

        const main = createElement("div", "agone-experion-main");
        const robotHero = createElement("div", "agone-experion-robot-hero");
        const robotMeta = createElement("div", "agone-experion-robot-meta");
        robotMeta.appendChild(createElement("strong", "", "Experion AI Assistant"));
        robotMeta.appendChild(createElement("span", "", "Context aware · Conversation memory"));
        const robotAvatar = createElement("div", "agone-experion-robot-avatar");
        robotAvatar.appendChild(createElement("div", "agone-experion-robot-head"));
        robotAvatar.appendChild(createElement("div", "agone-experion-robot-visor"));
        robotAvatar.appendChild(createElement("div", "agone-experion-robot-dot left"));
        robotAvatar.appendChild(createElement("div", "agone-experion-robot-dot right"));
        robotAvatar.appendChild(createElement("div", "agone-experion-robot-body"));
        robotHero.appendChild(robotMeta);
        robotHero.appendChild(robotAvatar);

        ui.suggestions = createElement("div", "agone-experion-suggestions");
        ui.messages = createElement("div", "agone-experion-messages");
        ui.authNotice = createElement("div", "agone-experion-auth-notice");
        const inputWrap = createElement("div", "agone-experion-input-wrap");
        ui.input = createElement("input", "agone-experion-input");
        ui.input.setAttribute("placeholder", "Tell Experion what you want to do...");
        ui.input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                sendMessage();
            }
        });
        ui.sendButton = createElement("button", "agone-experion-send", "Send");
        ui.sendButton.setAttribute("type", "button");
        ui.sendButton.addEventListener("click", function () { sendMessage(); });
        inputWrap.appendChild(ui.input);
        inputWrap.appendChild(ui.sendButton);

        main.appendChild(robotHero);
        main.appendChild(ui.suggestions);
        main.appendChild(ui.messages);
        main.appendChild(ui.authNotice);
        main.appendChild(inputWrap);
        content.appendChild(sidebar);
        content.appendChild(main);

        ui.panel.appendChild(header);
        ui.panel.appendChild(ui.status);
        ui.panel.appendChild(content);

        document.body.appendChild(ui.panel);
        document.body.appendChild(ui.launcher);
    }

    function setStatus(text) {
        if (ui.status) {
            ui.status.textContent = text;
        }
    }

    function clearMessages() {
        if (ui.messages) {
            ui.messages.innerHTML = "";
        }
    }

    function addMessage(role, text, preventScroll) {
        if (!ui.messages) {
            return;
        }

        const safeRole = role === "user" ? "user" : "bot";
        const message = createElement("div", "agone-experion-msg " + safeRole, text || "");
        ui.messages.appendChild(message);
        if (!preventScroll) {
            ui.messages.scrollTop = ui.messages.scrollHeight;
        }
    }

    function renderMessages(messages) {
        clearMessages();
        const list = Array.isArray(messages) ? messages.slice() : [];
        list.sort(function (a, b) {
            const first = Date.parse(a.occurredAtUtc || "") || 0;
            const second = Date.parse(b.occurredAtUtc || "") || 0;
            return first - second;
        });
        for (let i = 0; i < list.length; i++) {
            addMessage(list[i].role === "user" ? "user" : "bot", list[i].content || "", true);
        }
        if (ui.messages) {
            ui.messages.scrollTop = ui.messages.scrollHeight;
        }
    }

    function renderSuggestionChips(suggestions) {
        if (!ui.suggestions) {
            return;
        }

        ui.suggestions.innerHTML = "";
        const items = Array.isArray(suggestions) ? suggestions.filter(Boolean).slice(0, 4) : [];
        if (items.length === 0) {
            ui.suggestions.style.display = "none";
            return;
        }

        ui.suggestions.style.display = "flex";
        for (let i = 0; i < items.length; i++) {
            const text = String(items[i]).trim();
            if (!text) {
                continue;
            }
            const chip = createElement("button", "agone-experion-chip", text);
            chip.setAttribute("type", "button");
            chip.addEventListener("click", function () {
                if (!ui.input) {
                    return;
                }
                ui.input.value = text;
                sendMessage();
            });
            ui.suggestions.appendChild(chip);
        }
    }

    function safeNow() {
        return Date.now();
    }

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function generateGuid() {
        if (window.crypto && typeof window.crypto.randomUUID === "function") {
            return window.crypto.randomUUID();
        }
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (char) {
            const random = Math.random() * 16 | 0;
            const value = char === "x" ? random : (random & 0x3 | 0x8);
            return value.toString(16);
        });
    }

    function setAuthRequired(message) {
        state.authRequired = true;
        state.session = null;
        state.currentConversationId = null;
        state.history = null;
        if (ui.input) {
            ui.input.disabled = true;
        }
        if (ui.sendButton) {
            ui.sendButton.disabled = true;
        }
        if (ui.newChatButton) {
            ui.newChatButton.disabled = true;
        }
        if (ui.authNotice) {
            ui.authNotice.style.display = "block";
            ui.authNotice.textContent = message || "Sign in required. Experion is available only for logged-in users.";
        }
        clearMessages();
        renderConversationThreads([], null);
        setStatus("Login required");
    }

    function clearAuthRequired() {
        state.authRequired = false;
        if (ui.input) {
            ui.input.disabled = false;
        }
        if (ui.sendButton) {
            ui.sendButton.disabled = false;
        }
        if (ui.newChatButton) {
            ui.newChatButton.disabled = false;
        }
        if (ui.authNotice) {
            ui.authNotice.style.display = "none";
            ui.authNotice.textContent = "";
        }
    }

    function formatTimeLabel(utcString) {
        if (!utcString) {
            return "";
        }
        const date = new Date(utcString);
        if (isNaN(date.getTime())) {
            return "";
        }
        return date.toLocaleString(undefined, {
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    function getDayBucket(utcString) {
        const date = new Date(utcString);
        if (isNaN(date.getTime())) {
            return "Earlier";
        }
        const now = new Date();
        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000);
        const day = new Date(date.getFullYear(), date.getMonth(), date.getDate());
        if (day.getTime() === today.getTime()) {
            return "Today";
        }
        if (day.getTime() === yesterday.getTime()) {
            return "Yesterday";
        }
        return "Earlier";
    }

    function renderConversationThreads(threads, activeConversationId) {
        if (!ui.sidebarList) {
            return;
        }
        ui.sidebarList.innerHTML = "";
        const list = Array.isArray(threads) ? threads.slice() : [];
        if (list.length === 0) {
            ui.sidebarList.appendChild(createElement("div", "agone-experion-history-label", "No previous chats"));
            return;
        }

        const grouped = {
            Today: [],
            Yesterday: [],
            Earlier: []
        };
        for (let i = 0; i < list.length; i++) {
            const bucket = getDayBucket(list[i].lastOccurredAtUtc);
            if (!grouped[bucket]) {
                grouped[bucket] = [];
            }
            grouped[bucket].push(list[i]);
        }

        ["Today", "Yesterday", "Earlier"].forEach(function (bucket) {
            const items = grouped[bucket] || [];
            if (items.length === 0) {
                return;
            }
            const group = createElement("div", "agone-experion-thread-group");
            group.appendChild(createElement("div", "agone-experion-thread-group-title", bucket));
            for (let i = 0; i < items.length; i++) {
                const thread = items[i];
                const button = createElement("button", "agone-experion-thread-item");
                button.setAttribute("type", "button");
                if (String(thread.conversationId || "") === String(activeConversationId || "")) {
                    button.classList.add("active");
                }
                button.appendChild(createElement("div", "agone-experion-thread-title", thread.title || "Conversation"));
                button.appendChild(createElement("div", "agone-experion-thread-meta", formatTimeLabel(thread.lastOccurredAtUtc)));
                button.addEventListener("click", function () {
                    openConversation(thread.conversationId);
                });
                group.appendChild(button);
            }
            ui.sidebarList.appendChild(group);
        });
    }

    function positionPanelDefault() {
        if (!ui.panel) {
            return;
        }
        ui.panel.style.left = "";
        ui.panel.style.top = "";
        ui.panel.style.right = "20px";
        ui.panel.style.bottom = "88px";
    }

    function positionPanelNearAnchor(anchor) {
        if (!ui.panel || !anchor) {
            positionPanelDefault();
            return;
        }

        const margin = 12;
        const gap = 14;
        const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
        const panelWidth = ui.panel.offsetWidth || 760;
        const panelHeight = ui.panel.offsetHeight || 560;
        const anchorX = Math.max(0, anchor.x || 0);
        const anchorY = Math.max(0, anchor.y || 0);
        const anchorWidth = Math.max(24, anchor.width || 0);
        const anchorHeight = Math.max(24, anchor.height || 0);

        let left = anchorX + anchorWidth + gap;
        if (left + panelWidth > viewportWidth - margin) {
            left = anchorX - panelWidth - gap;
        }
        if (left < margin) {
            left = clamp(anchorX, margin, Math.max(margin, viewportWidth - panelWidth - margin));
        }

        let top = anchorY;
        if (top + panelHeight > viewportHeight - margin) {
            top = viewportHeight - panelHeight - margin;
        }
        top = clamp(top, margin, Math.max(margin, viewportHeight - panelHeight - margin));

        ui.panel.style.right = "auto";
        ui.panel.style.bottom = "auto";
        ui.panel.style.left = Math.round(left) + "px";
        ui.panel.style.top = Math.round(top) + "px";
    }

    function showHighlight(anchor) {
        if (!anchor) {
            return;
        }
        if (!ui.highlight) {
            ui.highlight = createElement("div", "agone-experion-highlight");
            document.body.appendChild(ui.highlight);
        }
        const pad = 8;
        ui.highlight.style.display = "block";
        ui.highlight.style.left = Math.max(0, anchor.x - pad) + "px";
        ui.highlight.style.top = Math.max(0, anchor.y - pad) + "px";
        ui.highlight.style.width = Math.max(24, anchor.width + pad * 2) + "px";
        ui.highlight.style.height = Math.max(24, anchor.height + pad * 2) + "px";
    }

    function hideHighlight() {
        if (ui.highlight) {
            ui.highlight.style.display = "none";
        }
    }

    function getDomContextFromPoint(anchor) {
        if (!anchor) {
            return "";
        }
        const centerX = Math.round(anchor.x + (anchor.width || 0) / 2);
        const centerY = Math.round(anchor.y + (anchor.height || 0) / 2);
        const element = document.elementFromPoint(centerX, centerY);
        if (!element) {
            return "";
        }
        const chunks = [];
        if (element.tagName) {
            chunks.push("target:" + element.tagName.toLowerCase());
        }
        const block = element.closest("section,article,main,div,td,tr,li");
        if (block) {
            const heading = block.querySelector("h1,h2,h3,h4,h5,h6,strong,label");
            if (heading && heading.textContent) {
                chunks.push("heading:" + heading.textContent.trim());
            }
            if (block.textContent) {
                chunks.push("blockText:" + block.textContent.replace(/\s+/g, " ").trim().slice(0, 260));
            }
        }
        if (element.getAttribute("aria-label")) {
            chunks.push("ariaLabel:" + element.getAttribute("aria-label"));
        }
        return chunks.join(" | ");
    }

    async function resolveAuthToken() {
        if (typeof state.config.authTokenProvider !== "function") {
            return null;
        }
        try {
            const token = await state.config.authTokenProvider();
            return token || null;
        } catch {
            return null;
        }
    }

    function collectContext() {
        return {
            pageUrl: window.location.href,
            pageTitle: document.title || "",
            route: window.location.pathname || "/",
            locale: navigator.language || "en",
            userTimezone: Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC",
            triggerTimestampUtc: new Date().toISOString()
        };
    }

    async function toApiError(response) {
        const text = await response.text();
        let message = text;
        try {
            const parsed = JSON.parse(text);
            if (parsed && parsed.message) {
                message = parsed.message;
            }
        } catch {
            if (!message) {
                message = "Unknown API error.";
            }
        }
        const error = new Error(message || ("Experion API error (" + response.status + ")"));
        error.status = response.status;
        return error;
    }

    async function apiRequest(method, path, payload, query) {
        const queryString = query
            ? "?" + Object.keys(query)
                .filter(function (key) {
                    return query[key] !== undefined && query[key] !== null && String(query[key]).length > 0;
                })
                .map(function (key) {
                    return encodeURIComponent(key) + "=" + encodeURIComponent(String(query[key]));
                })
                .join("&")
            : "";
        const url = state.config.apiBaseUrl.replace(/\/+$/, "") + path + queryString;
        const token = await resolveAuthToken();
        const headers = {
            "X-AGONE-Product": state.config.productCode || "unknown",
            "X-AGONE-WorkspaceId": state.config.workspaceId || ""
        };

        if (method !== "GET") {
            headers["Content-Type"] = "application/json";
        }
        if (state.config.sourceToken) {
            headers["X-AGONE-SourceToken"] = state.config.sourceToken;
        }
        if (state.config.userId) {
            headers["X-AGONE-UserId"] = state.config.userId;
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
            throw await toApiError(response);
        }
        return await response.json();
    }

    async function loadConversationHistory(conversationId) {
        if (state.authRequired) {
            return null;
        }
        const response = await apiRequest("GET", "/conversation/history", null, {
            conversationId: conversationId || "",
            conversationTake: 20,
            messageTake: 120
        });
        state.history = response;
        state.currentConversationId = response.activeConversationId || state.currentConversationId;
        renderConversationThreads(response.conversations || [], state.currentConversationId);
        renderMessages(response.messages || []);
        if (!response.messages || response.messages.length === 0) {
            addMessage("bot", "Hi, I am Experion. Start with a new chat or continue an old one.");
        }
        return response;
    }

    async function bootstrapSession(triggerType) {
        clearAuthRequired();
        const payload = Object.assign({}, collectContext(), {
            triggerType: triggerType || "manual",
            conversationId: state.currentConversationId
        });
        const session = await apiRequest("POST", "/session/bootstrap", payload);
        state.session = session;
        state.currentConversationId = session.conversationId || state.currentConversationId || generateGuid();
        setStatus("Connected · session " + String(session.sessionId || "").slice(0, 8));
        await loadConversationHistory(state.currentConversationId);
        return session;
    }

    async function notifyTrigger(triggerType, extra) {
        if (!state.session) {
            return null;
        }
        const box = extra && extra.boundingBox ? extra.boundingBox : null;
        const payload = Object.assign({}, collectContext(), {
            sessionId: state.session.sessionId,
            triggerType: triggerType,
            domContext: extra && extra.domContext ? extra.domContext : "",
            selectionText: extra && extra.selectionText ? extra.selectionText : "",
            x: box ? (box.x || 0) : 0,
            y: box ? (box.y || 0) : 0,
            width: box ? (box.width || 0) : 0,
            height: box ? (box.height || 0) : 0
        });
        return await apiRequest("POST", "/context/trigger", payload);
    }

    function applyTriggerSuggestions(triggerResponse, anchor) {
        state.lastTriggerResponse = triggerResponse || null;
        if (!triggerResponse) {
            addMessage("bot", "I detected this area. Tell me what you want done here.");
            return;
        }
        const lines = [];
        if (anchor) {
            lines.push("I detected the section you circled.");
        }
        if (triggerResponse.detectedIntent) {
            lines.push("Intent: " + triggerResponse.detectedIntent);
        }
        if (typeof triggerResponse.confidence === "number") {
            lines.push("Confidence: " + Math.round(triggerResponse.confidence * 100) + "%");
        }
        if (triggerResponse.suggestedPrompts && triggerResponse.suggestedPrompts.length > 0) {
            renderSuggestionChips(triggerResponse.suggestedPrompts);
        }
        addMessage("bot", lines.join("\n").trim() || "I detected this area. How can I help?");
    }

    async function openConversation(conversationId) {
        if (!conversationId || state.authRequired) {
            return;
        }
        state.currentConversationId = conversationId;
        setStatus("Loading chat...");
        try {
            await loadConversationHistory(conversationId);
            setStatus("Connected");
        } catch (error) {
            if (error && error.status === 401) {
                setAuthRequired(error.message);
                return;
            }
            setStatus("Could not load chat");
        }
    }

    async function startNewChat() {
        if (state.authRequired) {
            return;
        }
        state.currentConversationId = generateGuid();
        if (state.session) {
            state.session.conversationId = state.currentConversationId;
        }
        clearMessages();
        addMessage("bot", "New chat started. Tell me what you want done.");
        renderConversationThreads(state.history ? state.history.conversations : [], state.currentConversationId);
        if (ui.input) {
            ui.input.focus();
        }
    }

    async function sendMessage() {
        if (state.authRequired) {
            setStatus("Login required");
            return;
        }
        const text = (ui.input && ui.input.value ? ui.input.value : "").trim();
        if (!text) {
            return;
        }
        ui.input.value = "";
        addMessage("user", text);

        try {
            if (!state.currentConversationId) {
                state.currentConversationId = generateGuid();
            }
            if (!state.session) {
                await bootstrapSession("manual");
            }
            setStatus("Thinking...");
            const response = await apiRequest("POST", "/conversation/message", {
                sessionId: state.session.sessionId,
                conversationId: state.currentConversationId,
                message: text,
                contextVersion: 1
            });
            state.currentConversationId = response.conversationId || state.currentConversationId;
            addMessage("bot", response.assistantMessage || "I am ready to help.");
            await loadConversationHistory(state.currentConversationId);
            setStatus("Connected");
        } catch (error) {
            if (error && error.status === 401) {
                setAuthRequired(error.message);
                return;
            }
            addMessage("bot", "I could not complete that request. Please try again.");
            setStatus("Error: " + (error && error.message ? error.message : "unknown"));
        }
    }

    async function openPanel(triggerType, anchor, triggerExtra) {
        ensureUi();
        state.currentAnchor = anchor || null;
        ui.panel.style.display = "flex";
        state.isOpen = true;

        if (anchor) {
            positionPanelNearAnchor(anchor);
            showHighlight(anchor);
        } else {
            positionPanelDefault();
            hideHighlight();
        }

        setStatus("Connecting...");

        try {
            if (!state.session) {
                await bootstrapSession(triggerType || "manual");
            } else {
                await loadConversationHistory(state.currentConversationId || state.session.conversationId);
            }
            const triggerResponse = await notifyTrigger(triggerType || "manual", triggerExtra || {});
            if (triggerType === "circle") {
                applyTriggerSuggestions(triggerResponse, anchor);
            }
            setStatus(state.authRequired ? "Login required" : "Connected");
        } catch (error) {
            if (error && error.status === 401) {
                setAuthRequired(error.message);
                return;
            }
            setStatus("Connection failed");
            addMessage("bot", "Connection failed. Please verify AG AI Hub configuration.");
        }
    }

    function closePanel() {
        if (!ui.panel) {
            return;
        }
        ui.panel.style.display = "none";
        state.isOpen = false;
        hideHighlight();
    }

    function togglePanel() {
        if (!ui.panel || ui.panel.style.display === "none") {
            openPanel("manual", null, {});
            return;
        }
        closePanel();
    }

    function isCircleGesture(points) {
        if (!points || points.length < 12) {
            return false;
        }
        const first = points[0];
        const last = points[points.length - 1];
        if (Math.hypot(last.x - first.x, last.y - first.y) > 32) {
            return false;
        }

        let minX = Number.POSITIVE_INFINITY;
        let minY = Number.POSITIVE_INFINITY;
        let maxX = Number.NEGATIVE_INFINITY;
        let maxY = Number.NEGATIVE_INFINITY;
        for (let i = 0; i < points.length; i++) {
            const point = points[i];
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }
        const width = maxX - minX;
        const height = maxY - minY;
        if (width < 40 || height < 40) {
            return false;
        }
        const ratio = width / height;
        return ratio > 0.45 && ratio < 2.2;
    }

    function boundingBox(points) {
        let minX = Number.POSITIVE_INFINITY;
        let minY = Number.POSITIVE_INFINITY;
        let maxX = Number.NEGATIVE_INFINITY;
        let maxY = Number.NEGATIVE_INFINITY;
        for (let i = 0; i < points.length; i++) {
            const point = points[i];
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }
        return {
            x: Math.max(0, Math.round(minX)),
            y: Math.max(0, Math.round(minY)),
            width: Math.max(0, Math.round(maxX - minX)),
            height: Math.max(0, Math.round(maxY - minY))
        };
    }

    function isUiTarget(target) {
        if (!target) {
            return false;
        }
        return (ui.panel && ui.panel.contains(target))
            || (ui.launcher && ui.launcher.contains(target));
    }

    function captureCircleGesture() {
        let drawing = false;
        let points = [];
        document.addEventListener("pointerdown", function (event) {
            if (event.button !== 0 || isUiTarget(event.target)) {
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

        document.addEventListener("pointerup", async function (event) {
            if (!drawing) {
                return;
            }
            drawing = false;
            points.push({ x: event.clientX, y: event.clientY });
            if (safeNow() < state.triggerCooldownUntil) {
                return;
            }
            if (!isCircleGesture(points)) {
                return;
            }
            state.triggerCooldownUntil = safeNow() + 5000;
            const box = boundingBox(points);
            const selectionText = window.getSelection ? String(window.getSelection()).trim() : "";
            const domContext = getDomContextFromPoint(box);
            await openPanel("circle", box, {
                domContext: domContext || (document.title || ""),
                selectionText: selectionText,
                boundingBox: box
            });
        }, true);
    }

    async function initialize(config) {
        if (state.initialized) {
            return;
        }
        state.config = Object.assign({}, state.config, config || {});
        ensureUi();
        captureCircleGesture();
        state.initialized = true;
        setStatus("Ready");

        if (state.config.autoOpen) {
            await openPanel("auto", null, {});
        }
    }

    window.AGONEExperion = {
        init: initialize,
        open: function () { return openPanel("manual", null, {}); },
        openNear: function (rect) { return openPanel("manual", rect, {}); },
        openForSelection: function (rect, selectionText, domContext) {
            return openPanel("circle", rect || null, {
                selectionText: selectionText || "",
                domContext: domContext || "",
                boundingBox: rect || null
            });
        },
        close: closePanel,
        toggle: togglePanel,
        newChat: startNewChat,
        send: function (text) {
            if (!ui.input) {
                return;
            }
            ui.input.value = text || "";
            return sendMessage();
        },
        resetSession: function () {
            state.session = null;
            state.currentConversationId = null;
            state.lastTriggerResponse = null;
            state.history = null;
            clearAuthRequired();
            clearMessages();
            renderConversationThreads([], null);
            setStatus("Ready");
        }
    };
})(window, document);
