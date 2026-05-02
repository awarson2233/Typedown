using Newtonsoft.Json.Linq;

namespace Typedown.Core.Interfaces
{
    public interface IFloatViewService
    {
        void OpenImageToolbar(JToken args);

        void OpenFrontMenu(JToken args);

        void OpenImageSelector(JToken args);

        void OpenTableTools(JToken args);

        void OpenToolTip(JToken args);
    }
}
