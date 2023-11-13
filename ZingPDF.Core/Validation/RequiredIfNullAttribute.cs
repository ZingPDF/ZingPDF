using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Validation
{
    /// <summary>
    /// Marks a field as required if the specified other fields are null.
    /// </summary>
    public class RequiredIfNullAttribute : RequiredAttribute
    {
        private readonly IEnumerable<string> _propertyNames;

        public RequiredIfNullAttribute(params string[] propertyNames)
        {
            _propertyNames = propertyNames;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            object instance = context.ObjectInstance;
            Type type = instance.GetType();

            foreach (var property in _propertyNames)
            {
                object propertyValue = type.GetProperty(property).GetValue(instance, null);
                if (propertyValue == null)
                {
                    continue;
                }
                else
                {
                    // If any fields have a value, return this field as valid
                    return ValidationResult.Success;
                }
            }

            // If we get here, all dependent fields are null.
            // Therefore, validate this field as required.
            ValidationResult result = base.IsValid(value, context);
            return result;
        }
    }
}
