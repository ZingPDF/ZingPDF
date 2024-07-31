namespace ZingPDF.Graphics.FormXObjects
{
    /// <summary>
    /// ISO 32000-2:2020 8.10.2 - Form dictionaries
    /// </summary>
    internal abstract class FormDictionary : XObjectDictionary
    {
        protected FormDictionary() : base(Subtypes.Form)
        {
        }
    }
}
