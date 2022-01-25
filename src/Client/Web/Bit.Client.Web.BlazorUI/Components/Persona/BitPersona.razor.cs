using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPersona
    {
        private readonly Regex UNSUPPORTED_TEXT_REGEX = new(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uAC00-\uD7AF\uD7B0-\uD7FF\u3040-\u309F\u30A0-\u30FF\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]|[\uD840-\uD869][\uDC00-\uDED6]");

        private readonly Regex PHONENUMBER_REGEX = new(@"^\d+[\d\s]*(:?ext|x|)\s*\d+$");

        private readonly Regex UNWANTED_CHARS_REGEX = new(@"\([^)]*\)|[\0-\u001F\!-/:-@\[-`\{-\u00BF\u0250-\u036F\uD800-\uFFFF]");

        private readonly Regex MULTIPLE_WHITESPACES_REGEX = new(@"\s+");

        private string size;

        private string? presenceHeightWidth;

        private string? presenceFontSize;

        private const int coinSizeFontScaleFactor = 6;

        private const int coinSizePresenceScaleFactor = 3;

        private const int presenceMaxSize = 40;

        private const int presenceFontMaxSize = 32;

        public bool RenderIcon { get; set; }
        public string? PresenceStyle { get; set; }
        public string? IconStyle { get; set; }
        public string? PresenceAfterStyle { get; set; }
        public string? PresenceBeforeStyle { get; set; }

        [Parameter] public bool HidePersonaDetails { get; set; }

        [Parameter] public bool IsOutOfOffice { get; set; }

        [Parameter] public bool ShowInitialsUntilImageLoads { get; set; }

        [Parameter] public int CoinSize { get; set; } = -1;

        [Parameter] public string? ImageUrl { get; set; }

        [Parameter] public string? PresenceTitle { get; set; }

        [Parameter] public string? PresenceIcon { get; set; }

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

        [Parameter] public string? ImageAlt { get; set; }

        [Parameter] public BitPersonaInitialsColor? InitialsColor { get; set; }

        [Parameter] public string? ImageInitials { get; set; }

        [Parameter] public string? Text { get; set; }

        [Parameter] public string? SecondaryText { get; set; }

        [Parameter] public string? TertiaryText { get; set; }

        [Parameter] public string? OptionalText { get; set; }

        [Parameter] public bool ShowUnknownPersonaCoin { get; set; }

        [Parameter] public BitPersonaPresenceStatus Presence { get; set; } = BitPersonaPresenceStatus.None;

        [Parameter] public bool AllowPhoneInitials { get; set; }


        protected override Task OnParametersSetAsync()
        {
            if (CoinSize != -1)
            {
                presenceHeightWidth = CoinSize / coinSizePresenceScaleFactor < presenceMaxSize
                    ? CoinSize / coinSizePresenceScaleFactor + "px"
                    : presenceMaxSize + "px";

                presenceFontSize = CoinSize / coinSizePresenceScaleFactor < presenceMaxSize
                    ? CoinSize / coinSizePresenceScaleFactor + "px"
                    : presenceMaxSize + "px";
            }

            RenderIcon = !(Size == BitPersonaSize.Size8 || Size == BitPersonaSize.Size24 || Size == BitPersonaSize.Size32) && (CoinSize == -1 || CoinSize > 32);


            return base.OnParametersSetAsync();
        }

        protected override string RootElementClass => "bit-persona";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false ? string.Empty : "");

            ClassBuilder.Register(() => Size != null ? $"bit-persona-{Size}" : string.Empty);

            ClassBuilder.Register(()=> Presence !=BitPersonaPresenceStatus.None? $"bit-persona-{Presence.ToString()}":string.Empty);
        }

        protected static string DetermineIcon(BitPersonaPresenceStatus presence, bool isOutofOffice)
        {
            if (presence == BitPersonaPresenceStatus.None)
                return "";
            string? oofIcon = "presence_oof";

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
            string? initials = "";

            string[] splits = displayName.Split(' ');

            if (splits.Length == 2)
            {
                initials += splits[0].ToUpper()[0];
                initials += splits[1].ToUpper()[0];
            }
            else if (splits.Length == 3)
            {
                initials += splits[0].ToUpper()[0];
                initials += splits[2].ToUpper()[0];
            }
            else if (splits.Length != 0)
            {
                initials += splits[0].ToUpper()[0];
            }

            if (isRtl && initials.Length > 1)
            {
                return "" + initials[1] + initials[0];
            }

            return initials;
        }
    }
}
