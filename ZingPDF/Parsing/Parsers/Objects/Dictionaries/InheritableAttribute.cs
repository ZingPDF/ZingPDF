using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

/// <summary>
/// Marks a property as inheritable.
/// </summary>
/// <remarks>
/// A <see cref="DictionaryProperty{T}"/> or <see cref="DictionaryMultiProperty{T1, T2}"/> marked with 
/// this attribute will attempt to retrieve the value from the parent dictionary if it is not present in the current dictionary.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InheritableAttribute : Attribute;