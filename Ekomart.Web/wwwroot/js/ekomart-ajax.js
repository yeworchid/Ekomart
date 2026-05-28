(function () {
  'use strict';

  const ajaxHeaders = {
    'X-Requested-With': 'XMLHttpRequest'
  };
  const deliveryStorageKey = 'ekomartDeliveryMethod';
  let localizationConfig;

  document.addEventListener('DOMContentLoaded', function () {
    initCategoryDropdowns();
    initQuantityControls();
    initCheckoutValidation();
    hydrateHomeProductCards();
    initStorefrontLocalization();
    initAccountLinks();
    initPriceFilterLabels();
    initStandaloneSearch();
    loadCartCount();
    startOrderHub();
  });

  document.addEventListener('click', function (event) {
    const target = getEventElement(event.target);
    if (!target) {
      return;
    }

    const quantityButton = target.closest('.quantity-edit .button');
    if (!quantityButton) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    updateQuantityInput(quantityButton);
  }, true);

  document.addEventListener('submit', function (event) {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
      return;
    }

    if (form.id === 'checkout-form' && !validateCheckoutForm(form)) {
      event.preventDefault();
      return;
    }

    if (form.matches('[data-catalog-filter]')) {
      event.preventDefault();
      submitCatalogFilter(form);
      return;
    }

    if (form.matches('[data-ajax-cart]')) {
      event.preventDefault();
      submitCartForm(form, event.submitter);
      return;
    }

    if (form.matches('[data-ajax-subscription]')) {
      event.preventDefault();
      submitSubscriptionForm(form, event.submitter);
      return;
    }

    if (form.matches('[data-ajax-order-status]')) {
      event.preventDefault();
      submitOrderStatusForm(form, event.submitter);
    }
  });

  document.addEventListener('click', function (event) {
    const target = getEventElement(event.target);
    if (!target) {
      return;
    }

    if (handleCheckoutChoiceClick(event, target)) {
      return;
    }

    const checkoutSubmitter = target.closest(
      'button[form="checkout-form"], input[type="submit"][form="checkout-form"], #checkout-form button[type="submit"], #checkout-form input[type="submit"]'
    );
    if (checkoutSubmitter) {
      const form = document.getElementById('checkout-form');
      if (form && !validateCheckoutForm(form)) {
        event.preventDefault();
        event.stopPropagation();
        return;
      }
    }

    const cartCheckoutLink = target.closest('[data-cart-checkout-link]');
    if (cartCheckoutLink) {
      handleCartCheckoutClick(event, cartCheckoutLink);
      return;
    }

    const templateCartButton = target.closest('[data-template-cart-add]');
    if (templateCartButton) {
      event.preventDefault();
      submitTemplateCartAdd(templateCartButton);
      return;
    }

    const link = target.closest('a');
    if (!link || !isCatalogLink(link)) {
      return;
    }

    event.preventDefault();
    loadCatalog(link.href, true);
  });

  window.addEventListener('popstate', function () {
    if (document.querySelector('[data-catalog-results]')) {
      loadCatalog(window.location.href, false);
    }
  });

  async function submitCatalogFilter(form) {
    const isPriceFilter = form.classList.contains('price-input-area');
    const url = isPriceFilter
      ? new URL(window.location.href)
      : new URL(form.action, window.location.origin);
    const formData = new FormData(form);

    if (!isPriceFilter) {
      for (const key of Array.from(url.searchParams.keys())) {
        url.searchParams.delete(key);
      }
    }

    for (const [key, value] of formData.entries()) {
      if (isPriceFilter && !['MinPrice', 'MaxPrice', 'PageSize'].includes(key)) {
        continue;
      }

      const stringValue = String(value).trim();
      if (stringValue.length > 0) {
        url.searchParams.set(key, stringValue);
      }
    }

    url.searchParams.delete('PageNumber');
    await loadCatalog(url.toString(), true);
  }

  async function loadCatalog(url, pushState) {
    const container = document.querySelector('[data-catalog-results]');
    if (!container) {
      return;
    }

    container.setAttribute('aria-busy', 'true');

    try {
      const response = await fetch(url, {
        headers: ajaxHeaders,
        credentials: 'same-origin'
      });

      if (!response.ok) {
        throw new Error(t('Could not update catalog.'));
      }

      container.innerHTML = await response.text();
      if (pushState) {
        history.pushState({}, '', url);
      }
      initQuantityControls(container);
      localizeStaticTexts(container);
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      container.removeAttribute('aria-busy');
      initPriceFilterLabels();
    }
  }

  async function submitCartForm(form, submitter) {
    if (!isUserAuthenticated()) {
      showNotice(t('Login is required.'), 'warning');
      return;
    }

    setSubmitterState(submitter, true);

    try {
      const data = await postFormJson(form);
      updateCartUi(data, form);
      showNotice(data.message || t('Cart updated.'), 'success');
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      setSubmitterState(submitter, false);
    }
  }

  async function submitTemplateCartAdd(button) {
    if (button.getAttribute('aria-disabled') === 'true') {
      return;
    }

    if (!isUserAuthenticated()) {
      showNotice(t('Login is required.'), 'warning');
      return;
    }

    const productId = Number(button.dataset.productId);
    if (!productId) {
      return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
      showNotice(t('Could not prepare cart request.'), 'danger');
      return;
    }

    const actionArea = button.closest('.cart-counter-action') || button.closest('.single-shopping-card-one');
    const quantityInput = actionArea?.querySelector('.quantity-edit .input');
    const quantity = clampQuantity(quantityInput ? quantityInput.value : 1, quantityInput);
    const formData = new FormData();
    formData.append('__RequestVerificationToken', token);
    formData.append('productId', String(productId));
    formData.append('quantity', String(quantity));
    formData.append('returnUrl', `${window.location.pathname}${window.location.search}`);

    setSubmitterState(button, true);

    try {
      const data = await postFormDataJson('/Cart/Add', formData);
      updateCartBadges(data.totalQuantity);
      showNotice(data.message || t('Product added to cart.'), 'success');
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      setSubmitterState(button, false);
    }
  }

  async function submitSubscriptionForm(form, submitter) {
    setSubmitterState(submitter, true);

    try {
      const data = await postFormJson(form);
      const details = document.querySelector('[data-current-subscription-details]');
      if (details) {
        details.innerHTML = `${escapeHtml(data.planName)}<br>${escapeHtml(t('Status'))}: ${escapeHtml(data.status)}<br>${escapeHtml(t('Active until'))} ${escapeHtml(data.endsAt)}`;
      }

      showNotice(data.message || t('Subscription updated.'), 'success');
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      setSubmitterState(submitter, false);
    }
  }

  async function submitOrderStatusForm(form, submitter) {
    setSubmitterState(submitter, true);

    try {
      const data = await postFormJson(form);
      const badge = document.querySelector('[data-order-status-badge]');
      if (badge) {
        badge.textContent = data.status;
      }

      showNotice(data.message || t('Order status updated.'), 'success');
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      setSubmitterState(submitter, false);
    }
  }

  async function postFormJson(form) {
    return postFormDataJson(resolveFormAction(form), new FormData(form), form.method || 'POST');
  }

  async function postFormDataJson(url, formData, method) {
    const response = await fetch(url, {
      method: method || 'POST',
      body: formData,
      headers: ajaxHeaders,
      credentials: 'same-origin'
    });

    if (response.redirected && response.url.includes('/Account/Login')) {
      window.location.href = response.url;
      throw new Error(t('Login is required.'));
    }

    const data = await response.json().catch(function () {
      return {};
    });

    if (response.status === 401 && data.loginUrl) {
      window.location.href = data.loginUrl;
      throw new Error(data.message || t('Login is required.'));
    }

    if (!response.ok || data.success === false) {
      throw new Error(data.message || t('Request failed.'));
    }

    return data;
  }

  function resolveFormAction(form) {
    const rawAction = form.getAttribute('action') || form.action || window.location.href;
    const path = new URL(rawAction, window.location.origin).pathname.toLowerCase();

    if (form.matches('[data-ajax-cart]')) {
      if (path.endsWith('/add')) {
        return '/Cart/Add';
      }

      if (path.endsWith('/update')) {
        return '/Cart/Update';
      }

      if (path.endsWith('/remove')) {
        return '/Cart/Remove';
      }
    }

    return new URL(rawAction, window.location.origin).toString();
  }

  async function loadCartCount() {
    if (!isUserAuthenticated() || !document.querySelector('.btn-border-only.cart')) {
      return;
    }

    try {
      const response = await fetch('/Cart/Count', {
        headers: ajaxHeaders,
        credentials: 'same-origin'
      });

      if (!response.ok || response.redirected) {
        return;
      }

      const data = await response.json();
      updateCartBadges(data.totalQuantity);
    } catch {
      // Counter is a progressive enhancement; the page can work without it.
    }
  }

  function updateCartUi(data, form) {
    updateCartBadges(data.totalQuantity);

    document.querySelectorAll('.cart-total-area-start-right h6.price').forEach(function (element) {
      element.textContent = data.itemsTotalText;
    });

    document.querySelectorAll('.cart-total-area-start-right .bold').forEach(function (element) {
      element.textContent = `${data.totalQuantity} ${t('item(s)')}`;
    });

    const productIdInput = form.querySelector('input[name="ProductId"], input[name="productId"]');
    const productId = productIdInput ? Number(productIdInput.value) : null;
    const row = form.closest('.single-cart-area-list.main');
    const path = new URL(form.action, window.location.origin).pathname.toLowerCase();

    if (row && path.endsWith('/remove')) {
      row.remove();
    }

    if (row && path.endsWith('/update') && productId) {
      const item = data.items.find(function (cartItem) {
        return cartItem.productId === productId;
      });

      const lineTotal = row.querySelector('.subtotal p');
      if (item && lineTotal) {
        lineTotal.textContent = item.lineTotalText;
      }
    }

    if (data.isEmpty) {
      showEmptyCartState();
    }
  }

  function updateCartBadges(totalQuantity) {
    const quantity = Number(totalQuantity) || 0;
    document.querySelectorAll('.btn-border-only.cart .number').forEach(function (badge) {
      badge.textContent = quantity;
    });

    document.querySelectorAll('.shopping-cart-number').forEach(function (heading) {
      heading.textContent = `${t('Shopping Cart')} (${quantity})`;
    });
  }

  function getEventElement(target) {
    if (target instanceof Element) {
      return target;
    }

    return target?.parentElement || null;
  }

  function updateQuantityInput(button) {
    const wrapper = button.closest('.quantity-edit');
    const input = wrapper?.querySelector('.input');
    if (!input) {
      return;
    }

    const currentValue = clampQuantity(input.value, input);
    const isIncrease = button.classList.contains('plus')
      || button.textContent.trim().startsWith('+')
      || !!button.querySelector('.fa-plus, .fa-chevron-up');
    const nextValue = isIncrease ? currentValue + 1 : currentValue - 1;
    input.value = clampQuantity(nextValue, input);
    input.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function initQuantityControls(root) {
    const scope = root || document;
    scope.querySelectorAll('.quantity-edit').forEach(normalizeQuantityControl);
    scope.querySelectorAll('.quantity-edit .input').forEach(function (input) {
      input.setAttribute('inputmode', 'numeric');
      input.setAttribute('autocomplete', 'off');

      if (input.getAttribute('type') === 'number') {
        input.setAttribute('type', 'text');
      }

      input.value = clampQuantity(input.value, input);

      if (input.dataset.quantityControlReady) {
        return;
      }

      input.addEventListener('change', function () {
        input.value = clampQuantity(input.value, input);
        const cartQuantityForm = input.closest('form[data-cart-quantity-form]');
        if (cartQuantityForm) {
          submitCartForm(cartQuantityForm, null);
        }
      });
      input.addEventListener('blur', function () {
        input.value = clampQuantity(input.value, input);
      });
      input.dataset.quantityControlReady = 'true';
    });
  }

  function normalizeQuantityControl(wrapper) {
    const input = wrapper.querySelector('.input');
    if (!input) {
      return;
    }

    wrapper.classList.add('quantity-control');

    let buttons = Array.from(wrapper.querySelectorAll('.button'));
    let minusButton = buttons.find(isDecreaseButton);
    let plusButton = buttons.find(isIncreaseButton);

    if (!minusButton) {
      minusButton = document.createElement('button');
      minusButton.className = 'button minus';
    }

    if (!plusButton) {
      plusButton = document.createElement('button');
      plusButton.className = 'button plus';
    }

    prepareQuantityButton(minusButton, 'minus', '-', 'Decrease quantity');
    prepareQuantityButton(plusButton, 'plus', '+', 'Increase quantity');

    wrapper.insertBefore(minusButton, input);
    input.insertAdjacentElement('afterend', plusButton);

    wrapper.querySelectorAll('.button-wrapper-action').forEach(function (buttonWrapper) {
      if (buttonWrapper.children.length === 0) {
        buttonWrapper.remove();
      }
    });
  }

  function prepareQuantityButton(button, className, text, label) {
    button.type = 'button';
    button.classList.add('button', className);
    button.textContent = text;
    button.setAttribute('aria-label', label);
  }

  function isDecreaseButton(button) {
    return button.classList.contains('minus')
      || button.textContent.trim().startsWith('-')
      || !!button.querySelector('.fa-minus, .fal.fa-minus, .fa-chevron-down');
  }

  function isIncreaseButton(button) {
    return button.classList.contains('plus')
      || button.textContent.trim().startsWith('+')
      || !!button.querySelector('.fa-plus, .fal.fa-plus, .fa-chevron-up');
  }

  function clampQuantity(value, input) {
    const parsedValue = Number.parseInt(value, 10);
    const min = Number.parseInt(input?.getAttribute('min') || '1', 10) || 1;
    const maxAttribute = Number.parseInt(input?.getAttribute('max') || '', 10);
    const max = Number.isFinite(maxAttribute) && maxAttribute > 0 ? maxAttribute : Number.MAX_SAFE_INTEGER;
    return Math.min(Math.max(Number.isFinite(parsedValue) ? parsedValue : min, min), max);
  }

  function hydrateHomeProductCards() {
    const source = document.getElementById('home-product-data');
    if (!source || source.dataset.hydrated) {
      return;
    }

    let products;
    try {
      products = JSON.parse(source.textContent || '[]');
    } catch {
      products = [];
    }

    if (!Array.isArray(products) || products.length === 0) {
      return;
    }

    let productIndex = 0;
    document.querySelectorAll('.single-shopping-card-one').forEach(function (card) {
      if (card.querySelector('[data-ajax-cart]') || card.closest('.modal-compare-area-start, .product-details-popup-wrapper:not(.in-shopdetails)')) {
        return;
      }

      const hasProductShape = card.querySelector('.thumbnail-preview, .body-content .title, .cart-counter-action > a.rts-btn');
      if (!hasProductShape) {
        return;
      }

      const product = products[productIndex % products.length];
      productIndex += 1;
      applyProductToCard(card, product);
    });

    source.dataset.hydrated = 'true';
    initQuantityControls();
  }

  function applyProductToCard(card, product) {
    const detailsUrl = `/Catalog/Details?slug=${encodeURIComponent(product.slug)}`;
    card.dataset.templateProductCard = 'true';

    card.querySelectorAll('a[href="/Catalog/Details"], a[href^="/Catalog/Details?"]').forEach(function (link) {
      link.href = detailsUrl;
    });

    const image = card.querySelector('.thumbnail-preview img');
    if (image) {
      image.src = product.image;
      image.alt = product.name;
    }

    const title = card.querySelector('.body-content h4.title');
    if (title) {
      title.textContent = product.name;
    }

    const category = card.querySelector('.body-content .availability');
    if (category) {
      category.textContent = product.category;
    }

    const currentPrice = card.querySelector('.body-content .price-area .current');
    if (currentPrice) {
      currentPrice.textContent = product.price;
    }

    const previousPrice = card.querySelector('.body-content .price-area .previous');
    if (previousPrice) {
      previousPrice.style.display = 'none';
    }

    const badge = card.querySelector('.thumbnail-preview .badge span');
    if (badge) {
      badge.innerHTML = Number(product.stockQuantity) > 0 ? 'In<br>Stock' : 'Sold<br>Out';
    }

    const isAvailable = Number(product.stockQuantity) > 0;
    card.classList.toggle('is-in-stock', isAvailable);
    card.classList.toggle('is-sold-out', !isAvailable);

    const cartAction = ensureTemplateCartAction(card, product);
    const addButton = cartAction?.querySelector('[data-template-cart-add], .rts-btn');
    if (addButton) {
      if (addButton instanceof HTMLAnchorElement) {
        addButton.href = '#';
      }

      addButton.dataset.templateCartAdd = 'true';
      addButton.dataset.productId = product.id;
      addButton.dataset.stockQuantity = product.stockQuantity;
      addButton.classList.toggle('disabled', !isAvailable);
      addButton.setAttribute('aria-disabled', isAvailable ? 'false' : 'true');
      const buttonText = addButton.querySelector('.btn-text');
      if (buttonText) {
        buttonText.textContent = isAvailable ? t('Add To Cart') : t('Sold Out');
      }
    }

    const quantityInput = cartAction?.querySelector('.quantity-edit .input') || card.querySelector('.quantity-edit .input');
    if (quantityInput) {
      quantityInput.type = 'text';
      quantityInput.name = 'quantity';
      quantityInput.min = '1';
      quantityInput.max = String(Math.max(Number(product.stockQuantity) || 0, 1));
      quantityInput.value = clampQuantity(quantityInput.value, quantityInput);
      quantityInput.disabled = !isAvailable;
      quantityInput.closest('.quantity-edit')?.classList.toggle('disabled', !isAvailable);
      quantityInput.closest('.quantity-edit')?.classList.remove('visually-hidden');
    }
  }

  function ensureTemplateCartAction(card, product) {
    let action = card.querySelector('.cart-counter-action');
    const body = card.querySelector('.body-content');

    if (!body) {
      return action;
    }

    if (!action) {
      action = document.createElement('div');
      action.className = 'cart-counter-action';
      action.append(createTemplateQuantityControl(product), createTemplateCartButton());

      const priceArea = body.querySelector('.price-area');
      if (priceArea) {
        priceArea.insertAdjacentElement('afterend', action);
      } else {
        body.appendChild(action);
      }

      return action;
    }

    if (!action.querySelector('.quantity-edit')) {
      action.insertBefore(createTemplateQuantityControl(product), action.firstChild);
    }

    if (!action.querySelector('[data-template-cart-add], .rts-btn')) {
      action.appendChild(createTemplateCartButton());
    }

    return action;
  }

  function createTemplateQuantityControl(product) {
    const wrapper = document.createElement('div');
    const input = document.createElement('input');

    wrapper.className = 'quantity-edit quantity-control';
    input.type = 'text';
    input.name = 'quantity';
    input.className = 'input';
    input.value = '1';
    input.min = '1';
    input.max = String(Math.max(Number(product.stockQuantity) || 0, 1));

    wrapper.appendChild(input);
    return wrapper;
  }

  function createTemplateCartButton() {
    const button = document.createElement('a');
    button.href = '#';
    button.className = 'rts-btn btn-primary radious-sm with-icon';
    button.innerHTML = [
      `<div class="btn-text">${escapeHtml(t('Add To Cart'))}</div>`,
      '<div class="arrow-icon"><i class="fa-regular fa-cart-shopping"></i></div>',
      '<div class="arrow-icon"><i class="fa-regular fa-cart-shopping"></i></div>'
    ].join('');
    return button;
  }

  function initStorefrontLocalization(root) {
    const config = getLocalizationConfig();
    if (config.currentCulture) {
      document.documentElement.lang = config.currentCulture;
    }

    initLanguageSwitchers(config);
    localizeStaticTexts(root || document);
  }

  function initLanguageSwitchers(config) {
    document.querySelectorAll('.nav-h_top.language').forEach(function (menu) {
      const languageItems = Array.from(menu.querySelectorAll('.language-hover'));
      const languageItem = languageItems[0];
      if (!languageItem) {
        return;
      }

      languageItems.slice(1).forEach(function (item) {
        item.hidden = true;
      });

      const currentCulture = config.currentCulture === 'ru' ? 'ru' : 'en';
      const currentLabel = currentCulture === 'ru' ? t('Russian') : t('English');
      const toggle = Array.from(languageItem.children).find(function (child) {
        return child.matches?.('a');
      });

      if (toggle) {
        toggle.textContent = currentLabel;
        toggle.href = '#';
        toggle.setAttribute('role', 'button');
        toggle.setAttribute('aria-haspopup', 'true');
        toggle.setAttribute('aria-expanded', 'false');
      }

      const submenu = languageItem.querySelector('.category-sub-menu');
      if (submenu) {
        submenu.replaceChildren(
          createLanguageMenuItem('en', t('English'), currentCulture === 'en'),
          createLanguageMenuItem('ru', t('Russian'), currentCulture === 'ru')
        );
      }

      if (!menu.dataset.languageSwitcherReady) {
        menu.addEventListener('click', function (event) {
          const target = getEventElement(event.target);
          const clickedToggle = target?.closest('.language-hover > a');
          if (!clickedToggle || !menu.contains(clickedToggle)) {
            return;
          }

          event.preventDefault();
          const currentItem = clickedToggle.closest('.language-hover');
          const isOpen = currentItem.classList.toggle('is-open');
          clickedToggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });

        document.addEventListener('click', function (event) {
          const target = getEventElement(event.target);
          if (target && menu.contains(target)) {
            return;
          }

          closeLanguageMenu(menu);
        });

        document.addEventListener('keydown', function (event) {
          if (event.key === 'Escape') {
            closeLanguageMenu(menu);
          }
        });

        menu.dataset.languageSwitcherReady = 'true';
      }
    });
  }

  function closeLanguageMenu(menu) {
    menu.querySelectorAll('.language-hover.is-open').forEach(function (item) {
      item.classList.remove('is-open');
      item.querySelector(':scope > a')?.setAttribute('aria-expanded', 'false');
    });
  }

  function createLanguageMenuItem(culture, label, isCurrent) {
    const item = document.createElement('li');
    const link = document.createElement('a');
    const text = document.createElement('span');
    const url = new URL('/Localization/SetLanguage', window.location.origin);

    url.searchParams.set('culture', culture);
    url.searchParams.set('returnUrl', `${window.location.pathname}${window.location.search}`);

    link.className = 'menu-item';
    link.href = url.toString();
    link.setAttribute('hreflang', culture);
    if (isCurrent) {
      link.setAttribute('aria-current', 'true');
    }

    text.textContent = label;
    link.appendChild(text);
    item.appendChild(link);
    return item;
  }

  function localizeStaticTexts(root) {
    const translations = getLocalizationConfig().translations || {};
    const scope = root instanceof Document ? root.body : root;
    if (!scope) {
      return;
    }

    translateAttributes(scope, translations);

    const walker = document.createTreeWalker(
      scope,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: function (node) {
          const parent = node.parentElement;
          if (!parent || parent.closest('[data-no-localize]')) {
            return NodeFilter.FILTER_REJECT;
          }

          if (['SCRIPT', 'STYLE', 'TEXTAREA', 'NOSCRIPT'].includes(parent.tagName)) {
            return NodeFilter.FILTER_REJECT;
          }

          return normalizeLocalizableText(node.nodeValue) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT;
        }
      }
    );

    const nodes = [];
    let node = walker.nextNode();
    while (node) {
      nodes.push(node);
      node = walker.nextNode();
    }

    nodes.forEach(function (textNode) {
      const key = normalizeLocalizableText(textNode.nodeValue);
      const translated = translations[key];
      if (!translated || translated === key) {
        return;
      }

      const leading = textNode.nodeValue.match(/^\s*/)?.[0] || '';
      const trailing = textNode.nodeValue.match(/\s*$/)?.[0] || '';
      textNode.nodeValue = `${leading}${translated}${trailing}`;
    });
  }

  function translateAttributes(scope, translations) {
    scope.querySelectorAll('[placeholder], [title], [aria-label]').forEach(function (element) {
      ['placeholder', 'title', 'aria-label'].forEach(function (attribute) {
        const value = element.getAttribute(attribute);
        const key = normalizeLocalizableText(value);
        if (key && translations[key] && translations[key] !== key) {
          element.setAttribute(attribute, translations[key]);
        }
      });
    });
  }

  function normalizeLocalizableText(value) {
    return String(value || '').replace(/\s+/g, ' ').trim();
  }

  function getLocalizationConfig() {
    if (localizationConfig) {
      return localizationConfig;
    }

    const source = document.getElementById('storefront-localization');
    if (!source) {
      localizationConfig = { currentCulture: 'en', translations: {} };
      return localizationConfig;
    }

    try {
      localizationConfig = JSON.parse(source.textContent || '{}');
    } catch {
      localizationConfig = { currentCulture: 'en', translations: {} };
    }

    localizationConfig.currentCulture = localizationConfig.currentCulture || 'en';
    localizationConfig.translations = localizationConfig.translations || {};
    return localizationConfig;
  }

  function getAuthConfig() {
    const source = document.getElementById('storefront-auth');
    if (!source) {
      return { isAuthenticated: false };
    }

    try {
      return JSON.parse(source.textContent || '{}');
    } catch {
      return { isAuthenticated: false };
    }
  }

  function isUserAuthenticated() {
    return getAuthConfig().isAuthenticated === true;
  }

  function initAccountLinks() {
    const isAuthenticated = isUserAuthenticated();
    const href = isAuthenticated ? '/Profile' : '/Account/Login';
    const label = isAuthenticated ? t('Account') : t('Login');

    document.querySelectorAll('a[href="/Profile"], a[href="/Account/Login"]').forEach(function (link) {
      link.href = href;

      const labelTarget = link.querySelector('span') || link;
      labelTarget.textContent = label;
    });
  }

  function t(key) {
    const translations = getLocalizationConfig().translations || {};
    return translations[key] || key;
  }

  function formatLocalized(key) {
    const args = Array.prototype.slice.call(arguments, 1);
    return args.reduce(function (message, value, index) {
      return message.replaceAll(`{${index}}`, value);
    }, t(key));
  }

  async function initCategoryDropdowns() {
    const menus = document.querySelectorAll(
      '.category-search-wrapper .category-btn > .category-sub-menu, .category-btn.menu-category > .category-sub-menu'
    );
    if (menus.length === 0) {
      return;
    }

    try {
      const response = await fetch('/Catalog/CategoriesMenu', {
        headers: ajaxHeaders,
        credentials: 'same-origin'
      });

      if (!response.ok) {
        return;
      }

      const categories = await response.json();
      if (!Array.isArray(categories) || categories.length === 0) {
        return;
      }

      menus.forEach(function (menu) {
        menu.replaceChildren(...categories.map(createCategoryMenuItem));
      });
    } catch {
      // If categories cannot be loaded, keep the server-rendered fallback menu.
    }
  }

  function createCategoryMenuItem(category) {
    const item = document.createElement('li');
    const link = document.createElement('a');
    const icon = document.createElement('img');
    const text = document.createElement('span');

    link.className = 'menu-item';
    link.href = category.href || `/Catalog?categorySlug=${encodeURIComponent(category.slug || '')}`;
    icon.src = category.icon || '/assets/ekomart/images/icons/01.svg';
    icon.alt = category.name || 'Category';
    text.textContent = category.name || 'Category';

    link.append(icon, text);
    item.appendChild(link);
    return item;
  }

  function initPriceFilterLabels() {
    document.querySelectorAll('.price-input-area').forEach(function (form) {
      const minInput = form.querySelector('input[name="MinPrice"]');
      const maxInput = form.querySelector('input[name="MaxPrice"]');
      const label = form.querySelector('[data-price-filter-label]');

      if (!minInput || !maxInput || !label) {
        return;
      }

      const updateLabel = function () {
        const minValue = String(minInput.value || '').trim();
        const maxValue = String(maxInput.value || '').trim();
        label.textContent = `${t('Price')}: ${minValue || '0'} — ${maxValue || t('Any')}`;
      };

      if (!form.dataset.priceFilterLabelReady) {
        minInput.addEventListener('input', updateLabel);
        maxInput.addEventListener('input', updateLabel);
        form.dataset.priceFilterLabelReady = 'true';
      }

      updateLabel();
    });
  }

  function initStandaloneSearch() {
    document.querySelectorAll('.search-input-area').forEach(function (area) {
      if (area.dataset.searchReady) {
        return;
      }

      const input = area.querySelector('.search-input');
      const button = area.querySelector('.input-div button');
      if (!input || !button) {
        return;
      }

      const submitSearch = function () {
        const search = String(input.value || '').trim();
        if (!search) {
          input.focus();
          return;
        }

        const url = new URL('/Catalog', window.location.origin);
        url.searchParams.set('Search', search);
        window.location.href = url.toString();
      };

      button.addEventListener('click', function (event) {
        event.preventDefault();
        submitSearch();
      });

      input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
          event.preventDefault();
          submitSearch();
        }
      });

      area.dataset.searchReady = 'true';
    });
  }

  function initCheckoutValidation() {
    const deliveryFromUrl = new URLSearchParams(window.location.search).get('deliveryMethod');
    const selectedDelivery = deliveryFromUrl || sessionStorage.getItem(deliveryStorageKey);
    const checkoutDeliveryInputs = Array.from(document.querySelectorAll('input[name="DeliveryMethod"][form="checkout-form"]'));

    if (selectedDelivery && checkoutDeliveryInputs.length > 0 && !checkoutDeliveryInputs.some(function (input) { return input.checked; })) {
      const matchingInput = checkoutDeliveryInputs.find(function (input) {
        return input.value === selectedDelivery;
      });

      if (matchingInput) {
        matchingInput.checked = true;
      }
    }

    document.querySelectorAll('input[name="DeliveryMethod"], input[name="PaymentMethod"], input[name="TermsAccepted"]').forEach(function (input) {
      input.addEventListener('change', function () {
        if (input.name === 'DeliveryMethod') {
          sessionStorage.setItem(deliveryStorageKey, input.value);
          setCheckoutFieldError('DeliveryMethod', false);
          setCartDeliveryError(false);
        }

        if (input.name === 'PaymentMethod') {
          setCheckoutFieldError('PaymentMethod', false);
        }

        if (input.name === 'TermsAccepted') {
          setCheckoutFieldError('TermsAccepted', false);
        }
      });
    });
  }

  function handleCheckoutChoiceClick(event, target) {
    const choice = target.closest(
      '[data-checkout-choice-group] label, [data-checkout-choice-group] li, .right-card-sidebar-checkout .single-category label, .right-card-sidebar-checkout .single-category'
    );

    if (!choice) {
      return false;
    }

    const input = getChoiceInput(choice);
    if (!input || !['radio', 'checkbox'].includes(input.type)) {
      return false;
    }

    event.preventDefault();

    if (input.type === 'checkbox') {
      input.checked = !input.checked;
    } else {
      input.checked = true;
    }

    input.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  }

  function getChoiceInput(choice) {
    if (choice instanceof HTMLLabelElement && choice.htmlFor) {
      return document.getElementById(choice.htmlFor);
    }

    return choice.querySelector('input[type="radio"], input[type="checkbox"]');
  }

  function handleCartCheckoutClick(event, link) {
    const selectedDelivery = document.querySelector('input[name="DeliveryMethod"][data-cart-delivery-method]:checked');
    if (!selectedDelivery) {
      event.preventDefault();
      setCartDeliveryError(true);
      document.querySelector('[data-cart-delivery-section]')?.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      });
      return;
    }

    sessionStorage.setItem(deliveryStorageKey, selectedDelivery.value);
    const url = new URL(link.href, window.location.origin);
    url.searchParams.set('deliveryMethod', selectedDelivery.value);
    link.href = url.toString();
    setCartDeliveryError(false);
  }

  function validateCheckoutForm() {
    let isValid = true;

    if (!document.querySelector('input[name="DeliveryMethod"][form="checkout-form"]:checked')) {
      setCheckoutFieldError('DeliveryMethod', true);
      isValid = false;
    } else {
      setCheckoutFieldError('DeliveryMethod', false);
    }

    if (!document.querySelector('input[name="PaymentMethod"][form="checkout-form"]:checked')) {
      setCheckoutFieldError('PaymentMethod', true);
      isValid = false;
    } else {
      setCheckoutFieldError('PaymentMethod', false);
    }

    const termsAccepted = document.querySelector('input[name="TermsAccepted"][form="checkout-form"]');
    if (termsAccepted && !termsAccepted.checked) {
      setCheckoutFieldError('TermsAccepted', true);
      isValid = false;
    } else {
      setCheckoutFieldError('TermsAccepted', false);
    }

    if (!isValid) {
      document.querySelector('.checkout-field-error:not([hidden])')?.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      });
      showNotice(t('Fill required checkout fields.'), 'danger');
    }

    return isValid;
  }

  function setCartDeliveryError(show) {
    const error = document.querySelector('[data-cart-delivery-error]');
    const section = document.querySelector('[data-cart-delivery-section]');
    if (error) {
      error.hidden = !show;
    }

    section?.classList.toggle('is-invalid-choice', show);
  }

  function setCheckoutFieldError(fieldName, show) {
    const error = document.querySelector(`[data-checkout-error-for="${fieldName}"]`);
    if (error) {
      error.hidden = !show;
    }

    const group = document.querySelector(`[data-checkout-choice-group="${fieldName}"]`)
      || error?.closest('.single-category');
    group?.classList.toggle('is-invalid-choice', show);
  }

  function showEmptyCartState() {
    const list = document.querySelector('.rts-cart-list-area');
    if (!list || list.querySelector('[data-empty-cart-row]')) {
      return;
    }

    const header = list.querySelector('.single-cart-area-list.head');
    const emptyRow = document.createElement('div');
    emptyRow.className = 'single-cart-area-list main item-parent';
    emptyRow.setAttribute('data-empty-cart-row', '');
    emptyRow.innerHTML = `<div class="product-main-cart"><div class="information"><h6 class="title">${escapeHtml(t('Cart is empty.'))}</h6><span>${escapeHtml(t('Add products from the catalog to continue.'))}</span></div></div>`;
    header?.insertAdjacentElement('afterend', emptyRow);

    const buttonArea = document.querySelector('.cart-total-area-start-right .button-area');
    if (buttonArea) {
      buttonArea.innerHTML = `<a href="/Catalog" class="rts-btn btn-primary">${escapeHtml(t('Go Shopping'))}</a>`;
    }
  }

  function startOrderHub() {
    if (!document.querySelector('[data-order-realtime]') || !window.signalR) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/orders')
      .withAutomaticReconnect()
      .build();

    connection.on('OrderCreated', function (payload) {
      refreshOrdersTable(payload);
      showNotice(payload.message || formatLocalized('New order #{0}', payload.id), 'success');
    });

    connection.on('OrderStatusChanged', function (payload) {
      const badge = document.querySelector('[data-order-status-badge]');
      if (badge) {
        badge.textContent = payload.status;
      }

      showNotice(payload.message || formatLocalized('Order #{0} status updated.', payload.id), 'success');
    });

    connection.start().catch(function () {
      showNotice(t('Realtime notifications are temporarily unavailable.'), 'warning');
    });
  }

  function refreshOrdersTable(payload) {
    const ordersTable = window.ekomartOrdersTable;
    const table = document.querySelector('.datatables-order');
    const tbody = table?.querySelector('tbody');
    const row = createOrderRow(payload);

    if (!table || !tbody || !row) {
      return;
    }

    if (tbody.querySelector(`tr[data-order-id="${payload.id}"]`)) {
      return;
    }

    if (ordersTable && typeof ordersTable.row === 'function') {
      ordersTable.row.add(row).draw(false);

      if (typeof ordersTable.order === 'function') {
        ordersTable.order([2, 'desc']).draw(false);
      }

      if (typeof ordersTable.page === 'function') {
        ordersTable.page('first').draw('page');
      }
      return;
    }

    tbody.prepend(row);
  }

  function createOrderRow(payload) {
    if (!payload || !payload.id) {
      return null;
    }

    const row = document.createElement('tr');
    const detailsUrl = payload.detailsUrl || `/Admin/Orders/Details/${encodeURIComponent(payload.id)}`;
    const customer = payload.customer || t('Customer');
    const email = payload.email || '';
    const initials = payload.initials || customer.slice(0, 2).toUpperCase();

    row.setAttribute('data-order-id', String(payload.id));
    row.innerHTML = `
      <td></td>
      <td><input type="checkbox" class="dt-checkboxes form-check-input" /></td>
      <td data-order="${escapeHtml(payload.id)}"><a href="${escapeHtml(detailsUrl)}"><span>#${escapeHtml(payload.id)}</span></a></td>
      <td><span class="text-nowrap">${escapeHtml(payload.createdAt || '')}</span></td>
      <td>
        <div class="d-flex justify-content-start align-items-center order-name text-nowrap">
          <div class="avatar-wrapper">
            <div class="avatar avatar-sm me-3">
              <span class="avatar-initial rounded-circle bg-label-primary">${escapeHtml(initials)}</span>
            </div>
          </div>
          <div class="d-flex flex-column">
            <h6 class="m-0">${escapeHtml(customer)}</h6>
            <small>${escapeHtml(email)}</small>
          </div>
        </div>
      </td>
      <td><span class="badge ${escapeHtml(payload.paymentBadge || 'bg-label-secondary')}">${escapeHtml(payload.paymentStatus || '')}</span></td>
      <td><span class="badge ${escapeHtml(payload.statusBadge || 'bg-label-secondary')}">${escapeHtml(payload.status || '')}</span></td>
      <td>${escapeHtml(payload.total || '')}</td>
      <td>
        <a href="${escapeHtml(detailsUrl)}" class="btn btn-text-secondary rounded-pill waves-effect btn-icon">
          <i class="icon-base ti tabler-eye icon-22px"></i>
        </a>
      </td>`;

    return row;
  }

  function isCatalogLink(link) {
    if (!link.matches('[data-catalog-link], [data-catalog-results] a, .sidebar-filter-main a')) {
      return false;
    }

    const url = new URL(link.href, window.location.origin);
    return url.origin === window.location.origin && url.pathname.toLowerCase() === '/catalog';
  }

  function setSubmitterState(submitter, disabled) {
    if (submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement) {
      submitter.disabled = disabled;
    }

    if (submitter instanceof HTMLAnchorElement) {
      submitter.classList.toggle('disabled', disabled);
      submitter.setAttribute('aria-disabled', disabled ? 'true' : 'false');
    }
  }

  function showNotice(message, type) {
    if (!message) {
      return;
    }

    const container = getNoticeContainer();
    const notice = document.createElement('div');
    notice.className = `alert alert-${type || 'info'}`;
    notice.style.marginBottom = '8px';
    notice.textContent = message;
    container.appendChild(notice);

    window.setTimeout(function () {
      notice.remove();
    }, 3500);
  }

  function getNoticeContainer() {
    let container = document.querySelector('[data-ajax-notices]');
    if (container) {
      return container;
    }

    container = document.createElement('div');
    container.setAttribute('data-ajax-notices', '');
    container.style.position = 'fixed';
    container.style.right = '20px';
    container.style.bottom = '20px';
    container.style.zIndex = '1100';
    container.style.maxWidth = '360px';
    document.body.appendChild(container);

    return container;
  }

  function escapeHtml(value) {
    const element = document.createElement('div');
    element.textContent = value == null ? '' : String(value);
    return element.innerHTML;
  }
})();
