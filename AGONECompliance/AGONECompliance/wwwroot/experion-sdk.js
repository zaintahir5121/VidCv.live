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
        messages: null,
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
  width: 56px;
  height: 56px;
  color: #fff;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 8px 24px rgba(71,121,247,0.35);
  background: linear-gradient(135deg, #4779F7 0%, #6C3AED 100%);
}
.agone-experion-panel {
  position: fixed;
  right: 20px;
  bottom: 88px;
  width: 360px;
  max-width: calc(100vw - 24px);
  height: 520px;
  max-height: calc(100vh - 120px);
  border-radius: 14px;
  background: #fff;
  color: #111827;
  box-shadow: 0 14px 38px rgba(15, 23, 42, 0.22);
  z-index: 2147483000;
  display: none;
  overflow: hidden;
  border: 1px solid #e5e7eb;
}
.agone-experion-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px;
  color: #fff;
  background: linear-gradient(135deg, #4779F7 0%, #6C3AED 100%);
}
.agone-experion-title {
  font-size: 14px;
  font-weight: 700;
}
.agone-experion-close {
  border: none;
  background: rgba(255,255,255,0.18);
  color: #fff;
  border-radius: 8px;
  width: 30px;
  height: 30px;
  cursor: pointer;
}
.agone-experion-status {
  font-size: 12px;
  color: #475569;
  padding: 8px 14px;
  border-bottom: 1px solid #eef2f7;
}
.agone-experion-messages {
  padding: 12px 14px;
  height: calc(100% - 160px);
  overflow: auto;
  background: #f8fafc;
}
.agone-experion-msg {
  font-size: 13px;
  line-height: 1.35;
  padding: 10px 11px;
  border-radius: 10px;
  margin-bottom: 8px;
  max-width: 85%;
  white-space: pre-wrap;
}
.agone-experion-msg.user {
  margin-left: auto;
  background: #dbeafe;
  color: #1e3a8a;
}
.agone-experion-msg.bot {
  margin-right: auto;
  background: #fff;
  border: 1px solid #e2e8f0;
  color: #0f172a;
}
.agone-experion-input-wrap {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  gap: 8px;
  padding: 10px 12px;
  border-top: 1px solid #e5e7eb;
  background: #fff;
}
.agone-experion-input {
  flex: 1;
  min-width: 0;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 9px 10px;
  font-size: 13px;
}
.agone-experion-send {
  border: 0;
  border-radius: 10px;
  color: #fff;
  padding: 9px 12px;
  cursor: pointer;
  background: linear-gradient(135deg, #4779F7 0%, #6C3AED 100%);
}
.agone-experion-highlight {
  position: fixed;
  z-index: 2147482990;
  border: 2px solid #6C3AED;
  border-radius: 14px;
  box-shadow: 0 0 0 9999px rgba(108,58,237,0.08), 0 0 0 4px rgba(71,121,247,0.20);
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

        ui.launcher = createElement("button", "agone-experion-launcher", "AI");
        ui.launcher.setAttribute("type", "button");
        ui.launcher.setAttribute("aria-label", "Open AG ONE Experion");
        ui.launcher.addEventListener("click", function () {
            togglePanel();
        });

        ui.panel = createElement("section", "agone-experion-panel");
        ui.panel.setAttribute("role", "dialog");
        ui.panel.setAttribute("aria-label", "AG ONE Experion assistant");

        const header = createElement("div", "agone-experion-header");
        ui.headerTitle = createElement("div", "agone-experion-title", "AG ONE Experion");
        ui.closeButton = createElement("button", "agone-experion-close", "×");
        ui.closeButton.setAttribute("type", "button");
        ui.closeButton.addEventListener("click", function () {
            closePanel();
        });
        header.appendChild(ui.headerTitle);
        header.appendChild(ui.closeButton);

        ui.status = createElement("div", "agone-experion-status", "Disconnected");
        ui.messages = createElement("div", "agone-experion-messages");
        const inputWrap = createElement("div", "agone-experion-input-wrap");
        ui.input = createElement("input", "agone-experion-input");
        ui.input.setAttribute("placeholder", "Ask Experion anything about this page...");
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
