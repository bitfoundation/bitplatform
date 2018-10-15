using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bit.ViewModel
{
    #region ValidationRule

    public interface IValidationRule
    {
        bool IsValid(object value);

        string ValidationMessage { get; set; }
    }

    public interface IValidationRule<in T> : IValidationRule
    {
        bool IsValid(T value);
    }

    public abstract class ValidationRuleBase : IValidationRule
    {
        public virtual string ValidationMessage { get; set; }

        public abstract bool IsValid(object value);
    }

    public abstract class ValidationRuleBase<T> : ValidationRuleBase, IValidationRule<T>
    {
        public override bool IsValid(object value)
        {
            return IsValid((T)value);
        }

        public abstract bool IsValid(T value);
    }

    public class MinLengthValidationRule : ValidationRuleBase<string>
    {
        public int MinLength { get; set; }

        public override bool IsValid(string value)
        {
            return value != null && value.Length >= MinLength;
        }
    }

    public class MaxLengthValidationRule : ValidationRuleBase<string>
    {
        public int MaxLength { get; set; }

        public override bool IsValid(string value)
        {
            return value == null || value.Length <= MaxLength;
        }
    }

    public class RequiredValidationRule<T> : ValidationRuleBase<T>
    {
        public RequiredValidationRule(string validationMessage)
        {
            ValidationMessage = validationMessage;
        }

        public override bool IsValid(T value)
        {
            if (value == null)
                return false;

            if (value is string str)
                return !string.IsNullOrEmpty(str);

            return true;
        }
    }

    public class RegexValidationRule : ValidationRuleBase<string>
    {
        public RegexValidationRule(string validationMessage, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException(nameof(pattern));

            ValidationMessage = validationMessage;
            Pattern = pattern;
        }

        public string Pattern { get; set; }

        public override bool IsValid(string value)
        {
            if (value == null)
                return false;

            return Regex.IsMatch(value, Pattern);
        }
    }

    #endregion

    public class Validatable : BindableBase
    {
        public Validatable(string notValidErrorMessage, params IValidationRule[] validationRules)
        {
            NotValidErrorMessage = notValidErrorMessage;
            ValidationRules = validationRules;
        }

        public virtual IValidationRule[] ValidationRules { get; }

        public string NotValidErrorMessage { get; }

        public virtual IEnumerable<string> Errors
        {
            get
            {
                return ValidationRules.Where(r => !r.IsValid(Value))
                    .Select(r => r.ValidationMessage);
            }
        }

        public virtual string ErrorMessages => string.Join(Environment.NewLine, Errors);

        public virtual bool IsValid
        {
            get
            {
                return ValidationRules.All(r => r.IsValid(Value));
            }
        }

        public virtual object Value { get; set; }

        public virtual void OnValueChanged()
        {
            RaisePropertyChanged(nameof(IsValid));
            RaisePropertyChanged(nameof(Errors));
            RaisePropertyChanged(nameof(ErrorMessages));
        }
    }

    public class Validatable<T> : Validatable
    {
        public Validatable(string notValidErrorMessage, params IValidationRule[] validationRules)
            : base(notValidErrorMessage, validationRules)
        {
        }
    }

    public class ValidatablesGroup : BindableBase
    {
        public Validatable[] Validatables { get; set; }

        public ValidatablesGroup(string notValidErrorMessage, params Validatable[] validatables)
        {
            Validatables = validatables;

            foreach (Validatable validatable in Validatables)
            {
                validatable.PropertyChanged += Validatable_PropertyChanged;
            }

            NotValidErrorMessage = notValidErrorMessage;
        }

        public string NotValidErrorMessage { get; }

        private void Validatable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Validatable.IsValid))
            {
                RaisePropertyChanged(nameof(IsValid));
                RaisePropertyChanged(nameof(ErrorMessages));
            }
        }

        public virtual bool IsValid
        {
            get
            {
                return Validatables.All(validatable => validatable.IsValid);
            }
        }

        public virtual string ErrorMessages
        {
            get
            {
                return string.Join(Environment.NewLine, Validatables.Select(v => v.ErrorMessages));
            }
        }
    }
}
