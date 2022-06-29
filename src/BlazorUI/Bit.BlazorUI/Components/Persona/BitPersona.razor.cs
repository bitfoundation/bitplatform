using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI
{
    public partial class BitPersona
    {
        private readonly Regex UNSUPPORTED_TEXT_REGEX = new(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uAC00-\uD7AF\uD7B0-\uD7FF\u3040-\u309F\u30A0-\u30FF\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]|[\uD840-\uD869][\uDC00-\uDED6]");

        private readonly Regex PHONENUMBER_REGEX = new(@"^\d+[\d\s]*(:?ext|x|)\s*\d+$");

        private readonly Regex UNWANTED_CHARS_REGEX = new(@"\([^)]*\)|[\0-\u001F\!-/:-@\[-`\{-\u00BF\u0250-\u036F\uD800-\uFFFF]");

        private readonly Regex MULTIPLE_WHITESPACES_REGEX = new(@"\s+");

        private const int PRESENCE_MAX_SIZE = 40;
        private const int COIN_SIZE_PRESENCE_SCALE_FACTOR = 3;

        private string? size;
        private string? presenceFontSize;
        private string? presenceHeightWidth;

        private bool RenderIcon { get; set; }
        private string? IconStyle { get; set; }
        private string? PresenceStyle { get; set; }
        private string? PresenceAfterStyle { get; set; }
        private string? PresenceBeforeStyle { get; set; }

        /// <summary>
        /// Whether to not render persona details, and just render the persona image/initials.
        /// </summary>
        [Parameter] public bool HidePersonaDetails { get; set; }

        /// <summary>
        /// This flag can be used to signal the persona is out of office. This will change the way the presence icon looks for statuses that support dual-presence.
        /// </summary>
        [Parameter] public bool IsOutOfOffice { get; set; }

        /// <summary>
        /// If true renders the initials while the image is loading. This only applies when an imageUrl is provided.
        /// </summary>
        [Parameter] public bool ShowInitialsUntilImageLoads { get; set; }

        /// <summary>
        /// Optional custom persona coin size in pixel.
        /// </summary>
        [Parameter] public int CoinSize { get; set; } = -1;

        /// <summary>
        /// Url to the image to use, should be a square aspect ratio and big enough to fit in the image area.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        [Parameter] public string? ImageUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// Presence title to be shown as a tooltip on hover over the presence icon.
        /// </summary>
        [Parameter] public string? PresenceTitle { get; set; }

        /// <summary>
        /// Decides the size of the control.
        /// </summary>
        [Parameter]
        public string? Size
        {
            get => size;
            set
            {
                size = value!;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Alt text for the image to use. default is empty string.
        /// </summary>
        [Parameter] public string? ImageAlt { get; set; }

        /// <summary>
        /// The background color when the user's initials are displayed.
        /// </summary>
        [Parameter] public BitPersonaInitialsColor? InitialsColor { get; set; }

        /// <summary>
        /// The user's initials to display in the image area when there is no image.
        /// </summary>
        [Parameter] public string? ImageInitials { get; set; }

        /// <summary>
        /// Primary text to display, usually the name of the person.
        /// </summary>
        [Parameter] public string? Text { get; set; }

        /// <summary>
        /// Secondary text to display, usually the role of the user.
        /// </summary>
        [Parameter] public string? SecondaryText { get; set; }

        /// <summary>
        /// Tertiary text to display, usually the status of the user.
        /// The tertiary text will only be shown when using size72 or size100.
        /// </summary>
        [Parameter] public string? TertiaryText { get; set; }

        /// <summary>
        /// Optional text to display, usually a custom message set.
        /// The optional text will only be shown when using size100.
        /// </summary>
        [Parameter] public string? OptionalText { get; set; }

        /// <summary>
        /// If true, show the special coin for unknown persona. 
        /// It has '?' in place of initials, with static font and background colors.
        /// </summary>
        [Parameter] public bool ShowUnknownPersonaCoin { get; set; }

        /// <summary>
        /// Presence of the person to display - will not display presence if undefined.
        /// </summary>
        [Parameter] public BitPersonaPresenceStatus Presence { get; set; } = BitPersonaPresenceStatus.None;

        /// <summary>
        /// Whether initials are calculated for phone numbers and number sequences.
        /// </summary>
        [Parameter] public bool AllowPhoneInitials { get; set; }

        protected override Task OnParametersSetAsync()
        {
            if (CoinSize != -1)
            {
                presenceHeightWidth = CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR < PRESENCE_MAX_SIZE
                    ? CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR + "px"
                    : PRESENCE_MAX_SIZE + "px";

                presenceFontSize = CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR < PRESENCE_MAX_SIZE
                    ? CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR + "px"
                    : PRESENCE_MAX_SIZE + "px";
            }

            RenderIcon = !(Size == BitPersonaSize.Size8 || Size == BitPersonaSize.Size24 || Size == BitPersonaSize.Size32) && (CoinSize == -1 || CoinSize > 32);


            return base.OnParametersSetAsync();
        }

        protected override string RootElementClass => "bit-prs";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => Size.HasValue() ? $"bit-prs-{Size}" : string.Empty);

            ClassBuilder.Register(() => Presence != BitPersonaPresenceStatus.None ? $"bit-prs-{Presence.ToString().ToLower(Thread.CurrentThread.CurrentCulture)}" : string.Empty);
        }

        private static string DetermineIcon(BitPersonaPresenceStatus presence, bool isOutofOffice)
        {
            if (presence == BitPersonaPresenceStatus.None)
                return string.Empty;
            string oofIcon = "presence_oof";

            return presence switch
            {
                BitPersonaPresenceStatus.Online => "presence_available",
                BitPersonaPresenceStatus.Busy => "presence_busy",
                BitPersonaPresenceStatus.Away => isOutofOffice ? oofIcon : "presence_away",
                BitPersonaPresenceStatus.DND => "presence_dnd",
                BitPersonaPresenceStatus.Offline => isOutofOffice ? oofIcon : "presence_offline",
                _ => "presence_unknown",
            };
        }

        private string GetCoinBackgroundColor()
        {
            return InitialsColor is not null ? BitPersonaColorUtils.GetPersonaColorHexCode(InitialsColor.Value) : BitPersonaColorUtils.GetPersonaColorHexCode(BitPersonaColorUtils.GetInitialsColorFromName(Text));
        }

        protected string GetInitials(string? displayName, bool isRTL, bool allowPhoneInitials)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "";

            displayName = CleanupDisplayName(displayName);

            if (UNSUPPORTED_TEXT_REGEX.IsMatch(displayName) || (!allowPhoneInitials && PHONENUMBER_REGEX.IsMatch(displayName)))
            {
                return "";
            }

            return GetInitialsLatin(displayName, isRTL);
        }

        private string CleanupDisplayName(string displayName)
        {
            displayName = UNWANTED_CHARS_REGEX.Replace(displayName, "");
            displayName = MULTIPLE_WHITESPACES_REGEX.Replace(displayName, " ");
            displayName = displayName.Trim();
            return displayName;
        }

        private static string GetInitialsLatin(string displayName, bool isRtl)
        {
            StringBuilder? initials = new("");

            string[] splits = displayName.Split(' ');

            if (splits.Length == 2)
            {
                initials.Append(splits[0].ToUpper(Thread.CurrentThread.CurrentCulture)[0]);
                initials.Append(splits[1].ToUpper(Thread.CurrentThread.CurrentCulture)[0]);
            }
            else if (splits.Length == 3)
            {
                initials.Append(splits[0].ToUpper(Thread.CurrentThread.CurrentCulture)[0]);
                initials.Append(splits[2].ToUpper(Thread.CurrentThread.CurrentCulture)[0]);
            }
            else if (splits.Length != 0)
            {
                initials.Append(splits[0].ToUpper(Thread.CurrentThread.CurrentCulture)[0]);
            }

            if (isRtl && initials.Length > 1)
            {
                StringBuilder returnValue = new();
                returnValue.Append(initials[1].ToString(Thread.CurrentThread.CurrentCulture));
                returnValue.Append(initials[0].ToString(Thread.CurrentThread.CurrentCulture));
            }

            return initials.ToString();
        }
    }
}
