﻿using Celestial.UIToolkit.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Celestial.UIToolkit.Controls
{

    /// <summary>
    /// A control which is used in conjunction with the <see cref="ThemeShadow"/>.
    /// When added to a control's template, it displays a <see cref="Controls.ThemeShadow"/>
    /// which is attached to an element via the <see cref="ThemeShadow.ShadowProperty"/>.
    /// </summary>
    [ContentProperty(nameof(InnerChild))]
    public class ThemeShadowPresenter : Decorator
    {

        // Used when no ThemeShadow could be located in the TemplatedParent.
        // If none is defined, don't render any shadow.
        private static readonly ThemeShadow DisabledFallbackThemeShadow = new ThemeShadow()
        {
            IsShadowEnabled = false
        };

        private UIElement _child;
        private DropShadowDecorator _dropShadowDecorator;

        /// <summary>
        /// Identifies the <see cref="ThemeShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeShadowProperty =
            DependencyProperty.Register(
                nameof(ThemeShadow),
                typeof(ThemeShadow),
                typeof(ThemeShadowPresenter),
                new PropertyMetadata(null));
        
        /// <summary>
        /// Gets or sets the <see cref="ThemeShadow"/> which is displayed by this host.
        /// </summary>
        public ThemeShadow ThemeShadow
        {
            get { return (ThemeShadow)GetValue(ThemeShadowProperty); }
            set { SetValue(ThemeShadowProperty, value); }
        }
        
        /// <summary>
        /// Gets the actual child of the <see cref="ThemeShadowPresenter"/>.
        /// This is an element which renders the shadow around the <see cref="InnerChild"/>
        /// and is automatically generated by the <see cref="ThemeShadowPresenter"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown if an attempt to set this property was made.
        /// </exception>
        public override UIElement Child
        {
            get { return ShadowDecorator; }
            set
            {
                throw new NotSupportedException(
                    $"Cannot directly set the {nameof(Child)} property, since it is managed by " +
                    $"the {nameof(ThemeShadowPresenter)}. To set the child to be displayed, " +
                    $"use the {nameof(InnerChild)} property.");
            }
        }

        internal DropShadowDecorator ShadowDecorator
        {
            get { return _dropShadowDecorator; }
            set
            {
                _dropShadowDecorator = value;
                base.Child = value;
            }
        }

        /// <summary>
        /// Gets or sets the child to be displayed and shadowed by the
        /// <see cref="ThemeShadowPresenter"/>.
        /// </summary>
        public virtual UIElement InnerChild
        {
            get { return _child; }
            set
            {
                if (_child != value)
                {
                    _child = value;
                    _dropShadowDecorator.Child = value;
                }
            }
        }
        
        static ThemeShadowPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ThemeShadowPresenter), 
                new FrameworkPropertyMetadata(typeof(ThemeShadowPresenter)));
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeShadowPresenter"/> class.
        /// </summary>
        public ThemeShadowPresenter()
        {
            ShadowDecorator = new DropShadowDecorator();
            Loaded += ThemeShadowPresenter_Loaded;
        }
        
        private void ThemeShadowPresenter_Loaded(object sender, RoutedEventArgs e)
        {
            SetShadowDecoratorBindings();
        }
        
        private void SetShadowDecoratorBindings()
        {
            // All of these values would normally be set in XAML via a ControlTemplate.
            // We are creating a decorator though, which means that we don't have a template.
            // As a result, we need to define all bindings in code.
            BindThemeShadowToTemplatedParent();
            SetDecoratorThemeShadowBindings();
            SetDecoratorTemplatedParentBindings();
        }

        private void BindThemeShadowToTemplatedParent()
        {
            // If no ThemeShadow is explicitly set, bind it to the TemplatedParent.
            // This way, the ThemeShadow can be set directly on the control, or in a style
            // and this presenter will get the correct one.
            if (!this.IsDependencyPropertySet(ThemeShadowProperty))
            {
                SetBinding(ThemeShadowProperty, new Binding()
                {
                    RelativeSource = RelativeSource.TemplatedParent,
                    Path = new PropertyPath(ThemeShadow.ShadowProperty),
                    FallbackValue = DisabledFallbackThemeShadow,
                    TargetNullValue = DisabledFallbackThemeShadow
                });
            }
        }

        private void SetDecoratorThemeShadowBindings()
        {
            // The non-attached dependency properties of the ThemeShadow can be bound
            // directly to the DropShadowDecorator.
            var renderingBiasBinding = CreateThemeShadowBinding(ThemeShadow.RenderingBiasProperty);
            var shadowColorBinding = CreateThemeShadowBinding(ThemeShadow.ShadowColorProperty);
            var shadowOpacityBinding = CreateThemeShadowBinding(ThemeShadow.ShadowOpacityProperty);
            var shadowEnabledBinding = CreateThemeShadowBinding(ThemeShadow.IsShadowEnabledProperty);

            ShadowDecorator.SetBinding(DropShadowDecorator.RenderingBiasProperty, renderingBiasBinding);
            ShadowDecorator.SetBinding(DropShadowDecorator.ShadowColorProperty, shadowColorBinding);
            ShadowDecorator.SetBinding(DropShadowDecorator.ShadowOpacityProperty, shadowOpacityBinding);
            ShadowDecorator.SetBinding(DropShadowDecorator.IsShadowEnabledProperty, shadowEnabledBinding);
        }

        private void SetDecoratorTemplatedParentBindings()
        {
            // The attached dependency properties are a little trickier.
            // Each of them is set on the TemplatedParent. As a result, we must bind to that
            // element.
            var blurRadiusBinding = CreateTemplatedParentBinding(ThemeShadow.BlurRadiusProperty);
            var offsetXBinding = CreateTemplatedParentBinding(ThemeShadow.OffsetXProperty);
            var offsetYBinding = CreateTemplatedParentBinding(ThemeShadow.OffsetYProperty);
            
            ShadowDecorator.SetBinding(DropShadowDecorator.BlurRadiusProperty, blurRadiusBinding);
            ShadowDecorator.SetBinding(DropShadowDecorator.OffsetXProperty, offsetXBinding);
            ShadowDecorator.SetBinding(DropShadowDecorator.OffsetYProperty, offsetYBinding);
        }

        private Binding CreateThemeShadowBinding(DependencyProperty dp)
        {
            string themeShadowPropName = ThemeShadowProperty.Name;
            string dpName = dp.Name;
            return new Binding()
            {
                Path = new PropertyPath($"{themeShadowPropName}.{dpName}"),
                Source = this
            };
        }

        private Binding CreateTemplatedParentBinding(DependencyProperty dp)
        {
            return new Binding()
            {
                Path = new PropertyPath(dp),
                Source = TemplatedParent
            };
        }

        /// <summary>
        /// Returns a string representation of this <see cref="ThemeShadowPresenter"/>.
        /// </summary>
        /// <returns>A string representing this <see cref="ThemeShadowPresenter"/>.</returns>
        public override string ToString()
        {
            return $"{nameof(ThemeShadowPresenter)}: " +
                   $"{nameof(ThemeShadow)}: {ThemeShadow}";
        }

    }

}
