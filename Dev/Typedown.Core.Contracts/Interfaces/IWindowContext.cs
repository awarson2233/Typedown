namespace Typedown.Core.Interfaces
{
    public interface IWindowContext
    {
        nint WindowHandle { get; set; }

        object? ViewRoot { get; set; }

        string? Title { get; set; }

        bool IsActive { get; set; }

        void Activate();

        void BringToFront();

        void RequestClose();
    }
}
