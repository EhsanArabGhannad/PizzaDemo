let categories = [];
let products = [];

const yearElement = document.querySelector("[data-current-year]");
const navToggle = document.querySelector("[data-nav-toggle]");
const navigation = document.querySelector("[data-navigation]");
const desktopNavigation = window.matchMedia("(min-width: 68rem)");
const categoryFilter = document.querySelector("[data-category-filter]");
const productGrid = document.querySelector("[data-product-grid]");
const cartCountElements = document.querySelectorAll("[data-cart-count]");
const showAllButton = document.querySelector("[data-show-all]");
const toast = document.querySelector("[data-toast]");
const customizer = document.querySelector("[data-customizer]");
const customizerForm = document.querySelector("[data-customizer-form]");
const customizerClose = document.querySelector("[data-customizer-close]");
const customizerImage = document.querySelector("[data-customizer-image]");
const customizerName = document.querySelector("[data-customizer-name]");
const customizerDescription = document.querySelector("[data-customizer-description]");
const customizerOptions = document.querySelector("[data-customizer-options]");
const customizerTotal = document.querySelector("[data-customizer-total]");
const quantityOutput = document.querySelector("[data-quantity]");
const customizerAddButton = customizerForm?.querySelector('button[type="submit"]');
const basket = document.querySelector("[data-basket]");
const basketOpenButtons = document.querySelectorAll("[data-basket-open]");
const basketCloseButton = document.querySelector("[data-basket-close]");
const basketBrowseButton = document.querySelector("[data-basket-browse]");
const basketEmpty = document.querySelector("[data-basket-empty]");
const basketItems = document.querySelector("[data-basket-items]");
const basketSubtotal = document.querySelector("[data-basket-subtotal]");
const deliveryFeeElement = document.querySelector("[data-delivery-fee]");
const serviceFeeElement = document.querySelector("[data-service-fee]");
const basketTotal = document.querySelector("[data-basket-total]");
const basketNote = document.querySelector("[data-basket-note]");
const basketClearButton = document.querySelector("[data-basket-clear]");
const checkoutButton = document.querySelector("[data-checkout]");
const checkoutTotal = document.querySelector("[data-checkout-total]");
const checkoutDialog = document.querySelector("[data-checkout-dialog]");
const checkoutForm = document.querySelector("[data-checkout-form]");
const checkoutCloseButton = document.querySelector("[data-checkout-close]");
const checkoutDeliveryFields = document.querySelector("[data-checkout-delivery]");
const checkoutCollectionInfo = document.querySelector("[data-checkout-collection]");
const checkoutPreviewItems = document.querySelector("[data-checkout-preview-items]");
const checkoutPreviewType = document.querySelector("[data-checkout-preview-type]");
const checkoutPreviewSubtotal = document.querySelector("[data-checkout-preview-subtotal]");
const checkoutPreviewDelivery = document.querySelector("[data-checkout-preview-delivery]");
const checkoutPreviewService = document.querySelector("[data-checkout-preview-service]");
const checkoutPreviewTotal = document.querySelector("[data-checkout-preview-total]");
const checkoutSubmitTotal = document.querySelector("[data-checkout-submit-total]");
const checkoutSubmitButton = checkoutForm?.querySelector('button[type="submit"]');
const checkoutSubmitLabel = document.querySelector("[data-checkout-submit-label]");
const confirmationDialog = document.querySelector("[data-confirmation-dialog]");
const confirmationCloseButton = document.querySelector("[data-confirmation-close]");
const confirmationName = document.querySelector("[data-confirmation-name]");
const confirmationNumber = document.querySelector("[data-confirmation-number]");
const confirmationTypeLabel = document.querySelector("[data-confirmation-type-label]");
const confirmationEta = document.querySelector("[data-confirmation-eta]");
const confirmationType = document.querySelector("[data-confirmation-type]");
const confirmationItems = document.querySelector("[data-confirmation-items]");
const confirmationSubtotal = document.querySelector("[data-confirmation-subtotal]");
const confirmationDelivery = document.querySelector("[data-confirmation-delivery]");
const confirmationService = document.querySelector("[data-confirmation-service]");
const confirmationTotal = document.querySelector("[data-confirmation-total]");
const feeInfoButton = document.querySelector("[data-fee-info]");
const shopStatusElements = document.querySelectorAll("[data-shop-status]");
const deliveryEtaElements = document.querySelectorAll("[data-shop-delivery-eta]");
const collectionEtaElements = document.querySelectorAll("[data-shop-collection-eta]");
const deliveryFeeCopyElements = document.querySelectorAll("[data-shop-delivery-fee]");
const serviceFeeCopyElements = document.querySelectorAll("[data-shop-service-fee]");
const deliveryMinimumElements = document.querySelectorAll("[data-shop-delivery-minimum]");
const deliveryCoverageElements = document.querySelectorAll("[data-shop-delivery-coverage]");
const todayHoursElements = document.querySelectorAll("[data-shop-today-hours]");

