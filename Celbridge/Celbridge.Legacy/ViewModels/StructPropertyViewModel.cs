﻿namespace Celbridge.Legacy.ViewModels;

public abstract partial class StructPropertyViewModel<T> : ObservableObject where T : struct
{
    public Property? Property { get; private set; }

    public void SetProperty(Property property, string labelText)
    {
        Property = property;
        LabelText = labelText;
    }

    public string LabelText { get; private set; } = string.Empty;
    public int ItemIndex { get; set; }
    public bool HasLabelText => !string.IsNullOrEmpty(LabelText);

    public T Value
    {
        get
        {
            Guard.IsNotNull(Property);
            var propertyInfo = Property.PropertyInfo;
            if (Property.CollectionType != null)
            {
                var list = propertyInfo.GetValue(Property.Object) as List<T>;
                Guard.IsNotNull(list);
                Guard.IsTrue(ItemIndex < list.Count);
                return list[ItemIndex];
            }
            return (T)propertyInfo.GetValue(Property.Object)!;
        }

        set
        {
            if (value.Equals(Value))
            {
                return;
            }

            Guard.IsNotNull(Property);
            var propertyInfo = Property.PropertyInfo;
            if (Property.CollectionType != null)
            {
                var list = propertyInfo.GetValue(Property.Object) as List<T>;
                Guard.IsNotNull(list);
                Guard.IsTrue(ItemIndex < list.Count);
                list[ItemIndex] = value;
            }
            else 
            { 
                propertyInfo.SetValue(Property.Object, value);
            }
            OnPropertyChanged(nameof(Value));
        }
    }

    public StructPropertyViewModel()
    {
        PropertyChanged += PropertyViewModel_PropertyChanged;
    }

    private void PropertyViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Value))
        {
            Guard.IsNotNull(Property);
            Property.NotifyPropertyChanged();
        }
    }
}