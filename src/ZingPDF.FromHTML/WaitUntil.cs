namespace ZingPDF.FromHTML
{
    [Flags]
    public enum WaitUntil
    {
        None = 0,

        /// <summary>
        /// Consider navigation to be finished when the <c>load</c> event is fired.
        /// </summary>
        Load = 1,

        /// <summary>
        /// Consider navigation to be finished when the <c>DOMContentLoaded</c> event is fired.
        /// </summary>
        DOMContentLoaded = 2,

        /// <summary>
        /// Consider navigation to be finished when there are no more than 0 network connections for at least <c>500</c> ms.
        /// </summary>
        Networkidle0 = 4,

        /// <summary>
        /// Consider navigation to be finished when there are no more than 2 network connections for at least <c>500</c> ms.
        /// </summary>
        Networkidle2 = 8,
    }
}
