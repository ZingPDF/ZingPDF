using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

/// <summary>
/// Marks a property as inheritable.
/// </summary>
/// <remarks>
/// A <see cref="RequiredProperty{T}"/> or <see cref="OptionalMultiProperty{T1, T2}"/> marked with 
/// this attribute will attempt to retrieve the value from the parent dictionary if it is not present in the current dictionary.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InheritableAttribute : Attribute;