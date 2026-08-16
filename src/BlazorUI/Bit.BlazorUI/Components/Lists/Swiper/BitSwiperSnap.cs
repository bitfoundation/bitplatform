namespace Bit.BlazorUI;

/// <summary>
/// The place an item of a <see cref="BitSwiper"/> comes to rest at when the swiper snaps.
/// </summary>
public enum BitSwiperSnap
{
    /// <summary>
    /// The item settles with its leading edge at the start of the swiper.
    /// </summary>
    Start,

    /// <summary>
    /// The item settles in the middle of the swiper.
    /// </summary>
    Center,

    /// <summary>
    /// The item settles with its trailing edge at the end of the swiper.
    /// </summary>
    End
}
