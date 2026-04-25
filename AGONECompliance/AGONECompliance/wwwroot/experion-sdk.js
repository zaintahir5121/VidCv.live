/* AG ONE Experion SDK (Phase 0 POC)
 * Central conversational launcher shell for all AG ONE products.
 * This file is intentionally framework-agnostic so it can be embedded in any host app.
 */
(function (window, document) {
    "use strict";

    if (window.AGONEExperion) {
        return;
    }

    const state = {
        initialized: false,
        isOpen: false,
        config: {
            apiBaseUrl: "/api/experion",
            sourceToken: "",
            productCode: "unknown",
            workspaceId: "",
            authTokenProvider: null,
            autoOpen: false
        },
        session: null,
        triggerCooldownUntil: 0,
        currentAnchor: null,
        lastTriggerResponse: null
    };

    const ui = {
        launcher: null,
        panel: null,
        headerTitle: null,
        headerSubtitle: null,
        messages: null,
        suggestions: null,
        input: null,
        sendButton: null,
        closeButton: null,
        status: null,
        highlight: null
    };

    const styleId = "agone-experion-style";

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
  width: 460px;
  max-width: calc(100vw - 24px);
  height: 600px;
  max-height: calc(100vh - 110px);
  border-radius: 24px;
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
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px 8px;
  border-bottom: 1px solid #e6ecfb;
}
.agone-experion-brand {
  display: flex;
  align-items: center;
  gap: 10px;
}
.agone-experion-brand-orb {
  width: 30px;
  height: 30px;
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
  padding: 9px 16px 0;
}
.agone-experion-robot-hero {
  margin: 8px 14px 10px;
  border: 1px solid #e4ebfb;
  border-radius: 16px;
  background: linear-gradient(135deg, #f8fbff 0%, #eef3ff 52%, #f3edff 100%);
  padding: 11px 12px;
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
  width: 72px;
  height: 58px;
}
.agone-experion-robot-head {
  position: absolute;
  left: 10px;
  top: 0;
  width: 46px;
  height: 34px;
  border-radius: 16px;
  background: #ffffff;
  border: 1px solid #dce6fb;
  box-shadow: 0 8px 16px rgba(39, 72, 167, 0.15);
}
.agone-experion-robot-visor {
  position: absolute;
  left: 16px;
  top: 9px;
  width: 34px;
  height: 14px;
  border-radius: 8px;
  background: linear-gradient(135deg, #3f4fff, #6f45f2);
}
.agone-experion-robot-dot {
  position: absolute;
  top: 12px;
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: #c8f0ff;
  box-shadow: 0 0 8px rgba(181, 241, 255, 0.9);
}
.agone-experion-robot-dot.left { left: 22px; }
.agone-experion-robot-dot.right { left: 36px; }
.agone-experion-robot-body {
  position: absolute;
  left: 19px;
  top: 34px;
  width: 28px;
  height: 22px;
  border-radius: 13px;
  background: #ffffff;
  border: 1px solid #dce6fb;
}
.agone-experion-suggestions {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  padding: 8px 14px 10px;
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
  padding: 8px 14px 12px;
  overflow: auto;
  flex: 1 1 auto;
}
.agone-experion-msg {
  font-size: 13px;
  line-height: 1.43;
  padding: 10px 12px;
  border-radius: 13px;
  margin-bottom: 10px;
  max-width: 88%;
  white-space: pre-wrap;
}
.agone-experion-msg.user {
  margin-left: auto;
  background: linear-gradient(135deg, rgba(80,127,255,0.85) 0%, rgba(122,74,255,0.92) 100%);
  border: 1px solid rgba(171, 191, 255, 0.28);
  color: #f3f7ff;
}
.agone-experion-msg.bot {
  margin-right: auto;
  background: #ffffff;
  border: 1px solid #e1e8f8;
  color: #2f3b57;
}
.agone-experion-input-wrap {
  display: flex;
  gap: 8px;
  padding: 10px 12px 12px;
  border-top: 1px solid #e6ecfb;
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
  min-width: 56px;
  padding: 10px 12px;
  cursor: pointer;
  font-weight: 600;
  background: linear-gradient(135deg, #4a89ff 0%, #6C3AED 100%);
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
        const el = document.createElement(tag);
        if (className) {
            el.className = className;
        }
        if (typeof text === "string") {
            el.textContent = text;
        }
        return el;
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
        ui.closeButton.addEventListener("click", function () {
            closePanel();
        });
        header.appendChild(brand);
        header.appendChild(ui.closeButton);

        ui.status = createElement("div", "agone-experion-status", "Disconnected");
        const robotHero = createElement("div", "agone-experion-robot-hero");
        const robotMeta = createElement("div", "agone-experion-robot-meta");
        const robotStrong = createElement("strong", "", "Experion AI Assistant");
        const robotSpan = createElement("span", "", "Action-driven guidance · Context aware");
        robotMeta.appendChild(robotStrong);
        robotMeta.appendChild(robotSpan);
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
        const inputWrap = createElement("div", "agone-experion-input-wrap");
        ui.input = createElement("input", "agone-experion-input");
        ui.input.setAttribute("placeholder", "Tell Experion what you want to do...");
        ui.input.addEventListener("keydown", function (ev) {
            if (ev.key === "Enter") {
                sendMessage();
            }
        });
        ui.sendButton = createElement("button", "agone-experion-send", "Send");
        ui.sendButton.setAttribute("type", "button");
        ui.sendButton.addEventListener("click", function () {
            sendMessage();
        });
        inputWrap.appendChild(ui.input);
        inputWrap.appendChild(ui.sendButton);

        ui.panel.appendChild(header);
        ui.panel.appendChild(ui.status);
        ui.panel.appendChild(robotHero);
        ui.panel.appendChild(ui.suggestions);
        ui.panel.appendChild(ui.messages);
        ui.panel.appendChild(inputWrap);

        document.body.appendChild(ui.panel);
        document.body.appendChild(ui.launcher);
    }

    function setStatus(text) {
        if (ui.status) {
            ui.status.textContent = text;
        }
    }

    function addMessage(role, text) {
        const msg = createElement("div", "agone-experion-msg " + (role === "user" ? "user" : "bot"), text);
        ui.messages.appendChild(msg);
        ui.messages.scrollTop = ui.messages.scrollHeight;
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

        const panelWidth = ui.panel.offsetWidth || 360;
        const panelHeight = ui.panel.offsetHeight || 520;

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

    async function apiCall(path, payload) {
        const url = state.config.apiBaseUrl.replace(/\/+$/, "") + path;
        const token = await resolveAuthToken();
        const headers = {
            "Content-Type": "application/json",
            "X-AGONE-Product": state.config.productCode || "unknown",
            "X-AGONE-WorkspaceId": state.config.workspaceId || ""
        };

        if (state.config.sourceToken) {
            headers["X-AGONE-SourceToken"] = state.config.sourceToken;
        }

        if (token) {
            headers.Authorization = "Bearer " + token;
        }

        const response = await fetch(url, {
            method: "POST",
            headers: headers,
            body: JSON.stringify(payload || {})
        });

        if (!response.ok) {
            const body = await response.text();
            throw new Error("Experion API error (" + response.status + "): " + body);
        }

        return await response.json();
    }

    async function bootstrapSession(triggerType) {
        const payload = Object.assign({}, collectContext(), {
            triggerType: triggerType || "manual"
        });

        const session = await apiCall("/session/bootstrap", payload);
        state.session = session;
        setStatus("Connected · session " + (session.sessionId || "").slice(0, 8));
        return session;
    }

    async function notifyTrigger(triggerType, extra) {
        if (!state.session) {
            return null;
        }

        const bbox = extra && extra.boundingBox ? extra.boundingBox : null;
        const payload = Object.assign({}, collectContext(), {
            sessionId: state.session.sessionId,
            triggerType: triggerType,
            domContext: extra && extra.domContext ? extra.domContext : "",
            selectionText: extra && extra.selectionText ? extra.selectionText : "",
            x: bbox ? (bbox.x || 0) : 0,
            y: bbox ? (bbox.y || 0) : 0,
            width: bbox ? (bbox.width || 0) : 0,
            height: bbox ? (bbox.height || 0) : 0
        });

        return await apiCall("/context/trigger", payload);
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
            lines.push("");
            lines.push("Suggested prompts:");
            for (let i = 0; i < Math.min(3, triggerResponse.suggestedPrompts.length); i++) {
                lines.push("- " + triggerResponse.suggestedPrompts[i]);
            }
        }

        if (triggerResponse.recommendedActions && triggerResponse.recommendedActions.length > 0) {
            lines.push("");
            lines.push("Recommended actions:");
            for (let j = 0; j < Math.min(3, triggerResponse.recommendedActions.length); j++) {
                lines.push("- " + triggerResponse.recommendedActions[j]);
            }
        }

        addMessage("bot", lines.join("\n").trim() || "I detected this area. How can I help?");
    }

    async function sendMessage() {
        const text = (ui.input.value || "").trim();
        if (!text) {
            return;
        }

        ui.input.value = "";
        addMessage("user", text);

        try {
            if (!state.session) {
                await bootstrapSession("manual");
            }

            setStatus("Thinking...");
            const response = await apiCall("/conversation/message", {
                sessionId: state.session.sessionId,
                message: text,
                contextVersion: 1
            });

            addMessage("bot", response.assistantMessage || "I am ready to help.");
            setStatus("Connected");
        } catch (error) {
            addMessage("bot", "I could not complete that request. Please try again.");
            setStatus("Error: " + (error && error.message ? error.message : "unknown"));
        }
    }

    async function openPanel(triggerType, anchor, triggerExtra) {
        ensureUi();
        state.currentAnchor = anchor || null;
        ui.panel.style.display = "block";
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
            }

            const triggerResponse = await notifyTrigger(triggerType || "manual", triggerExtra || {});
            if (triggerType === "circle") {
                applyTriggerSuggestions(triggerResponse, anchor);
            } else if (ui.messages.childElementCount === 0) {
                addMessage("bot", "Hi, I am Experion. I can help you complete this journey faster.");
            }

            setStatus("Connected");
        } catch (error) {
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

    function boundingBox(points) {
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
            const bbox = boundingBox(points);
            const selectionText = window.getSelection ? String(window.getSelection()).trim() : "";
            const domContext = getDomContextFromPoint(bbox);

            await openPanel("circle", bbox, {
                domContext: domContext || (document.title || ""),
                selectionText: selectionText,
                boundingBox: bbox
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
        send: function (text) {
            if (!ui.input) {
                return;
            }

            ui.input.value = text || "";
            return sendMessage();
        },
        resetSession: function () {
            state.session = null;
            state.lastTriggerResponse = null;
            setStatus("Ready");
        }
    };
})(window, document);
