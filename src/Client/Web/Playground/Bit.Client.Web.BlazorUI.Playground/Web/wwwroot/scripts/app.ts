function copyCodeSample(element){
    element.select();
    document.execCommand('copy');
    element.setSelectionRange(0, 0);
}