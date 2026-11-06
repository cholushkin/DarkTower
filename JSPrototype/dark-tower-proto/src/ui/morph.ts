// Patches `root`'s DOM in place to match `html` instead of replacing it outright.
//
// The old `innerHTML = html` approach tore down and rebuilt every element on every render,
// including the one the player's mouse was currently pressed on — a periodic re-render landing
// between mousedown and mouseup swapped the button/checkbox out from under the gesture, so the
// browser never fired the click/change event at all. Morphing keeps existing elements (and their
// focus, scroll position, and pending native input state) alive across renders and only touches
// attributes/text/children that actually changed.
//
// Matching reuses the app's own `data-*` action attributes and `id` attributes as identity keys —
// no new convention is introduced. Siblings without either fall back to positional matching,
// which is safe here since list ordering only ever comes from stable config/state iteration order.

export function morph(root: Element, html: string): void {
    const template = document.createElement("template");
    template.innerHTML = html;
    morphChildren(root, template.content);
}

function elementKey(node: Node): string | null {
    if (node.nodeType !== Node.ELEMENT_NODE) return null;
    const el = node as Element;
    const id = el.getAttribute("id");
    const dataAttrs = el.getAttributeNames()
        .filter(n => n.startsWith("data-"))
        .sort()
        .map(n => `${n}=${el.getAttribute(n)}`)
        .join("&");
    if (!id && !dataAttrs) return null;
    return `${el.tagName}|${id ?? ""}|${dataAttrs}`;
}

function sameNodeType(a: Node, b: Node): boolean {
    if (a.nodeType !== b.nodeType) return false;
    return a.nodeType !== Node.ELEMENT_NODE || (a as Element).tagName === (b as Element).tagName;
}

function morphChildren(oldParent: Node, newParent: Node): void {
    const oldNodes = Array.from(oldParent.childNodes);
    const newNodes = Array.from(newParent.childNodes);

    const oldByKey = new Map<string, Node>();
    for (const n of oldNodes) {
        const key = elementKey(n);
        if (key !== null && !oldByKey.has(key)) oldByKey.set(key, n);
    }

    const used = new Set<Node>();
    let cursor: Node | null = oldParent.firstChild;

    for (const newNode of newNodes) {
        const key = elementKey(newNode);
        let matched = key !== null ? oldByKey.get(key) ?? null : null;
        if (matched && used.has(matched)) matched = null;

        if (!matched && cursor && !used.has(cursor) && elementKey(cursor) === null && sameNodeType(cursor, newNode)) {
            matched = cursor;
        }

        if (matched) {
            used.add(matched);
            if (matched !== cursor) oldParent.insertBefore(matched, cursor);
            cursor = matched.nextSibling;
            morphNode(matched, newNode);
        } else {
            oldParent.insertBefore(newNode.cloneNode(true), cursor);
        }
    }

    for (const n of oldNodes) {
        if (!used.has(n) && n.parentNode === oldParent) oldParent.removeChild(n);
    }
}

function morphNode(oldNode: Node, newNode: Node): void {
    if (oldNode.nodeType === Node.TEXT_NODE || oldNode.nodeType === Node.COMMENT_NODE) {
        if (oldNode.textContent !== newNode.textContent) oldNode.textContent = newNode.textContent;
        return;
    }
    if (oldNode.nodeType !== Node.ELEMENT_NODE) return;

    const oldEl = oldNode as Element;
    const newEl = newNode as Element;
    if (oldEl.tagName !== newEl.tagName) {
        oldEl.replaceWith(newEl.cloneNode(true));
        return;
    }
    syncAttributes(oldEl, newEl);
    syncFormState(oldEl, newEl);
    morphChildren(oldEl, newEl);
}

// Attributes whose live DOM state can diverge from the markup through the user's own direct
// interaction with the element (checking a box, collapsing a <details>) rather than through our
// render state. Skip these in the generic sync so a periodic re-render can't clobber them; each is
// handled on its own terms (or deliberately left alone) in syncFormState.
const USER_OWNED_ATTRS: Partial<Record<string, string>> = { INPUT: "checked", DETAILS: "open" };

function syncAttributes(oldEl: Element, newEl: Element): void {
    const skip = USER_OWNED_ATTRS[oldEl.tagName];
    for (const name of oldEl.getAttributeNames()) {
        if (name === skip) continue;
        if (!newEl.hasAttribute(name)) oldEl.removeAttribute(name);
    }
    for (const name of newEl.getAttributeNames()) {
        if (name === skip) continue;
        const value = newEl.getAttribute(name)!;
        if (oldEl.getAttribute(name) !== value) oldEl.setAttribute(name, value);
    }
}

function syncFormState(oldEl: Element, newEl: Element): void {
    if (!(oldEl instanceof HTMLInputElement) || !(newEl instanceof HTMLInputElement)) return;
    if (oldEl === document.activeElement) return; // never stomp the field the user is mid-interaction with
    const wantChecked = newEl.hasAttribute("checked");
    if (oldEl.checked !== wantChecked) oldEl.checked = wantChecked;
}
