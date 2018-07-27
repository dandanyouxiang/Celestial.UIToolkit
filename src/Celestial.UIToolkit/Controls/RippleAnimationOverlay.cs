﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static System.Math;

namespace Celestial.UIToolkit.Controls
{

    /// <summary>
    /// A content control which provides a ripple animation effect to the underlying content,
    /// whenever the user clicks on it.
    /// The effect is based on the ripple effect by the Material Design language.
    /// </summary>
    [TemplateVisualState(GroupName = AnimationStatesVisualStateGroup, Name = NormalVisualStateName)]
    [TemplateVisualState(GroupName = AnimationStatesVisualStateGroup, Name = ExpandingVisualStateName)]
    [TemplateVisualState(GroupName = AnimationStatesVisualStateGroup, Name = FadingVisualStateName)]
    public class RippleAnimationOverlay : ContentControl
    {
        
        internal const string AnimationStatesVisualStateGroup = "AnimationStates";
        internal const string NormalVisualStateName = "Normal";
        internal const string ExpandingVisualStateName = "Expanding";
        internal const string FadingVisualStateName = "Fading";

        private bool _isMousePressed = false;

        private static readonly DependencyPropertyKey AnimationOriginXPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AnimationOriginX), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(0d));

        private static readonly DependencyPropertyKey AnimationOriginYPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AnimationOriginY), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(0d));

        private static readonly DependencyPropertyKey AnimationPositionXPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AnimationPositionX), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(0d));

        private static readonly DependencyPropertyKey AnimationPositionYPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AnimationPositionY), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(0d));

        private static readonly DependencyPropertyKey AnimationDiameterPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AnimationDiameter), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(0d));

        /// <summary>
        /// Identifies the <see cref="AnimationOriginX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationOriginXProperty = AnimationOriginXPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="AnimationOriginY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationOriginYProperty = AnimationOriginYPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="AnimationPositionX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationPositionXProperty = AnimationPositionXPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="AnimationPositionY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationPositionYProperty = AnimationPositionYPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="AnimationDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationDiameterProperty = AnimationDiameterPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="ActualAnimationScale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActualAnimationScaleProperty = DependencyProperty.Register(
            nameof(ActualAnimationScale), typeof(double), typeof(RippleAnimationOverlay), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, AnimationScaleChanged));

        /// <summary>
        /// Identifies the <see cref="AnimationScale"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationScaleProperty = DependencyProperty.Register(
            nameof(AnimationScale), typeof(double), typeof(RippleAnimationOverlay), new PropertyMetadata(1d));

        /// <summary>
        /// Gets the x-coordinate of the animation's origin point.
        /// </summary>
        public double AnimationOriginX
        {
            get { return (double)GetValue(AnimationOriginXProperty); }
            protected set { SetValue(AnimationOriginXPropertyKey, value); }
        }

        /// <summary>
        /// Gets the y-coordinate of the animation's origin point.
        /// </summary>
        public double AnimationOriginY
        {
            get { return (double)GetValue(AnimationOriginYProperty); }
            protected set { SetValue(AnimationOriginYPropertyKey, value); }
        }

        /// <summary>
        /// Gets the y-coordinate of the top-left location which the animation
        /// will reach.
        /// This is equal to <c><see cref="AnimationOriginX"/> - <see cref="AnimationDiameter"/> / 2</c>
        /// and can be used to align an element which displays the animation.
        /// </summary>
        public double AnimationPositionX
        {
            get { return (double)GetValue(AnimationPositionXProperty); }
            protected set { SetValue(AnimationPositionXPropertyKey, value); }
        }

        /// <summary>
        /// Gets the y-coordinate of the top-left location which the animation
        /// will reach.
        /// This is equal to <c><see cref="AnimationOriginY"/> - <see cref="AnimationDiameter"/> / 2</c>
        /// and can be used to align an element which displays the animation.
        /// </summary>
        public double AnimationPositionY
        {
            get { return (double)GetValue(AnimationPositionYProperty); }
            protected set { SetValue(AnimationPositionYPropertyKey, value); }
        }

        /// <summary>
        /// Gets the diameter of the animated circle.
        /// </summary>
        public double AnimationDiameter
        {
            get { return (double)GetValue(AnimationDiameterProperty); }
            protected set { SetValue(AnimationDiameterPropertyKey, value); }
        }

        /// <summary>
        /// Gets or sets the target scale of the ripple animation
        /// (the scale which it will have, when it is fully expanded).
        /// </summary>
        public double AnimationScale
        {
            get { return (double)GetValue(AnimationScaleProperty); }
            set { SetValue(AnimationScaleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the current scale of the animation.
        /// This value is only supposed to be used by animations.
        /// </summary>
        public double ActualAnimationScale
        {
            get { return (double)GetValue(ActualAnimationScaleProperty); }
            set { SetValue(ActualAnimationScaleProperty, value); }
        }
        
        /// <summary>
        /// Overrides the element's default style.
        /// </summary>
        static RippleAnimationOverlay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RippleAnimationOverlay),
                new FrameworkPropertyMetadata(typeof(RippleAnimationOverlay)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RippleAnimationOverlay"/> class.
        /// </summary>
        public RippleAnimationOverlay()
        {
        }

        /// <summary>
        /// Loads template parts for the animation control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            VisualStateManager.GoToState(this, NormalVisualStateName, true);
        }

        /// <summary>
        /// Called whenever the element's render size changes.
        /// This updates the <see cref="AnimationDiameter"/> property.
        /// </summary>
        /// <param name="sizeInfo">Information about the new render size.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // The max. required radius is the diagonal of this element.
            double width = sizeInfo.NewSize.Width;
            double height = sizeInfo.NewSize.Height;
            this.AnimationDiameter = Sqrt(Pow(width, 2) + Pow(Height, 2)) * 2;

            base.OnRenderSizeChanged(sizeInfo);
        }

        /// <summary>
        /// Called when the user clicks on this element (with the left mouse button).
        /// This sets the animation origin properties and starts the animation effect.
        /// </summary>
        /// <param name="e">Event args about the click.</param>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // The animation starts from a specific point (the mouse press location).
            var rippleOrigin = e.GetPosition(this);
            this.AnimationOriginX = rippleOrigin.X;
            this.AnimationOriginY = rippleOrigin.Y;
            this.AnimationPositionX = this.AnimationOriginX - this.AnimationDiameter / 2;
            this.AnimationPositionY = this.AnimationOriginY - this.AnimationDiameter / 2;
            _isMousePressed = true;

            // Always start expanding the animation when the element is pressed.
            VisualStateManager.GoToState(this, NormalVisualStateName, false);
            VisualStateManager.GoToState(this, ExpandingVisualStateName, true);

            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Called when the user lifts the left mouse button again.
        /// This stops the animation, if it has finished.
        /// </summary>
        /// <param name="e">Event args about the mouse data.</param>
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _isMousePressed = false;
            this.TryEnterFading();
            base.OnPreviewMouseLeftButtonUp(e);
        }

        /// <summary>
        /// Called when the <see cref="ActualAnimationScaleProperty"/>'s value is changed.
        /// </summary>
        /// <param name="d">The <see cref="DependencyObject"/> whose local value got changed.</param>
        /// <param name="e">Event data about the new value.</param>
        private static void AnimationScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RippleAnimationOverlay)d).TryEnterFading();
        }
        
        /// <summary>
        /// Called when either of the following events happens:
        /// <list type="bullet">
        ///     <item>
        ///         <description>The user stops pressing the element.</description>
        ///     </item>
        ///     <item>
        ///         <description>The <see cref="ActualAnimationScale"/> reaches the max. value.</description>
        ///     </item>
        /// </list>
        /// When both of these events have happened, it means that the animation can enter
        /// the 'Fading' state, meaning that it will disappear.
        /// </summary>
        private void TryEnterFading()
        {
            if (this.ActualAnimationScale == 1d && !_isMousePressed)
            {
                VisualStateManager.GoToState(this, FadingVisualStateName, true);
            }
        }

    }

}