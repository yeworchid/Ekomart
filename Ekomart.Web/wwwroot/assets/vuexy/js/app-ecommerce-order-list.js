/**
 * app-ecommerce-order-list
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const dtOrderTable = document.querySelector('.datatables-order');

  if (!dtOrderTable) {
    return;
  }

  const dtOrders = new DataTable(dtOrderTable, {
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
        responsivePriority: 2
      },
      {
        targets: 4,
        responsivePriority: 1
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
    order: [3, 'desc'],
    layout: {
      topStart: {
        search: {
          placeholder: 'Search Order',
          text: '_INPUT_'
        }
      },
      topEnd: {
        rowClass: 'row mx-3 my-0 justify-content-between',
        features: [
          {
            pageLength: {
              menu: [7, 10, 25, 50, 100],
              text: '_MENU_'
            }
          },
          {
            buttons: [
              {
                extend: 'collection',
                className: 'btn btn-label-primary dropdown-toggle',
                text: '<span class="d-flex align-items-center gap-1"><i class="icon-base ti tabler-upload icon-xs"></i> <span class="d-none d-sm-inline-block">Export</span></span>',
                buttons: [
                  exportButton('print', 'Print', 'tabler-printer'),
                  exportButton('csv', 'Csv', 'tabler-file'),
                  exportButton('excel', 'Excel', 'tabler-upload'),
                  exportButton('pdf', 'Pdf', 'tabler-file-text'),
                  exportButton('copy', 'Copy', 'tabler-copy')
                ]
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
            return row.node().querySelector('.order-name h6')?.textContent.trim() || 'Order details';
          }
        }),
        type: 'column',
        renderer: responsiveRenderer
      }
    }
  });

  window.ekomartOrdersTable = dtOrders;

  document.addEventListener('click', function (event) {
    if (!event.target.classList.contains('delete-record')) {
      return;
    }

    dtOrders.row(event.target.closest('tr')).remove().draw();

    const modalEl = document.querySelector('.dtr-bs-modal');
    if (modalEl && modalEl.classList.contains('show')) {
      bootstrap.Modal.getInstance(modalEl)?.hide();
    }
  });

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
    { selector: '.dt-buttons .btn', classToRemove: 'btn-secondary', classToAdd: 'btn-label-secondary' },
    { selector: '.dt-search .form-control', classToRemove: 'form-control-sm', classToAdd: 'ms-0' },
    { selector: '.dt-length .form-select', classToRemove: 'form-select-sm' },
    { selector: '.dt-length', classToAdd: 'mt-md-6 mt-0' },
    { selector: '.dt-layout-table', classToRemove: 'row mt-2' },
    { selector: '.dt-layout-end', classToAdd: 'px-3 mt-0' },
    { selector: '.dt-layout-end .dt-buttons', classToAdd: 'gap-2 px-3 mt-0 mb-md-0 mb-6' },
    { selector: '.dt-layout-end .dt-buttons .btn-group', classToAdd: 'mx-auto' },
    { selector: '.dt-layout-start', classToAdd: 'px-3 mt-0' },
    { selector: '.dt-layout-full', classToRemove: 'col-md col-12', classToAdd: 'table-responsive' }
  ];

  elementsToModify.forEach(({ selector, classToRemove, classToAdd }) => {
    document.querySelectorAll(selector).forEach(element => {
      classToRemove?.split(' ').forEach(className => element.classList.remove(className));
      classToAdd?.split(' ').forEach(className => element.classList.add(className));
    });
  });
}
