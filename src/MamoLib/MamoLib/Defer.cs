namespace MamoLib
{
    public class Defer : IDisposable
    {
        private Action? _action { get; set; }
        private Action<object?>? _func { get; set; }

        public Defer(Action action)
        {
            _action = action;
        }

        public Defer(Action<object?> func)
        {
            _func = func;
        }

        public void Dispose()
        {
            _action?.Invoke();
            _func?.Invoke(null);
        }
    }
}