const BASKET_STORAGE_KEY = "pizza-knight-basket-v2";

let shopOperations = {
  acceptingOnlineOrders: true,
  statusMessage: "Open for online orders",
  deliveryMinimum: 10,
  deliveryFee: 2.5,
  serviceFee: 0.5,
  deliveryEta: "35–50 mins",
  collectionEta: "20–30 mins",
  deliveryZones: [{ name: "Consett and nearby DH8 addresses", prefix: "DH8" }],
  openingHours: [],
};

const currencyFormatter = new Intl.NumberFormat("en-GB", {
  style: "currency",
  currency: "GBP",
});

async function loadMenuData() {
  try {
    const response = await fetch("/api/menu", {
      headers: { Accept: "application/json" },
    });

    if (!response.ok) {
      throw new Error(`Menu request failed with status ${response.status}.`);
    }

    const menu = await response.json();
    if (!Array.isArray(menu.categories) || !Array.isArray(menu.products)) {
      throw new Error("Menu response has an invalid shape.");
    }

    categories = menu.categories;
    products = menu.products;
  } catch (error) {
    console.error("Could not load the database menu; using the local fallback.", error);
    const fallback = await import("./products.js");
    categories = fallback.categories;
    products = fallback.products;
    showToast("The live menu is temporarily unavailable. Showing the saved menu.");
  }
}

async function loadShopOperations() {
  try {
    const response = await fetch("/api/shop", {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) throw new Error(`Shop settings request failed with status ${response.status}.`);

    const settings = await response.json();
    if (typeof settings.acceptingOnlineOrders !== "boolean") {
      throw new Error("Shop settings response has an invalid shape.");
    }

    shopOperations = {
      ...shopOperations,
      ...settings,
      deliveryZones: Array.isArray(settings.deliveryZones) ? settings.deliveryZones : [],
    };
  } catch (error) {
    console.error("Could not load live shop settings; using the saved defaults.", error);
    showToast("Live shop settings are temporarily unavailable. Showing saved information.");
  }
}

let activeCategory = "all";
let showAllPopular = false;
let toastTimer;
let selectedProduct;
let selectedQuantity = 1;
let lastCustomizerTrigger;
let cartItems = [];
let orderType = "delivery";
let checkoutIsCompleting = false;

if (yearElement) yearElement.textContent = new Date().getFullYear();

function setNavigationState(isOpen) {
  if (!navToggle || !navigation) return;
  navToggle.setAttribute("aria-expanded", String(isOpen));
  navToggle.setAttribute("aria-label", isOpen ? "Close navigation" : "Open navigation");
  navigation.classList.toggle("is-open", isOpen);
  document.body.classList.toggle("nav-is-open", isOpen && !desktopNavigation.matches);
}

navToggle?.addEventListener("click", () => setNavigationState(navToggle.getAttribute("aria-expanded") !== "true"));
navigation?.addEventListener("click", (event) => {
  if (event.target.closest("a")) setNavigationState(false);
});
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    if (confirmationDialog?.open) {
      closeConfirmation();
      return;
    }
    if (checkoutDialog?.open) {
      closeCheckout();
      return;
    }
    if (customizer?.open) {
      closeCustomizer();
      return;
    }
    if (basket?.open) {
      closeBasket();
      return;
    }
    setNavigationState(false);
    navToggle?.focus();
  }
});
desktopNavigation.addEventListener("change", () => setNavigationState(false));

function productCardTemplate(product) {
  const actionLabel = product.customisable ? "Customise" : "Add";
  const productId = escapeHtml(product.id);
  const productCategory = escapeHtml(product.category);
  const productName = escapeHtml(product.name);
  const productDescription = escapeHtml(product.description);
  const productImage = escapeHtml(product.image);
  const actionIcon = product.customisable
    ? '<path d="M4 7h10M17 7h3M4 17h3M10 17h10M14 4v6M7 14v6" />'
    : '<path d="M12 5v14M5 12h14" />';

  return `
    <article class="product-card" data-category="${productCategory}" data-product-id="${productId}">
      <div class="product-card__media">
        <img class="product-card__image" src="${productImage}" alt="${productName}" width="720" height="720" loading="lazy" />
        ${product.badge ? `<span class="product-card__badge">${escapeHtml(product.badge)}</span>` : ""}
      </div>
      <div class="product-card__body">
        <div class="product-card__heading">
          <h3 class="product-card__title">${productName}</h3>
          <span class="product-card__price">From ${currencyFormatter.format(product.price)}</span>
        </div>
        <p class="product-card__description">${productDescription}</p>
        <div class="product-card__footer">
          <span class="product-card__note">${product.customisable ? "Choose size & extras" : "Quick add"}</span>
          <button class="product-card__button" type="button" data-product-action="${productId}" aria-label="${actionLabel} ${productName}">
            <svg aria-hidden="true" viewBox="0 0 24 24">${actionIcon}</svg>
            ${actionLabel}
          </button>
        </div>
      </div>
    </article>`;
}

