window.TabVisibility = (() => {
    const handlers = new Map();
    let nextId = 1;

    function register(dotNetRef) {
        const id = nextId++;
        const handler = () => {
            if (document.visibilityState !== 'visible') return;
            dotNetRef.invokeMethodAsync('OnTabVisibleAsync').catch(() => { });
        };
        document.addEventListener('visibilitychange', handler);
        handlers.set(id, { handler, dotNetRef });
        return id;
    }

    function unregister(id) {
        const entry = handlers.get(id);
        if (!entry) return;
        document.removeEventListener('visibilitychange', entry.handler);
        handlers.delete(id);
    }

    return { register, unregister };
})();
