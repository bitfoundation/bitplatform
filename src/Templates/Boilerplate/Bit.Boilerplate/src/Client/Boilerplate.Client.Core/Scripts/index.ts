//+:cnd:noEmit

import './bswup';
import './theme';
import './events';
import { App } from './App';
import { WebInteropApp } from './WebInteropApp';
//#if (ads == true)
import { Ads } from './Ads';
//#endif

// Expose classes on window global
//#if (ads == true)
(window as any).Ads = Ads;
//#endif
(window as any).App = App;
(window as any).WebInteropApp = WebInteropApp;

// Temporary bridge until Bit.Butil ships Document.SetLang (See ButilDocumentExtensions.cs).
(window as any).setDocumentLang = (lang: string) => document.documentElement.lang = lang;

// Temporary bridge until Bit.Butil ships ElementReference.Click (See ButilElementReferenceExtensions.cs).
(window as any).clickElement = (element: HTMLElement) => element.click();