function renderCategoryButtons() {
  if (!categoryFilter) return;

  categoryFilter.innerHTML = categories
    .map(
      (category) => `
        <button
          class="category-button${category.id === activeCategory ? " is-active" : ""}"
          type="button"
          data-category-id="${escapeHtml(category.id)}"
          aria-pressed="${category.id === activeCategory}"
        >${escapeHtml(category.label)}</button>`,
    )
    .join("");
}

function renderProducts() {
  if (!productGrid) return;

  const filteredProducts = activeCategory === "all"
    ? products
    : products.filter((product) => product.category === activeCategory);
  const visibleProducts = activeCategory === "all" && !showAllPopular
    ? filteredProducts.slice(0, 4)
    : filteredProducts;

  productGrid.innerHTML = visibleProducts.map(productCardTemplate).join("");

  if (showAllButton) {
    showAllButton.hidden = activeCategory !== "all" || showAllPopular;
  }
}

function showToast(message) {
  if (!toast) return;

  window.clearTimeout(toastTimer);
  toast.textContent = message;
  toast.classList.add("is-visible");
  toastTimer = window.setTimeout(() => toast.classList.remove("is-visible"), 2400);
}

function syncModalState() {
  document.body.classList.toggle(
    "modal-is-open",
    Boolean(customizer?.open || basket?.open || checkoutDialog?.open || confirmationDialog?.open),
  );
}

