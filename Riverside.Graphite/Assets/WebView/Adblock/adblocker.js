(function () {
	var selectors = [
		'#sidebar-wrap', '#advert', '#xrail', '#middle-article-advert-container',
		'#sponsored-recommendations', '#around-the-web', '#sponsored-recommendations',
		'#taboola-content', '#taboola-below-taboola-native-thumbnails', '#inarticle_wrapper_div',
		'#rc-row-container', '#ads', '#at-share-dock', '#at4-share', '#at4-follow', '#right-ads-rail',
		'div#ad-interstitial', 'div#advert-article', 'div#ac-lre-player-ph',
		'.ad', '.avert', '.avert__wrapper', '.middle-banner-ad', '.advertisement',
		'.GoogleActiveViewClass', '.advert', '.cns-ads-stage', '.teads-inread', '.ad-banner',
		'.ad-anchored', '.js_shelf_ads', '.ad-slot', '.antenna', '.xrail-content',
		'.advertisement__leaderboard', '.ad-leaderboard', '.trc_rbox_outer', '.ks-recommended',
		'.article-da', 'div.sponsored-stories-component', 'div.addthis-smartlayers',
		'div.article-adsponsor', 'div.signin-prompt', 'div.article-bumper', 'div.video-placeholder',
		'div.top-ad-container', 'div.header-ad', 'div.ad-unit', 'div.demo-block', 'div.OUTBRAIN',
		'div.ob-widget', 'div.nwsrm-wrapper', 'div.announcementBar', 'div.partner-resources-block',
		'div.arrow-down', 'div.m-ad', 'div.story-interrupt', 'div.taboola-recommended',
		'div.ad-cluster-container', 'div.ctx-sidebar', 'div.incognito-modal', '.OUTBRAIN', '.subscribe-button',
		'.ads9', '.leaderboards', '.GoogleActiveViewElement', '.mpu-container', '.ad-300x600', '.tf-ad-block',
		'.sidebar-ads-holder-top', '.ads-one', '.FullPageModal__scroller',
		'.content-ads-holder', '.widget-area', '.social-buttons', '.ac-player-ph',
		'script', 'iframe', 'video', 'aside#sponsored-recommendations', 'aside[role=banner]', 'aside',
		'amp-ad', 'span[id^=ad_is_]', 'div[class*=indianapolis-optin]', 'div[id^=google_ads_iframe]',
		'div[data-google-query-id]', 'section[data-response]', 'ins.adsbygoogle', 'div[data-google-query-id]',
		'div[data-test-id=fullPageSignupModal]', 'div[data-test-id=giftWrap]'];

	function findAllShadowRoots(root = document) {
		const elementsWithShadowRoot = [];
		const traverse = (node) => {
			if (node.shadowRoot) {
				elementsWithShadowRoot.push(node);
			}
			node.childNodes.forEach(child => {
				if (child.nodeType === Node.ELEMENT_NODE) {
					traverse(child);
				}
			});
		}
		traverse(root);
		return elementsWithShadowRoot;
	}

	function graphiteRemoveAds() {
		let _graphiteCollection = new Array();
		for (let i in selectors) {
			let nodesList = document.querySelectorAll(selectors[i]);
			for (let i = 0; i < nodesList.length; i++) {
				let el = nodesList[i];
				if (el && el.parentNode) {
					var out = {
						'message': 'Ad removal by graphite browser',
						'element': el
					}
					_graphiteCollection.push(out);
					el.parentNode.removeChild(el);
				}
			}
		}
		console.dir(_graphiteCollection);
		graphiteShadow();
	}

	function matchExcluded(element, selectors) { return selectors.some(selector => { return element.parentNode.querySelectorAll(selector).includes(element); }); }
	function graphiteShadow() {

		const shadowRootElements = findAllShadowRoots();

		let _graphiteCollection = new Array();
		selectors.map(selector => document.querySelector(selector)).filter(el => el !== null);


		let nodesList = shadowRootElements;
		let shadowChild = [];
		for (let a in nodesList) {
			let el = nodesList[a];
			const traverse = (node) => {

				node.childNodes.forEach(child => {
					if (child.nodeType === Node.ELEMENT_NODE) {
						traverse(child);
					}
					else {
						shadowChild.push(child);
					}

				});
			};

			if (el.shadowRoot) {
				if (el.shadowRoot.childNodes)
					el.shadowRoot.childNodes.forEach(child => traverse(child));
			}
		}

		// ie: if a list doesn't have a function assign a function from another type.. 
		if (NodeList.prototype.includes === undefined) { NodeList.prototype.includes = Array.prototype.includes; }

		var shadowMatches = Array.from(shadowChild).filter(element => matchExcluded(element, selectors));

		for (let i = 0; i < shadowMatches.length; i++) {
			let el = shadowChild[i];
			if (el && el.parentNode) {
				var out = {
					'message': 'Ad removal by graphite browser',
					'element': el
				}
				_graphiteCollection.push(out);
				el.parentNode.removeChild(el);
			}
		}


		console.dir(_graphiteCollection);

	}

	graphiteRemoveAds();

	new MutationObserver(graphiteRemoveAds).observe(document.body, {
		childList: true,
		subtree: true,
	});

})();