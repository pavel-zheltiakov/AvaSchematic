// The two things on this site that need script: the story slider, and the release list.
//
// Progressive enhancement rather than components. Without JavaScript the slides stack and every image and
// caption is still readable, and the releases page still says where the release history is. No dependencies,
// no build step — the whole site is files a static host can serve.

// ---- the story slider ------------------------------------------------------------------------------------
//
// The markup is nine <figure>s and the stylesheet shows all of them. This adds a class that collapses them to
// one at a time and wires up the controls, so a reader with no script gets the same nine pictures and
// captions in the same order — just scrolled instead of clicked.

(function () {
    'use strict';

    document.querySelectorAll('[data-slider]').forEach(function (slider) {
        var slides = Array.prototype.slice.call(slider.querySelectorAll('.slide'));
        if (slides.length < 2) return;

        var dots = slider.querySelector('[data-dots]');
        var prev = slider.querySelector('[data-prev]');
        var next = slider.querySelector('[data-next]');
        var index = 0;
        var buttons = [];

        slides.forEach(function (slide, i) {
            var heading = slide.querySelector('h3');
            var label = heading ? heading.textContent.trim() : 'Slide ' + (i + 1);

            var button = document.createElement('button');
            button.type = 'button';
            button.setAttribute('role', 'tab');
            // The dot shows its number; the accessible name is the slide's own heading, which is far more
            // use to a screen reader than "3 of 8".
            button.textContent = String(i + 1);
            button.setAttribute('aria-label', label);
            button.addEventListener('click', function () { show(i); });

            dots.appendChild(button);
            buttons.push(button);
        });

        function show(target) {
            index = (target + slides.length) % slides.length;

            slides.forEach(function (slide, i) {
                var current = i === index;
                slide.classList.toggle('current', current);
                // Hidden slides are removed from the accessibility tree as well as from the layout, so a
                // screen reader is not read all eight captions in a row.
                slide.setAttribute('aria-hidden', current ? 'false' : 'true');
            });

            buttons.forEach(function (button, i) {
                button.setAttribute('aria-selected', i === index ? 'true' : 'false');
            });
        }

        prev.addEventListener('click', function () { show(index - 1); });
        next.addEventListener('click', function () { show(index + 1); });

        // Arrow keys, but only once the slider has been interacted with or scrolled to — binding them to the
        // document unconditionally would steal them from someone reading the rest of the page.
        slider.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowLeft') { show(index - 1); event.preventDefault(); }
            if (event.key === 'ArrowRight') { show(index + 1); event.preventDefault(); }
        });

        // Swipe, for the platform most likely to be reading this on a phone.
        var startX = null;
        slider.addEventListener('touchstart', function (event) {
            startX = event.touches[0].clientX;
        }, { passive: true });

        slider.addEventListener('touchend', function (event) {
            if (startX === null) return;
            var dx = event.changedTouches[0].clientX - startX;
            if (Math.abs(dx) > 40) show(index + (dx < 0 ? 1 : -1));
            startX = null;
        }, { passive: true });

        slider.classList.add('live');
        slider.setAttribute('tabindex', '0');
        show(0);
    });
})();

// ---- copy a command --------------------------------------------------------------------------------------
//
// Every [data-copy] button copies the <code> beside it. The command is in the markup either way, so a reader
// with no script — or on a page served over plain HTTP, where the clipboard API is not available at all —
// still has something they can select and copy by hand. That is the whole fallback, and it needs no code.

(function () {
    'use strict';

    document.querySelectorAll('[data-copy]').forEach(function (button) {
        var source = button.parentNode.querySelector('code');
        if (!source) return;

        var timer = null;

        button.addEventListener('click', function () {
            var text = source.textContent;

            // execCommand is the fallback rather than the plan: navigator.clipboard is missing on http://
            // and in a few older browsers, and a copy button that silently does nothing is worse than one
            // that is not there.
            var written = navigator.clipboard
                ? navigator.clipboard.writeText(text)
                : new Promise(function (resolve, reject) {
                    var field = document.createElement('textarea');
                    field.value = text;
                    field.setAttribute('readonly', '');
                    field.style.cssText = 'position:fixed;top:-1000px';
                    document.body.appendChild(field);
                    field.select();
                    var ok = document.execCommand && document.execCommand('copy');
                    document.body.removeChild(field);
                    ok ? resolve() : reject();
                });

            written.then(function () {
                button.setAttribute('data-copied', '');
                clearTimeout(timer);
                timer = setTimeout(function () { button.removeAttribute('data-copied'); }, 1400);
            }).catch(function () { /* nothing to say; the text is on the page to be selected */ });
        });
    });
})();