function escapeHtml(value) {
  const characters = { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" };
  return String(value).replace(/[&<>"']/g, (character) => characters[character]);
}

function normaliseSelections(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};

  return Object.fromEntries(
    Object.entries(value)
      .filter(([group, options]) => typeof group === "string" && group.length <= 80 && Array.isArray(options))
      .slice(0, 20)
      .map(([group, options]) => [
        group,
        [...new Set(options.filter((option) => typeof option === "string" && option.length <= 120))].slice(0, 20),
      ]),
  );
}

function resolveCartConfiguration(product, selections) {
  if (!product.customisable) {
    return { unitPrice: product.price, details: "Standard serving" };
  }

  if (!Array.isArray(product.optionGroups)) return null;

  let unitPrice = Number(product.price);
  const labels = [];

  for (const group of product.optionGroups) {
    const selectedIds = selections[group.id] ?? [];
    if (selectedIds.length < group.minimumSelections || selectedIds.length > group.maximumSelections) return null;

    for (const selectedId of selectedIds) {
      const option = group.options.find((candidate) => candidate.id === selectedId);
      if (!option) return null;
      unitPrice += Number(option.price);
      labels.push(option.name);
    }
  }

  return {
    unitPrice: Number(unitPrice.toFixed(2)),
    details: labels.join(" · ") || "Standard serving",
  };
}

function restoreBasketState() {
  try {
    const storedState = JSON.parse(window.localStorage.getItem(BASKET_STORAGE_KEY) ?? "null");
    if (!storedState || !Array.isArray(storedState.items)) return;

    const restoredItems = [];
    const seenKeys = new Set();

    storedState.items.forEach((storedItem) => {
      const product = products.find((item) => item.id === storedItem?.productId);
      const quantity = Number(storedItem?.quantity);
      const key = typeof storedItem?.key === "string" ? storedItem.key.slice(0, 100) : "";

      if (!product || !key || seenKeys.has(key)) return;
      if (!Number.isInteger(quantity) || quantity < 1 || quantity > 10) return;
      const selections = normaliseSelections(storedItem?.selections);
      const configuration = resolveCartConfiguration(product, selections);
      if (!configuration) return;

      seenKeys.add(key);
      restoredItems.push({
        key,
        productId: product.id,
        name: product.name,
        image: product.image,
        unitPrice: configuration.unitPrice,
        quantity,
        details: configuration.details,
        selections,
      });
    });

    cartItems = restoredItems;
    if (storedState.orderType === "delivery" || storedState.orderType === "collection") {
      orderType = storedState.orderType;
    }
  } catch {
    cartItems = [];
    orderType = "delivery";
  }
}

function persistBasketState() {
  try {
    window.localStorage.setItem(BASKET_STORAGE_KEY, JSON.stringify({ items: cartItems, orderType }));
  } catch {
    // The basket still works for the current page if storage is unavailable.
  }
}

function getBasketTotals() {
  const itemCount = cartItems.reduce((total, item) => total + item.quantity, 0);
  const subtotal = cartItems.reduce((total, item) => total + item.unitPrice * item.quantity, 0);
  const hasItems = itemCount > 0;
  const serviceFee = hasItems ? Number(shopOperations.serviceFee) : 0;
  const deliveryFee = hasItems && orderType === "delivery" ? Number(shopOperations.deliveryFee) : 0;

  return {
    itemCount,
    subtotal,
    hasItems,
    serviceFee,
    deliveryFee,
    total: subtotal + serviceFee + deliveryFee,
  };
}

function normalisePostcode(postcode) {
  return String(postcode ?? "").replace(/\s+/g, "").toUpperCase();
}

function coversDeliveryPostcode(postcode) {
  const normalized = normalisePostcode(postcode);
  return normalized.length > 0 && shopOperations.deliveryZones.some((zone) =>
    normalized.startsWith(normalisePostcode(zone.prefix)),
  );
}

function checkoutBlockReason() {
  if (!shopOperations.acceptingOnlineOrders) return shopOperations.statusMessage;
  const totals = getBasketTotals();
  if (orderType === "delivery" && totals.subtotal < Number(shopOperations.deliveryMinimum)) {
    return `Delivery requires a minimum food subtotal of ${currencyFormatter.format(shopOperations.deliveryMinimum)}.`;
  }
  return null;
}

function renderShopOperations() {
  shopStatusElements.forEach((element) => {
    element.textContent = shopOperations.statusMessage;
    element.closest(".hero-fact")?.classList.toggle("is-closed", !shopOperations.acceptingOnlineOrders);
  });
  deliveryEtaElements.forEach((element) => { element.textContent = shopOperations.deliveryEta; });
  collectionEtaElements.forEach((element) => { element.textContent = shopOperations.collectionEta; });
  deliveryFeeCopyElements.forEach((element) => { element.textContent = currencyFormatter.format(shopOperations.deliveryFee); });
  serviceFeeCopyElements.forEach((element) => { element.textContent = currencyFormatter.format(shopOperations.serviceFee); });
  deliveryMinimumElements.forEach((element) => { element.textContent = currencyFormatter.format(shopOperations.deliveryMinimum); });

  const coverage = shopOperations.deliveryZones.length
    ? shopOperations.deliveryZones.map((zone) => zone.name).join(", ")
    : "No delivery areas are currently active";
  deliveryCoverageElements.forEach((element) => { element.textContent = coverage; });

  const today = shopOperations.openingHours.find((hours) => hours.day === shopOperations.currentDay);
  const todayHours = !today || today.isClosed
    ? "Closed today"
    : `${formatShopTime(today.opensAt)}–${formatShopTime(today.closesAt)}`;
  todayHoursElements.forEach((element) => { element.textContent = todayHours; });
}

function formatShopTime(value) {
  const [hours, minutes] = String(value).split(":").map(Number);
  const suffix = hours >= 12 ? "pm" : "am";
  return `${hours % 12 || 12}:${String(minutes).padStart(2, "0")}${suffix}`;
}

function basketItemTemplate(item) {
  return `
    <article class="basket-item" data-basket-item="${escapeHtml(item.key)}">
      <img src="${escapeHtml(item.image)}" alt="" width="72" height="72" />
      <div class="basket-item__content">
        <div class="basket-item__heading">
          <h3>${escapeHtml(item.name)}</h3>
          <strong>${currencyFormatter.format(item.unitPrice * item.quantity)}</strong>
        </div>
        <p>${escapeHtml(item.details || "Standard serving")}</p>
        <div class="basket-item__actions">
          <div class="basket-item__quantity" aria-label="Change ${escapeHtml(item.name)} quantity">
            <button type="button" data-basket-action="decrease" data-basket-key="${escapeHtml(item.key)}" aria-label="Decrease ${escapeHtml(item.name)} quantity">−</button>
            <span>${item.quantity}</span>
            <button type="button" data-basket-action="increase" data-basket-key="${escapeHtml(item.key)}" aria-label="Increase ${escapeHtml(item.name)} quantity">+</button>
          </div>
          <button class="basket-item__remove" type="button" data-basket-action="remove" data-basket-key="${escapeHtml(item.key)}" aria-label="Remove ${escapeHtml(item.name)}">Remove</button>
        </div>
      </div>
    </article>`;
}

function renderBasket() {
  const { itemCount, subtotal, hasItems, serviceFee, deliveryFee, total } = getBasketTotals();

  cartCountElements.forEach((element) => {
    element.textContent = String(itemCount);
  });
  basketOpenButtons.forEach((button) => {
    button.setAttribute("aria-label", `Open basket, ${itemCount} item${itemCount === 1 ? "" : "s"}`);
  });
  if (basketEmpty) basketEmpty.hidden = hasItems;
  if (basketItems) basketItems.innerHTML = cartItems.map(basketItemTemplate).join("");
  if (basketSubtotal) basketSubtotal.textContent = currencyFormatter.format(subtotal);
  if (deliveryFeeElement) deliveryFeeElement.textContent = deliveryFee ? currencyFormatter.format(deliveryFee) : "Free";
  if (serviceFeeElement) serviceFeeElement.textContent = currencyFormatter.format(serviceFee);
  if (basketTotal) basketTotal.textContent = currencyFormatter.format(total);
  if (checkoutTotal) checkoutTotal.textContent = currencyFormatter.format(total);
  if (checkoutButton) checkoutButton.disabled = !hasItems;
  if (basketClearButton) basketClearButton.hidden = !hasItems;
  if (basketNote) {
    const blocked = hasItems ? checkoutBlockReason() : null;
    basketNote.textContent = blocked || (hasItems
      ? `${itemCount} item${itemCount === 1 ? "" : "s"} · ${orderType === "delivery" ? `Delivery in ${shopOperations.deliveryEta}` : `Ready to collect in ${shopOperations.collectionEta}`}`
      : "Add items to see your order total.");
  }

  document.querySelectorAll("[data-order-type]").forEach((button) => {
    const isActive = button.dataset.orderType === orderType;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", String(isActive));
  });

  persistBasketState();
}

function addQuickProduct(product) {
  const key = `standard-${product.id}`;
  const existingItem = cartItems.find((item) => item.key === key);

  if (existingItem) {
    existingItem.quantity += 1;
  } else {
    cartItems.push({
      key,
      productId: product.id,
      name: product.name,
      image: product.image,
      unitPrice: product.price,
      quantity: 1,
      details: "Standard serving",
      selections: {},
    });
  }

  renderBasket();
}

function openBasket() {
  if (!basket) return;
  renderBasket();
  basket.showModal();
  syncModalState();
}

function closeBasket() {
  if (basket?.open) basket.close();
}

function checkoutItemTemplate(item) {
  return `
    <div class="checkout-order-item">
      <strong>${item.quantity} &times; ${escapeHtml(item.name)}</strong>
      <span>${currencyFormatter.format(item.unitPrice * item.quantity)}</span>
      <small>${escapeHtml(item.details || "Standard serving")}</small>
    </div>`;
}

function renderCheckout() {
  const { subtotal, serviceFee, deliveryFee, total } = getBasketTotals();
  const isDelivery = orderType === "delivery";

  if (checkoutPreviewItems) checkoutPreviewItems.innerHTML = cartItems.map(checkoutItemTemplate).join("");
  if (checkoutPreviewType) checkoutPreviewType.textContent = isDelivery ? "Delivery" : "Collection";
  if (checkoutPreviewSubtotal) checkoutPreviewSubtotal.textContent = currencyFormatter.format(subtotal);
  if (checkoutPreviewDelivery) checkoutPreviewDelivery.textContent = deliveryFee ? currencyFormatter.format(deliveryFee) : "Free";
  if (checkoutPreviewService) checkoutPreviewService.textContent = currencyFormatter.format(serviceFee);
  if (checkoutPreviewTotal) checkoutPreviewTotal.textContent = currencyFormatter.format(total);
  if (checkoutSubmitTotal) checkoutSubmitTotal.textContent = currencyFormatter.format(total);
  if (checkoutDeliveryFields) {
    checkoutDeliveryFields.hidden = !isDelivery;
    checkoutDeliveryFields.querySelectorAll("input").forEach((input) => {
      input.required = isDelivery;
    });
  }
  if (checkoutCollectionInfo) checkoutCollectionInfo.hidden = isDelivery;
}

function openCheckout() {
  if (!checkoutDialog || !getBasketTotals().hasItems) return;

  const blocked = checkoutBlockReason();
  if (blocked) {
    showToast(blocked);
    return;
  }

  renderCheckout();
  closeBasket();
  checkoutDialog.showModal();
  syncModalState();
}

function closeCheckout() {
  if (checkoutDialog?.open) checkoutDialog.close();
}

function renderConfirmation({ customerName, items, type, totals, orderNumber, estimatedTime }) {
  const isDelivery = type === "delivery";

  if (confirmationName) confirmationName.textContent = customerName || "pizza fan";
  if (confirmationNumber) confirmationNumber.textContent = orderNumber;
  if (confirmationTypeLabel) confirmationTypeLabel.textContent = isDelivery ? "Estimated delivery" : "Ready to collect";
  if (confirmationEta) confirmationEta.textContent = estimatedTime;
  if (confirmationType) confirmationType.textContent = isDelivery ? "Delivery" : "Collection";
  if (confirmationItems) confirmationItems.innerHTML = items.map(checkoutItemTemplate).join("");
  if (confirmationSubtotal) confirmationSubtotal.textContent = currencyFormatter.format(totals.subtotal);
  if (confirmationDelivery) confirmationDelivery.textContent = totals.deliveryFee ? currencyFormatter.format(totals.deliveryFee) : "Free";
  if (confirmationService) confirmationService.textContent = currencyFormatter.format(totals.serviceFee);
  if (confirmationTotal) confirmationTotal.textContent = currencyFormatter.format(totals.total);
}

function closeConfirmation() {
  if (confirmationDialog?.open) confirmationDialog.close();
}

function calculateCustomisedPrice() {
  if (!selectedProduct || !customizerForm) return 0;

  const optionsTotal = [...customizerForm.querySelectorAll("input[data-price]:checked")]
    .reduce((total, input) => total + Number(input.dataset.price), 0);

  return (selectedProduct.price + optionsTotal) * selectedQuantity;
}

function updateCustomizerSummary() {
  if (quantityOutput) quantityOutput.textContent = String(selectedQuantity);
  if (customizerTotal) customizerTotal.textContent = currencyFormatter.format(calculateCustomisedPrice());

  const decreaseButton = customizerForm?.querySelector('[data-quantity-change="-1"]');
  if (decreaseButton) decreaseButton.disabled = selectedQuantity === 1;
}

function renderCustomizerOptions(product) {
  if (!customizerOptions) return false;

  const groups = Array.isArray(product.optionGroups) ? product.optionGroups : [];
  if (!groups.length) {
    customizerOptions.innerHTML = '<p class="customizer-options-empty">Customisation choices are temporarily unavailable.</p>';
    return false;
  }

  customizerOptions.innerHTML = groups.map((group, groupIndex) => {
    const singleChoice = group.maximumSelections === 1;
    const inputType = singleChoice ? "radio" : "checkbox";
    const availableOptions = Array.isArray(group.options) ? group.options : [];
    const gridClass = availableOptions.length <= 3 ? " option-grid--three" : "";
    const requirement = group.minimumSelections > 0 ? "Required" : "Optional";

    return `
      <fieldset class="option-group" data-option-group-container="${escapeHtml(group.id)}">
        <legend><span>${groupIndex + 1}</span> ${escapeHtml(group.name)} <small>${requirement}</small></legend>
        <div class="option-grid${gridClass}">
          ${availableOptions.map((option, optionIndex) => {
            const isChecked = singleChoice && group.minimumSelections > 0 && optionIndex === 0;
            const priceLabel = Number(option.price) > 0 ? `+${currencyFormatter.format(option.price)}` : "Included";
            return `
              <label class="choice-card${singleChoice ? "" : " choice-card--checkbox"}">
                <input
                  type="${inputType}"
                  name="product-option-${escapeHtml(group.id)}"
                  value="${escapeHtml(option.id)}"
                  data-option-group="${escapeHtml(group.id)}"
                  data-price="${Number(option.price)}"
                  ${isChecked ? "checked" : ""}
                />
                <span><strong>${escapeHtml(option.name)}</strong><small>${escapeHtml(option.description || "No extra details")}</small></span>
                <b>${priceLabel}</b>
              </label>`;
          }).join("")}
        </div>
        ${!singleChoice ? `<p class="option-group__hint">Choose ${group.minimumSelections}–${group.maximumSelections}</p>` : ""}
      </fieldset>`;
  }).join("");

  return groups.every((group) => Array.isArray(group.options) && group.options.length >= group.minimumSelections);
}

function openCustomizer(product, trigger) {
  if (!customizer || !customizerForm) return;

  selectedProduct = product;
  selectedQuantity = 1;
  lastCustomizerTrigger = trigger;
  const canConfigure = renderCustomizerOptions(product);
  customizerForm.reset();
  if (customizerAddButton) customizerAddButton.disabled = !canConfigure;

  if (customizerImage) {
    customizerImage.src = product.image;
    customizerImage.alt = product.name;
  }
  if (customizerName) customizerName.textContent = product.name;
  if (customizerDescription) customizerDescription.textContent = product.description;

  updateCustomizerSummary();
  customizer.showModal();
  syncModalState();
}

function closeCustomizer() {
  if (customizer?.open) customizer.close();
}

categoryFilter?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-category-id]");
  if (!button) return;

  activeCategory = button.dataset.categoryId;
  showAllPopular = false;
  renderCategoryButtons();
  renderProducts();
});

