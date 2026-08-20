/*
 * Translations for the landing page — and, today, the shape of them rather than any.
 *
 * The rule this follows is AvaDevTools': one file holds every string on the site, so translating a page is
 * editing a data file rather than forking its markup. The rule it also follows is this site's own: every
 * page works with no JavaScript at all, which is why the reference is plain files that open from a file://
 * URL.
 *
 * Those two would collide if the English were in here, because then a reader with no script would get an
 * empty page. So the English lives in the markup, where it renders whatever happens, and every translatable
 * node carries a `data-i18n` key naming its entry here. A language other than English replaces the text; a
 * missing key leaves the English in place, which is the right failure for a half-finished translation.
 *
 * To add one: copy the key list below, translate the values, and add it as `uk: { … }`. The language button
 * appears in the header by itself as soon as there is something to switch to — site.js builds it from what
 * it finds in here, so nothing else changes. Keys are `section.element`, and a key ending in `.html` holds
 * markup rather than text.
 *
 *   nav.*        the header links
 *   fb.*         the feedback popup
 *   hero.*       the first screen
 *   story.*      the thirteen slides — `.h` is a heading, `.p` the paragraph under it
 *   concepts.*   the diagram section and the three guide cards
 *   book.*       the invitation to the book
 *   features.*   the six cards
 *   measured.*   the table
 *   start.*      the quick start
 *   limits.*     what it does not do
 *   footer.*
 */

window.I18N = {
  // en is deliberately absent: the English is the markup.
};
