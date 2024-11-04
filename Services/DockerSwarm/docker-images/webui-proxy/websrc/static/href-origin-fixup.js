function fixHref(anchor) {
    const upstreamHostFromPathRegex = /^\/([a-zA-Z0-9-_\.]+:\d+?)(?:\/|$)/
    if (anchor.href && /^https?:$/.test(new URL(anchor.href).protocol)) {
        let currentURL = new URL(anchor.href)
        if (currentURL.origin !== window.origin) {
            let newURL = `${window.origin}/${currentURL.host}${currentURL.pathname}${currentURL.search}`
            anchor.href = newURL
            console.log(
                'Fixed href of ',
                anchor,
                ` from ${currentURL} to ${newURL}`
            )
        } else if (!upstreamHostFromPathRegex.test(currentURL.pathname)) {
            let newURL = `${window.origin}/${
                upstreamHostFromPathRegex.exec(window.location.pathname)[1]
            }${currentURL.pathname}`
            anchor.href = newURL
            console.log(
                'Fixed href of ',
                anchor,
                ` from ${currentURL} to ${newURL}`
            )
        } else {
            console.log(
                'Not fixing href of ',
                anchor,
                ` from ${currentURL} - origin and path ok`
            )
        }
    } else {
        console.log('Not fixing href of ', anchor, ' - non http href')
    }
}

// fix href on anchor elements when they are added to DOM (attr change callback not fired if they already have href set)
// also search children of appended elements for anchor elems because mutations aren't reported for the appended elem's children
new MutationObserver((mutationList, observer) => {
    for (const mutation of mutationList) {
        mutation.addedNodes.forEach((node) => {
            ;[
                ...(node.tagName === 'A' ? [node] : []),
                ...(node.querySelectorAll?.('a') ?? []),
            ].forEach((node) => {
                fixHref(node)
            })
        })
    }
}).observe(document, {
    childList: true,
    subtree: true,
})

// fix href on anchor elems if they change dynamically
new MutationObserver((mutationList, observer) => {
    for (const mutation of mutationList) {
        if (mutation.target.tagName === 'A') {
            fixHref(mutation.target)
        }
    }
}).observe(document, {
    attributes: true,
    attributeFilter: ['href'],
    subtree: true,
})