productGrid?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-product-action]");
  if (!button) return;

  const product = products.find((item) => item.id === button.dataset.productAction);
  if (!product) return;

  if (product.customisable) {
    openCustomizer(product, button);
    return;
  }

  addQuickProduct(product);
  showToast(`${product.name} added to your demo basket.`);
});

showAllButton?.addEventListener("click", () => {
  activeCategory = "all";
  showAllPopular = true;
  renderCategoryButtons();
  renderProducts();
});

customizerClose?.addEventListener("click", closeCustomizer);

customizer?.addEventListener("click", (event) => {
  if (event.target === customizer) closeCustomizer();
});

customizer?.addEventListener("close", () => {
  syncModalState();
  lastCustomizerTrigger?.focus();
});

customizer?.addEventListener("cancel", () => {
  syncModalState();
});

customizerForm?.addEventListener("change", (event) => {
  const changedOption = event.target.closest('input[data-option-group][type="checkbox"]');
  if (changedOption?.checked && selectedProduct) {
    const group = selectedProduct.optionGroups.find((candidate) => candidate.id === changedOption.dataset.optionGroup);
    const selectedInGroup = customizerForm.querySelectorAll(`input[data-option-group="${CSS.escape(changedOption.dataset.optionGroup)}"]:checked`);
    if (group && selectedInGroup.length > group.maximumSelections) {
      changedOption.checked = false;
      showToast(`Choose no more than ${group.maximumSelections} option${group.maximumSelections === 1 ? "" : "s"} for ${group.name}.`);
    }
  }
  updateCustomizerSummary();
});

