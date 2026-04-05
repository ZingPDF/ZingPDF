namespace ZingPDF.InteractiveFeatures.Forms
{
    [Flags]
    internal enum FieldFlags
    {
        None = 0,
        ReadOnly = 1 << 0,
        Required = 1 << 1,
        NoExport = 1 << 2,
        Multiline = 1 << 12,
        Password = 1 << 13,
        NoToggleToOff = 1 << 14,
        Radio = 1 << 15,
        Pushbutton = 1 << 16,
        Combo = 1 << 17,
        Edit = 1 << 18,
        Sort = 1 << 19,
        FileSelect = 1 << 20,
        MultiSelect = 1 << 21,
        DoNotSpellCheck = 1 << 22,
        DoNotScroll = 1 << 23,
        Comb = 1 << 24,
        RichText = 1 << 25,
        RadiosInUnison = 1 << 26
    }
}
