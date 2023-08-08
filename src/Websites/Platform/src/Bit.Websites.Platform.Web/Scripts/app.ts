//declare var hljs: any;

function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth < 1440) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "auto";
        }
    }
}

//function scrollToElement(targetElementId: string) {
//    const element = document.getElementById(targetElementId);

//    if (element != null) {
//        element.scrollIntoView({
//            behavior: "smooth",
//            block: "start",
//            inline: "nearest"
//        });
//    }
//}

//function copyToClipboard(codeSampleContentForCopy: string) {
//    navigator.clipboard.writeText(codeSampleContentForCopy);
//}

function RegisterOnScrollToChangeGettingStartedSideRailStyle(element: any) {
    window.addEventListener('scroll', (event) => {
        if (document.documentElement.scrollTop >= 500) {
            element.classList.add("fixed-getting-started-side-rail-section");
        }
        else {
            element.classList.remove("fixed-getting-started-side-rail-section");
        }
    });
}

// function initTrustPilot() {
//     const trustpilotWidgetElements = document.getElementsByClassName("trustpilot-widget");
//     const thisGlobal = window as any;
//     for (let i = 0; i < trustpilotWidgetElements.length; i++) {
//         thisGlobal.Trustpilot.loadFromElement(trustpilotWidgetElements[i]);
//     }
// }

function goToTop() {
    window.scrollTo({ top: 0 });
}

declare class BitTheme { static init(options: any): void; };

BitTheme.init({
    system: true,
    onChange: (newTheme: string, oldThem: string) => {
        if (newTheme === 'dark') {
            document.body.classList.add('bit-theme-dark');
            document.body.classList.remove('bit-theme-light');
        } else {
            document.body.classList.add('bit-theme-light');
            document.body.classList.remove('bit-theme-dark');
        }
    }
});