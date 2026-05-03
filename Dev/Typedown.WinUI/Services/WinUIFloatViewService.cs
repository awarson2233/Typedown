using Newtonsoft.Json.Linq;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFloatViewService : IFloatViewService
    {
        public void OpenFrontMenu(JToken args)
        {
            throw new NotSupportedException("WinUI front menu float view is not wired in this migration slice.");
        }

        public void OpenImageSelector(JToken args)
        {
            throw new NotSupportedException("WinUI image selector float view is not wired in this migration slice.");
        }

        public void OpenImageToolbar(JToken args)
        {
            throw new NotSupportedException("WinUI image toolbar float view is not wired in this migration slice.");
        }

        public void OpenTableTools(JToken args)
        {
            throw new NotSupportedException("WinUI table tools float view is not wired in this migration slice.");
        }

        public void OpenToolTip(JToken args)
        {
            throw new NotSupportedException("WinUI tooltip float view is not wired in this migration slice.");
        }
    }
}
