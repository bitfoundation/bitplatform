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
(window as any).App = App;
(window as any).WebInteropApp = WebInteropApp;
//#if (ads == true)
(window as any).Ads = Ads;
//#endif
