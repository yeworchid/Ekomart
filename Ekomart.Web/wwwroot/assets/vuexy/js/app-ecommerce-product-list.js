/**
 * app-ecommerce-product-list
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const dtProductTable = document.querySelector('.datatables-products');

  if (!dtProductTable) {
    return;
  }

  const productAdd = '/Admin/Products/Create';

  const dtProducts = new DataTable(dtProductTable, {
    columnDefs: [
      {
        className: 'control',
        searchable: false,
        orderable: false,
        responsivePriority: 2,
        targets: 0
      },
      {
        targets: 1,
        orderable: false,
        searchable: false,
        responsivePriority: 3,
        checkboxes: {
          selectAllRender: '<input type="checkbox" class="form-check-input">'
        }
      },
      {
        targets: 2,
        responsivePriority: 1
      },
      {
        targets: 3,
        responsivePriority: 5
      },
      {
        targets: 4,
        orderable: false,
        responsivePriority: 3
      },
      {
        targets: 7,
        responsivePriority: 4
      },
      {
        targets: -1,
        searchable: false,
        orderable: false
      }
    ],
    select: {
      style: 'multi',
      selector: 'td:nth-child(2)'
    },
    order: [2, 'asc'],
    displayLength: 7,
    layout: {
      topStart: {
        rowClass: 'card-header d-flex border-top rounded-0 flex-wrap py-0 flex-column flex-md-row align-items-start',
        features: [
          {
            search: {
              className: 'me-5 ms-n4 pe-5 mb-n6 mb-md-0',
              placeholder: 'Search Product',
              text: '_INPUT_'
            }
          }
        ]
      },
      topEnd: {
        rowClass: 'row m-3 my-0 justify-content-between',
        features: [
          {
            pageLength: {
              menu: [7, 10, 25, 50, 100],
              text: '_MENU_'
            },
            buttons: [
              {
                extend: 'collection',
                className: 'btn btn-label-secondary dropdown-toggle me-4',
                text: '<span class="d-flex align-items-center gap-1"><i class="icon-base ti tabler-upload icon-xs"></i> <span class="d-none d-sm-inline-block">Export</span></span>',
                buttons: [
                  exportButton('print', 'Print', 'tabler-printer'),
                  exportButton('csv', 'Csv', 'tabler-file'),
                  exportButton('excel', 'Excel', 'tabler-upload'),
                  exportButton('pdf', 'Pdf', 'tabler-file-text'),
                  exportButton('copy', 'Copy', 'tabler-copy')
                ]
              },
              {
                text: '<i class="icon-base ti tabler-plus me-0 me-sm-1 icon-16px"></i><span class="d-none d-sm-inline-block">Add Product</span>',
                className: 'add-new btn btn-primary',
                action: function () {
                  window.location.href = productAdd;
                }
              }
            ]
          }
        ]
      },
      bottomStart: {
        rowClass: 'row mx-3 justify-content-between',
        features: ['info']
      },
      bottomEnd: 'paging'
    },
    language: {
      paginate: {
        next: '<i class="icon-base ti tabler-chevron-right scaleX-n1-rtl icon-18px"></i>',
        previous: '<i class="icon-base ti tabler-chevron-left scaleX-n1-rtl icon-18px"></i>',
        first: '<i class="icon-base ti tabler-chevrons-left scaleX-n1-rtl icon-18px"></i>',
        last: '<i class="icon-base ti tabler-chevrons-right scaleX-n1-rtl icon-18px"></i>'
      }
    },
    responsive: {
      details: {
        display: DataTable.Responsive.display.modal({
          header: function (row) {
            return row.node().querySelector('.product-name h6')?.textContent.trim() || 'Product details';
          }
        }),
        type: 'column',
        renderer: responsiveRenderer
      }
    },
    initComplete: function () {
      const api = this.api();

      addColumnFilter(api, -2, '.product_status', 'Status');
      addColumnFilter(api, 3, '.product_category', 'Category');
      addColumnFilter(api, 4, '.product_stock', 'Stock');
    }
  });

  window.ekomartProductsTable = dtProducts;

  applyDatatableClasses();
  window.setTimeout(applyDatatableClasses, 100);
});

function exportButton(extend, title, icon) {
  return {
    extend,
    text: `<span class="d-flex align-items-center"><i class="icon-base ti ${icon} me-1"></i>${title}</span>`,
    className: 'dropdown-item',
    exportOptions: {
      columns: [3, 4, 5, 6, 7],
      format: {
        body: function (inner) {
          return textFromHtml(inner);
        }
      }
    },
    customize: function (win) {
      if (extend !== 'print') {
        return;
      }

      const table = win.document.body.querySelector('table');
      win.document.body.style.color = config.colors.headingColor;
      win.document.body.style.borderColor = config.colors.borderColor;
      win.document.body.style.backgroundColor = config.colors.bodyBg;
      table?.classList.add('compact');
    }
  };
}

function addColumnFilter(api, columnIndex, containerSelector, placeholder) {
  const container = document.querySelector(containerSelector);
  if (!container) {
    return;
  }

  const column = api.column(columnIndex);
  const select = document.createElement('select');
  const values = new Set();

  select.className = 'form-select text-capitalize';
  select.innerHTML = `<option value="">${placeholder}</option>`;
  container.replaceChildren(select);

  column
    .data()
    .unique()
    .sort()
    .each(function (value) {
      const label = textFromHtml(value);
      if (label.length > 0) {
        values.add(label);
      }
    });

  Array.from(values)
    .sort()
    .forEach(function (label) {
      const option = document.createElement('option');
      option.value = label;
      option.textContent = label;
      select.appendChild(option);
    });

  select.addEventListener('change', function () {
    column.search(select.value, false, true).draw();
  });
}

function responsiveRenderer(api, rowIdx, columns) {
  const data = columns
    .map(function (col) {
      return col.title !== ''
        ? `<tr data-dt-row="${col.rowIndex}" data-dt-column="${col.columnIndex}">
            <td>${col.title}:</td>
            <td>${col.data}</td>
          </tr>`
        : '';
    })
    .join('');

  if (!data) {
    return false;
  }

  const div = document.createElement('div');
  const table = document.createElement('table');
  const tbody = document.createElement('tbody');

  div.classList.add('table-responsive');
  table.classList.add('table');
  tbody.innerHTML = data;
  table.appendChild(tbody);
  div.appendChild(table);

  return div;
}

function textFromHtml(value) {
  const wrapper = document.createElement('div');
  wrapper.innerHTML = String(value || '');

  return (wrapper.textContent || wrapper.innerText || '').replace(/\s+/g, ' ').trim();
}

function applyDatatableClasses() {
  const elementsToModify = [
    { selector: '.dt-buttons .btn', classToRemove: 'btn-secondary' },
    { selector: '.dt-buttons.btn-group', classToAdd: 'mb-md-0 mb-6' },
    { selector: '.dt-search .form-control', classToRemove: 'form-control-sm', classToAdd: 'ms-0' },
    { selector: '.dt-search', classToAdd: 'mb-0 mb-md-6' },
    { selector: '.dt-length .form-select', classToRemove: 'form-select-sm' },
    { selector: '.dt-layout-end', classToAdd: 'gap-md-2 gap-0 mt-0' },
    { selector: '.dt-layout-start', classToAdd: 'mt-0' },
    { selector: '.dt-layout-table', classToRemove: 'row mt-2' },
    { selector: '.dt-layout-full', classToRemove: 'col-md col-12', classToAdd: 'table-responsive' }
  ];

  elementsToModify.forEach(({ selector, classToRemove, classToAdd }) => {
    document.querySelectorAll(selector).forEach(element => {
      classToRemove?.split(' ').forEach(className => element.classList.remove(className));
      classToAdd?.split(' ').forEach(className => element.classList.add(className));
    });
  });
}