// ---- language --------------------------------------------------------------------------------------------
//
// The English is in the markup, so a page with no script — and a page with no translations, which is every
// page today — is already correct and this does nothing at all. When i18n.js grows a language, every node
// carrying a data-i18n key can be replaced from it, and the switcher builds itself.
//
// A key that a translation has not covered leaves the English where it is, which is the right way for a
// half-finished translation to fail.

(function () {
    'use strict';

    var catalogue = window.I18N || {};
    var languages = Object.keys(catalogue);
    if (!languages.length) return;

    var stored = null;
    try { stored = localStorage.getItem('lang'); } catch (e) { /* private browsing */ }

    var wanted = new URLSearchParams(location.search).get('lang') ||
                 stored ||
                 (navigator.language || 'en').slice(0, 2).toLowerCase();

    function apply(lang) {
        var strings = catalogue[lang];
        document.documentElement.lang = strings ? lang : 'en';

        document.querySelectorAll('[data-i18n]').forEach(function (node) {
            var value = strings && strings[node.dataset.i18n];
            if (value) node.textContent = value;
        });

        document.querySelectorAll('[data-i18n-html]').forEach(function (node) {
            var value = strings && strings[node.dataset.i18nHtml];
            if (value) node.innerHTML = value;
        });

        document.querySelectorAll('[data-setlang]').forEach(function (link) {
            link.classList.toggle('on', link.dataset.setlang === lang);
        });
    }

    function choose(lang) {
        try { localStorage.setItem('lang', lang); } catch (e) { /* private browsing */ }
        apply(lang);
    }

    // The switcher goes where AvaDevTools puts it: with the outbound links, on the right.
    var tools = document.querySelector('.bar .tools');
    if (tools) {
        var names = { en: 'English', uk: 'Українська', zh: '中文' };
        var box = document.createElement('span');
        box.className = 'langs';

        ['en'].concat(languages.filter(function (l) { return l !== 'en'; })).forEach(function (lang) {
            var link = document.createElement('a');
            link.href = '#';
            link.lang = lang;
            link.dataset.setlang = lang;
            link.textContent = names[lang] || lang.toUpperCase();
            link.addEventListener('click', function (e) { e.preventDefault(); choose(lang); });
            box.appendChild(link);
        });

        tools.insertBefore(box, tools.firstChild);
    }

    apply(catalogue[wanted] ? wanted : 'en');
})();

// ---- the release list ------------------------------------------------------------------------------------
//
// Fed live from the public repository's releases, so the page is never a second list of versions somebody has
// to remember to update. The release being prepared right now has no tag yet and therefore no GitHub release,
// so releases-local.js carries it until the tag is pushed; an entry whose tag already exists there is dropped
// rather than shown twice.
//
// The bodies are the release notes, which are Markdown, so there is a small renderer here. It handles exactly
// what RELEASE_NOTES.md uses — headings, paragraphs, bold, inline code, tables, links — and nothing else. A
// Markdown library would be larger than this whole site.

