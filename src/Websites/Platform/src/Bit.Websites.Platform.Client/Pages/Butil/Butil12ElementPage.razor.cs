using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil12ElementPage
{
    private string? attributeName;
    private string? currentAttribute;
    private ElementReference getAttributeElementRef;

    private string? currentAttributeNames;
    private ElementReference getAttributeNamesElementRef;

    private string? currentBoundingClientRect;
    private ElementReference getBoundingClientRectElementRef;

    private string? hasAttribute;
    private string? hasAttributeName;
    private ElementReference hasAttributeElementRef;

    private string? hasAttributes;
    private ElementReference hasAttributesElementRef;

    private ElementReference requestPointerLockElementRef;

    private ElementReference requestFullScreenElementRef;

    private string? matches;
    private string? selector = "#target";
    private ElementReference matchesElementRef;

    private double scrollX = 0;
    private double scrollY = 100;
    private ScrollBehavior scrollBehavior;
    private ElementReference scrollElementRef;

    private double scrollByX = 0;
    private double scrollByY = 156;
    private ScrollBehavior scrollByBehavior;
    private ElementReference scrollByElementRef;

    private ScrollBehavior scrollIntoViewBehavior;
    private ScrollLogicalPosition blockScrollPosition;
    private ScrollLogicalPosition inlineScrollPosition;
    private ElementReference scrollIntoViewElementRef;

    private string? removeAttributeName;
    private ElementReference removeAttributeElementRef;

    private string? setAttributeName;
    private string? setAttributeValue;
    private ElementReference setAttributeElementRef;

    private bool toggleAttributeForce;
    private string? toggleAttributeName;
    private ElementReference toggleAttributeElementRef;

    private string? newAccessKey;
    private string? currentAccessKey;
    private ElementReference accessKeyElementRef;

    private string? newClassName;
    private string? currentClassName;
    private ElementReference classNameElementRef;

    private string? currentClientHeight;
    private ElementReference getClientHeightElementRef;

    private string? currentClientLeft;
    private ElementReference getClientLeftElementRef;

    private string? currentClientTop;
    private ElementReference getClientTopElementRef;

    private string? currentClientWidth;
    private ElementReference getClientWidthElementRef;

    private string? newId;
    private string? currentId;
    private ElementReference idElementRef;

    private string? newInnerHTML;
    private string? currentInnerHTML;
    private ElementReference innerHTMLElementRef;

    private string? currentOuterHTML;
    private ElementReference outerHTMLElementRef;

    private string? currentScrollHeight;
    private ElementReference getScrollHeightElementRef;

    private string? currentScrollLeft;
    private ElementReference getScrollLeftElementRef;

    private string? currentScrollTop;
    private ElementReference getScrollTopElementRef;

    private string? currentScrollWidth;
    private ElementReference getScrollWidthElementRef;

    private string? currentTagName;
    private ElementReference getTagNameElementRef;

    private string? currentContentEditable;
    private ContentEditable newContentEditable;
    private ElementReference contentEditableElementRef;

    private string? isContentEditable;
    private ElementReference isContentEditableElementRef;

    private ElementDir newDir;
    private string? currentDir;
    private ElementReference dirElementRef;

    private string? currentEnterKeyHint;
    private EnterKeyHint newEnterKeyHint;
    private ElementReference enterKeyHintElementRef;

    private bool newInert;
    private string? currentInert;
    private ElementReference inertElementRef;

    private string? newInnerText;
    private string? currentInnerText;
    private ElementReference innerTextElementRef;

    private string? currentInputMode;
    private InputMode newInputMode;
    private ElementReference inputModeElementRef;

    private double newTabIndex;
    private string? currentTabIndex;
    private ElementReference tabIndexElementRef;

    private string? currentOffsetHeight;
    private ElementReference getOffsetHeightElementRef;

    private string? currentOffsetLeft;
    private ElementReference getOffsetLeftElementRef;

    private string? currentOffsetTop;
    private ElementReference getOffsetTopElementRef;

    private string? currentOffsetWidth;
    private ElementReference getOffsetWidthElementRef;

    private ElementReference blurElementRef;


    private async Task GetAttribute()
    {
        currentAttribute = await getAttributeElementRef.GetAttribute(attributeName!);
    }

    private async Task GetAttributeNames()
    {
        var result = await getAttributeNamesElementRef.GetAttributeNames();
        currentAttributeNames = string.Join(", ", result);
    }

    private async Task GetBoundingClientRect()
    {
        var result = await getBoundingClientRectElementRef.GetBoundingClientRect();
        currentBoundingClientRect = string.Join(", ",
            $"Height: {result.Height}",
            $"Width: {result.Width}",
            $"X: {result.X}",
            $"Y: {result.Y}"
        );
    }

    private async Task GetHasAttribute()
    {
        var result = await hasAttributeElementRef.HasAttribute(hasAttributeName!);
        hasAttribute = result.ToString();
    }

    private async Task GetHasAttributes()
    {
        var result = await hasAttributesElementRef.HasAttributes();
        hasAttributes = result.ToString();
    }

    private async Task RequestFullScreen()
    {
        var options = new FullScreenOptions() { NavigationUI = FullScreenNavigationUI.Show };
        await requestFullScreenElementRef.RequestFullScreen(options);
    }

    private async Task Matches()
    {
        var result = await matchesElementRef.Matches(selector!);
        matches = result.ToString();
    }

    private async Task Scroll()
    {
        var scrollOptions = new ScrollOptions() 
        {
            Top = scrollY,
            Left = scrollX,
            Behavior = scrollBehavior
        };

        await scrollElementRef.Scroll(scrollOptions);
    }

    private async Task ScrollBy()
    {
        var scrollOptions = new ScrollOptions()
        { 
            Top = scrollByY,
            Left = scrollByX,
            Behavior = scrollByBehavior
        };

        await scrollByElementRef.ScrollBy(scrollOptions);
    }

    private async Task ScrollIntoView()
    {
        var scrollOptions = new ScrollIntoViewOptions()
        {
            Inline = inlineScrollPosition,
            Block = blockScrollPosition,
            Behavior = scrollIntoViewBehavior
        };

        await scrollIntoViewElementRef.ScrollIntoView(scrollOptions);
    }

    private async Task RemoveAttribute()
    {
        await removeAttributeElementRef.RemoveAttribute(removeAttributeName!);
    }

    private async Task SetAttribute()
    {
        await setAttributeElementRef.SetAttribute(setAttributeName!, setAttributeValue!);
    }

    private async Task ToggleAttribute()
    {
        await toggleAttributeElementRef.ToggleAttribute(toggleAttributeName!, toggleAttributeForce);
    }

    private async Task SetAccessKey()
    {
        await accessKeyElementRef.SetAccessKey(newAccessKey!);
    }

    private async Task GetAccessKey()
    {
        currentAccessKey = await accessKeyElementRef.GetAccessKey();
    }

    private async Task SetClassName()
    {
        await classNameElementRef.SetClassName(newClassName!);
    }

    private async Task GetClassName()
    {
        currentClassName = await classNameElementRef.GetClassName();
    }

    private async Task GetClientHeight()
    {
        var result = await getClientHeightElementRef.GetClientHeight();
        currentClientHeight = result.ToString();
    }

    private async Task GetClientLeft()
    {
        var result = await getClientLeftElementRef.GetClientLeft();
        currentClientLeft = result.ToString();
    }

    private async Task GetClientTop()
    {
        var result = await getClientTopElementRef.GetClientTop();
        currentClientTop = result.ToString();
    }

    private async Task GetClientWidth()
    {
        var result = await getClientWidthElementRef.GetClientWidth();
        currentClientWidth = result.ToString();
    }

    private async Task SetId()
    {
        await idElementRef.SetId(newId!);
    }

    private async Task GetId()
    {
        currentId = await idElementRef.GetId();
    }

    private async Task SetInnerHTML()
    {
        await innerHTMLElementRef.SetInnerHtml(newInnerHTML!);
    }

    private async Task GetInnerHTML()
    {
        currentInnerHTML = await innerHTMLElementRef.GetInnerHtml();
    }

    private async Task GetOuterHTML()
    {
        currentOuterHTML = await outerHTMLElementRef.GetOuterHtml();
    }

    private async Task GetScrollHeight()
    {
        var result = await getScrollHeightElementRef.GetScrollHeight();
        currentScrollHeight = result.ToString();
    }

    private async Task GetScrollLeft()
    {
        var result = await getScrollLeftElementRef.GetScrollLeft();
        currentScrollLeft = result.ToString();
    }

    private async Task GetScrollTop()
    {
        var result = await getScrollTopElementRef.GetScrollTop();
        currentScrollTop = result.ToString();
    }

    private async Task GetScrollWidth()
    {
        var result = await getScrollWidthElementRef.GetScrollWidth();
        currentScrollWidth = result.ToString();
    }

    private async Task GetTagName()
    {
        var result = await getTagNameElementRef.GetTagName();
        currentTagName = result.ToString();
    }

    private async Task GetIsContentEditable()
    {
        var result = await isContentEditableElementRef.IsContentEditable();
        isContentEditable = result.ToString();
    }

    private async Task SetContentEditable()
    {
        await contentEditableElementRef.SetContentEditable(newContentEditable);
    }

    private async Task GetContentEditable()
    {
        var result = await contentEditableElementRef.GetContentEditable();
        currentContentEditable = result.ToString();
    }

    private async Task SetDir()
    {
        await dirElementRef.SetDir(newDir);
    }

    private async Task GetDir()
    {
        var result = await dirElementRef.GetDir();
        currentDir = result.ToString();
    }

    private async Task SetEnterKeyHint()
    {
        await enterKeyHintElementRef.SetEnterKeyHint(newEnterKeyHint);
    }

    private async Task GetEnterKeyHint()
    {
        var result = await enterKeyHintElementRef.GetEnterKeyHint();
        currentEnterKeyHint = result.ToString();
    }

    private async Task SetInert()
    {
        await inertElementRef.SetInert(newInert);
    }

    private async Task GetInert()
    {
        var result = await inertElementRef.GetInert();
        currentInert = result.ToString();
    }

    private async Task SetInnerText()
    {
        await innerTextElementRef.SetInnerText(newInnerText!);
    }

    private async Task GetInnerText()
    {
        currentInnerText = await innerTextElementRef.GetInnerText();
    }

    private async Task SetInputMode()
    {
        await inputModeElementRef.SetInputMode(newInputMode);
    }

    private async Task GetInputMode()
    {
        var result = await inputModeElementRef.GetInputMode();
        currentInputMode = result.ToString();
    }

    private async Task SetTabIndex()
    {
        await tabIndexElementRef.SetTabIndex((int)newTabIndex);
    }

    private async Task GetTabIndex()
    {
        var result = await tabIndexElementRef.GetTabIndex();
        currentTabIndex = result.ToString();
    }

    private async Task GetOffsetHeight()
    {
        var result = await getOffsetHeightElementRef.GetOffsetHeight();
        currentOffsetHeight = result.ToString();
    }

    private async Task GetOffsetLeft()
    {
        var result = await getOffsetLeftElementRef.GetOffsetLeft();
        currentOffsetLeft = result.ToString();
    }

    private async Task GetOffsetTop()
    {
        var result = await getOffsetTopElementRef.GetOffsetTop();
        currentOffsetTop = result.ToString();
    }

    private async Task GetOffsetWidth()
    {
        var result = await getOffsetWidthElementRef.GetOffsetWidth();
        currentOffsetWidth = result.ToString();
    }


    private string getAttributeExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getAttributeElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""attributeName"" Label=""Attribute name"" />

<BitButton OnClick=""GetAttribute"">GetAttribute</BitButton>

<div>Attribute: @currentAttribute</div>

@code {
    private string? attributeName;
    private string? currentAttribute;
    private ElementReference getAttributeElementRef;
    
    private async Task GetAttribute()
    {
        currentAttribute = await getAttributeElementRef.GetAttribute(attributeName!);
    }
}";
    private string getAttributeNamesExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getAttributeNamesElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetAttributeNames"">GetAttributeNames</BitButton>

<div>Attribute names: @currentAttributeNames</div>

@code {
    private string? currentAttributeNames;
    private ElementReference getAttributeNamesElementRef;
    
    private async Task GetAttributeNames()
    {
        var result = await getAttributeNamesElementRef.GetAttributeNames();
        currentAttributeNames = string.Join("", "", result);
    }
}";
    private string getBoundingClientRectExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getBoundingClientRectElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetBoundingClientRect"">GetBoundingClientRect</BitButton>

<div>Bounding client rect: @currentBoundingClientRect</div>

@code {
    private string? currentBoundingClientRect;
    private ElementReference getBoundingClientRectElementRef;

    private async Task GetBoundingClientRect()
    {
        var result = await getBoundingClientRectElementRef.GetBoundingClientRect();
        currentBoundingClientRect = string.Join("", "",
            $""Height: {result.Height}"",
            $""Width: {result.Width}"",
            $""X: {result.X}"",
            $""Y: {result.Y}""
        );
    }
}";
    private string hasAttributeExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""hasAttributeElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""hasAttributeName"" Label=""Attribute name"" />

<BitButton OnClick=""GetHasAttribute"">GetHasAttribute</BitButton>

<div>Has attribute: @hasAttribute</div>

@code {
    private string? hasAttribute;
    private string? hasAttributeName;
    private ElementReference hasAttributeElementRef;

    private async Task GetHasAttribute()
    {
        var result = await hasAttributeElementRef.HasAttribute(hasAttributeName!);
        hasAttribute = result.ToString();
    }
}";
    private string hasAttributesExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""hasAttributesElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetHasAttributes"">GetHasAttributes</BitButton>

<div>Has attributes: @hasAttributes</div>

@code {
    private string? hasAttributes;
    private ElementReference hasAttributesElementRef;

    private async Task GetHasAttributes()
    {
        var result = await hasAttributesElementRef.HasAttributes();
        hasAttributes = result.ToString();
    }
}";
    private string requestPointerLockExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""requestPointerLockExampleCode""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""@(() => requestPointerLockElementRef.RequestPointerLock())"">
    RequestPointerLock
</BitButton>

@code {
    private ElementReference requestPointerLockExampleCode;
}";
    private string requestFullScreenExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""requestFullScreenElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""RequestFullScreen"">RequestFullScreen</BitButton>

@code {
    private ElementReference requestFullScreenElementRef;

    private async Task RequestFullScreen()
    {
        var options = new FullScreenOptions() { NavigationUI = FullScreenNavigationUI.Show };
        await requestFullScreenElementRef.RequestFullScreen(options);
    }
}";
    private string matchesExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""matchesElementRef""
     id=""target""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""selector"" Label=""Selector"" Style=""max-width: 18.75rem;"" />

<BitButton OnClick=""Matches"">Matches</BitButton>

<div>Matches: @matches</div>

@code {
    private string? matches;
    private string? selector = ""#target"";
    private ElementReference matchesElementRef;

    private async Task Matches()
    {
        var result = await matchesElementRef.Matches(selector!);
        matches = result.ToString();
    }
}";
    private string scrollExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""scrollElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitSpinButton @bind-Value=""scrollY"" Label=""Scroll Y"" Mode=""BitSpinButtonMode.Inline"" />

<BitSpinButton @bind-Value=""scrollX"" Label=""Scroll X"" Mode=""BitSpinButtonMode.Inline"" />

<BitChoiceGroup @bind-Value=""scrollBehavior""
                Label=""Scroll behavior""
                TItem=""BitChoiceGroupOption<ScrollBehavior>"" TValue=""ScrollBehavior"">
    <BitChoiceGroupOption Text=""Auto"" Value=""ScrollBehavior.Auto"" />
    <BitChoiceGroupOption Text=""Instant"" Value=""ScrollBehavior.Instant"" />
    <BitChoiceGroupOption Text=""Smooth"" Value=""ScrollBehavior.Smooth"" />
</BitChoiceGroup>

<BitButton OnClick=""Scroll"">Scroll</BitButton>

@code {
    private float scrollX = 0;
    private float scrollY = 100;
    private ScrollBehavior scrollBehavior;
    private ElementReference scrollElementRef;

    private async Task Scroll()
    {
        var scrollOptions = new ScrollToOptions() 
        {
            Top = scrollY,
            Left = scrollX,
            Behavior = scrollBehavior
        };

        await scrollElementRef.Scroll(scrollOptions);
    }
}";
    private string scrollByExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""scrollByElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitSpinButton @bind-Value=""scrollByY"" Label=""ScrollBy Y"" Mode=""BitSpinButtonMode.Inline"" />

<BitSpinButton @bind-Value=""scrollByX"" Label=""ScrollBy X"" Mode=""BitSpinButtonMode.Inline"" />

<BitChoiceGroup @bind-Value=""scrollByBehavior""
                Label=""Scroll behavior""
                TItem=""BitChoiceGroupOption<ScrollBehavior>"" TValue=""ScrollBehavior"">
    <BitChoiceGroupOption Text=""Auto"" Value=""ScrollBehavior.Auto"" />
    <BitChoiceGroupOption Text=""Instant"" Value=""ScrollBehavior.Instant"" />
    <BitChoiceGroupOption Text=""Smooth"" Value=""ScrollBehavior.Smooth"" />
</BitChoiceGroup>

<BitButton OnClick=""ScrollBy"">ScrollBy</BitButton>

@code {
    private float scrollByX = 0;
    private float scrollByY = 156;
    private ScrollBehavior scrollByBehavior;
    private ElementReference scrollByElementRef;

    private async Task ScrollBy()
    {
        var scrollByOptions = new ScrollByToOptions() 
        {
            Top = scrollByY,
            Left = scrollByX,
            Behavior = scrollByBehavior
        };

        await scrollByElementRef.ScrollBy(scrollByOptions);
    }
}";
    private string scrollIntoViewExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""scrollIntoViewElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitChoiceGroup @bind-Value=""inlineScrollPosition""
                Label=""Inline scroll position""
                TItem=""BitChoiceGroupOption<ScrollLogicalPosition>"" TValue=""ScrollLogicalPosition"">
    <BitChoiceGroupOption Text=""Start"" Value=""ScrollLogicalPosition.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""ScrollLogicalPosition.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""ScrollLogicalPosition.End"" />
    <BitChoiceGroupOption Text=""Nearest"" Value=""ScrollLogicalPosition.Nearest"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""blockScrollPosition""
                Label=""Block scroll position""
                TItem=""BitChoiceGroupOption<ScrollLogicalPosition>"" TValue=""ScrollLogicalPosition"">
    <BitChoiceGroupOption Text=""Start"" Value=""ScrollLogicalPosition.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""ScrollLogicalPosition.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""ScrollLogicalPosition.End"" />
    <BitChoiceGroupOption Text=""Nearest"" Value=""ScrollLogicalPosition.Nearest"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""scrollIntoViewBehavior""
                Label=""ScrollIntoView behavior""
                TItem=""BitChoiceGroupOption<ScrollBehavior>"" TValue=""ScrollBehavior"">
    <BitChoiceGroupOption Text=""Auto"" Value=""ScrollBehavior.Auto"" />
    <BitChoiceGroupOption Text=""Instant"" Value=""ScrollBehavior.Instant"" />
    <BitChoiceGroupOption Text=""Smooth"" Value=""ScrollBehavior.Smooth"" />
</BitChoiceGroup>

<BitButton OnClick=""ScrollIntoView"">ScrollIntoView</BitButton>

@code {
    private ScrollBehavior scrollIntoViewBehavior;
    private ScrollLogicalPosition blockScrollPosition;
    private ScrollLogicalPosition inlineScrollPosition;
    private ElementReference scrollIntoViewElementRef;

    private async Task ScrollIntoView()
    {
        var scrollOptions = new ScrollIntoViewOptions()
        {
            Inline = inlineScrollPosition,
            Block = blockScrollPosition,
            Behavior = scrollIntoViewBehavior
        };

        await scrollIntoViewElementRef.ScrollIntoView(scrollOptions);
    }
}";
    private string removeAttributeExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""removeAttributeElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""removeAttributeName"" Label=""Attribute name"" />

<BitButton OnClick=""RemoveAttribute"">RemoveAttribute</BitButton>

@code {
    private string? removeAttributeName;
    private ElementReference removeAttributeElementRef;

    private async Task RemoveAttribute()
    {
        await removeAttributeElementRef.RemoveAttribute(removeAttributeName!);
    }
}";
    private string setAttributeExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""setAttributeElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""setAttributeName"" Label=""Attribute name"" />

<BitTextField @bind-Value=""setAttributeValue"" Label=""Attribute value"" />

<BitButton OnClick=""SetAttribute"">SetAttribute</BitButton>

@code {
    private string? setAttributeName;
    private string? setAttributeValue;
    private ElementReference setAttributeElementRef;

    private async Task SetAttribute()
    {
        await setAttributeElementRef.SetAttribute(setAttributeName!, setAttributeValue!);
    }
}";
    private string toggleAttributeExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""toggleAttributeElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""toggleAttributeName"" Label=""Attribute name"" />

<BitCheckbox @bind-Value=""toggleAttributeForce"" Label=""Force"" />

<BitButton OnClick=""ToggleAttribute"">ToggleAttribute</BitButton>

@code {
    private bool toggleAttributeForce;
    private string? toggleAttributeName;
    private ElementReference toggleAttributeElementRef;

    private async Task ToggleAttribute()
    {
        await toggleAttributeElementRef.ToggleAttribute(toggleAttributeName!, toggleAttributeForce);
    }
}";
    private string accessKeyExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""accessKeyElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""newAccessKeyValue"" />

<BitButton OnClick=""SetAccessKey"">SetAccessKey</BitButton>

<BitButton OnClick=""GetAccessKey"">GetAccessKey</BitButton>
                            
<div>Access key: @currentAccessKey</div>

@code {
    private string? newAccessKey;
    private string? currentAccessKey;
    private ElementReference accessKeyElementRef;

    private async Task SetAccessKey()
    {
        await accessKeyElementRef.SetAccessKey(newAccessKey!);
    }

    private async Task GetAccessKey()
    {
        currentAccessKey = await accessKeyElementRef.GetAccessKey();
    }
}";
    private string classNameExampleCode  =
@"@inject Bit.Butil.Element element

<div @ref=""classNameElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""newClassName"" />

<BitButton OnClick=""SetClassName"">SetClassName</BitButton>

<BitButton OnClick=""GetClassName"">GetClassName</BitButton>

<div>ClassName: @currentClassName</div>

@code {
    private string? newClassName;
    private string? currentClassName;
    private ElementReference classNameElementRef;

    private async Task SetClassName()
    {
        await classNameElementRef.SetClassName(newClassName!);
    }

    private async Task GetClassName()
    {
        currentClassName = await classNameElementRef.GetClassName();
    }
}";
    private string getClientHeightExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getClientHeightElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetClientHeight"">GetClientHeight</BitButton>

<div>ClientHeight: @currentClientHeight</div>

@code {
    private string? currentClientHeight;
    private ElementReference getClientHeightElementRef;

    private async Task GetClientHeight()
    {
        var result = await getClientHeightElementRef.GetClientHeight();
        currentClientHeight = result.ToString();
    }
}";
    private string getClientLeftExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getClientLeftElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetClientLeft"">GetClientLeft</BitButton>

<div>ClientLeft: @currentClientLeft</div>

@code {
    private string? currentClientLeft;
    private ElementReference getClientLeftElementRef;

    private async Task GetClientLeft()
    {
        var result = await getClientLeftElementRef.GetClientLeft();
        currentClientLeft = result.ToString();
    }
}";
    private string getClientTopExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getClientTopElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetClientTop"">GetClientTop</BitButton>

<div>ClientTop: @currentClientTop</div>

@code {
    private string? currentClientTop;
    private ElementReference getClientTopElementRef;

    private async Task GetClientTop()
    {
        var result = await getClientTopElementRef.GetClientTop();
        currentClientTop = result.ToString();
    }
}";
    private string getClientWidthExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getClientWidthElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetClientWidth"">GetClientWidth</BitButton>

<div>ClientWidth: @currentClientWidth</div>

@code {
    private string? currentClientWidth;
    private ElementReference getClientWidthElementRef;

    private async Task GetClientWidth()
    {
        var result = await getClientWidthElementRef.GetClientWidth();
        currentClientWidth = result.ToString();
    }
}";
    private string idExampleCode  =
@"@inject Bit.Butil.Element element

<div @ref=""idElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""newId"" />

<BitButton OnClick=""SetId"">SetId</BitButton>

<BitButton OnClick=""GetId"">GetId</BitButton>

<div>Id: @currentId</div>

@code {
    private string? newId;
    private string? currentId;
    private ElementReference idElementRef;

    private async Task SetId()
    {
        await idElementRef.SetId(newId!);
    }

    private async Task GetId()
    {
        currentId = await idElementRef.GetId();
    }
}";
    private string innerHTMLExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""innerHTMLElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""newInnerHTML"" />

<BitButton OnClick=""SetInnerHTML"">SetInnerHTML</BitButton>

<BitButton OnClick=""GetInnerHTML"">GetInnerHTML</BitButton>

<div>InnerHTML: @currentInnerHTML</div>

@code {
    private string? newInnerHTML;
    private string? currentInnerHTML;
    private ElementReference innerHTMLElementRef;

    private async Task SetInnerHTML()
    {
        await innerHTMLElementRef.SetInnerHTML(newInnerHTML!);
    }

    private async Task GetInnerHTML()
    {
        currentInnerHTML = await innerHTMLElementRef.GetInnerHTML();
    }
}";
    private string outerHTMLExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""outerHTMLElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetOuterHTML"">GetOuterHTML</BitButton>

<div>OuterHTML: @currentOuterHTML</div>

@code {
    private string? currentOuterHTML;
    private ElementReference outerHTMLElementRef;

    private async Task GetOuterHTML()
    {
        currentOuterHTML = await outerHTMLElementRef.GetOuterHTML();
    }
}";
    private string getScrollHeightExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getScrollHeightElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitButton OnClick=""GetScrollHeight"">GetScrollHeight</BitButton>

<div>ScrollHeight: @currentScrollHeight</div>

@code {
    private string? currentScrollHeight;
    private ElementReference getScrollHeightElementRef;

    private async Task GetScrollHeight()
    {
        var result = await getScrollHeightElementRef.GetScrollHeight();
        currentScrollHeight = result.ToString();
    }
}";
    private string getScrollLeftExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getScrollLeftElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitButton OnClick=""GetScrollLeft"">GetScrollLeft</BitButton>

<div>ScrollLeft: @currentScrollLeft</div>

@code {
    private string? currentScrollLeft;
    private ElementReference getScrollLeftElementRef;

    private async Task GetScrollLeft()
    {
        var result = await getScrollLeftElementRef.GetScrollLeft();
        currentScrollLeft = result.ToString();
    }
}";
    private string getScrollTopExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getScrollTopElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitButton OnClick=""GetScrollTop"">GetScrollTop</BitButton>

<div>ScrollTop: @currentScrollTop</div>

@code {
    private string? currentScrollTop;
    private ElementReference getScrollTopElementRef;

    private async Task GetScrollTop()
    {
        var result = await getScrollTopElementRef.GetScrollTop();
        currentScrollTop = result.ToString();
    }
}";
    private string getScrollWidthExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getScrollWidthElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitButton OnClick=""GetScrollWidth"">GetScrollWidth</BitButton>

<div>ScrollWidth: @currentScrollWidth</div>

@code {
    private string? currentScrollWidth;
    private ElementReference getScrollWidthElementRef;

    private async Task GetScrollWidth()
    {
        var result = await getScrollWidthElementRef.GetScrollWidth();
        currentScrollWidth = result.ToString();
    }
}";
    private string getTagNameExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getTagNameElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetTagName"">GetTagName</BitButton>

<div>TagName: @currentTagName</div>

@code {
    private string? currentTagName;
    private ElementReference getTagNameElementRef;

    private async Task GetTagName()
    {
        var result = await getTagNameElementRef.GetTagName();
        currentTagName = result.ToString();
    }
}";
    private string isContentEditableExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""isContentEditableElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetIsContentEditable"">GetIsContentEditable</BitButton>

<div>Is content editable: @isContentEditable</div>

@code {
    private string? isContentEditable;
    private ElementReference isContentEditableElementRef;

    private async Task GetIsContentEditable()
    {
        var result = await isContentEditableElementRef.IsContentEditable();
        isContentEditable = result.ToString();
    }
}";
    private string contentEditableExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""contentEditableElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitChoiceGroup @bind-Value=""newContentEditable""
                Label=""Content editable""
                TItem=""BitChoiceGroupOption<ContentEditable>"" TValue=""ContentEditable"">
    <BitChoiceGroupOption Text=""Inherit"" Value=""ContentEditable.Inherit"" />
    <BitChoiceGroupOption Text=""True"" Value=""ContentEditable.True"" />
    <BitChoiceGroupOption Text=""False"" Value=""ContentEditable.False"" />
    <BitChoiceGroupOption Text=""PlainTextOnly"" Value=""ContentEditable.PlainTextOnly"" />
</BitChoiceGroup>

<BitButton OnClick=""SetContentEditable"">SetContentEditable</BitButton>

<BitButton OnClick=""GetContentEditable"">GetContentEditable</BitButton>

<div>ContentEditable: @currentContentEditable</div>

@code {
    private string? currentContentEditable;
    private ContentEditable newContentEditable;
    private ElementReference contentEditableElementRef;

    private async Task SetContentEditable()
    {
        await contentEditableElementRef.SetContentEditable(newContentEditable);
    }

    private async Task GetContentEditable()
    {
        var result = await contentEditableElementRef.GetContentEditable();
        currentContentEditable = result.ToString();
    }
}";
    private string dirExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""dirElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitChoiceGroup @bind-Value=""newDir""
                Label=""Element direction""
                TItem=""BitChoiceGroupOption<ElementDir>"" TValue=""ElementDir"">
    <BitChoiceGroupOption Text=""Inherit"" Value=""ElementDir.NotSet"" />
    <BitChoiceGroupOption Text=""True"" Value=""ElementDir.Ltr"" />
    <BitChoiceGroupOption Text=""False"" Value=""ElementDir.Rtl"" />
    <BitChoiceGroupOption Text=""PlainTextOnly"" Value=""ElementDir.Auto"" />
</BitChoiceGroup>

<BitButton OnClick=""SetDir"">SetDir</BitButton>

<BitButton OnClick=""GetDir"">GetDir</BitButton>

<div>Dir: @currentDir</div>

@code {
    private ElementDir newDir;
    private string? currentDir;
    private ElementReference dirElementRef;

    private async Task SetDir()
    {
        await dirElementRef.SetDir(newDir);
    }

    private async Task GetDir()
    {
        var result = await dirElementRef.GetDir();
        currentDir = result.ToString();
    }
}";
    private string enterKeyHintExampleCode =
@"@inject Bit.Butil.Element element

<input @ref=""enterKeyHintElementRef"" placeholder=""Element""
       style=""width:6rem; height:6rem; background: dodgerblue;"" />

<BitChoiceGroup @bind-Value=""newEnterKeyHint""
                Label=""Enter key hint""
                TItem=""BitChoiceGroupOption<EnterKeyHint>"" TValue=""EnterKeyHint"">
    <BitChoiceGroupOption Text=""NotSet"" Value=""EnterKeyHint.NotSet"" />
    <BitChoiceGroupOption Text=""Enter"" Value=""EnterKeyHint.Enter"" />
    <BitChoiceGroupOption Text=""Done"" Value=""EnterKeyHint.Done"" />
    <BitChoiceGroupOption Text=""Go"" Value=""EnterKeyHint.Go"" />
    <BitChoiceGroupOption Text=""Next"" Value=""EnterKeyHint.Next"" />
    <BitChoiceGroupOption Text=""Previous"" Value=""EnterKeyHint.Previous"" />
    <BitChoiceGroupOption Text=""Search"" Value=""EnterKeyHint.Search"" />
    <BitChoiceGroupOption Text=""Send"" Value=""EnterKeyHint.Send"" />
</BitChoiceGroup>

<BitButton OnClick=""SetEnterKeyHint"">SetEnterKeyHint</BitButton>

<BitButton OnClick=""GetEnterKeyHint"">GetEnterKeyHint</BitButton>

<div>EnterKeyHint: @currentEnterKeyHint</div>

@code {
    private string? currentEnterKeyHint;
    private EnterKeyHint newEnterKeyHint;
    private ElementReference enterKeyHintElementRef;

    private async Task SetEnterKeyHint()
    {
        await enterKeyHintElementRef.SetEnterKeyHint(newEnterKeyHint);
    }

    private async Task GetEnterKeyHint()
    {
        var result = await enterKeyHintElementRef.GetEnterKeyHint();
        currentEnterKeyHint = result.ToString();
    }
}";
    private string inertExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""inertElementRef""
     style=""max-width:6rem;
            max-height:6rem;
            color: white;
            overflow: auto;
            background: dodgerblue;"">
    <div style=""height: 10rem; width: 10rem;"">Element</div>
</div>

<BitCheckbox @bind-Value=""newInert"" Label=""Inert"" />

<BitButton OnClick=""SetInert"">SetInert</BitButton>

<BitButton OnClick=""GetInert"">GetInert</BitButton>

<div>Inert: @currentInert</div>

@code {
    private bool newInert;
    private string? currentInert;
    private ElementReference inertElementRef;

    private async Task SetInert()
    {
        await inertElementRef.SetInert(newInert);
    }

    private async Task GetInert()
    {
        var result = await inertElementRef.GetInert();
        currentInert = result.ToString();
    }
}";
    private string innerTextExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""innerTextElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitTextField @bind-Value=""newInnerText"" />

<BitButton OnClick=""SetInnerText"">SetInnerText</BitButton>

<BitButton OnClick=""GetInnerText"">GetInnerText</BitButton>

<div>InnerText: @currentInnerText</div>

@code {
    private string? newInnerText;
    private string? currentInnerText;
    private ElementReference innerTextElementRef;

    private async Task SetInnerText()
    {
        await innerTextElementRef.SetInnerText(newInnerText!);
    }

    private async Task GetInnerText()
    {
        currentInnerText = await innerTextElementRef.GetInnerText();
    }
}";
    private string inputModeExampleCode =
@"@inject Bit.Butil.Element element

<input @ref=""inputModeElementRef"" placeholder=""Element""
       style=""width:6rem; height:6rem; background: dodgerblue;"" />

<BitChoiceGroup @bind-Value=""newInputMode""
                Label=""Enter key hint""
                TItem=""BitChoiceGroupOption<InputMode>"" TValue=""InputMode"">
    <BitChoiceGroupOption Text=""NotSet"" Value=""InputMode.NotSet"" />
    <BitChoiceGroupOption Text=""Decimal"" Value=""InputMode.Decimal"" />
    <BitChoiceGroupOption Text=""Email"" Value=""InputMode.Email"" />
    <BitChoiceGroupOption Text=""None"" Value=""InputMode.None"" />
    <BitChoiceGroupOption Text=""Numeric"" Value=""InputMode.Numeric"" />
    <BitChoiceGroupOption Text=""Search"" Value=""InputMode.Search"" />
    <BitChoiceGroupOption Text=""Tel"" Value=""InputMode.Tel"" />
    <BitChoiceGroupOption Text=""Text"" Value=""InputMode.Text"" />
    <BitChoiceGroupOption Text=""Url"" Value=""InputMode.Url"" />
</BitChoiceGroup>

<BitButton OnClick=""SetInputMode"">SetInputMode</BitButton>

<BitButton OnClick=""GetInputMode"">GetInputMode</BitButton>

<div>InputMode: @currentInputMode</div>

@code {
    private string? currentInputMode;
    private InputMode newInputMode;
    private ElementReference inputModeElementRef;

    private async Task SetInputMode()
    {
        await inputModeElementRef.SetInputMode(newInputMode);
    }

    private async Task GetInputMode()
    {
        var result = await inputModeElementRef.GetInputMode();
        currentInputMode = result.ToString();
    }
}";
    private string tabIndexExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""tabIndexElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitSpinButton @bind-Value=""newTabIndex"" Mode=""BitSpinButtonMode.Inline"" />

<BitButton OnClick=""SetTabIndex"">SetTabIndex</BitButton>

<BitButton OnClick=""GetTabIndex"">GetTabIndex</BitButton>

<div>TabIndex: @currentTabIndex</div>

@code {
    private double newTabIndex;
    private string? currentTabIndex;
    private ElementReference tabIndexElementRef;

    private async Task SetTabIndex()
    {
        await tabIndexElementRef.SetTabIndex((int)newTabIndex);
    }

    private async Task GetTabIndex()
    {
        var result = await tabIndexElementRef.GetTabIndex();
        currentTabIndex = result.ToString();
    }
}";
    private string getOffsetHeightExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getOffsetHeightElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetOffsetHeight"">GetOffsetHeight</BitButton>

<div>OffsetHeight: @currentOffsetHeight</div>

@code {
    private string? currentOffsetHeight;
    private ElementReference getOffsetHeightElementRef;

    private async Task GetOffsetHeight()
    {
        var result = await getOffsetHeightElementRef.GetOffsetHeight();
        currentOffsetHeight = result.ToString();
    }
}";
    private string getOffsetLeftExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getOffsetLeftElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetOffsetLeft"">GetOffsetLeft</BitButton>

<div>OffsetLeft: @currentOffsetLeft</div>

@code {
    private string? currentOffsetLeft;
    private ElementReference getOffsetLeftElementRef;

    private async Task GetOffsetLeft()
    {
        var result = await getOffsetLeftElementRef.GetOffsetLeft();
        currentOffsetLeft = result.ToString();
    }
}";
    private string getOffsetTopExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getOffsetTopElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetOffsetTop"">GetOffsetTop</BitButton>

<div>OffsetTop: @currentOffsetTop</div>

@code {
    private string? currentOffsetTop;
    private ElementReference getOffsetTopElementRef;

    private async Task GetOffsetTop()
    {
        var result = await getOffsetTopElementRef.GetOffsetTop();
        currentOffsetTop = result.ToString();
    }
}";
    private string getOffsetWidthExampleCode =
@"@inject Bit.Butil.Element element

<div @ref=""getOffsetWidthElementRef""
     style=""width:6rem; height:6rem; background: dodgerblue;"">
    Element
</div>

<BitButton OnClick=""GetOffsetWidth"">GetOffsetWidth</BitButton>

<div>OffsetWidth: @currentOffsetWidth</div>

@code {
    private string? currentOffsetWidth;
    private ElementReference getOffsetWidthElementRef;

    private async Task GetOffsetWidth()
    {
        var result = await getOffsetWidthElementRef.GetOffsetWidth();
        currentOffsetWidth = result.ToString();
    }
}";
    private string blurExampleCode =
@"@inject Bit.Butil.Element element

<input @ref=""blurElementRef"" placeholder=""Element""
       style=""width:6rem; height:6rem; background: dodgerblue;"" />

<BitButton OnClick=""@(() => blurElementRef.Blur())"">Blur</BitButton>

@code {
    private ElementReference blurElementRef;
}";
}
