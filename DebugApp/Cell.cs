namespace DebugApp
{
    public class Cell : BindableBase
    {
        private bool _value;
        public bool Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public Cell(bool value)
        {
            _value = value;
        }
    }
}
