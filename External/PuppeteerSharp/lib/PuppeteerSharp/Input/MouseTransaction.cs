using System;

namespace PuppeteerSharp.Input
{
    internal class MouseTransaction
    {
        public Action<TransactionData> Update { get; set; }

        public Action Commit { get; set; }

        public Action Rollback { get; set; }

        internal class TransactionData
        {
            public Point? Position { get; set; }

            public MouseButton? Buttons { get; set; }
        }
    }
}
