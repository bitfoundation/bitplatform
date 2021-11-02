function showHideNavbar() {
    let navbarElement: HTMLElement = document.getElementById('navbar');
    let mainElement: HTMLElement = document.getElementById('content');

    let elementStyle = window.getComputedStyle(navbarElement);

    if (elementStyle.display === "none") {
        ShowNavbar();
    }

    mainElement.addEventListener('click', function (event) {
        HideNavbar();
    });

    let allATags = document.querySelectorAll(".nav-container a");

    allATags.forEach(aTag => {
        aTag.addEventListener('click', function (event) {
            HideNavbar();
        });
    });

    function HideNavbar() {
        navbarElement.style.display = "none";
        navbarElement.style.zIndex = "0";

        mainElement.style.backgroundColor = "";
    }

    function ShowNavbar() {
        navbarElement.style.zIndex = "1";
        navbarElement.style.display = "block";

        mainElement.style.backgroundColor = "#7a7979";
        mainElement.style.opacity = "0.9";
    }
}