customizerForm?.addEventListener("click", (event) => {
  const quantityButton = event.target.closest("[data-quantity-change]");
  if (!quantityButton) return;

  selectedQuantity = Math.min(10, Math.max(1, selectedQuantity + Number(quantityButton.dataset.quantityChange)));
  updateCustomizerSummary();
});

customizerForm?.addEventListener("submit", (event) => {
  event.preventDefault();
  if (!selectedProduct) return;

  const selections = {};
  for (const group of selectedProduct.optionGroups) {
    const selectedIds = [...customizerForm.querySelectorAll(`input[data-option-group="${CSS.escape(group.id)}"]:checked`)]
      .map((input) => input.value);
    if (selectedIds.length < group.minimumSelections || selectedIds.length > group.maximumSelections) {
      showToast(`Choose between ${group.minimumSelections} and ${group.maximumSelections} option${group.maximumSelections === 1 ? "" : "s"} for ${group.name}.`);
      return;
    }
    selections[group.id] = selectedIds;
  }

  const configuration = resolveCartConfiguration(selectedProduct, selections);
  if (!configuration) {
    showToast("These choices are no longer available. Please reopen the product and try again.");
    return;
  }

  cartItems.push({
    key: `${selectedProduct.id}-${Date.now()}`,
    productId: selectedProduct.id,
    name: selectedProduct.name,
    image: selectedProduct.image,
    unitPrice: configuration.unitPrice,
    quantity: selectedQuantity,
    details: configuration.details,
    selections,
  });
  renderBasket();
  showToast(`${selectedQuantity} × ${selectedProduct.name} added to your demo basket.`);
  closeCustomizer();
});

