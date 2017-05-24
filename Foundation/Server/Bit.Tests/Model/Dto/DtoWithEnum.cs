using Foundation.Model.Contracts;

namespace Foundation.Test.Model.Dto
{
    /// <summary>
    /// Test
    /// </summary>
    public class DtoWithEnum : IDto
    {
        public virtual int Id { get; set; }

        public virtual TestGender? Gender { get; set; }

        public virtual string Test { get; set; }
    }

    public enum TestGender
    {
        Man = 3, Woman = 12, Other
    }

    public enum TestGender2
    {
        /// <summary>
        /// Man
        /// </summary>
        Man = 3,
        Woman = 12,
        Other
    }
}
