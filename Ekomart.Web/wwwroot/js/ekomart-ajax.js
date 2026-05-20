(function () {
  'use strict';

  const ajaxHeaders = {
    'X-Requested-With': 'XMLHttpRequest'
  };

  document.addEventListener('DOMContentLoaded', function () {
    loadCartCount();
    startOrderHub();
  });

  document.addEventListener('submit', function (event) {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
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
    if (!(event.target instanceof Element)) {
      return;
    }

    const link = event.target.closest('a');
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
    } catch (error) {
      showNotice(error.message, 'danger');
    } finally {
      container.removeAttribute('aria-busy');
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
    const response = await fetch(resolveFormAction(form), {
      method: form.method || 'POST',
      body: new FormData(form),
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
    document.querySelectorAll('.btn-border-only.cart > .number').forEach(function (badge) {
      badge.textContent = totalQuantity;
    });
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
