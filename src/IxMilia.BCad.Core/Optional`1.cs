namespace IxMilia.BCad
{
    public struct Optional<T>
    {
        private bool hasValue;
        private T value;

        public Optional(T value)
        {
            this.hasValue = true;
            this.value = value;
        }

        public bool HasValue
        {
            get { return this.hasValue; }
        }

        public T Value
        {
            get { return this.value; }
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }
    }
}