basketOpenButtons.forEach((button) => button.addEventListener("click", openBasket));
basketCloseButton?.addEventListener("click", closeBasket);

basket?.addEventListener("click", (event) => {
  if (event.target === basket) closeBasket();
});

basket?.addEventListener("close", syncModalState);
basket?.addEventListener("cancel", syncModalState);

basketBrowseButton?.addEventListener("click", () => {
  closeBasket();
  document.querySelector("#menu")?.scrollIntoView({ behavior: "smooth" });
});

basketItems?.addEventListener("click", (event) => {
  const actionButton = event.target.closest("[data-basket-action]");
  if (!actionButton) return;

  const item = cartItems.find((entry) => entry.key === actionButton.dataset.basketKey);
  if (!item) return;

  if (actionButton.dataset.basketAction === "increase") item.quantity = Math.min(10, item.quantity + 1);
  if (actionButton.dataset.basketAction === "decrease") item.quantity -= 1;
  if (actionButton.dataset.basketAction === "remove" || item.quantity < 1) {
    cartItems = cartItems.filter((entry) => entry.key !== item.key);
  }
  renderBasket();
});

document.querySelectorAll("[data-order-type]").forEach((button) => {
  button.addEventListener("click", () => {
    orderType = button.dataset.orderType;
    renderBasket();
  });
});

