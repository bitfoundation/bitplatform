//+:cnd:noEmit
namespace Boilerplate.Server.Api.Features.Attachments;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(attachment => new { attachment.Id, attachment.Kind });

        //#if (signalR == true)
        builder.HasIndex(attachment => new { attachment.Kind, attachment.CreatedOn });
        //#endif
    }
}
