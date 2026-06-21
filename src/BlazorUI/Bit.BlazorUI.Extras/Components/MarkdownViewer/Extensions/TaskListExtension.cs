using System.Text.RegularExpressions;
using Bit.BlazorUI.Markdown.Parsing;
using Bit.BlazorUI.Markdown.Rendering;
using Bit.BlazorUI.Markdown.Syntax;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Markdown.Extensions;

/// <summary>A GitHub task-list checkbox at the start of a list item.</summary>
public sealed class TaskCheckboxNode : MarkdownNode
{
    public bool Checked { get; init; }
}

/// <summary>
/// Rewrites list items beginning with <c>[ ]</c> / <c>[x]</c> into a
/// <see cref="TaskCheckboxNode"/> followed by the remaining text.
/// </summary>
public sealed partial class TaskListAstProcessor : AstProcessor
{
    [GeneratedRegex(@"^\[([ xX])\]\s+(.*)$")]
    private static partial Regex TaskMarker();

    public override void Process(DocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var list in AstHelper.Descendants(document).OfType<ListNode>())
        {
            foreach (var item in list.Items)
            {
                if (item.Children.FirstOrDefault() is not ParagraphNode para) continue;
                if (para.Inlines.FirstOrDefault() is not TextNode text) continue;

                var m = TaskMarker().Match(text.Text);
                if (!m.Success) continue;

                text.Text = m.Groups[2].Value;
                para.Inlines.Insert(0, new TaskCheckboxNode { Checked = m.Groups[1].Value is "x" or "X" });
            }
        }
    }
}

/// <summary>Renders <see cref="TaskCheckboxNode"/> as a disabled checkbox.</summary>
public sealed class TaskCheckboxRenderer : NodeRenderer
{
    public override bool Accept(MarkdownNode node) => node is TaskCheckboxNode;

    public override void Write(MarkdownRenderer r, RenderTreeBuilder b, MarkdownNode node)
    {
        var task = (TaskCheckboxNode)node;
        b.OpenElement(r.NextSeq(), "input");
        b.AddAttribute(r.NextSeq(), "type", "checkbox");
        b.AddAttribute(r.NextSeq(), "class", "task-list-item-checkbox");
        b.AddAttribute(r.NextSeq(), "disabled", true);
        if (task.Checked) b.AddAttribute(r.NextSeq(), "checked", true);
        b.CloseElement();
    }
}

/// <summary>Enables GitHub-style task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
public sealed class TaskListExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new TaskListAstProcessor());
        builder.Renderers.Add(new TaskCheckboxRenderer());
    }
}
