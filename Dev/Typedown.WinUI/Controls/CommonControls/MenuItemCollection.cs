using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Typedown.WinUI.Controls;

[ContentProperty(Name = nameof(Items))]
public class MenuItemCollection : DependencyObject
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached("Value", typeof(MenuItemCollection), typeof(MenuItemCollection), new PropertyMetadata(null, OnValuePropertyChanged));

    public static MenuItemCollection? GetValue(DependencyObject target) => (MenuItemCollection?)target.GetValue(ValueProperty);

    public static void SetValue(DependencyObject target, MenuItemCollection? value) => target.SetValue(ValueProperty, value);

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IList<MenuFlyoutItemBase>), typeof(MenuItemCollection), new PropertyMetadata(null));

    public IList<MenuFlyoutItemBase> Items
    {
        get => (IList<MenuFlyoutItemBase>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public MenuItemCollection()
    {
        Items = new ObservableCollection<MenuFlyoutItemBase>();
    }

    private static void OnValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is MenuFlyoutSubItem subItem && e.NewValue is MenuItemCollection collection)
        {
            subItem.Items.Clear();
            foreach (var item in collection.Items)
            {
                subItem.Items.Add(item);
            }
        }
    }
}