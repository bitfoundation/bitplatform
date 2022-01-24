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

        [Parameter] public string? Size { get; set; }

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

            SetStyle();

            return base.OnParametersSetAsync();
        }

        protected override string RootElementClass => "bit-persona";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false ? string.Empty : "");
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

        private void SetStyle()
        {
            string? borderSize = (Size == BitPersonaSize.Size72 || Size == BitPersonaSize.Size100 ? "2px" : "1px");
            bool isOpenCirclePresence = Presence == BitPersonaPresenceStatus.Offline || (IsOutOfOffice && (Presence == BitPersonaPresenceStatus.Online || Presence == BitPersonaPresenceStatus.Busy || Presence == BitPersonaPresenceStatus.Away || Presence == BitPersonaPresenceStatus.DND));

            if (presenceHeightWidth != null)
            {
                PresenceStyle = $"width:{presenceHeightWidth}px;height:{presenceHeightWidth}px;";

                IconStyle = $"font-size:{presenceHeightWidth}px;line-height:{presenceHeightWidth}px;";
            }

            string? pRight = null;
            string? pTop = null;
            string? pLeft = null;
            string? pBorder = null;
            string? pHeight = null;
            string? pWidth = null;
            string? pBackgroundColor = null;

            string? iPosition = null;
            string? iFontSize = null;
            string? iLineHeight = null;
            string? iLeft = null;
            string? iColor = null;
            string? iBackgroundColor = null;


            string bpBorderColor = BitPresenceColor.Busy;

            if (Size == BitPersonaSize.Size8)
            {
                pRight = "auto";
                pTop = "7px";
                pLeft = "0";
                pBorder = "0";
            }
            if (Size == BitPersonaSize.Size8 || Size == BitPersonaSize.Size24 || Size == BitPersonaSize.Size32)
            {
                pWidth = BitPersonaPresenceSize.Size8;
                pHeight = BitPersonaPresenceSize.Size8;
            }
            else if (Size == BitPersonaSize.Size40 || Size == BitPersonaSize.Size48)
            {
                pWidth = BitPersonaPresenceSize.Size12;
                pHeight = BitPersonaPresenceSize.Size12;
            }
            else if (Size == BitPersonaSize.Size56)
            {
                pWidth = BitPersonaPresenceSize.Size16;
                pHeight = BitPersonaPresenceSize.Size16;
            }
            else if (Size == BitPersonaSize.Size72)
            {
                pWidth = BitPersonaPresenceSize.Size20;
                pHeight = BitPersonaPresenceSize.Size20;
            }
            else if (Size == BitPersonaSize.Size100)
            {
                pWidth = BitPersonaPresenceSize.Size28;
                pHeight = BitPersonaPresenceSize.Size28;
            }
            else if (Size == BitPersonaSize.Size120)
            {
                pWidth = BitPersonaPresenceSize.Size32;
                pHeight = BitPersonaPresenceSize.Size32;
            }

            if (Presence == BitPersonaPresenceStatus.Online)
                iBackgroundColor = BitPresenceColor.Available;

            if (Presence == BitPersonaPresenceStatus.Away)
                iBackgroundColor = BitPresenceColor.Away;
            if (Presence == BitPersonaPresenceStatus.Blocked)
            {
                if (Size == BitPersonaSize.Size40 || Size == BitPersonaSize.Size48 || Size == BitPersonaSize.Size72 || Size == BitPersonaSize.Size100)
                {
                    PresenceAfterStyle = $"content:'';" +
                              $"width:100%;" +
                              $"height:{borderSize};" +
                              $"background-color:{BitPresenceColor.Busy};" +
                              $"transform:translateY(-50%) rotate(-45deg);" +
                              $"position:absolute;" +
                              $"top:50%;" +
                              $"left:0;";
                }
                else
                {
                    PresenceAfterStyle = "";
                }
            }
            if (Presence == BitPersonaPresenceStatus.Busy)
                iBackgroundColor = BitPresenceColor.Busy;
            if (Presence == BitPersonaPresenceStatus.DND)
                iBackgroundColor = BitPresenceColor.Dnd;
            if (Presence == BitPersonaPresenceStatus.Offline)
                iBackgroundColor = BitPresenceColor.Offline;


            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.Online)
            {
                bpBorderColor = BitPresenceColor.Available;
                pBackgroundColor = BitPresenceColor.Available;
            }
            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.Busy)
            {
                bpBorderColor = BitPresenceColor.Busy;
                pBackgroundColor = BitPresenceColor.Busy;
            }
            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.Away)
            {
                bpBorderColor = BitPresenceColor.Oof;
                pBackgroundColor = BitPresenceColor.Oof;
            }
            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.DND)
            {
                bpBorderColor = BitPresenceColor.Dnd;
                pBackgroundColor = BitPresenceColor.Dnd;
            }
            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.Offline)
            {
                bpBorderColor = BitPresenceColor.Offline;
                pBackgroundColor = BitPresenceColor.Offline;
            }
            if (isOpenCirclePresence && Presence == BitPersonaPresenceStatus.Offline && IsOutOfOffice)
            {
                bpBorderColor = BitPresenceColor.Oof;
                pBackgroundColor = BitPresenceColor.Oof;
            }


            if (isOpenCirclePresence || Presence == BitPersonaPresenceStatus.Blocked)
            {
                PresenceBeforeStyle = $"content:'';" +
                    $"width:100%;" +
                    $"height:100%;" +
                    $"position:absolute;" +
                    $"top:0;" +
                    $"left:0;" +
                    $"border:{borderSize} solid {bpBorderColor};" +
                    $"border-radius:50%;" +
                    $"box-sizing:border-box;";
            }

            PresenceStyle = (pWidth != null ? $"width:{pWidth};" : "") +
                      (pHeight != null ? $"height:{pHeight};" : "") +
                      (pRight != null ? $"right:{pRight};" : "") +
                      (pTop != null ? $"top:{pTop};" : "") +
                      (pLeft != null ? $"left:{pLeft};" : "") +
                      (pBorder != null ? $"border:{pBorder};" : "") +
                      (pBackgroundColor != null ? $"background-color:{pBackgroundColor};" : "");


            switch (Size)
            {
                case BitPersonaSize.Size24:
                    iFontSize = BitPersonaPresenceSize.Size12; //"8px";
                    iLineHeight = BitPersonaPresenceSize.Size12;
                    break;
                case BitPersonaSize.Size32:
                    iFontSize = BitPersonaPresenceSize.Size12; //Theme?.FontStyle.FontSize.Small;
                    iLineHeight = BitPersonaPresenceSize.Size12;
                    break;
                case BitPersonaSize.Size40:
                    iFontSize = BitPersonaPresenceSize.Size16; //Theme?.FontStyle.FontSize.Medium;
                    iLineHeight = BitPersonaPresenceSize.Size16;
                    break;
                case BitPersonaSize.Size48:
                    iFontSize = BitPersonaPresenceSize.Size16; //Theme?.FontStyle.FontSize.Medium;
                    iLineHeight = BitPersonaPresenceSize.Size16;
                    break;
                case BitPersonaSize.Size56:
                    iFontSize = BitPersonaPresenceSize.Size16; //"8px";
                    iLineHeight = BitPersonaPresenceSize.Size16;
                    break;
                case BitPersonaSize.Size72:
                    iFontSize = BitPersonaPresenceSize.Size20; //Theme?.FontStyle.FontSize.Small;
                    iLineHeight = BitPersonaPresenceSize.Size20;
                    break;
                case BitPersonaSize.Size100:
                    iFontSize = BitPersonaPresenceSize.Size28; //Theme?.FontStyle.FontSize.Medium;
                    iLineHeight = BitPersonaPresenceSize.Size28;
                    break;
                case BitPersonaSize.Size120:
                    iFontSize = BitPersonaPresenceSize.Size32; //Theme?.FontStyle.FontSize.Medium;
                    iLineHeight = BitPersonaPresenceSize.Size32;
                    break;
            }
            if (Presence == BitPersonaPresenceStatus.Away)
            {
                iPosition = "relative";
                if (!isOpenCirclePresence)
                    iLeft = "0px"; // was 1px
            }
            if (isOpenCirclePresence)
            {
                switch (Presence)
                {
                    case BitPersonaPresenceStatus.Online:
                        iColor = BitPresenceColor.Available;
                        pBackgroundColor = BitPresenceColor.Available;
                        break;
                    case BitPersonaPresenceStatus.Busy:
                        iColor = BitPresenceColor.Busy;
                        pBackgroundColor = BitPresenceColor.Busy;
                        break;
                    case BitPersonaPresenceStatus.Away:
                        iColor = BitPresenceColor.Away;
                        pBackgroundColor = BitPresenceColor.Away;
                        break;
                    case BitPersonaPresenceStatus.DND:
                        iColor = BitPresenceColor.Dnd;
                        pBackgroundColor = BitPresenceColor.Dnd;
                        break;
                    case BitPersonaPresenceStatus.Offline:
                        iColor = BitPresenceColor.Offline;
                        pBackgroundColor = BitPresenceColor.Offline;
                        break;
                }
                if ((Presence == BitPersonaPresenceStatus.Offline || Presence == BitPersonaPresenceStatus.Away) && IsOutOfOffice)
                {
                    iColor = BitPresenceColor.Oof;
                    pBackgroundColor = BitPresenceColor.Oof;
                }
            }


            IconStyle = (iPosition != null ? $"position:{iPosition};" : "") +
                      (iFontSize != null ? $"font-size:{iFontSize};" : "") +
                      (iLineHeight != null ? $"line-height:{iLineHeight};" : "") +
                      (iLeft != null ? $"left:{iLeft};" : "") +
                      (iColor != null ? $"color:{iColor};" : "") +
                      (iBackgroundColor != null ? $"color:{iBackgroundColor};" : "");
        }
    }
}
