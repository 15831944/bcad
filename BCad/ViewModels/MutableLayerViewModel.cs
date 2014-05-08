﻿using BCad.Collections;
using BCad.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BCad.ViewModels
{
    public class MutableLayerViewModel : INotifyPropertyChanged
    {
        private bool? isVisible = true;
        private ColorViewModel color;
        private string name;
        private Layer layer;

        public MutableLayerViewModel(string name, ColorMap colorMap)
        {
            this.name = name;
            this.color = new ColorViewModel(IndexedColor.Auto, colorMap[IndexedColor.Auto]);
        }

        public MutableLayerViewModel(Layer layer, ColorMap colorMap)
        {
            this.layer = layer;
            this.name = layer.Name;
            this.color = new ColorViewModel(layer.Color, colorMap[layer.Color]);
            this.isVisible = layer.IsVisible;
        }

        public bool? IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible == value)
                    return;
                isVisible = value;
                OnPropertyChanged();
                OnPropertyChangedDirect("IsDirty");
            }
        }

        public ColorViewModel Color
        {
            get { return color; }
            set
            {
                if (color == value)
                    return;
                color = value;
                OnPropertyChanged();
                OnPropertyChangedDirect("IsDirty");
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;
                name = value;
                OnPropertyChanged();
                OnPropertyChangedDirect("IsDirty");
            }
        }

        public bool IsDirty
        {
            get
            {
                return this.layer == null
                    ? true
                    : this.Name != this.layer.Name ||
                      this.IsVisible != this.layer.IsVisible ||
                      this.Color.Color != this.layer.Color;
            }
        }

        public Layer GetUpdatedLayer()
        {
            if (this.layer == null)
            {
                return new Layer(this.Name, Color.Color, IsVisible ?? false, new ReadOnlyTree<uint, Entity>());
            }
            else if (this.IsDirty)
            {
                return this.layer.Update(name: this.Name, color: this.Color.Color, isVisible: this.IsVisible ?? false);
            }
            else
            {
                return this.layer;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            OnPropertyChangedDirect(propertyName);
        }

        protected void OnPropertyChangedDirect(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed != null)
                changed(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