(function () {
    'use strict';

    var host = document.getElementById('releases');
    if (!host) return;

    var REPOSITORY = 'pavel-zheltiakov/AvaSchematic';
    var GITHUB = 'https://github.com/' + REPOSITORY + '/releases';

    function escape(text) {
        return String(text)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function inline(text) {
        return escape(text)
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            .replace(/(^|[\s(])\*([^*\n]+)\*/g, '$1<em>$2</em>')
            .replace(/\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g, '<a href="$2">$1</a>')
            .replace(/(^|[\s>])(https?:\/\/[^\s<)]+)/g, '$1<a href="$2">$2</a>');
    }

    function cells(row) {
        return row.replace(/^\s*\|/, '').replace(/\|\s*$/, '').split('|').map(function (c) { return c.trim(); });
    }

    // Whether a line is the continuation of the bullet above it rather than the start of anything.
    function continues(line) {
        var trimmed = line.trim();
        return trimmed !== '' && !/^[-*]\s+/.test(trimmed) && !/^#{1,3}\s+/.test(trimmed) &&
            trimmed.charAt(0) !== '|' && trimmed !== '---' && !/^(Install|Docs|NuGet):/.test(trimmed);
    }

    function render(markdown) {
        var lines = String(markdown).replace(/\r\n/g, '\n').split('\n');
        var html = '';
        var paragraph = [];

        function flush() {
            if (!paragraph.length) return;
            html += '<p>' + inline(paragraph.join(' ')) + '</p>';
            paragraph = [];
        }

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            var trimmed = line.trim();

            // The footer the release workflow appends is already on the page as a button and three links.
            if (/^(Install|Docs|NuGet):/.test(trimmed) || trimmed === '---') { flush(); continue; }

            if (!trimmed) { flush(); continue; }

            if (/^###\s+/.test(trimmed)) { flush(); html += '<h4>' + inline(trimmed.slice(4)) + '</h4>'; continue; }
            if (/^##\s+/.test(trimmed)) { flush(); html += '<h3>' + inline(trimmed.slice(3)) + '</h3>'; continue; }
            if (/^#\s+/.test(trimmed)) { flush(); html += '<h3>' + inline(trimmed.slice(2)) + '</h3>'; continue; }

            // A table: a header row, an alignment row, then body rows until the block ends.
            if (trimmed.charAt(0) === '|' && /^\|[\s:|-]+\|$/.test((lines[i + 1] || '').trim())) {
                flush();
                html += '<div class="scroller"><table><thead><tr>' + cells(trimmed).map(function (c) {
                    return '<th>' + inline(c) + '</th>';
                }).join('') + '</tr></thead><tbody>';

                for (i += 2; i < lines.length && lines[i].trim().charAt(0) === '|'; i++)
                    html += '<tr>' + cells(lines[i].trim()).map(function (c) {
                        return '<td>' + inline(c) + '</td>';
                    }).join('') + '</tr>';

                i--;
                html += '</tbody></table></div>';
                continue;
            }

            if (/^[-*]\s+/.test(trimmed)) {
                flush();
                html += '<ul>';
                for (; i < lines.length && /^[-*]\s+/.test(lines[i].trim()); i++) {
                    var item = lines[i].trim().slice(2);

                    // A bullet wrapped over several lines is one bullet — markdown's lazy continuation.
                    // Worth the four lines: the release notes are written to a column width like every
                    // other file here, and without this every wrapped bullet ended its own list and came
                    // out as a paragraph underneath it.
                    while (i + 1 < lines.length && continues(lines[i + 1]))
                        item += ' ' + lines[++i].trim();

                    html += '<li>' + inline(item) + '</li>';
                }
                i--;
                html += '</ul>';
                continue;
            }

            paragraph.push(trimmed);
        }

        flush();
        return html;
    }

    function card(release) {
        var version = (release.tag_name || '').replace(/^v/, '');
        // Release dates are date-only stamps at UTC midnight; formatting them in local time shows the
        // previous day everywhere west of UTC.
        var date = release.published_at
            ? new Date(release.published_at).toLocaleDateString('en-GB', {
                timeZone: 'UTC', year: 'numeric', month: 'long', day: 'numeric'
            })
            : '';

        var prerelease = release.prerelease || /-/.test(version);

        return '<article class="rel">' +
            '<h3>' + escape(release.name || release.tag_name) +
            (prerelease ? ' <span class="rel-pre">prerelease</span>' : '') +
            (date ? ' <span class="rel-date">' + escape(date) + '</span>' : '') + '</h3>' +
            '<pre class="install"><code>dotnet add package AvaSchematic --version ' + escape(version) +
            '</code></pre>' +
            '<div class="rel-body">' + render(release.body || '') + '</div>' +
            '<p class="rel-links"><a href="api/index.html">Documentation</a> · ' +
            '<a href="' + escape(release.html_url || GITHUB) + '">GitHub</a> · ' +
            '<a href="https://www.nuget.org/packages/AvaSchematic/' + escape(version) + '">NuGet</a></p>' +
            '</article>';
    }

    function note(text) {
        return '<p class="footnote">' + text + ' Read it on <a href="' + GITHUB + '">GitHub</a>.</p>';
    }

    fetch('https://api.github.com/repos/' + REPOSITORY + '/releases')
        .then(function (response) { return response.ok ? response.json() : null; })
        .catch(function () { return null; })
        .then(function (fetched) {
            var published = Array.isArray(fetched) ? fetched : [];
            var pending = (window.LOCAL_RELEASES || []).filter(function (local) {
                return !published.some(function (r) { return r.tag_name === local.tag_name; });
            });

            var all = pending.concat(published);

            // A failed fetch with a pending entry still needs the notice — otherwise the page presents the
            // unreleased version as the only one there has ever been.
            var warning = fetched === null ? note('The release history could not be loaded.') : '';

            host.innerHTML = all.length
                ? warning + all.map(card).join('')
                : warning || note('No releases yet — the first one is on its way.');
        });
})();
