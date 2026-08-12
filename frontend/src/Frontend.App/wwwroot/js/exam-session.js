window.ExamSession = (() => {
    let shouldWarnOnUnload = false;

    function read(storageKey) {
        const raw = window.sessionStorage.getItem(storageKey);
        if (!raw) return null;
        try {
            return JSON.parse(raw);
        } catch {
            window.sessionStorage.removeItem(storageKey);
            return null;
        }
    }

    function snapshot(record) {
        const secondsRemaining = record.completed
            ? 0
            : Math.max(0, Math.ceil((record.endsAt - Date.now()) / 1000));
        return {
            secondsRemaining,
            isCompleted: Boolean(record.completed),
            isExpired: !record.completed && secondsRemaining === 0,
            isTimedOut: Boolean(record.timedOut)
        };
    }

    function write(storageKey, record) {
        window.sessionStorage.setItem(storageKey, JSON.stringify(record));
        return snapshot(record);
    }

    function start(storageKey, durationSeconds) {
        let record = read(storageKey);
        if (!record || record.durationSeconds !== durationSeconds) {
            record = {
                durationSeconds,
                endsAt: Date.now() + durationSeconds * 1000,
                completed: false
            };
        }
        return write(storageKey, record);
    }

    function get(storageKey) {
        const record = read(storageKey);
        return record ? snapshot(record) : null;
    }

    function complete(storageKey, timedOut) {
        const record = read(storageKey);
        if (record) {
            record.completed = true;
            record.timedOut = Boolean(timedOut);
            write(storageKey, record);
        }
    }

    function setUnloadWarning(enabled) {
        shouldWarnOnUnload = enabled;
    }

    window.addEventListener('beforeunload', (event) => {
        if (!shouldWarnOnUnload) return;
        event.preventDefault();
        event.returnValue = '';
    });

    function confirmLeave() {
        return window.confirm('Leave this test? Your timer will continue running for this browser session.');
    }

    function lockTestContent(selector) {
        document.querySelectorAll(`${selector} input, ${selector} select, ${selector} textarea, ${selector} button`)
            .forEach(element => {
                element.disabled = true;
                element.setAttribute('aria-disabled', 'true');
            });
    }

    return { start, get, complete, setUnloadWarning, confirmLeave, lockTestContent };
})();
