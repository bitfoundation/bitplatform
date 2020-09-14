# Optimizations of images in Xamarin Forms

I invite you in advance to read this article even if you use other methods of mobile application development such as Flutter, React native, etc instead of Xamarin Forms; Because the general idea and its important points can be useful for you as well.
When you get your design from a designer and let's say you want to display a glass image on a page of a mobile app, you are given a PNG or SVG file. Although there are generally ways to use the same file directly on your application page, this is not recommended for several reasons:
1. Different devices have different resolutions, and the mobile (sometimes weak) hardware must scale the image for optimal display, which is a waste of CPU and memory.
2. Even when the scaling operation is done, when the relevant page (containing the scaled image) is closed and we go to another page, in reopening the relevant page, this costly scaling operation will occur again.

To solve these problems, two steps can be taken:
The first step is to pre-save the SVG or PNG image of that glass in different scales. This has two drawbacks though. The first one is that if this is done manually, the probability of error increases as it needs to save dozens of different images for every source image based on different scales for Android/iOS/Windows. The second drawback is by storing an image multiple times on different scales, the final size of the project will increase.

To automate this manual process, Xamarin Forms, React native, etc. have tools that the [`ResizetizerNT`](https://github.com/Redth/ResizetizerNT) is an example of Xamarin Forms tool that takes an SVG or PNG image from you and saves it in different scales for Android/iOS/Windows, so the chances of making a mistake during scaling will decrease and the scaling job becomes much simpler.

To solve the problem of final project output size, a new format has been replaced with `apk` in Android and Google Play Store called `aab` or [`Android App Bundle`](https://developer.android.com/platform/technology/app-bundle), which builds a file based on the resolution of the device that is downloading the app, therefore there will be only photos with appropriate scale for the device; So we can at least not worry about Android devices on the project size issue.

Of course, the impact of this scaling operation on performance is so positively high that it is better for various stores that do not support `aab` and only support `apk` to still store the image on different scales.

There is a tool called `pngcrunch` in iOS, which optimizes and compresses PNG images and is effective in reducing its file size. To use this tool, in `Xamarin.iOS` inside the `Project Settings`, in the `iOS Build` section, select the `Optimize PNG Images` checkbox and you are good to go.

However, even if the images are stored in different scales, there is a stage where the PNG becomes a displayable bitmap. This is also costly and will be repeated when you close and reopen a page. Also, if you have five items in a ListView that have a picture (in each item), this process is repeated five times.

Android has introduced a tool called `Glide`, which caches the PNG to displayable bitmap conversion step, therefore there will be no problems with opening and closing pages or using a photo in ListView anymore.

In Xamarin Forms using [`GlideX.Forms`](https://github.com/jonathanpeppers/glidex), you can make this optimization very effective in Android and achieve soft scroll in ListView and quick opening of pages. In iOS, the [`Nuke`](https://github.com/kean/Nuke) library does the same job, which you can use it in Xamarin Forms with [this Package](https://github.com/roubachof/Xamarin.Forms.Nuke).

Another advantage of using `GlideX.Forms` and `Nuke` are that if you receive and display photos from the server somewhere in the application (for example, photos of clients in the client's list), these two tools will cache the final image after scaling and converting to Bitmap, for later use. So the next time we require the same image to be displayed, instead of re-scaling and becoming a Bitmap again, only ready-to-display content is displayed to the user from the cache.

Finally, with a few simple configuration lines and installing some packages, you can have an optimized SVG or PNG to be displayed in Android/iOS/Windows.

[This project on Github](https://github.com/ysmoradi/XFImageBestPractices) has put all these things together, which you can also use its source to better understand the concepts.

