// Boots the application, and says how it is going while it does.
//
// The Avalonia template's version is four lines and starts the runtime. The extra work here is all about
// the megabytes in between: counting them as they arrive so the page can show a bar, and saying something
// useful if the runtime never manages to start.

const line = document.getElementById('boot-line');
const bar = document.getElementById('boot-bar');
const hint = document.getElementById('boot-hint');
const boot = document.querySelector('.boot');

let loaded = 0;
let total = 0;

const megabytes = bytes => (bytes / 1048576).toFixed(1);

function report() {
    if (!total) {
        line.textContent = `Downloading — ${megabytes(loaded)} MB`;
        return;
    }

    const fraction = Math.min(1, loaded / total);
    bar.style.width = `${(fraction * 100).toFixed(1)}%`;
    line.textContent = `Downloading — ${megabytes(loaded)} of ${megabytes(total)} MB`;
}

function failed(what, detail) {
    boot?.classList.add('failed');
    line.textContent = what;
    hint.innerHTML = `${detail} <a href="../index.html">Back to AvaSchematic</a>`;
}

// Count the bytes as the runtime pulls them in.
//
// The body has to be re-wrapped rather than read, because the runtime needs it too - a stream can only be
// consumed once. Anything that is not part of the runtime is passed straight through.
const inner = globalThis.fetch;
globalThis.fetch = async (input, init) => {
    const response = await inner(input, init);
    const url = typeof input === 'string' ? input : (input && input.url) || '';

    if (!/_framework\//.test(url) || !response.body || !response.ok)
        return response;

    const reader = response.body.getReader();
    const counted = new ReadableStream({
        async pull(controller) {
            const { done, value } = await reader.read();
            if (done) {
                controller.close();
                return;
            }
            loaded += value.byteLength;
            report();
            controller.enqueue(value);
        },
        cancel: reason => reader.cancel(reason)
    });

    return new Response(counted, {
        status: response.status,
        statusText: response.statusText,
        headers: response.headers
    });
};

// The denominator, written at publish time by tools/build-demo.sh. A missing or unreadable boot.json is
// not worth failing over - the bar then shows megabytes downloaded instead of a fraction of them.
try {
    const measured = await inner('./boot.json', { cache: 'no-cache' });
    if (measured.ok)
        total = (await measured.json()).bytes || 0;
} catch {
    total = 0;
}

report();

try {
    const { dotnet } = await import('./_framework/dotnet.js');

    const runtime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();

    line.textContent = 'Starting…';
    bar.style.width = '100%';

    await runtime.runMain(runtime.getConfig().mainAssemblyName, [globalThis.location.href]);
} catch (error) {
    failed('This browser could not start the demo.',
        `It needs WebAssembly. ${(error && error.message) || error || ''}`);
    console.error('[AvaSchematic.Demo] the runtime did not start', error);
}
