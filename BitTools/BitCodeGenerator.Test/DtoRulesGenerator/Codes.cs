namespace BitCodeGenerator.Test.DtoRulesGenerator
{
    public class Codes
    {
        public const string BitDtoRules = @"

    namespace Foundation.Model.Dto
    {
        public interface IDto
        {

        }
    }

    namespace Foundation.Api.DtoRules
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public sealed class AutoGenerateAttribute : Attribute
        {

        }

        public class DtoRules<TDto>
            where TDto : class, Model.Dto.IDto
        {
            public virtual TDto Model { get; set; }

            public virtual System.Action<string,string,bool> SetMemberValidaty { get; set; }

            public virtual void ValidateMember(string memberName, object newValue, object oldValue)
            {

            }
        }
    }        
";
    }
}
