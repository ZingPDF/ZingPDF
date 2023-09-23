using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    internal class FilterParams : Dictionary
    {
        public bool Modified = false;

        public override PdfObject this[Name key]
        {
            get => base[key];
            set
            {
                base[key] = value;

                Modified = true;
            }
        }

        public override void Add(KeyValuePair<Name, PdfObject> item)
        {
            base.Add(item);

            Modified = true;
        }

        public override void Add(Name key, PdfObject value)
        {
            base.Add(key, value);

            Modified = true;
        }

        public override void Clear()
        {
            base.Clear();

            Modified = false;
        }

        public override bool Remove(KeyValuePair<Name, PdfObject> item)
        {
            if (Count == 1)
            {
                Modified = false;
            }

            return base.Remove(item);
        }

        public override bool Remove(Name key)
        {
            if (Count == 1)
            {
                Modified = false;
            }

            return base.Remove(key);
        }
    }
}