feeInfoButton?.addEventListener("click", () => {
  showToast(`A fixed ${currencyFormatter.format(shopOperations.serviceFee)} service fee applies once per successful online order.`);
});

basketClearButton?.addEventListener("click", () => {
  cartItems = [];
  renderBasket();
  showToast("Your demo basket has been cleared.");
  basketBrowseButton?.focus();
});

checkoutButton?.addEventListener("click", openCheckout);

checkoutCloseButton?.addEventListener("click", closeCheckout);

checkoutDialog?.addEventListener("click", (event) => {
  if (event.target === checkoutDialog) closeCheckout();
});

checkoutDialog?.addEventListener("close", () => {
  checkoutForm?.reset();
  syncModalState();
  if (checkoutIsCompleting) {
    checkoutIsCompleting = false;
  } else {
    [...basketOpenButtons].find((button) => button.getClientRects().length > 0)?.focus();
  }
});

checkoutDialog?.addEventListener("cancel", syncModalState);

checkoutForm?.addEventListener("submit", async (event) => {
  event.preventDefault();

  if (!cartItems.length || !checkoutSubmitButton) return;

  const formData = new FormData(checkoutForm);
  const blocked = checkoutBlockReason();
  if (blocked) {
    showToast(blocked);
    return;
  }

  const postcode = String(formData.get("checkout-postcode") ?? "").trim();
  if (orderType === "delivery" && !coversDeliveryPostcode(postcode)) {
    showToast("Sorry, we do not currently deliver to that postcode.");
    checkoutForm.querySelector('[name="checkout-postcode"]')?.focus();
    return;
  }

  const customerName = String(formData.get("checkout-name") ?? "").trim();
  const submittedItems = cartItems.map((item) => ({
    productId: item.productId,
    quantity: item.quantity,
    selections: item.selections,
  }));
  const confirmationItemsSnapshot = cartItems.map((item) => ({ ...item }));
  const submittedOrderType = orderType;

  checkoutSubmitButton.disabled = true;
  checkoutSubmitButton.setAttribute("aria-busy", "true");
  if (checkoutSubmitLabel) checkoutSubmitLabel.textContent = "Saving test order…";

  try {
    const response = await fetch("/api/orders", {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        customerName,
        customerEmail: String(formData.get("checkout-email") ?? "").trim(),
        customerPhone: String(formData.get("checkout-phone") ?? "").trim(),
        orderType: submittedOrderType,
        postcode: postcode || null,
        addressLine: String(formData.get("checkout-address") ?? "").trim() || null,
        orderNotes: String(formData.get("checkout-notes") ?? "").trim() || null,
        items: submittedItems,
      }),
    });

    const result = await response.json().catch(() => null);
    if (!response.ok) {
      const serverMessage = result?.errors
        ? Object.values(result.errors).flat().find(Boolean)
        : null;
      throw new Error(serverMessage || "The test order could not be saved. Please try again.");
    }

    renderConfirmation({
      customerName: customerName.split(/\s+/)[0],
      items: confirmationItemsSnapshot,
      type: result.type,
      orderNumber: result.orderNumber,
      estimatedTime: result.estimatedTime,
      totals: {
        subtotal: result.subtotal,
        deliveryFee: result.deliveryFee,
        serviceFee: result.serviceFee,
        total: result.total,
      },
    });

    checkoutIsCompleting = true;
    closeCheckout();
    cartItems = [];
    renderBasket();
    confirmationDialog?.showModal();
    syncModalState();
  } catch (error) {
    console.error("Could not submit the test order.", error);
    showToast(error instanceof Error ? error.message : "The test order could not be saved.");
  } finally {
    checkoutSubmitButton.disabled = false;
    checkoutSubmitButton.removeAttribute("aria-busy");
    if (checkoutSubmitLabel) checkoutSubmitLabel.textContent = "Save test order";
  }
});

confirmationCloseButton?.addEventListener("click", () => {
  closeConfirmation();
  document.querySelector("#menu")?.scrollIntoView({ behavior: "smooth" });
});

confirmationDialog?.addEventListener("click", (event) => {
  if (event.target === confirmationDialog) closeConfirmation();
});

confirmationDialog?.addEventListener("close", syncModalState);
confirmationDialog?.addEventListener("cancel", syncModalState);

async function initialiseApp() {
  await Promise.all([loadMenuData(), loadShopOperations()]);
  restoreBasketState();
  renderShopOperations();
  renderCategoryButtons();
  renderProducts();
  renderBasket();
}

void initialiseApp();
