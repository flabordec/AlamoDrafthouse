using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using System.ComponentModel;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public class DataValidatingObservableObject : ObservableObject, IDataErrorInfo
    {
        public string Error => throw new NotSupportedException("IDataErrorInfo.Error is not supported, use IDataErrorInfo.this[propertyName] instead.");

        public string this[string propertyName]
        {
            get
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentException("Invalid property name", propertyName);
                }
                string error = string.Empty;

                var value = GetValue(propertyName);

                var results = new List<ValidationResult>(1);
                var result = Validator.TryValidateProperty(
                    value,
                    new ValidationContext(this, null, null)
                    {
                        MemberName = propertyName
                    },
                    results);

                if (!result)
                {
                    var validationResult = results.First();
                    error = validationResult.ErrorMessage;
                }
                return error;
            }
        }

        private object GetValue(string propertyName)
        {
            PropertyInfo propInfo = GetType().GetProperty(propertyName);
            return propInfo.GetValue(this);
        }
    }

    public class IsTrueAttribute : ValidationAttribute
    {
        public IsTrueAttribute()
            : base() { }

        public IsTrueAttribute(string errorMessage)
            : base(errorMessage) { }

        public IsTrueAttribute(Func<string> errorMessageAccessor)
            : base(errorMessageAccessor) { }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is bool?)
            {
                var boolValue = (bool?)value;
                if (boolValue != true)
                {
                    var errorMessage = FormatErrorMessage(validationContext.DisplayName);
                    return new ValidationResult(errorMessage);
                }
            }
            else if (value is bool)
            {
                var boolValue = (bool)value;
                if (boolValue != true)
                {
                    var errorMessage = FormatErrorMessage(validationContext.DisplayName);
                    return new ValidationResult(errorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}
