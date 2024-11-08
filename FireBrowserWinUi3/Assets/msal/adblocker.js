// adblocker.js
(function() {
    'use strict';

    // Common ad class prefixes, ids, and attributes
    const adClasses = [
        'ad-', 'ads-', 'advert-', 'advertisement', 'adsbygoogle', 'googlead'
    ];
    const adIds = [
        'ad', 'ads', 'ad_banner', 'ad-container', 'advertisement', 'google_ads'
    ];
    const adAttributes = [
        'data-ad', 'data-ads', 'data-google-ad', 'data-ad-client', 'data-ad-slot'
    ];

    // Helper function to remove elements
    function removeElements(elements) {
        elements.forEach(element => {
            if (element && element.parentElement) {
                console.dir(element);
                element.parentElement.removeChild(element);
            }
        });
    }

    // Remove ad elements by class names
    adClasses.forEach(adClass => {
        removeElements(document.querySelectorAll(`[class*="${adClass}"]`));
    });

    // Remove ad elements by IDs
    adIds.forEach(adId => {
        removeElements(document.querySelectorAll(`#${adId}`));
    });

    // Remove ad elements by attributes
    adAttributes.forEach(adAttr => {
        removeElements(document.querySelectorAll(`[${adAttr}]`));
    });

    // Additional selectors for common ad elements
    const additionalSelectors = [
        'iframe[src*="ads"]', // Block iframes containing "ads" in the src attribute
        'div[id^="ad_"]', // Block divs with ids starting with "ad_"
    ];

    // Remove additional ad elements
    additionalSelectors.forEach(selector => {
        removeElements(document.querySelectorAll(selector));
    });

    console.log('Adblocker script executed');
})();
