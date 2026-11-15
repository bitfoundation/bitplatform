namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PopConfirm;

public partial class BitPopConfirmDemo
{
    private readonly string example1RazorCode = @"
<BitPopConfirm Title=""Delete the file?""
               Message=""It cannot be brought back once it is deleted.""
               OnConfirm=""@(() => basicAnswer = ""Deleted"")""
               OnCancel=""@(() => basicAnswer = ""Kept"")"">
    <BitButton IconName=""@BitIconName.Delete"">Delete</BitButton>
</BitPopConfirm>

<div>Answer: <b>@basicAnswer</b></div>";
    private readonly string example1CsharpCode = @"
private string? basicAnswer;";

    private readonly string example2RazorCode = @"
<BitPopConfirm Message=""Discard the changes?"">
    <BitButton Variant=""BitVariant.Outline"">Discard</BitButton>
</BitPopConfirm>";

    private readonly string example3RazorCode = @"
<BitPopConfirm Title=""Leave the meeting?""
               Message=""The others will carry on without you.""
               ConfirmText=""Leave""
               CancelText=""Stay"">
    <BitButton IconName=""@BitIconName.Leave"">Leave</BitButton>
</BitPopConfirm>";

    private readonly string example4RazorCode = @"
<BitPopConfirm Message=""Placed on top."" Placement=""BitPlacement.Top"">
    <BitButton Variant=""BitVariant.Outline"">Top</BitButton>
</BitPopConfirm>

<BitPopConfirm Message=""Placed at the bottom."" Placement=""BitPlacement.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Bottom</BitButton>
</BitPopConfirm>

<BitPopConfirm Message=""Placed at the start."" Placement=""BitPlacement.Start"">
    <BitButton Variant=""BitVariant.Outline"">Start</BitButton>
</BitPopConfirm>

<BitPopConfirm Message=""Placed at the end."" Placement=""BitPlacement.End"">
    <BitButton Variant=""BitVariant.Outline"">End</BitButton>
</BitPopConfirm>";

    private readonly string example5RazorCode = @"
<BitPopConfirm Title=""Archive the project?"" @bind-IsOpen=""isBoundOpen"">
    <BitButton IconName=""@BitIconName.Archive"">Archive</BitButton>
</BitPopConfirm>

<BitButton Variant=""BitVariant.Text"" OnClick=""() => isBoundOpen = true"">Ask from here</BitButton>

<BitText>IsOpen: <b>@isBoundOpen</b></BitText>";
    private readonly string example5CsharpCode = @"
private bool isBoundOpen;";

    private readonly string example6RazorCode = @"
<BitPopConfirm Title=""Delete the file?"" Disabled>
    <BitButton IconName=""@BitIconName.Delete"">Delete</BitButton>
</BitPopConfirm>";

    private readonly string example7RazorCode = @"
<BitPopConfirm Title=""Delete the account?""
               Message=""Every project of the account goes with it.""
               ConfirmText=""Delete""
               ConfirmColor=""BitColor.Error""
               OnConfirm=""() => accountDeleted = true"">
    <BitButton Color=""BitColor.Error"" IconName=""@BitIconName.Delete"">Delete account</BitButton>
</BitPopConfirm>

<div>Deleted: <b>@accountDeleted</b></div>";
    private readonly string example7CsharpCode = @"
private bool accountDeleted;";

    private readonly string example8RazorCode = @"
<style>
    .custom-popconfirm {
        border: 1px solid var(--bit-clr-brd-pri);
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-message {
        font-style: italic;
    }
</style>

<BitPopConfirm Title=""Styled popup""
               Message=""With custom styles.""
               Style=""--bit-PopConfirm-title-color: tomato; --bit-PopConfirm-max-width: 14rem;""
               Styles=""@(new() { Callout = ""border-radius: 1rem;"", Actions = ""justify-content: center;"" })"">
    <BitButton Variant=""BitVariant.Outline"">Styles</BitButton>
</BitPopConfirm>

<BitPopConfirm Title=""Classed popup""
               Message=""With custom classes.""
               Classes=""@(new() { Callout = ""custom-popconfirm"", Message = ""custom-message"" })"">
    <BitButton Variant=""BitVariant.Outline"">Classes</BitButton>
</BitPopConfirm>";

    private readonly string example9RazorCode = @"
<div dir=""rtl"">
    <BitPopConfirm Dir=""BitDir.Rtl""
                   Title=""فایل حذف شود؟""
                   Message=""پس از حذف قابل بازگشت نیست.""
                   ConfirmText=""حذف""
                   CancelText=""انصراف"">
        <BitButton IconName=""@BitIconName.Delete"">حذف</BitButton>
    </BitPopConfirm>
</div>";
}
