(function () {
  'use strict';

  const ajaxHeaders = {
    'X-Requested-With': 'XMLHttpRequest'
  };
  const deliveryStorageKey = 'ekomartDeliveryMethod';

  document.addEventListener('DOMContentLoaded', function () {
    initCategoryDropdowns();
    initPriceFilterLabels();
    initQuantityControls();
    initCheckoutValidation();
    hydrateHomeProductCards();
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
        throw new Error('Не удалось обновить каталог.');
      }

      container.innerHTML = await response.text();
      if (pushState) {
        history.pushState({}, '', url);
      }
      initQuantityControls(container);
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      container.removeAttribute('aria-busy');
      initPriceFilterLabels();
    }
  }

  async function submitCartForm(form, submitter) {
    setSubmitterState(submitter, true);

    try {
      const data = await postFormJson(form);
      updateCartUi(data, form);
      showNotice(data.message || 'Корзина обновлена.', 'success');
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

    const productId = Number(button.dataset.productId);
    if (!productId) {
      return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
      showNotice('Не удалось подготовить добавление в корзину.', 'danger');
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
      showNotice(data.message || 'Товар добавлен в корзину.', 'success');
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
        details.innerHTML = `${escapeHtml(data.planName)}<br>Status: ${escapeHtml(data.status)}<br>Active until ${escapeHtml(data.endsAt)}`;
      }

      showNotice(data.message || 'Подписка обновлена.', 'success');
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

      showNotice(data.message || 'Order status updated.', 'success');
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
      throw new Error('Нужно войти в аккаунт.');
    }

    const data = await response.json().catch(function () {
      return {};
    });

    if (!response.ok || data.success === false) {
      throw new Error(data.message || 'Запрос не выполнен.');
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
    if (!document.querySelector('.btn-border-only.cart')) {
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
      element.textContent = `${data.totalQuantity} item(s)`;
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
      heading.textContent = `Shopping Cart (${quantity})`;
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
      badge.innerHTML = `${Number(product.stockQuantity) > 0 ? 'In' : 'Out'}<br>Stock`;
    }

    const addButton = card.querySelector('.cart-counter-action > a.rts-btn');
    if (addButton) {
      addButton.href = '#';
      addButton.dataset.templateCartAdd = 'true';
      addButton.dataset.productId = product.id;
      addButton.dataset.stockQuantity = product.stockQuantity;
      addButton.classList.toggle('disabled', Number(product.stockQuantity) <= 0);
      addButton.setAttribute('aria-disabled', Number(product.stockQuantity) <= 0 ? 'true' : 'false');
    }

    const quantityInput = card.querySelector('.quantity-edit .input');
    if (quantityInput) {
      quantityInput.type = 'text';
      quantityInput.name = 'quantity';
      quantityInput.min = '1';
      quantityInput.max = String(Math.max(Number(product.stockQuantity) || 0, 1));
      quantityInput.value = clampQuantity(quantityInput.value, quantityInput);
    }
  }

  async function initCategoryDropdowns() {
    const menus = document.querySelectorAll('.category-search-wrapper .category-btn > .category-sub-menu');
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
        label.textContent = `Price: ${minValue || '0'} — ${maxValue || 'Any'}`;
      };

      if (!form.dataset.priceFilterLabelReady) {
        minInput.addEventListener('input', updateLabel);
        maxInput.addEventListener('input', updateLabel);
        form.dataset.priceFilterLabelReady = 'true';
      }

      updateLabel();
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
      showNotice('Заполните обязательные поля оформления заказа.', 'danger');
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
    emptyRow.innerHTML = '<div class="product-main-cart"><div class="information"><h6 class="title">Your cart is empty</h6><span>Add products from the catalog to continue.</span></div></div>';
    header?.insertAdjacentElement('afterend', emptyRow);

    const buttonArea = document.querySelector('.cart-total-area-start-right .button-area');
    if (buttonArea) {
      buttonArea.innerHTML = '<a href="/Catalog" class="rts-btn btn-primary">Go Shopping</a>';
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
      refreshOrdersTable();
      showNotice(payload.message || `Новый заказ #${payload.id}`, 'success');
    });

    connection.on('OrderStatusChanged', function (payload) {
      const badge = document.querySelector('[data-order-status-badge]');
      if (badge) {
        badge.textContent = payload.status;
      }

      showNotice(payload.message || `Статус заказа #${payload.id} обновлён`, 'success');
    });

    connection.start().catch(function () {
      showNotice('Realtime-уведомления временно недоступны.', 'warning');
    });
  }

  function refreshOrdersTable() {
    const ordersTable = window.ekomartOrdersTable;
    if (ordersTable && ordersTable.ajax && typeof ordersTable.ajax.reload === 'function') {
      ordersTable.ajax.reload(null, false);
    }
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
