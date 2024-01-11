class BitDropdown {
    static changeSelectedItem(dotnetObj: DotNetObject, inputElement: HTMLInputElement, scrollContainerId: string, isMultiSelect: boolean, isArrowUp: boolean) {
        const selectors = '#' + scrollContainerId + (isMultiSelect ? ' div.bit-drp-iwr:not([style*="display: none"], .bit-drp-ids)' : ' > div > button.bit-drp-itm:not([disabled])');
        const items = document.querySelectorAll(selectors);

        let selectedIndex = -1;
        for (let i = 0; i < items.length; i++) {
            if (items[i].classList.contains('bit-drp-sli')) {
                items[i].classList.remove('bit-drp-sli');
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0 || items.length == 1)
        {
            selectedIndex = isArrowUp ? items.length - 1 : 0;
        }
        else if (selectedIndex == items.length - 1 && !isArrowUp)
        {
            selectedIndex = 0;
        }
        else if (selectedIndex == 0 && isArrowUp)
        {
            selectedIndex = items.length - 1;
        }
        else if (isArrowUp)
        {
            selectedIndex--;
        }
        else
        {
            selectedIndex++;
        }

        let selectedItem = items[selectedIndex] as HTMLElement;

        selectedItem.classList.add('bit-drp-sli');

        //Get container properties
        const scrollContainer = document.getElementById(scrollContainerId);
        if (scrollContainer == null) return;

        const cTop = scrollContainer.scrollTop;
        const cBottom = cTop + scrollContainer.clientHeight;

        //Get element properties
        const eTop = selectedItem.offsetTop;
        const eBottom = eTop + selectedItem.clientHeight;

        //Check if in view    
        const isInView = (eTop >= cTop && eBottom <= cBottom);

        if (isInView === false) {
            scrollContainer.scrollTop = cTop + (scrollContainer.clientHeight / 2);
        }

        const selectedItemId = isMultiSelect ? (selectedItem.getElementsByTagName('button').item(0) as HTMLElement).id : selectedItem.id;

        if (selectedItemId != null && selectedItemId != '') {
            dotnetObj?.invokeMethodAsync('SetSelectedItemId', selectedItemId)
        }

        inputElement.selectionStart = inputElement.selectionEnd = inputElement.value.length;
    }
}
