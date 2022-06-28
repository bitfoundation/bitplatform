"use strict";
var BitOverflowDropDownMenu = /** @class */ (function () {
    function BitOverflowDropDownMenu() {
    }
    BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout = function (dotnetObjReference, dropDownWrapperId, dropDownId, dropDownCalloutId, dropDownOverlayId, isOpen) {
        var dropDownWrapper = document.getElementById(dropDownWrapperId);
        if (dropDownWrapper == null)
            return;
        var dropDown = document.getElementById(dropDownId);
        if (dropDown == null)
            return;
        var dropDownCallout = document.getElementById(dropDownCalloutId);
        if (dropDownCallout == null)
            return;
        var dropDownOverlay = document.getElementById(dropDownOverlayId);
        if (dropDownOverlay == null)
            return;
        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropDownCalloutId = "";
        }
        else {
            Bit.currentDropDownCalloutId = dropDownCalloutId;
            Bit.closeCurrentCalloutIfExists(dropDownCalloutId, dropDownOverlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
            var dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            var dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            var dropDownX = dropDown.getBoundingClientRect().x;
            dropDownCallout.style.maxWidth = dropDownWrapperWidth + dropDownWrapperX - dropDownX + "px";
            var dropDownCalloutHeight = dropDownCallout.offsetHeight;
            var dropDownCalloutWidth = dropDownCallout.offsetWidth;
            var dropDownTop = dropDown.getBoundingClientRect().y;
            var dropDownHeight = dropDown.offsetHeight;
            var dropDownWidth = dropDown.offsetWidth;
            var dropDownY = dropDown.getBoundingClientRect().y;
            var dropDownBottom = window.innerHeight - (dropDownHeight + dropDownY);
            var dropDownRight = window.innerWidth - (dropDownWidth + dropDownX);
            if (dropDownBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownY + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            }
            else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.bottom = dropDownBottom + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else if (dropDownRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownX + dropDownWidth + 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else {
                dropDownCallout.style.left = dropDownX - dropDownCalloutWidth - 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.right = "unset";
            }
        }
    };
    return BitOverflowDropDownMenu;
}());
var BitColorPicker = /** @class */ (function () {
    function BitColorPicker() {
    }
    BitColorPicker.registerOnWindowMouseUpEvent = function (dotnetHelper, callback) {
        var controller = new BitAbortController();
        var listenerOptions = new BitEventListenerOptions();
        listenerOptions.signal = controller.abortController.signal;
        document.addEventListener('mouseup', function (e) {
            var eventArgs = BitColorPicker.toMouseEventArgsMapper(e);
            dotnetHelper.invokeMethodAsync(callback, eventArgs);
        }, listenerOptions);
        BitColorPicker.listOfAbortControllers.push(controller);
        return controller.id;
    };
    BitColorPicker.registerOnWindowMouseMoveEvent = function (dotnetHelper, callback) {
        var controller = new BitAbortController();
        var listenerOptions = new BitEventListenerOptions();
        listenerOptions.signal = controller.abortController.signal;
        document.addEventListener('mousemove', function (e) {
            var eventArgs = BitColorPicker.toMouseEventArgsMapper(e);
            dotnetHelper.invokeMethodAsync(callback, eventArgs);
        }, listenerOptions);
        BitColorPicker.listOfAbortControllers.push(controller);
        return controller.id;
    };
    BitColorPicker.toMouseEventArgsMapper = function (e) {
        return {
            altKey: e.altKey,
            button: e.button,
            buttons: e.buttons,
            clientX: e.clientX,
            clientY: e.clientY,
            ctrlKey: e.ctrlKey,
            detail: e.detail,
            metaKey: e.metaKey,
            offsetX: e.offsetX,
            offsetY: e.offsetY,
            screenY: e.screenY,
            screenX: e.screenX,
            shiftKey: e.shiftKey,
            type: e.type
        };
    };
    BitColorPicker.abortProcedure = function (id) {
        var _a;
        var aborController = (_a = BitColorPicker.listOfAbortControllers.find(function (ac) { return ac.id == id; })) === null || _a === void 0 ? void 0 : _a.abortController;
        if (aborController) {
            aborController.abort();
        }
    };
    BitColorPicker.listOfAbortControllers = [];
    return BitColorPicker;
}());
var BitEventListenerOptions = /** @class */ (function () {
    function BitEventListenerOptions() {
    }
    return BitEventListenerOptions;
}());
var BitAbortController = /** @class */ (function () {
    function BitAbortController() {
        this.id = Date.now().toString();
        this.abortController = new AbortController();
    }
    return BitAbortController;
}());
var BitDatePicker = /** @class */ (function () {
    function BitDatePicker() {
    }
    BitDatePicker.toggleDatePickerCallout = function (dotnetObjReference, datePickerId, datePickerCalloutId, datePickerOverlayId, isOpen) {
        var datePicker = document.getElementById(datePickerId);
        if (datePicker == null)
            return;
        var datePickerCallout = document.getElementById(datePickerCalloutId);
        if (datePickerCallout == null)
            return;
        var datePickerOverlay = document.getElementById(datePickerOverlayId);
        if (datePickerOverlay == null)
            return;
        if (isOpen) {
            datePickerCallout.style.display = "none";
            datePickerOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        }
        else {
            Bit.closeCurrentCalloutIfExists(datePickerCalloutId, datePickerOverlayId, dotnetObjReference);
            datePickerCallout.style.display = "block";
            datePickerOverlay.style.display = "block";
            var datePickerCalloutHeight = datePickerCallout.offsetHeight;
            var datePickerCalloutWidth = datePickerCallout.offsetWidth;
            var datePickerHeight = datePicker.offsetHeight;
            var datePickerWidth = datePicker.offsetWidth;
            var datePickerX = datePicker.getBoundingClientRect().x;
            var datePickerY = datePicker.getBoundingClientRect().y;
            var datePickerTop = datePicker.getBoundingClientRect().y;
            var datePickerWrapperBottom = window.innerHeight - (datePickerHeight + datePickerY);
            var datePickerWrapperRight = window.innerWidth - (datePickerWidth + datePickerX);
            if (datePickerWrapperBottom >= datePickerCalloutHeight) {
                datePickerCallout.style.top = datePickerY + datePickerHeight + 1 + "px";
                datePickerCallout.style.left = datePickerX + "px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.bottom = "unset";
            }
            else if (datePickerTop >= datePickerCalloutHeight) {
                datePickerCallout.style.bottom = datePickerWrapperBottom + datePickerHeight + 1 + "px";
                ;
                datePickerCallout.style.left = datePickerX + "px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset";
            }
            else if (datePickerWrapperRight >= datePickerCalloutWidth) {
                datePickerCallout.style.left = datePickerX + datePickerWidth + 1 + "px";
                datePickerCallout.style.bottom = "2px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset";
            }
            else {
                datePickerCallout.style.left = datePickerX - datePickerCalloutWidth - 1 + "px";
                datePickerCallout.style.bottom = "2px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset";
            }
        }
    };
    BitDatePicker.checkMonthPickerWidth = function (datePickerCalloutId) {
        var datePickerCallout = document.getElementById(datePickerCalloutId);
        if (datePickerCallout == null)
            return;
        return datePickerCallout.offsetWidth > window.innerWidth;
    };
    return BitDatePicker;
}());
var BitDropDown = /** @class */ (function () {
    function BitDropDown() {
    }
    BitDropDown.toggleDropDownCallout = function (dotnetObjReference, dropDownWrapperId, dropDownId, dropDownCalloutId, dropDownOverlayId, isOpen) {
        var dropDownWrapper = document.getElementById(dropDownWrapperId);
        if (dropDownWrapper == null)
            return;
        var dropDown = document.getElementById(dropDownId);
        if (dropDown == null)
            return;
        var dropDownCallout = document.getElementById(dropDownCalloutId);
        if (dropDownCallout == null)
            return;
        var dropDownOverlay = document.getElementById(dropDownOverlayId);
        if (dropDownOverlay == null)
            return;
        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropDownCalloutId = "";
        }
        else {
            Bit.currentDropDownCalloutId = dropDownCalloutId;
            Bit.closeCurrentCalloutIfExists(dropDownCalloutId, dropDownOverlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
            var dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            dropDownCallout.style.width = dropDownWrapperWidth + 'px';
            var dropDownCalloutHeight = dropDownCallout.offsetHeight;
            var dropDownCalloutWidth = dropDownCallout.offsetWidth;
            var dropDownHeight = dropDown.offsetHeight;
            var dropDownTop = dropDown.getBoundingClientRect().y;
            var dropDownWrapperHeight = dropDownWrapper.offsetHeight;
            var dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            var dropDownWrapperY = dropDownWrapper.getBoundingClientRect().y;
            var dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            var dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);
            var minimumWidthForDropDownNormalOpen = 640;
            if (window.innerWidth < minimumWidthForDropDownNormalOpen) {
                dropDownCallout.style.top = "0";
                dropDownCallout.style.left = "unset";
                dropDownCallout.style.right = "0";
                dropDownCallout.style.bottom = "unset";
            }
            else if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                dropDownCallout.style.left = dropDownWrapperX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            }
            else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownWrapperX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownWrapperX + dropDownWrapperWidth + 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else {
                dropDownCallout.style.left = dropDownWrapperX - dropDownCalloutWidth - 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.right = "unset";
            }
        }
    };
    return BitDropDown;
}());
var BitFileUploader = /** @class */ (function () {
    function BitFileUploader() {
    }
    BitFileUploader.init = function (inputElement, dotnetReference, uploadEndpointUrl, headers) {
        var _this = this;
        var filesArray = Array.from(inputElement.files).map(function (file) { return ({
            name: file.name,
            size: file.size,
            type: file.type
        }); });
        this.bitFileUploads = [];
        this.headers = headers;
        filesArray.forEach(function (value, index) {
            var uploader = new BitFileUpload(dotnetReference, uploadEndpointUrl, inputElement, index, _this.headers);
            _this.bitFileUploads.push(uploader);
        });
        return filesArray;
    };
    BitFileUploader.upload = function (index, to, from) {
        if (index === -1) {
            this.bitFileUploads.forEach(function (bitFileUpload) {
                bitFileUpload.upload(to, from);
            });
        }
        else {
            var uploader = this.bitFileUploads.filter(function (f) { return f.index === index; })[0];
            uploader.upload(to, from);
        }
    };
    BitFileUploader.pause = function (index) {
        if (index === -1) {
            this.bitFileUploads.forEach(function (bitFileUpload) {
                bitFileUpload.pause();
            });
        }
        else {
            var uploader = this.bitFileUploads.filter(function (c) { return c.index === index; })[0];
            uploader.pause();
        }
    };
    return BitFileUploader;
}());
var BitFileUpload = /** @class */ (function () {
    function BitFileUpload(dotnetReference, uploadEndpointUrl, inputElement, index, headers) {
        this.xhr = new XMLHttpRequest();
        this.dotnetReference = dotnetReference;
        this.uploadEndpointUrl = uploadEndpointUrl;
        this.inputElement = inputElement;
        this.index = index;
        this.headers = headers;
        if (index < 0)
            return;
        this.xhr.upload.onprogress = function (e) {
            if (e.lengthComputable) {
                dotnetReference.invokeMethodAsync("HandleUploadProgress", index, e.loaded);
            }
        };
        var me = this;
        this.xhr.onreadystatechange = function (event) {
            if (me.xhr.readyState === 4) {
                dotnetReference.invokeMethodAsync("HandleFileUpload", index, me.xhr.status, me.xhr.responseText);
            }
        };
    }
    BitFileUpload.prototype.upload = function (to, from) {
        var _this = this;
        var files = this.inputElement.files;
        if (files === null)
            return;
        var file = files[this.index];
        var data = new FormData();
        var chunk = file.slice(to, from);
        data.append('file', chunk, file.name);
        this.xhr.open('POST', this.uploadEndpointUrl, true);
        Object.keys(this.headers).forEach(function (h) {
            _this.xhr.setRequestHeader(h, _this.headers[h]);
        });
        this.xhr.send(data);
    };
    BitFileUpload.prototype.pause = function () {
        this.xhr.abort();
    };
    return BitFileUpload;
}());
var BitLink = /** @class */ (function () {
    function BitLink() {
    }
    BitLink.scrollToFragmentOnClickEvent = function (targetElementId) {
        var element = document.getElementById(targetElementId);
        if (element != null) {
            element.scrollIntoView({
                behavior: "smooth",
                block: "start",
                inline: "nearest"
            });
        }
    };
    return BitLink;
}());
var CalloutComponent = /** @class */ (function () {
    function CalloutComponent() {
        this.calloutId = "";
        this.overlayId = "";
        this.objRef = null;
    }
    CalloutComponent.prototype.update = function (calloutId, overlayId, obj) {
        this.calloutId = calloutId;
        this.overlayId = overlayId;
        this.objRef = obj;
    };
    return CalloutComponent;
}());
var Bit = /** @class */ (function () {
    function Bit() {
    }
    Bit.setProperty = function (element, property, value) {
        element[property] = value;
    };
    Bit.getProperty = function (element, property) {
        return element[property];
    };
    Bit.getBoundingClientRect = function (element) {
        return element.getBoundingClientRect();
    };
    Bit.getClientHeight = function (element) {
        return element.clientHeight;
    };
    Bit.closeCurrentCalloutIfExists = function (calloutId, overlayId, obj) {
        var _a;
        if (Bit.currentCallout.calloutId.length === 0 || Bit.currentCallout.overlayId.length === 0) {
            Bit.currentCallout.update(calloutId, overlayId, obj);
            return;
        }
        if (calloutId !== Bit.currentCallout.calloutId && overlayId !== Bit.currentCallout.overlayId) {
            var callout = document.getElementById(Bit.currentCallout.calloutId);
            if (callout == null)
                return;
            var overlay = document.getElementById(Bit.currentCallout.overlayId);
            if (overlay == null)
                return;
            callout.style.display = "none";
            overlay.style.display = "none";
            (_a = Bit.currentCallout.objRef) === null || _a === void 0 ? void 0 : _a.invokeMethodAsync("CloseCallout");
            Bit.currentCallout.update(calloutId, overlayId, obj);
        }
    };
    Bit.selectText = function (element) {
        element.select();
    };
    Bit.currentCallout = new CalloutComponent();
    Bit.currentDropDownCalloutId = "";
    return Bit;
}());
window.addEventListener('scroll', function (e) {
    var minimumWidthForDropDownNormalOpen = 640;
    if ((Bit.currentDropDownCalloutId && window.innerWidth < minimumWidthForDropDownNormalOpen) ||
        (e.target.id && Bit.currentDropDownCalloutId === e.target.id))
        return;
    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);
window.addEventListener('resize', function (e) {
    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);
