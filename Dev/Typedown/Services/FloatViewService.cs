using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using Typedown.Controls.FloatControls;
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
using Typedown.Core.Utilities;
using Windows.Foundation;

namespace Typedown.Services
{
    public sealed class FloatViewService : IFloatViewService
    {
        private readonly IServiceProvider serviceProvider;
        private ToolTip openedToolTip;

        public FloatViewService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void OpenImageToolbar(JToken args)
        {
            var imageToolbar = serviceProvider.GetService<ImageToolbar>();
            var rect = args["boundingClientRect"].ToObject<Rect>();
            var attrs = args["attrs"];
            imageToolbar.Open(rect, attrs);
        }

        public void OpenFrontMenu(JToken args)
        {
            var frontMenu = serviceProvider.GetService<FrontMenu>();
            var rect = args["boundingClientRect"].ToObject<Rect>();
            frontMenu.Open(rect);
        }

        public void OpenImageSelector(JToken args)
        {
            var selector = serviceProvider.GetService<ImageSelector>();
            var rect = args["boundingClientRect"].ToObject<Rect>();
            var info = args["imageInfo"];
            selector.Open(rect, info);
        }

        public void OpenTableTools(JToken args)
        {
            var tableTools = serviceProvider.GetService<TableTools>();
            var rect = args["boundingClientRect"].ToObject<Rect>();
            var type = args["tableInfo"]["barType"].ToString();
            tableTools.Open(rect, type);
        }

        public void OpenToolTip(JToken args)
        {
            openedToolTip?.Hide();
            openedToolTip = null;
            if (args["open"].ToObject<bool>())
            {
                openedToolTip = serviceProvider.GetService<ToolTip>();
                var name = args["tooltip"].ToString();
                var text = Locale.GetString(name) ?? name;
                openedToolTip.Open(text);
            }
        }
    }
}
