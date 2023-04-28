namespace Bit.Websites.Careers.Shared.Dtos;

public class IntroBoxDto
{
    public int Id { get; set; }

    public string Subject;

    public string ImgUrl;

    public string AuthorName;

    public string AuthorAvatarUrl;

    public string UploadDate;

    public string? StudyTime;

    public string? WatchingTime;

    public IntroBoxType Type;
}
