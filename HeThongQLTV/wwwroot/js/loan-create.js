(() => {
    const form = document.getElementById('loan-create');
    if (!form) return;
    const member = document.getElementById('MemberId');
    const reservation = form.dataset.mode === 'reservation';
    const verb = reservation ? 'đặt trước' : 'mượn';
    const picker = document.getElementById('book-picker');
    const quantity = document.getElementById('book-quantity');
    const editor = document.getElementById('quantity-editor');
    const body = document.getElementById('basket-items');
    const confirm = document.getElementById('confirm-loan');
    const books = new Map([...picker.options].filter(o => o.value).map(o => [Number(o.value), {
        title: o.dataset.title, author: o.dataset.author, stock: Number(o.dataset.stock)
    }]));
    let items = JSON.parse(document.getElementById('initial-loan-items').textContent);
    const limit = 5;
    const remaining = () => Math.max(0, limit - Number(reservation ? form.dataset.pending : member?.selectedOptions[0]?.dataset.borrowed || 0));
    const total = () => items.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0);
    const message = (id, text) => {
        const element = document.getElementById(id);
        element.textContent = text;
        element.hidden = !text;
    };
    function validate() {
        let error = '';
        for (const item of items) {
            const book = books.get(item.bookId);
            if (!book) error = 'Có sách không còn tồn tại. Hãy xóa sách đó và chọn lại.';
            else if (!Number.isInteger(item.quantity) || item.quantity < 1) error = 'Số lượng mỗi sách phải là số nguyên lớn hơn 0.';
            else if (!reservation && item.quantity > book.stock) error = `Sách “${book.title}” chỉ còn ${book.stock} bản trong kho.`;
            if (error) break;
        }
        if (!error && total() > limit) error = 'Tổng số lượng không được vượt quá 5 quyển sách.';
        if (!error && (reservation || member?.value) && total() > remaining()) error = `Chỉ còn được ${verb} thêm ${remaining()} quyển.`;
        message('basket-error', error);
        document.getElementById('loan-total').textContent = `${total()} / ${limit} quyển`;
        document.getElementById('reader-limit').textContent = reservation
            ? `Bạn đang có ${limit - remaining()} quyển chờ nhận, có thể đặt thêm ${remaining()} quyển.`
            : member.value ? `Độc giả đang mượn ${limit - remaining()} quyển, còn được mượn thêm ${remaining()} quyển.`
            : 'Chọn độc giả để kiểm tra hạn mức mượn.';
        confirm.disabled = (!reservation && !member.value) || items.length === 0 || Boolean(error);
        return !confirm.disabled;
    }
    function render() {
        body.replaceChildren();
        items.forEach((item, index) => {
            const book = books.get(item.bookId);
            const row = document.createElement('tr');
            row.dataset.bookId = item.bookId;
            const name = row.insertCell();
            const title = document.createElement('b');
            title.textContent = book?.title || 'Sách không còn tồn tại';
            const author = document.createElement('small');
            author.textContent = book?.author || '';
            const id = document.createElement('input');
            id.type = 'hidden'; id.name = `Items[${index}].BookId`; id.value = item.bookId;
            name.append(title, author, id);
            row.insertCell().textContent = book?.stock ?? 0;
            const input = document.createElement('input');
            input.type = 'number'; input.min = '1'; input.max = reservation ? limit : Math.min(limit, Math.max(0, book?.stock || 0));
            input.step = '1'; input.required = true; input.name = `Items[${index}].Quantity`;
            input.value = item.quantity;
            input.setAttribute('aria-label', `Số lượng ${verb} ${book?.title || ''}`);
            input.addEventListener('input', () => { item.quantity = input.valueAsNumber; validate(); });
            row.insertCell().append(input);
            const remove = document.createElement('button');
            remove.type = 'button'; remove.className = 'text-button danger-text'; remove.textContent = 'Xóa';
            remove.setAttribute('aria-label', `Xóa ${book?.title || 'sách'}`);
            remove.addEventListener('click', () => {
                items.splice(index, 1); message('picker-error', ''); render(); picker.focus();
            });
            row.insertCell().append(remove);
            body.append(row);
        });
        document.getElementById('basket-empty').hidden = items.length > 0;
        document.getElementById('basket-table').hidden = items.length === 0;
        validate();
    }
    picker.addEventListener('change', () => {
        const book = books.get(Number(picker.value));
        editor.hidden = !book; quantity.disabled = !book; quantity.value = '1';
        quantity.max = reservation ? limit : Math.min(limit, Math.max(0, book?.stock || 0));
        document.getElementById('book-stock').textContent = book ? (reservation && book.stock <= 0 ? 'Sách hết bản sẵn có; yêu cầu sẽ vào hàng chờ.' : `Còn ${book.stock} bản trong kho.`) : '';
        message('picker-error', '');
    });
    function add() {
        const bookId = Number(picker.value), book = books.get(bookId), count = quantity.valueAsNumber;
        if (!book) return message('picker-error', 'Vui lòng chọn sách.');
        if (!Number.isInteger(count) || count < 1) return message('picker-error', 'Số lượng phải là số nguyên lớn hơn 0.');
        const existing = items.find(i => i.bookId === bookId);
        if (existing && (!Number.isInteger(existing.quantity) || existing.quantity < 1))
            return message('picker-error', 'Hãy sửa số lượng của sách đã chọn trong khung bên dưới.');
        if (!reservation && (existing?.quantity || 0) + count > book.stock)
            return message('picker-error', `Sách này chỉ còn ${book.stock} bản trong kho.`);
        if (total() + count > limit) return message('picker-error', 'Tổng số lượng không được vượt quá 5 quyển sách.');
        if ((reservation || member?.value) && total() + count > remaining())
            return message('picker-error', `Chỉ còn được ${verb} thêm ${remaining()} quyển.`);
        if (existing) existing.quantity += count;
        else items.push({ bookId, quantity: count });
        picker.value = ''; editor.hidden = true; quantity.disabled = true;
        message('picker-error', ''); render(); picker.focus();
    }
    document.getElementById('add-book').addEventListener('click', add);
    quantity.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); add(); } });
    member?.addEventListener('change', () => { message('picker-error', ''); validate(); });
    form.addEventListener('submit', event => { if (!validate()) event.preventDefault(); });
    render();
    if (picker.value) picker.dispatchEvent(new Event('change'));
})();
